using UnityEngine;

public class VisionConeDetector : MonoBehaviour
{
    [Header("Objetivo")]
    public Transform target;              // ASIGNAR: Transform del Player

    [Header("Parámetros de visión")]
    [Tooltip("Ángulo TOTAL del cono (ej: 60 significa ±30°)")]
    public float viewAngleDegTotal = 60f;
    [Tooltip("Alcance máximo en metros")]
    public float viewDistance = 10f;
    [Tooltip("Altura del 'ojo' (desde el suelo)")]
    public float eyeHeight = 1.6f;

    [Header("Capas")]
    public LayerMask targetMask;          // Layer del Player
    public LayerMask obstacleMask;        // Layer de Wall

    [Header("Cálculo")]
    [Tooltip("Si está ON, el ángulo se calcula en el plano XZ (ignora altura)")]
    public bool useHorizontalAngle = true;

    [Header("LOS avanzada (opcional)")]
    [Tooltip("Si está ON, además exige que el raycast golpee al Player")]
    public bool requireTargetHit = false;
    [Tooltip("Radio del spherecast para facilitar el 'hit' del Player")]
    public float targetHitSphereRadius = 0.2f;

    [Header("Debug")]
    public bool debugLogs = false;

    // ---- API pública ----
    public bool IsTargetVisible()
    {
        if (!target) return false;

        Vector3 eye = EyePos;
        Vector3 tgt = TargetEyePos;

        // 1) Distancia
        Vector3 to = tgt - eye;
        float dist = to.magnitude;
        if (dist > viewDistance) { if (debugLogs) Debug.Log($"[VISION] dist>N ({dist:F1}/{viewDistance})"); return false; }

        // 2) Ángulo (dot product)
        Vector3 fwd = transform.forward;
        Vector3 dir = to.normalized;

        if (useHorizontalAngle)
        {
            fwd.y = 0; dir.y = 0;
            if (fwd.sqrMagnitude < 1e-6f || dir.sqrMagnitude < 1e-6f) return false;
            fwd.Normalize(); dir.Normalize();
        }

        float half = viewAngleDegTotal * 0.5f;
        float cosHalf = Mathf.Cos(half * Mathf.Deg2Rad);
        float dot = Vector3.Dot(fwd, dir);
        bool angOk = dot >= cosHalf;
        if (!angOk) { if (debugLogs) Debug.Log($"[VISION] angX (dot={dot:F2} < cos({half})={cosHalf:F2})"); return false; }

        // 3) Línea de visión (pared bloquea)
        if (obstacleMask != 0)
        {
            if (Physics.Raycast(eye, (tgt - eye).normalized, out RaycastHit wallHit, dist, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                if (debugLogs) Debug.Log($"[VISION] bloqueado por {wallHit.collider.name}");
                return false;
            }
        }

        // 4) (Opcional) exigir que el ray golpee al Player
        if (requireTargetHit)
        {
            bool hitPlayer =
                Physics.SphereCast(new Ray(eye, (tgt - eye).normalized), targetHitSphereRadius,
                                   out RaycastHit thit, dist, targetMask, QueryTriggerInteraction.Collide);
            if (!hitPlayer)
            {
                if (debugLogs) Debug.Log($"[VISION] no 'pegué' al Player con spherecast (r={targetHitSphereRadius})");
                return false;
            }
        }

        if (debugLogs) Debug.Log($"[VISION] VISIBLE ok  dist={dist:F1}  dot={dot:F2}");
        return true;
    }

    // ---- Helpers ----
    private Vector3 EyePos => transform.position + Vector3.up * eyeHeight;
    private Vector3 TargetEyePos => target.position + Vector3.up * eyeHeight;

    // ---- Gizmos ----
    void OnDrawGizmosSelected()
    {
        Vector3 origin = EyePos;
        // arco del cono (plano horizontal)
        int steps = 24;
        float half = viewAngleDegTotal * 0.5f;
        float r = viewDistance;

        Vector3 fwd = transform.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = Quaternion.Euler(0, half, 0) * fwd;
        Vector3 left = Quaternion.Euler(0, -half, 0) * fwd;

        Gizmos.color = new Color(0, 1, 1, 0.15f);
        Vector3 prev = origin + right * r;
        for (int i = 1; i <= steps; i++)
        {
            float t = i / (float)steps;
            float ang = Mathf.Lerp(half, -half, t);
            Vector3 dir = Quaternion.Euler(0, ang, 0) * fwd;
            Vector3 p = origin + dir * r;
            Gizmos.DrawLine(prev, p);
            prev = p;
        }
        // bordes
        Gizmos.color = new Color(0, 1, 1, 0.35f);
        Gizmos.DrawLine(origin, origin + right * r);
        Gizmos.DrawLine(origin, origin + left * r);

        // línea al target
        if (target)
        {
            Vector3 tgt = TargetEyePos;
            bool visible = IsTargetVisible();
            Gizmos.color = visible ? Color.green : Color.red;
            Gizmos.DrawLine(origin, tgt);
        }
    }
}
