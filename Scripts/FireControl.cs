using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireControl : MonoBehaviour
{
    Transform projectileParent;
    [SerializeField] Transform owner;

    [SerializeField] Transform bullet;

    Transform barrel;

    public int bulletLimit = 5;
    public int bulletsFired { get; set; } = 0;
    public float fireCooldown = 1f;
    bool canFire = true;

    // Start is called before the first frame Update
    void Awake()
    {
        if (projectileParent == null)
        {
            projectileParent = GameObject.Find("Projectiles").transform;
        }

        if (transform.name != "Player")
        {
            barrel = transform.Find("Barrel");
        }
        else
        {
            barrel = transform.Find("Tank Origin").Find("Barrel");
        }
    }

    public IEnumerator Shoot()
    {
        if (canFire && bulletsFired < bulletLimit && Time.timeScale != 0)
        {
            canFire = false;
            bulletsFired++;

            Transform bulletClone = null;
            bulletClone = Instantiate(bullet, barrel.position + barrel.forward * 2, Quaternion.LookRotation(barrel.forward), projectileParent);
            bulletClone.localScale = new Vector3(1, 1, 1);

            yield return new WaitWhile(() => bulletClone.GetComponent<BulletBehaviour>() == null);

            if (bulletClone != null)
            {
                bulletClone.GetComponent<BulletBehaviour>().owner = owner;
            }
            else
            {
                bulletsFired--;
            }

            yield return new WaitForSeconds(fireCooldown);
            canFire = true;
        }
    }
}
