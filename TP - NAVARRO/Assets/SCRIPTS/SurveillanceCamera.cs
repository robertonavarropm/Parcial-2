using UnityEngine;

[RequireComponent(typeof(VisionConeDetector))]
public class SurveillanceCamera : MonoBehaviour, IDamageable
{
    [Header("Tipo (ScriptableObject)")]
    [SerializeField] private SurveillanceCameraSO type;

    [Header("Detección")]
    [SerializeField] private VisionConeDetector detector;
    [SerializeField] private bool logWhenSees = true;

    [Header("Visual (opcional)")]
    [SerializeField] private Renderer[] renderersToToggle; // si querés ocultarla al destruir
    [SerializeField] private Collider[] collidersToToggle;

    // internos
    private int health;
    private bool destroyed = false;

    void Reset()
    {
        detector = GetComponent<VisionConeDetector>();
    }

    void Awake()
    {
        if (!detector) detector = GetComponent<VisionConeDetector>();
        if (!type)
        {
            Debug.LogError("[SurveillanceCamera] Falta asignar SurveillanceCameraSO.");
            enabled = false; return;
        }

        // Inicializar desde el SO
        health = Mathf.Max(0, type.maxHealth);

        detector.viewAngleDegTotal = type.viewAngleDegTotal;
        detector.viewDistance = type.viewDistance;
        detector.eyeHeight = type.eyeHeight;
        detector.useHorizontalAngle = true; // ángulo en plano (usual)
    }

    void Update()
    {
        if (destroyed) return;

        // Solo detectar / loguear (no persigue)
        if (detector && detector.IsTargetVisible())
        {
            if (logWhenSees)
                Debug.Log("[CAMERA] Jugador detectado.");
            // acá podrías disparar alarma, pausar estamina del jugador, etc. (si te lo piden)
        }
    }

    // ---- IDamageable ----
    public void ApplyDamage(int amount)
    {
        if (destroyed) return;
        int dmg = Mathf.Max(0, amount);
        health = Mathf.Max(0, health - dmg);
        Debug.Log($"[CAMERA HIT] daño={dmg} hp={health}/{type.maxHealth}");

        if (health <= 0) DestroyCamera();
    }

    private void DestroyCamera()
    {
        destroyed = true;
        Debug.Log("[CAMERA] destruida.");

        // Apagar colisiones y visuales si se asignaron
        if (collidersToToggle != null)
            foreach (var c in collidersToToggle) if (c) c.enabled = false;

        if (renderersToToggle != null)
            foreach (var r in renderersToToggle) if (r) r.enabled = false;

        // Opcional: destruir el GameObject tras un tiempo:
        // Destroy(gameObject, 1f);
    }
}
