using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform cameraTransform; // arrastrá la Main Camera

    [Header("Movimiento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    [Header("Crouch")]
    [SerializeField] private bool enableCrouch = true;
    [SerializeField, Tooltip("x0.75 = -25% velocidad")]
    private float crouchSpeedMultiplier = 0.75f;
    [SerializeField, Tooltip("x0.5 = -50% altura del CharacterController")]
    private float crouchHeightMultiplier = 0.5f;

    private CharacterController controller;
    private Vector3 velocity;

    private bool isCrouched = false;
    private float baseHeight;
    private Vector3 baseCenter;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        baseHeight = controller.height;
        baseCenter = controller.center;
    }

    void Update()
    {
        // Entrada
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");
        Vector2 input = Vector2.ClampMagnitude(new Vector2(h, v), 1f);

        // Dirección relativa a cámara
        Vector3 camF = cameraTransform ? cameraTransform.forward : Vector3.forward;
        camF.y = 0f; camF.Normalize();
        Vector3 camR = cameraTransform ? cameraTransform.right : Vector3.right;
        camR.y = 0f; camR.Normalize();
        Vector3 moveDir = camF * input.y + camR * input.x;

        // Crouch con C (toggle)
        if (enableCrouch && Input.GetKeyDown(KeyCode.C))
        {
            isCrouched = !isCrouched;
            ApplyCrouch(isCrouched);
        }

        // Velocidad
        float currentSpeed = isCrouched ? moveSpeed * crouchSpeedMultiplier : moveSpeed;

        // Rotación
        if (moveDir.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, rotationSpeed * Time.deltaTime);
        }

        // Gravedad y movimiento
        if (controller.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        Vector3 horizontal = moveDir * currentSpeed;
        controller.Move((horizontal + velocity) * Time.deltaTime);
    }

    private void ApplyCrouch(bool crouch)
    {
        if (crouch)
        {
            controller.height = baseHeight * Mathf.Clamp01(crouchHeightMultiplier); // 50%
            controller.center = new Vector3(baseCenter.x, baseCenter.y * crouchHeightMultiplier, baseCenter.z);
        }
        else
        {
            controller.height = baseHeight;
            controller.center = baseCenter;
        }
    }
}
