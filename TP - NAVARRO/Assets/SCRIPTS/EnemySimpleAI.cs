using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class EnemySoldier : MonoBehaviour, IDamageable
{
    public enum EnemyState { Normal, Chase, Damage, Dead }

    [Header("Player (ASIGNAR en Inspector)")]
    [SerializeField] private Transform player;  // 👈 arrastrá el Player aquí

    [Header("Detección (vectores)")]
    [SerializeField] private VisionConeDetector detector;
    [SerializeField] private bool latchAggro = true;   // si te vio o lo dañaste, no suelta

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float turnSpeedDeg = 720f; // °/s

    [Header("Vida del enemigo")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int startHealthEnemy = 30;

    [Header("Ataque")]
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackRate = 1.0f;   // golpes/seg
    [SerializeField] private float attackRange = 2.5f;
    [SerializeField] private bool requireDetectorSeeToAttack = false; // si querés exigir cono

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

    // reflexión player stats
    private FieldInfo fiCurHP, fiMaxHP;

    // ---------- Helpers ----------
    private float EyeHeight => detector ? detector.eyeHeight : 1.6f;
    private Vector3 EyePos => transform.position + Vector3.up * EyeHeight;
    private Vector3 PlayerEyePos => (player ? player.position : Vector3.zero) + Vector3.up * EyeHeight;

    void OnValidate()
    {
        // Mantener el detector apuntando al mismo player del inspector
        if (detector && player) detector.target = player;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        spawnPos = transform.position; spawnRot = transform.rotation;

        if (!detector) detector = GetComponent<VisionConeDetector>();
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

        // seguridad: si no hay player asignado, no hacemos nada
        if (!player) { SetState(EnemyState.Normal); return; }
        if (state == EnemyState.Dead) return;

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
            ChaseMove();
            TryAttackPlayer(sees);
        }
    }

    private void ChaseMove()
    {
        if (!player) return;

        Vector3 to = player.position - transform.position;
        to.y = 0f;
        if (to.sqrMagnitude < 0.0001f) return;

        Vector3 dir = to.normalized;

        // giro suave hacia el jugador
        Quaternion desired = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation, desired, turnSpeedDeg * Time.deltaTime);

        // movimiento hacia el jugador
        Vector3 vel = dir * moveSpeed;
        if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;
        vel.y = yVelocity;

        cc.Move(vel * Time.deltaTime);
    }

    private void TryAttackPlayer(bool seesNow)
    {
        if (Time.time < nextAttackTime) return;
        if (playerStatsSO == null || fiCurHP == null || fiMaxHP == null || !player) return;

        // Exigir cono si lo marcaste
        if (requireDetectorSeeToAttack && !(detector && seesNow)) return;

        // Rango
        Vector3 dir = PlayerEyePos - EyePos;
        float dist = dir.magnitude;
        if (dist > attackRange) return;

        // LOS (pared bloquea)
        if (detector && detector.obstacleMask != 0 &&
            Physics.Raycast(EyePos, dir.normalized, out RaycastHit hit, attackRange, detector.obstacleMask, QueryTriggerInteraction.Ignore))
            return;

        int cur = Mathf.Clamp((int)fiCurHP.GetValue(playerStatsSO), 0, int.MaxValue);
        int max = Mathf.Max(1, (int)fiMaxHP.GetValue(playerStatsSO));
        if (cur <= 0) return;

        int newCur = Mathf.Clamp(cur - Mathf.Abs(attackDamage), 0, max);
        fiCurHP.SetValue(playerStatsSO, newCur);
        Debug.Log($"[ENEMY HIT] daño={attackDamage}  playerHP={newCur}/{max}");

        if (newCur <= 0)
        {
            var death = FindObjectOfType<PlayerDeathHandler>();
            if (death) death.OnKilledByEnemy();
        }

        nextAttackTime = Time.time + (1f / Mathf.Max(attackRate, 0.01f));
    }

    // ===== IDamageable =====
    public void ApplyDamage(int amount)
    {
        if (state == EnemyState.Dead) return;
        health = Mathf.Clamp(health - Mathf.Max(0, amount), 0, maxHealth);

        // aggro por daño
        hasAggro = true;

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

        // volver a sincronizar detector
        if (detector && player) detector.target = player;
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
