using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Types/Surveillance Camera")]
public class SurveillanceCameraSO : ScriptableObject
{
    [Header("Stats")]
    public int maxHealth = 100;

    [Header("Visión")]
    [Tooltip("Ángulo TOTAL del cono en grados (consigna: 60).")]
    public float viewAngleDegTotal = 60f;
    [Tooltip("Alcance máximo (consigna: 5 m).")]
    public float viewDistance = 5f;
    [Tooltip("Altura del 'ojo' para raycast y gizmos.")]
    public float eyeHeight = 2.0f;
}
