using UnityEngine;

public class Gun : Weapon
{
    public Projectile bullet;

    public float shootForce, upwardForce;

    public float spread = 0, reloadTime, timeBetweenShots;
    public int magazineSize, bulletsPerTap;
    public bool infiniteMagazine;

    int bulletsLeft, bulletsShot;

    public Rigidbody playerRb;
    public float recoilForce;

    bool reloading;

    //public Camera fpsCam;

    public GameObject muzzleFlash;

    public Transform bulletSource;

    public bool allowInvoke = true;

    private void Awake()
    {
        bulletsLeft = magazineSize;
    }

    public void TryReload() 
    {
        if ((bulletsLeft < magazineSize) && !infiniteMagazine && !reloading) Reload();
        if (CanUse && !reloading && bulletsLeft <= 0 && !infiniteMagazine) Reload();
    }

    public override bool Attack(GameObject target)
    {
        if (CanUse && !reloading)
        {
            if(bulletsLeft <= 0 && !infiniteMagazine) {
                TryReload();
                return false;
            }

            bulletsShot = 0;

            Shoot();

            return true;
        } 
        else
        {
            return false;
        }
    }

    private void Shoot()
    {
        CanUse = false;

        Vector3 source = bulletSource.transform.position;
        Vector3 target = bulletSource.forward;//new Vector3(bulletSource.transform.forward.x, bulletSource.forward.y, bulletSource.transform.forward.z).normalized * bullet.range;

        GameObject currentBullet = Instantiate(bullet.gameObject, source, Quaternion.Euler(target));
        currentBullet.SetActive(true);
        //currentBullet.transform.forward = target.normalized;

        currentBullet.GetComponent<Rigidbody>().AddForce(target.normalized * shootForce, ForceMode.Impulse);
        currentBullet.GetComponent<Rigidbody>().AddForce(transform.up * upwardForce, ForceMode.Impulse);

        if (muzzleFlash != null)
            Instantiate(muzzleFlash, transform.position, Quaternion.identity);

        bulletsLeft--;
        bulletsShot++;

        if (allowInvoke)
        {
            Invoke(nameof(ResetShot), cooldownTime);
            allowInvoke = false;

            if(playerRb != null && recoilForce != 0)
                playerRb.AddForce(-target.normalized * recoilForce, ForceMode.Impulse);
        }

        if (bulletsShot < bulletsPerTap && bulletsLeft > 0)
            Invoke(nameof(Shoot), timeBetweenShots);
    }

    private void ResetShot()
    {
        allowInvoke = true;
        CooldownFinished();
    }

    private void Reload()
    {
        reloading = true;
        Invoke(nameof(ReloadFinished), reloadTime);
    }

    private void ReloadFinished()
    {
        bulletsLeft = magazineSize;
        reloading = false;
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 source = bulletSource.transform.position;
        Vector3 target = new Vector3(transform.forward.x, bulletSource.forward.y, transform.forward.z).normalized * bullet.maxRange;

        Gizmos.DrawRay(source, target);
    }
}
