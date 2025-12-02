using UnityEngine;

public class GunSemiAuto : MonoBehaviour
{
    [Header("Daño & Rango")]
    [SerializeField] private int damage = 10;
    [SerializeField] private float range = 25f;

    [Header("Cadencia")]
    [SerializeField] private float fireRate = 4f; // disparos por segundo
    private float nextFireTime = 0f;

    [Header("Cargador")]
    [SerializeField] private int magazineSize = 15;
    [SerializeField] private int bulletsInMag = 15; // actual
    [SerializeField] private KeyCode reloadKey = KeyCode.R;
    private bool isReloading = false;

    [Header("Referencias")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform muzzle;

    [Header("Layers")]
    [SerializeField] private LayerMask hitMask = ~0; // tildá la Layer del ENEMY

    void Awake()
    {
        if (cam == null && Camera.main != null) cam = Camera.main;
        bulletsInMag = Mathf.Clamp(bulletsInMag, 0, magazineSize);
    }

    void Update()
    {
        if (Input.GetKeyDown(reloadKey)) TryReload();
        if (Input.GetMouseButtonDown(0)) TryFire();
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (isReloading) return;

        if (bulletsInMag <= 0)
        {
            Debug.Log("[GUN] Sin balas, recargar con R.");
            return;
        }

        nextFireTime = Time.time + (1f / fireRate);
        bulletsInMag--;

        Vector3 origin = cam ? cam.transform.position : transform.position;
        Vector3 dir = cam ? cam.transform.forward : transform.forward;

        bool hitSomething = false;

        // SphereCast finito (mejor pega a cápsulas)
        if (Physics.SphereCast(new Ray(origin, dir), 0.05f, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Collide))
        {
            hitSomething = true;
            ApplyDamageIfPossible(hit);
        }
        else if (Physics.Raycast(origin, dir, out RaycastHit hit2, range, hitMask, QueryTriggerInteraction.Collide))
        {
            hitSomething = true;
            ApplyDamageIfPossible(hit2);
        }

        if (!hitSomething)
            Debug.Log("[GUN] No hit (revisá máscaras/capas/rango)");

        if (muzzle != null) Debug.DrawRay(muzzle.position, dir * range, Color.red, 0.2f);
        else Debug.DrawRay(origin, dir * range, Color.red, 0.2f);
    }

    private void ApplyDamageIfPossible(RaycastHit hit)
    {
        var go = hit.collider.gameObject;
        Debug.Log($"[GUN] Hit '{go.name}' (layer={LayerMask.LayerToName(go.layer)})");

        var dmg = go.GetComponentInParent<IDamageable>();
        if (dmg != null)
        {
            dmg.ApplyDamage(damage);
            Debug.Log($"[GUN] Damage {damage}. Balas: {bulletsInMag}/{magazineSize}");
        }
        else
        {
            // Fallback por si olvidaste poner IDamageable en el enemigo
            var soldier = go.GetComponentInParent<EnemySoldier>();
            if (soldier != null)
            {
                soldier.ApplyDamage(damage);
                Debug.Log($"[GUN] Damage (fallback EnemySoldier) {damage}. Balas: {bulletsInMag}/{magazineSize}");
            }
            else
            {
                Debug.Log($"[GUN] Impacto en '{go.name}' sin IDamageable.");
            }
        }
    }

    private void TryReload()
    {
        if (isReloading) return;
        if (bulletsInMag >= magazineSize)
        {
            Debug.Log("[GUN] Cargador completo, no se puede recargar.");
            return;
        }
        bulletsInMag = magazineSize;
        Debug.Log($"[GUN] Recargado. Balas: {bulletsInMag}/{magazineSize}");
    }

    // getters para UI
    public int Bullets => bulletsInMag;
    public int MagSize => magazineSize;

    // usado por el respawn
    public void RefillFull()
    {
        bulletsInMag = magazineSize;
        Debug.Log($"[GUN] RefillFull -> {bulletsInMag}/{magazineSize}");
    }
}
