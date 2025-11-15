using System.Collections;
using UnityEngine;
using UnityEngine.UI; // para UI Text (legacy)

public class EnemySoldier : MonoBehaviour
{
    public enum EnemyState { Normal, Chase, Damage, Dead }

    [Header("Datos (SO)")]
    [SerializeField] private SoldierSO soldierData;

    [Header("Referencias (asigná en Inspector)")]
    [SerializeField] private Transform player;        // Tag = Player
    [SerializeField] private CharacterController cc;  // recomendable
    [SerializeField] private Canvas worldspaceCanvas; // Canvas World Space hijo
    [SerializeField] private Text stateText;          // UI Text dentro del canvas

    [Header("Movimiento")]
    [SerializeField] private float gravity = -9.81f;

    [Header("Comportamiento")]
    [Tooltip("Aggro permanente: si te ve una vez, persigue hasta morir.")]
    [SerializeField] private bool latchAggro = true;
    [SerializeField, Tooltip("Duración visible del estado 'damage' (seg).")]
    private float damageStateDuration = 0.25f;

    [Header("Línea de visión")]
    [SerializeField, Tooltip("Radio del SphereCast para evitar 'ver por rendijas'")]
    private float losRadius = 0.12f;

    private EnemyState state = EnemyState.Normal;
    private int health;
    private bool hasAggro = false; // una vez true, no vuelve a false hasta morir/respawn
    private Vector3 spawnPos;
    private Quaternion spawnRot;
    private Coroutine damageBlinkCo;

    void Reset()
    {
        cc = GetComponent<CharacterController>();
    }

