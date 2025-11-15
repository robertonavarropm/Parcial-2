using UnityEngine;

public class BillboardToCamera : MonoBehaviour
{
    Transform cam;
    void LateUpdate()
    {
        if (cam == null && Camera.main != null) cam = Camera.main.transform;
        if (cam == null) return;
        transform.forward = cam.forward; // el canvas siempre mira a la cámara
    }
}
