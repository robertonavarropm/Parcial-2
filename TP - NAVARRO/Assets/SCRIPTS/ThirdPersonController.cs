using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ThirdPersonController : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveSpeed = 4f;

    [Header("Crouch (agachado)")]
    public float crouchSpeedMultiplier = 0.75f; // -25%
    public float crouchHeightFactor = 0.5f;     // 50% altura
    public float crouchLerpSpeed = 12f;

    [Header("Gravedad")]
    public float gravity = -9.81f;

    [Header("Cámara")]
    [SerializeField] private Camera cam; // arrastrá tu cámara; si lo dejás vacío toma la main

    private CharacterController cc;
    private float yVelocity;
    private float originalHeight;
    private Vector3 originalCenter;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        originalHeight = cc.height;
        originalCenter = cc.center;
        if (!cam && Camera.main) cam = Camera.main;
    }

    void Update()
    {
        // --- Entrada WASD ---
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 input = new Vector3(h, 0f, v);
        input = Vector3.ClampMagnitude(input, 1f);

        // --- Dirección en espacio de CÁMARA (soluciona inversión) ---
        Vector3 moveDir = Vector3.zero;
        if (cam)
        {
            Vector3 camF = cam.transform.forward; camF.y = 0f; camF.Normalize();
            Vector3 camR = cam.transform.right; camR.y = 0f; camR.Normalize();
            moveDir = camF * input.z + camR * input.x;
        }
        else
        {
            // fallback: espacio mundo Z/X
            moveDir = new Vector3(input.x, 0f, input.z);
        }

        // --- Agachado (mantener C o Ctrl) ---
        bool crouching = Input.GetKey(KeyCode.C) ||
                         Input.GetKey(KeyCode.LeftControl) ||
                         Input.GetKey(KeyCode.RightControl);

        float speed = moveSpeed * (crouching ? crouchSpeedMultiplier : 1f);

        // --- Movimiento horizontal ---
        Vector3 velocity = moveDir * speed;

        // --- Gravedad / suelo (sin salto) ---
        if (cc.isGrounded && yVelocity < 0f) yVelocity = -2f;
        yVelocity += gravity * Time.deltaTime;
        velocity.y = yVelocity;

        cc.Move(velocity * Time.deltaTime);

        // --- Transición suave de altura/centro al agacharse ---
        float targetHeight = crouching ? originalHeight * crouchHeightFactor : originalHeight;
        Vector3 targetCenter = crouching
            ? new Vector3(originalCenter.x, originalCenter.y * crouchHeightFactor, originalCenter.z)
            : originalCenter;

        cc.height = Mathf.Lerp(cc.height, targetHeight, Time.deltaTime * crouchLerpSpeed);
        cc.center = Vector3.Lerp(cc.center, targetCenter, Time.deltaTime * crouchLerpSpeed);
    }
}