    void Awake()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        if (player == null)
        {
            var p = GameObject.FindWithTag("Player");
            if (p) player = p.transform;
        }
        if (cc == null) cc = GetComponent<CharacterController>();
        if (worldspaceCanvas == null) worldspaceCanvas = GetComponentInChildren<Canvas>(true);
        if (stateText == null && worldspaceCanvas != null) stateText = worldspaceCanvas.GetComponentInChildren<Text>(true);
    }

    void Start()
    {
        if (soldierData == null)
        {
            Debug.LogError("[EnemySoldier] Falta SoldierSO asignado.");
            enabled = false;
            return;
        }

        health = Mathf.Clamp(soldierData.startHealth, 0, soldierData.maxHealth);
        SetState(EnemyState.Normal);
        UpdateStateText();
    }

    void Update()
    {
        // Respawn con F3 (permitido por consigna previa)
        if (Input.GetKeyDown(KeyCode.F3)) RespawnAtSpawn();

        if (player == null || state == EnemyState.Dead) return;

        // Detección inicial (solo si aún no enganchó)
        if (!hasAggro)
        {
            if (IsPlayerInVisionCone())
            {
                hasAggro = true;          // queda latched
                SetState(EnemyState.Chase);
            }
            else
            {
                SetState(EnemyState.Normal);
            }
        }
        else
        {
            // Ya enganchó: no suelta hasta morir
            if (state != EnemyState.Damage && state != EnemyState.Dead)
                SetState(EnemyState.Chase);
        }

        // Movimiento en CHASE
        if (state == EnemyState.Chase)
        {
            Vector3 toPlayer = player.position - transform.position;
            toPlayer.y = 0f;
            Vector3 dir = toPlayer.sqrMagnitude > 0.0001f ? toPlayer.normalized : Vector3.zero;

            if (dir.sqrMagnitude > 0f)
            {
                Vector3 step = dir * soldierData.moveSpeed * Time.deltaTime;

                if (cc != null)
                {
                    step.y += gravity * Time.deltaTime;
                    cc.Move(step);
                }
                else
                {
                    transform.position += step;
                }

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(dir, Vector3.up),
                    10f * Time.deltaTime
                );
            }
        }
    }

    // ====== DETECCIÓN: cono + LOS bloqueada por estructuras ======
    private bool IsPlayerInVisionCone()
    {
        if (soldierData == null || player == null) return false;

        Vector3 eye = transform.position + Vector3.up * soldierData.eyeHeight;
        Vector3 target = player.position + Vector3.up * 1.6f;
        Vector3 toPlayer = target - eye;

        // 1) Distancia
        float dist = toPlayer.magnitude;
        if (dist > soldierData.viewDistance) return false;

        // 2) Ángulo (cono)
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        Vector3 flat = toPlayer; flat.y = 0f; flat.Normalize();
        float ang = Vector3.Angle(fwd, flat);
        if (ang > soldierData.viewHalfAngle) return false;

        // 3) Línea de visión: SOLO Player u Obstáculo (lo que llegue primero manda)
        int visionMask = soldierData.playerMask | soldierData.obstacleMask;
        Ray ray = new Ray(eye, toPlayer.normalized);

        if (Physics.SphereCast(ray, losRadius, out RaycastHit hit, dist, visionMask, QueryTriggerInteraction.Ignore))
        {
            int hitLayer = hit.collider.gameObject.layer;

            // ¿Fue Player? -> ve al jugador
            bool hitIsPlayerLayer = ((1 << hitLayer) & soldierData.playerMask) != 0;
            if (hitIsPlayerLayer) return true;

            // ¿Fue Obstáculo (Wall)? -> bloquea
            bool hitIsObstacle = ((1 << hitLayer) & soldierData.obstacleMask) != 0;
            if (hitIsObstacle) return false;
        }

        // Si no chocó ni Player ni Obstáculo, asumimos que no ve
        return false;
    }

    // ====== DAÑO / MUERTE ======
    public void ApplyDamage(int amount)
    {
        if (state == EnemyState.Dead) return;

        health = Mathf.Clamp(health - Mathf.Max(0, amount), 0, soldierData.maxHealth);

        // Mostrar "damage" un instante
        if (damageBlinkCo != null) StopCoroutine(damageBlinkCo);
        damageBlinkCo = StartCoroutine(DamageBlink());

        if (health <= 0) Kill();
    }

    private IEnumerator DamageBlink()
    {
        SetState(EnemyState.Damage);
        yield return new WaitForSeconds(damageStateDuration);

        if (state != EnemyState.Dead)
        {
            if (hasAggro && latchAggro) SetState(EnemyState.Chase);
            else SetState(IsPlayerInVisionCone() ? EnemyState.Chase : EnemyState.Normal);
        }
    }

    private void Kill()
    {
        SetState(EnemyState.Dead);

        // Apagar colisiones/visibilidad (mantenemos el canvas para ver "dead")
        if (cc) cc.enabled = false;
        foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = false;
        if (worldspaceCanvas) worldspaceCanvas.enabled = true;
    }

    // ====== ESTADOS + TEXTO ======
    private void SetState(EnemyState newState)
    {
        if (state == newState) return;
        state = newState;
        UpdateStateText();
        Debug.Log($"[EnemySoldier] state: {state}");
    }

    private void UpdateStateText()
    {
        if (!stateText) return;
        stateText.text = state.ToString().ToLower(); // "normal", "chase", "damage", "dead"
    }

    // ====== RESPAWN (F3) ======
    public void RespawnAtSpawn()
    {
        foreach (var col in GetComponentsInChildren<Collider>(true)) col.enabled = true;
        foreach (var r in GetComponentsInChildren<Renderer>(true)) r.enabled = true;
        if (cc) cc.enabled = true;

        transform.SetPositionAndRotation(spawnPos, spawnRot);
        health = soldierData.maxHealth;
        hasAggro = false;
        if (damageBlinkCo != null) { StopCoroutine(damageBlinkCo); damageBlinkCo = null; }

        SetState(EnemyState.Normal);
        if (worldspaceCanvas) worldspaceCanvas.enabled = true;
    }

    // ====== GIZMOS (cono) ======
    void OnDrawGizmosSelected()
    {
        if (soldierData == null) return;
        Gizmos.color = Color.cyan;

        Vector3 eye = transform.position + Vector3.up * soldierData.eyeHeight;
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();

        Quaternion L = Quaternion.AngleAxis(-soldierData.viewHalfAngle, Vector3.up);
        Quaternion R = Quaternion.AngleAxis(soldierData.viewHalfAngle, Vector3.up);

        Vector3 left = L * fwd * soldierData.viewDistance;
        Vector3 right = R * fwd * soldierData.viewDistance;

        Gizmos.DrawLine(eye, eye + left);
        Gizmos.DrawLine(eye, eye + right);

        int steps = 20;
        Vector3 prev = eye + left;
        for (int i = 1; i <= steps; i++)
        {
            float t = (float)i / steps;
            float ang = Mathf.Lerp(-soldierData.viewHalfAngle, soldierData.viewHalfAngle, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd * soldierData.viewDistance;
            Vector3 next = eye + dir;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
