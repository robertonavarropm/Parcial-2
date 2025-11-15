using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    [Header("Fuente de datos (SO)")]
    [SerializeField] private PlayerStatsData data; // ← arrastrás el asset acá

    [Header("Vida")]
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private int health = 100;

    [Header("Estamina")]
    [SerializeField] private int maxStamina = 10;
    [SerializeField] private int stamina = 10;
    [SerializeField] private float staminaRegenInterval = 1f;

    public int Health => health;
    public int Stamina => stamina;

    private bool staminaPaused = false;

    void Start()
    {
        // 1) Copiamos los valores del SO al empezar
        if (data != null)
        {
            maxHealth = data.maxHealth;
            health = Mathf.Clamp(data.startHealth, 0, data.maxHealth);

            maxStamina = data.maxStamina;
            stamina = Mathf.Clamp(data.startStamina, 0, data.maxStamina);
            staminaRegenInterval = data.staminaRegenInterval;
        }

        // 2) Normalizamos y arrancamos la regen
        health = Mathf.Clamp(health, 0, maxHealth);
        stamina = Mathf.Clamp(stamina, 0, maxStamina);
        StartCoroutine(RegenStamina());
    }

    System.Collections.IEnumerator RegenStamina()
    {
        var wait = new WaitForSeconds(staminaRegenInterval);
        while (true)
        {
            yield return wait;
            if (!staminaPaused && stamina < maxStamina)
                stamina = Mathf.Min(maxStamina, stamina + 1);
        }
    }

    public void SetStaminaPaused(bool paused) => staminaPaused = paused;
    public void TickStaminaDrain(int amount)
    {
        if (amount > 0 && stamina > 0) stamina = Mathf.Max(0, stamina - amount);
    }

    public void ApplyDamage(int amount)
    {
        health = Mathf.Clamp(health - Mathf.Max(0, amount), 0, maxHealth);
    }

    public void Heal(int amount)
    {
        health = Mathf.Clamp(health + Mathf.Max(0, amount), 0, maxHealth);
    }

    public bool TrySpendStamina(int amount)
    {
        if (stamina < amount) return false;
        stamina = Mathf.Clamp(stamina - amount, 0, maxStamina);
        return true;
    }

    public void ForceSetStamina(int value)
    {
        stamina = Mathf.Clamp(value, 0, maxStamina);
    }
}
