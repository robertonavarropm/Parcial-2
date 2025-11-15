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
    private bool isReloading = false; // por si luego agregás tiempo de recarga

    [Header("Referencias")]
    [SerializeField] private Camera cam;
    [SerializeField] private Transform muzzle;

    [Header("Layers")]
    [SerializeField] private LayerMask hitMask = ~0;

    void Awake()
    {
        if (cam == null && Camera.main != null) cam = Camera.main;
        bulletsInMag = Mathf.Clamp(bulletsInMag, 0, magazineSize);
    }

    void Update()
    {
        // recargar
        if (Input.GetKeyDown(reloadKey))
        {
            TryReload();
        }

        // disparar (semi-auto)
        if (Input.GetMouseButtonDown(0))
        {
            TryFire();
        }
    }

    private void TryFire()
    {
        if (Time.time < nextFireTime) return;
        if (isReloading) return;

        // sin balas => no dispara
        if (bulletsInMag <= 0)
        {
            Debug.Log("[GUN] Sin balas, recargar con R.");
            return;
        }

        nextFireTime = Time.time + (1f / fireRate);

        // consumir bala
        bulletsInMag--;

        Ray ray = new Ray(cam.transform.position, cam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            var enemy = hit.collider.GetComponentInParent<EnemySoldier>();
            if (enemy != null)
            {
                enemy.ApplyDamage(damage);
                Debug.Log($"[GUN] Hit enemy por {damage}. Balas: {bulletsInMag}/{magazineSize}");
            }
            else
            {
                Debug.Log($"[GUN] Hit {hit.collider.name}. Balas: {bulletsInMag}/{magazineSize}");
            }
        }

        // debug rayo
        if (muzzle != null) Debug.DrawRay(muzzle.position, cam.transform.forward * range, Color.red, 0.2f);
        else Debug.DrawRay(cam.transform.position, cam.transform.forward * range, Color.red, 0.2f);
    }

    private void TryReload()
    {
        if (isReloading) return;

        if (bulletsInMag >= magazineSize)
        {
            Debug.Log("[GUN] Cargador completo, no se puede recargar.");
            return;
        }

        // cargadores ilimitados: recarga instantánea al máximo
        bulletsInMag = magazineSize;
        Debug.Log($"[GUN] Recargado. Balas: {bulletsInMag}/{magazineSize}");
    }

    // setters opcionales
    public void SetDamage(int d) => damage = Mathf.Max(0, d);
    public void SetRange(float r) => range = Mathf.Max(0f, r);
    public void SetFireRate(float rps) => fireRate = Mathf.Max(0.1f, rps);

    // getters útiles por si querés UI
    public int Bullets => bulletsInMag;
    public int MagSize => magazineSize;

    public int magCapacity = 15;
    public int currentAmmo = 15;

}
