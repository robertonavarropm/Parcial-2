using UnityEngine;

public class CameraOrbitTPS : MonoBehaviour
{
    [Header("Objetivo")]
    [SerializeField] private Transform target;          // arrastra aquí el Player
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.6f, 0f);

    [Header("Órbita")]
    [SerializeField] private float distance = 4f;
    [SerializeField] private float mouseSensitivityX = 200f;
    [SerializeField] private float mouseSensitivityY = 120f;
    [SerializeField] private float minPitch = -35f;
    [SerializeField] private float maxPitch = 70f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private float yaw;   // rotación horizontal
    private float pitch; // rotación vertical

    void Start()
    {
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (target == null) return;

        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        yaw += mouseX * mouseSensitivityX * Time.deltaTime;
        pitch -= mouseY * mouseSensitivityY * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 desiredPos = target.position + targetOffset - (rot * Vector3.forward * distance);

        transform.position = desiredPos;
        transform.rotation = rot;

        // (Opcional) siempre mirar al target:
        // transform.LookAt(target.position + targetOffset);
    }
}
