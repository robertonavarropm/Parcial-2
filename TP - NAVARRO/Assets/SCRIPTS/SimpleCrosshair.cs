using UnityEngine;

public class SimpleCrosshair : MonoBehaviour
{
    private void OnGUI()
    {
        float size = 4f;
        float x = Screen.width / 2f - size / 2f;
        float y = Screen.height / 2f - size / 2f;

        GUI.Box(new Rect(x, y, size, size), "");
    }
}