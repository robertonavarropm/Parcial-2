using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class EnemySoldier : MonoBehaviour, IDamageable
{
    public enum EnemyState { Normal, Chase, Damage, Dead }

    [Header("Detección (tu detector de cono)")]
    [SerializeField] private VisionConeDetector detector;
    [SerializeField] private bool latchAggro = true;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Vida del enemigo")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int startHealthEnemy = 30;

    [Header("Ataque")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRate = 1.0f;
    [SerializeField] private float attackRange = 2.5f;

    [Header("Player Stats (ScriptableObject)")]
    [SerializeField] private ScriptableObject playerStatsSO; // PlayerStats_Default
    [SerializeField] private string currentHealthField = "startHealth";
    [SerializeField] private string maxHealthField = "maxHealth";

    [Header("UI estado (opcional)")]
    [SerializeField] private Canvas worldspaceCanvas;
    [SerializeField] private Text stateText;
    [SerializeField] private float damageFlashTime = 0.25f;

    // internos
    private CharacterController cc;
    private EnemyState state = EnemyState.Normal;
    private int health;
    private bool hasAggro = false;
    private Vector3 spawnPos; private Quaternion spawnRot;
    private float yVelocity;
    private Coroutine damageCo;
    private float nextAttackTime = 0f;

    // player / stats
    private Transform player;
    private FieldInfo fiCurHP, fiMaxHP;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        spawnPos = transform.position; spawnRot = transform.rotation;

        if (!detector) detector = GetComponent<VisionConeDetector>();

        var pGo = GameObject.FindWithTag("Player");
        if (pGo) player = pGo.transform;
        if (detector && player) detector.target = player;

        if (!worldspaceCanvas) worldspaceCanvas = GetComponentInChildren<Canvas>(true);
        if (!stateText && worldspaceCanvas) stateText = worldspaceCanvas.GetComponentInChildren<Text>(true);

        if (playerStatsSO)
        {
            var t = playerStatsSO.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            fiCurHP = t.GetField(currentHealthField, flags);
            fiMaxHP = t.GetField(maxHealthField, flags);
        }
    }

    void Start()
    {
        health = Mathf.Clamp(startHealthEnemy, 0, maxHealth);
        SetState(EnemyState.Normal);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3)) Respawn();
        if (state == EnemyState.Dead || player == null) return;

        bool sees = detector && detector.IsTargetVisible();

        if (!hasAggro)
        {
            if (sees) { hasAggro = true; SetState(EnemyState.Chase); }
            else { SetState(EnemyState.Normal); }
        }
        else if (state != EnemyState.Damage)
        {
            SetState(EnemyState.Chase);
        }

        if (state == EnemyState.Chase)
        {
            Vector3 to = player.position - transform.position; to.y = 0f;
            Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector3.zero;

            Vector3 step = dir * moveSpeed;
            if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
            yVelocity += gravity * Time.deltaTime;
            step.y = yVelocity;

            cc.Move(step * Time.deltaTime);

            if (dir.sqrMagnitude > 0f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);

            TryAttackPlayer();
        }
    }

    private void TryAttackPlayer()
    {
        if (Time.time < nextAttackTime) return;
        if (playerStatsSO == null || fiCurHP == null || fiMaxHP == null || player == null) return;

        float eyeH = (detector ? detector.eyeHeight : 1.6f);
        Vector3 eye = transform.position + Vector3.up * eyeH;
        Vector3 tgt = player.position + Vector3.up * eyeH;
        Vector3 dir = (tgt - eye);
        float dist = dir.magnitude;
        if (dist > attackRange) return;

        // pared bloquea
        if (detector && detector.obstacleMask != 0 &&
            Physics.Raycast(new Ray(eye, dir.normalized), out RaycastHit hit, attackRange, detector.obstacleMask, QueryTriggerInteraction.Ignore))
            return;

        int cur = Mathf.Clamp((int)fiCurHP.GetValue(playerStatsSO), 0, int.MaxValue);
        int max = Mathf.Max(1, (int)fiMaxHP.GetValue(playerStatsSO));
        if (cur <= 0) return;

        int newCur = Mathf.Clamp(cur - Mathf.Abs(attackDamage), 0, max);
        fiCurHP.SetValue(playerStatsSO, newCur);
        Debug.Log($"[ENEMY HIT] daño={attackDamage}  playerHP={newCur}/{max}");

        if (newCur <= 0)
        {
            Debug.Log("[PLAYER DEAD] el jugador ha muerto.");
            var death = FindObjectOfType<PlayerDeathHandler>();
            if (death) death.OnKilledByEnemy();
        }

        nextAttackTime = Time.time + (1f / Mathf.Max(attackRate, 0.01f));
    }

    // === ESTA ES LA FUNCIÓN QUE NECESITÁS PÚBLICA ===
    public void ApplyDamage(int amount)
    {
        if (state == EnemyState.Dead) return;

        health = Mathf.Clamp(health - Mathf.Max(0, amount), 0, maxHealth);
        hasAggro = true; // aggro por daño

        if (damageCo != null) StopCoroutine(damageCo);
        damageCo = StartCoroutine(DamageFlash());

        if (health <= 0) Kill();
    }

    private IEnumerator DamageFlash()
    {
        SetState(EnemyState.Damage);
        yield return new WaitForSeconds(damageFlashTime);
        if (state != EnemyState.Dead)
        {
            if (hasAggro && latchAggro) SetState(EnemyState.Chase);
            else SetState(EnemyState.Normal);
        }
    }

    private void Kill()
    {
        SetState(EnemyState.Dead);
        if (cc) cc.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        if (worldspaceCanvas) worldspaceCanvas.enabled = true;
    }

    private void Respawn()
    {
        foreach (var c in GetComponentsInChildren<Collider>(true)) c.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        if (cc) { cc.enabled = true; yVelocity = 0f; }

        transform.SetPositionAndRotation(spawnPos, spawnRot);
        health = maxHealth;
        hasAggro = false;
        if (damageCo != null) { StopCoroutine(damageCo); damageCo = null; }
        SetState(EnemyState.Normal);
        if (worldspaceCanvas) worldspaceCanvas.enabled = true;
    }

    private void SetState(EnemyState s)
    {
        if (state == s) return;
        state = s;
        if (stateText) stateText.text = state.ToString().ToLower();
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 0, 0, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
