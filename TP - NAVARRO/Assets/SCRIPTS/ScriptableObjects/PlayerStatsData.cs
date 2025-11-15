using UnityEngine;

[CreateAssetMenu(fileName = "PlayerStatsData", menuName = "Parcial/Player Stats")]
public class PlayerStatsData : ScriptableObject
{
    [Header("Vida")]
    public int maxHealth = 100;
    public int startHealth = 100;

    [Header("Estamina")]
    public int maxStamina = 10;
    public int startStamina = 10;
    public float staminaRegenInterval = 1f; // +1 por segundo

    [Header("Movimiento / Sprint")]
    public float moveSpeed = 5f;
    public float sprintSpeed = 8f;
    public int sprintDrainPerTick = 1;
    public float sprintDrainInterval = 1f;

    [Header("Arma")]
    public int gunDamage = 10;
    public float gunRange = 25f;
    public float gunFireRate = 4f;
}
