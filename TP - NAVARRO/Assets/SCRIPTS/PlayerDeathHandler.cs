using System.Reflection;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerDeathHandler : MonoBehaviour
{
    [Header("Player Stats (ScriptableObject)")]
    [SerializeField] private ScriptableObject playerStatsSO;   // arrastrá PlayerStats_Default
    [SerializeField] private string currentHealthField = "startHealth";
    [SerializeField] private string maxHealthField = "maxHealth";

    [Header("Arma (opcional)")]
    [SerializeField] private GunSemiAuto gun;                  // si lo dejas vacío, se auto-detecta
    [SerializeField] private bool refillAmmoOnRespawn = true;

    [Header("Opciones")]
    [SerializeField] private bool resetHealthOnPlay = true;    // al entrar a Play => vida llena

    // --- internos ---
    private FieldInfo fiCurHP, fiMaxHP;
    private bool isDead = false;
    private Vector3 spawnPos;
    private Quaternion spawnRot;

    // cache de cosas a desactivar/activar
    private CharacterController cc;
    private List<MonoBehaviour> autoBehaviours = new List<MonoBehaviour>();
    private List<Renderer> renders = new List<Renderer>();
    private List<Collider> colliders = new List<Collider>();

    void Awake()
    {
        spawnPos = transform.position;
        spawnRot = transform.rotation;

        // caches
        cc = GetComponent<CharacterController>();
        if (!gun) gun = GetComponentInChildren<GunSemiAuto>(true);

        // junta TODOS los renderers y colliders del player
        GetComponentsInChildren(true, renders);
        GetComponentsInChildren(true, colliders);

        // auto-detecta scripts típicos que queremos apagar al morir
        var mover = GetComponent<ThirdPersonController>(); if (mover) autoBehaviours.Add(mover);
        var gunMB = gun as MonoBehaviour; if (gunMB) autoBehaviours.Add(gunMB);

        // bind de campos del SO
        if (!playerStatsSO)
            Debug.LogError("[PlayerDeathHandler] Falta PlayerStatsSO.");
        else
        {
            var t = playerStatsSO.GetType();
            var flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            fiCurHP = t.GetField(currentHealthField, flags);
            fiMaxHP = t.GetField(maxHealthField, flags);
            if (fiCurHP == null || fiMaxHP == null)
                Debug.LogError("[PlayerDeathHandler] No encuentro campos 'startHealth'/'maxHealth' en PlayerStatsSO.");
        }
    }

    void Start()
    {
        if (resetHealthOnPlay && playerStatsSO && fiCurHP != null && fiMaxHP != null)
        {
            int max = Mathf.Max(1, (int)fiMaxHP.GetValue(playerStatsSO));
            fiCurHP.SetValue(playerStatsSO, max);
        }
    }

    void Update()
    {
        // chequear muerte por HP
        if (!isDead && playerStatsSO != null && fiCurHP != null)
        {
            int cur = Mathf.Clamp((int)fiCurHP.GetValue(playerStatsSO), 0, int.MaxValue);
            if (cur <= 0) Die();
        }

        // F1: Respawn
        if (Input.GetKeyDown(KeyCode.F1)) Respawn();

        // F2: Reiniciar escena
        if (Input.GetKeyDown(KeyCode.F2)) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnKilledByEnemy()
    {
        if (!isDead) Die();
    }

    private void Die()
    {
        isDead = true;
        Debug.Log("[PLAYER DEAD] Game Over.");

        // apagar movimiento/arma
        foreach (var mb in autoBehaviours) if (mb) mb.enabled = false;

        // apagar colliders y controller
        if (cc) cc.enabled = false;
        foreach (var c in colliders) if (c) c.enabled = false;

        // ocultar al jugador
        foreach (var r in renders) if (r) r.enabled = false;
    }

    private void Respawn()
    {
        // restaurar vida
        if (playerStatsSO != null && fiCurHP != null && fiMaxHP != null)
        {
            int max = Mathf.Max(1, (int)fiMaxHP.GetValue(playerStatsSO));
            fiCurHP.SetValue(playerStatsSO, max);
        }

        // mover a spawn
        bool hadCC = cc != null;
        if (hadCC) cc.enabled = false;
        transform.SetPositionAndRotation(spawnPos, spawnRot);
        if (hadCC) cc.enabled = true;

        // encender visuales y colisiones
        foreach (var r in renders) if (r) r.enabled = true;
        foreach (var c in colliders) if (c) c.enabled = true;

        // encender scripts
        foreach (var mb in autoBehaviours) if (mb) mb.enabled = true;

        // recargar arma
        if (refillAmmoOnRespawn && gun) gun.RefillFull();

        isDead = false;
        Debug.Log("[PLAYER RESPAWN] Vida y estado restaurados.");
    }
}
