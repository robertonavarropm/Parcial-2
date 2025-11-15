using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class UIHudController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text ammoText;
    [SerializeField] private Text healthText;

    [Header("Lógica")]
    [SerializeField] private GunSemiAuto gun;                 // ← tu arma
    [SerializeField] private ScriptableObject playerStats;    // ← tu SO de stats (opcional para HP)

    FieldInfo fiCurrentHealth, fiMaxHealth;

    void Awake()
    {
        if (!gun) gun = FindObjectOfType<GunSemiAuto>();

        if (playerStats != null)
        {
            var t = playerStats.GetType();
            fiCurrentHealth = t.GetField("currentHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            fiMaxHealth = t.GetField("maxHealth", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        }
    }

    void Update()
    {
        // ⚠️ CAMBIO CLAVE: usar las propiedades que sí se actualizan en tu arma
        if (gun && ammoText)
            ammoText.text = $"Ammo: {gun.Bullets}/{gun.MagSize}";

        if (playerStats && healthText && fiCurrentHealth != null && fiMaxHealth != null)
        {
            int cur = Mathf.Clamp((int)fiCurrentHealth.GetValue(playerStats), 0, int.MaxValue);
            int max = Mathf.Max(1, (int)fiMaxHealth.GetValue(playerStats));
            healthText.text = $"HP: {cur}/{max}";
        }
    }
}
