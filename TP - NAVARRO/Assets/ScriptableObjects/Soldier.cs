using UnityEngine;

[CreateAssetMenu(fileName = "Soldier", menuName = "Parcial/Enemy/Soldier")]
public class SoldierSO : ScriptableObject
{
    [Header("Identidad")]
    public string displayName = "Soldier";

    [Header("Vida")]
    public int maxHealth = 30;
    public int startHealth = 30;

    [Header("Movimiento")]
    public float moveSpeed = 4f;

    [Header("Visión (cono)")]
    [Tooltip("Medio ángulo del cono (60 => 120° totales)")]
    public float viewHalfAngle = 60f;
    [Tooltip("Alcance máximo de visión en metros")]
    public float viewDistance = 10f;
    [Tooltip("Altura de los ojos del enemigo")]
    public float eyeHeight = 1.6f;

    [Header("Capas de visión")]
    [Tooltip("Layer(s) del Player (marcar 'Player').")]
    public LayerMask playerMask;
    [Tooltip("Layer(s) que bloquean la visión (marcar 'Wall').")]
    public LayerMask obstacleMask;
}
