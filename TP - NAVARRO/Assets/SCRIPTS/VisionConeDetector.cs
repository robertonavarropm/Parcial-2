using UnityEngine;

[ExecuteAlways]
public class VisionConeDetector : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;                      // Player (Transform)

    [Header("Parámetros de visión")]
    [Tooltip("Ángulo TOTAL del cono en grados (consigna: 60).")]
    public float viewAngleDegTotal = 60f;         // 60° totales
    [Tooltip("Alcance máximo de visión en metros (consigna: 10).")]
    public float viewDistance = 10f;              // 10 m
    [Tooltip("Altura de los 'ojos' del enemigo.")]
    public float eyeHeight = 1.6f;

    [Header("Capas")]
    [Tooltip("Layer(s) del Player.")]
    public LayerMask targetMask;                  // marcar Player
    [Tooltip("Layer(s) de paredes/estructuras que bloquean la visión.")]
    public LayerMask obstacleMask;                // marcar Wall

    [Header("Cálculo")]
    [Tooltip("Ignorar diferencia de altura para el ángulo (plano horizontal).")]
    public bool useHorizontalAngle = true;

    [Header("LOS avanzada")]
    [Tooltip("Si está en true, además de no haber pared, exige tocar al Player (SphereCast).")]
    public bool requireTargetHit = false;
    [Tooltip("Radio del SphereCast para 'engordar' el rayo hacia el Player.")]
    public float targetHitSphereRadius = 0.2f;

    [Header("Debug")]
    public bool debugLogs = false;

    float HalfAngleRad => Mathf.Deg2Rad * (viewAngleDegTotal * 0.5f);

    public bool IsTargetVisible()
    {
        if (!target) return false;

        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 tgt = target.position + Vector3.up * eyeHeight;

        // 1) Distancia
        Vector3 to = tgt - eye;
        float dist = to.magnitude;
        bool distOk = dist <= viewDistance;
        if (!distOk) { if (debugLogs) Debug.Log("[Vision] distOk=false"); return false; }

        // 2) Ángulo (producto punto)
        Vector3 a = transform.forward;
        Vector3 b = to;
        if (useHorizontalAngle) { a.y = 0f; b.y = 0f; }
        if (a.sqrMagnitude < 1e-6f || b.sqrMagnitude < 1e-6f) return false;
        a.Normalize(); b.Normalize();

        float cosHalf = Mathf.Cos(HalfAngleRad);
        float dot = Vector3.Dot(a, b);
        bool angOk = dot >= cosHalf;
        if (!angOk) { if (debugLogs) Debug.Log("[Vision] angOk=false"); return false; }

        // 3) Línea de visión en 2 pasos:

        // 3.a) ¿Hay PARED primero? (bloquea)
        Ray ray = new Ray(eye, (tgt - eye).normalized);
        if (obstacleMask.value != 0 &&
            Physics.Raycast(ray, out RaycastHit hitObs, viewDistance, obstacleMask, QueryTriggerInteraction.Ignore))
        {
            if (debugLogs) Debug.Log($"[Vision] los=false (pared primero: {hitObs.collider.name})");
            return false;
        }

        // 3.b) (opcional) ¿Querés exigir que toque al Player?
        if (requireTargetHit && targetMask.value != 0)
        {
            bool hitPlayer =
                Physics.SphereCast(ray, targetHitSphereRadius, out RaycastHit hitTgt,
                                   viewDistance, targetMask, QueryTriggerInteraction.Ignore);

            if (!hitPlayer)
            {
                if (debugLogs) Debug.Log("[Vision] sin pared pero no tocó Player (SphereCast falló)");
                return false;
            }
        }

        if (debugLogs) Debug.Log("[Vision] OK (dist, ang, sin pared)");
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 eye = transform.position + Vector3.up * eyeHeight;
        Vector3 fwd = transform.forward; fwd.y = 0f; fwd.Normalize();
        if (fwd.sqrMagnitude < 1e-6f) fwd = transform.forward;

        // Cono
        Gizmos.color = new Color(0f, 0.8f, 1f, 0.8f);
        float half = viewAngleDegTotal * 0.5f;
        Quaternion L = Quaternion.AngleAxis(-half, Vector3.up);
        Quaternion R = Quaternion.AngleAxis(half, Vector3.up);
        Vector3 left = L * fwd * viewDistance;
        Vector3 right = R * fwd * viewDistance;
        Gizmos.DrawLine(eye, eye + left);
        Gizmos.DrawLine(eye, eye + right);

        int steps = 24;
        Vector3 prev = eye + left;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float ang = Mathf.Lerp(-half, half, t);
            Vector3 dir = Quaternion.AngleAxis(ang, Vector3.up) * fwd * viewDistance;
            Vector3 next = eye + dir;
            Gizmos.DrawLine(prev, next);
            prev = next;
        }

        // Línea al target (verde si lo ve, rojo si bloqueado)
        if (target)
        {
            Gizmos.color = IsTargetVisible() ? Color.green : Color.red;
            Vector3 tgt = target.position + Vector3.up * eyeHeight;
            Gizmos.DrawLine(eye, tgt);
        }
    }
}

