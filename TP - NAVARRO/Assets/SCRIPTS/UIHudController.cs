using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

public class UIHudController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Text ammoText;
    [SerializeField] private Text healthText;

    [Header("Arma")]
    [SerializeField] private GunSemiAuto gun;   // arrastrá el componente GunSemiAuto

    [Header("Player Stats (ScriptableObject)")]
    [SerializeField] private ScriptableObject playerStatsSO; // PlayerStats_Default
    [SerializeField] private string currentHealthField = "startHealth"; // tus nombres
    [SerializeField] private string maxHealthField = "maxHealth";

    private FieldInfo fiCurHP, fiMaxHP;

    void Awake()
    {
        if (!gun) gun = FindObjectOfType<GunSemiAuto>();

        if (playerStatsSO)
        {
            var t = playerStatsSO.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

            fiCurHP = t.GetField(currentHealthField, flags);
            fiMaxHP = t.GetField(maxHealthField, flags);

            if (fiCurHP == null) fiCurHP = FindFirstField(t, new[] { "currentHealth", "startHealth", "hp", "hpActual", "vidaActual" });
            if (fiMaxHP == null) fiMaxHP = FindFirstField(t, new[] { "maxHealth", "maxHp", "vidaMax", "maxVida" });
        }
    }

    void Update()
    {
        if (gun && ammoText)
            ammoText.text = $"Ammo: {gun.Bullets}/{gun.MagSize}";

        if (playerStatsSO && healthText && fiCurHP != null && fiMaxHP != null)
        {
            int cur = Mathf.Clamp((int)fiCurHP.GetValue(playerStatsSO), 0, int.MaxValue);
            int max = Mathf.Max(1, (int)fiMaxHP.GetValue(playerStatsSO));
            healthText.text = $"HP: {cur}/{max}";
        }
    }

    private FieldInfo FindFirstField(System.Type tt, string[] names)
    {
        var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        foreach (var n in names)
        {
            var f = tt.GetField(n, flags);
            if (f != null && f.FieldType == typeof(int)) return f;
        }
        return null;
    }
}
