using System.Collections;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI; // para el Text (World Space) del estado

[RequireComponent(typeof(CharacterController))]
public class EnemySoldier : MonoBehaviour
{
    public enum EnemyState { Normal, Chase, Damage, Dead }

    [Header("Detección (vectores)")]
    [SerializeField] private VisionConeDetector detector;      // mismo GameObject
    [Tooltip("Si ve al jugador o recibe daño, persigue hasta morir.")]
    [SerializeField] private bool latchAggro = true;

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Vida del enemigo")]
    [SerializeField] private int maxHealth = 30;
    [SerializeField] private int startHealthEnemy = 30;

    [Header("Ataque")]
    [Tooltip("Daño que aplica al jugador por golpe.")]
    [SerializeField] private int attackDamage = 10;
    [Tooltip("Golpes por segundo (1 = un golpe por segundo).")]
    [SerializeField] private float attackRate = 1.0f;
    [Tooltip("Alcance del ataque en metros.")]
    [SerializeField] private float attackRange = 2.5f;

    [Header("Aplicación de daño al Player (ScriptableObject)")]
    [Tooltip("Arrastrá aquí tu asset PlayerStats_Default.")]
    [SerializeField] private ScriptableObject playerStatsSO;

    [Header("Player Stats SO (nombres de campos)")]
    [Tooltip("Campo INT que representa la vida ACTUAL del jugador en tu SO.")]
    [SerializeField] private string currentHealthField = "startHealth"; // <- tu asset usa 'Start Health'
    [Tooltip("Campo INT que representa la vida MÁXIMA del jugador en tu SO.")]
    [SerializeField] private string maxHealthField = "maxHealth";

    [Header("Referencias opcionales")]
    [SerializeField] private Transform player;            // si está vacío, se toma por Tag "Player"
    [SerializeField] private Canvas worldspaceCanvas;     // Canvas (World Space) hijo
    [SerializeField] private Text stateText;              // Text (Legacy) dentro del canvas
    [SerializeField] private float damageFlashTime = 0.25f;

    // ---- internos ----
    private CharacterController cc;
    private EnemyState state = EnemyState.Normal;
    private int health;
    private bool hasAggro = false;             // en cuanto te ve o recibe daño, queda true
    private Vector3 spawnPos; private Quaternion spawnRot;
    private float yVelocity;
    private Coroutine damageCo;
    private float nextAttackTime = 0f;

    // reflexión para PlayerStatsSO
    private FieldInfo fiCurHP, fiMaxHP;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        if (!detector) detector = GetComponent<VisionConeDetector>();

        if (!player)
        {
            if (detector && detector.target) player = detector.target;
            else
            {
                var p = GameObject.FindWithTag("Player");
                if (p) player = p.transform;
                if (detector && player) detector.target = player;
            }
        }

        if (!worldspaceCanvas) worldspaceCanvas = GetComponentInChildren<Canvas>(true);
        if (!stateText && worldspaceCanvas) stateText = worldspaceCanvas.GetComponentInChildren<Text>(true);

        // ------- Bind de campos del ScriptableObject del Player -------
        if (playerStatsSO)
        {
            var t = playerStatsSO.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            // 1) Intentar con lo escrito en el Inspector
            fiCurHP = t.GetField(currentHealthField, flags);
            fiMaxHP = t.GetField(maxHealthField, flags);

            // 2) Alias comunes si no se encontró
            if (fiCurHP == null) fiCurHP = FindFirstField(t, new[] { "currentHealth", "startHealth", "hp", "hpActual", "vidaActual" });
            if (fiMaxHP == null) fiMaxHP = FindFirstField(t, new[] { "maxHealth", "maxHp", "vidaMax", "maxVida" });

            if (fiCurHP == null || fiMaxHP == null)
                Debug.LogError("[EnemySoldier] No encuentro campos de vida en PlayerStatsSO. Revisá nombres en el Inspector.");
        }
    }

    void Start()
    {
        health = Mathf.Clamp(startHealthEnemy, 0, maxHealth);
        SetState(EnemyState.Normal);
    }

    void Update()
    {
        // Respawn opcional con F3 (si tu práctica lo permite)
        if (Input.GetKeyDown(KeyCode.F3)) Respawn();

        if (state == EnemyState.Dead || player == null) return;

        // --- Detección por vectores (ángulo+distancia+LOS) ---
        bool sees = detector && detector.IsTargetVisible();

        if (!hasAggro)
        {
            if (sees) { hasAggro = true; SetState(EnemyState.Chase); }
            else { SetState(EnemyState.Normal); }
        }
        else
        {
            if (state != EnemyState.Damage) SetState(EnemyState.Chase);
        }

        // --- Persecución ---
        if (state == EnemyState.Chase)
        {
            Vector3 to = player.position - transform.position;
            to.y = 0f;
            Vector3 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector3.zero;

            Vector3 step = dir * moveSpeed;
            if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
            yVelocity += gravity * Time.deltaTime;
            step.y = yVelocity;

            cc.Move(step * Time.deltaTime);

            if (dir.sqrMagnitude > 0f)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir, Vector3.up), 10f * Time.deltaTime);

            // --- Ataque solo en Chase ---
            TryAttackPlayer();
        }
    }

    // ====== ATAQUE AL JUGADOR ======
    private void TryAttackPlayer()
    {
        if (Time.time < nextAttackTime) return;
        if (playerStatsSO == null || fiCurHP == null || fiMaxHP == null) return;
        if (player == null) return;

        // ¿dentro del rango?
        float eyeH = (detector ? detector.eyeHeight : 1.6f);
        Vector3 eye = transform.position + Vector3.up * eyeH;
        Vector3 tgt = player.position + Vector3.up * eyeH;
        Vector3 dir = (tgt - eye);
        float dist = dir.magnitude;
        if (dist > attackRange) return;

        // no pegar si hay pared entre medio (usa Obstacle Mask del detector)
        bool clear = true;
        if (detector)
        {
            LayerMask obsMask = detector.obstacleMask;
            if (obsMask != 0 &&
                Physics.Raycast(new Ray(eye, dir.normalized),
                                out RaycastHit hit, attackRange, obsMask, QueryTriggerInteraction.Ignore))
            {
                clear = false; // pared primero
            }
        }
        if (!clear) return;

        // Aplicar daño al SO del jugador
        int cur = Mathf.Clamp((int)fiCurHP.GetValue(playerStatsSO), 0, int.MaxValue);
        int max = Mathf.Max(1, (int)fiMaxHP.GetValue(playerStatsSO));
        if (cur <= 0) return;

        int newCur = Mathf.Clamp(cur - Mathf.Abs(attackDamage), 0, max);
        fiCurHP.SetValue(playerStatsSO, newCur);

        Debug.Log($"[ENEMY HIT] daño={attackDamage}  playerHP={newCur}/{max}");

        if (newCur <= 0)
        {
            Debug.Log("[PLAYER DEAD] el jugador ha muerto.");
            // acá podrías desactivar input, mostrar UI, recargar escena, etc.
        }

        nextAttackTime = Time.time + (1f / Mathf.Max(attackRate, 0.01f));
    }

    // ====== DAÑO RECIBIDO (aggro por daño del jugador) ======
    public void ApplyDamage(int amount)
    {
        if (state == EnemyState.Dead) return;

        health = Mathf.Clamp(health - Mathf.Max(0, amount), 0, maxHealth);

        // entra en chase si fue atacado por el jugador
        hasAggro = true;
        if (state != EnemyState.Damage && state != EnemyState.Dead) SetState(EnemyState.Chase);

        if (damageCo != null) StopCoroutine(damageCo);
        damageCo = StartCoroutine(DamageFlash());

        if (health <= 0) Kill();
    }

    private IEnumerator DamageFlash()
    {
        var prev = state;
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
        foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        if (worldspaceCanvas) worldspaceCanvas.enabled = true;
    }

    private void SetState(EnemyState newState)
    {
        if (state == newState) return;
        state = newState;
        if (stateText) stateText.text = state.ToString().ToLower(); // "normal", "chase", "damage", "dead"
        // Debug.Log($"[Enemy] state: {state}");
    }

    private void Respawn()
    {
        foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        if (cc) { cc.enabled = true; yVelocity = 0f; }

        transform.SetPositionAndRotation(spawnPos, spawnRot);

        health = maxHealth;
        hasAggro = false;
        if (damageCo != null) { StopCoroutine(damageCo); damageCo = null; }
        SetState(EnemyState.Normal);
        if (worldspaceCanvas) worldspaceCanvas.enabled = true;
    }

    // ------- Helper: busca el primer campo int con alguno de los nombres -------
    private FieldInfo FindFirstField(System.Type tt, string[] names)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var n in names)
        {
            var f = tt.GetField(n, flags);
            if (f != null && f.FieldType == typeof(int)) return f;
        }
        return null;
    }

    // Gizmo de alcance de ataque
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0f, 0f, 0.25f);
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
}
