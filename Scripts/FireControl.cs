using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireControl : MonoBehaviour
{
    public Transform projectileParent;

    public Transform bullet;

    Transform barrel;

    public int bulletLimit = 5;
    public int bulletsFired { get; set; } = 0;
    public float fireCooldown = 1f;
    bool canFire = true;

    // Start is called before the first frame update
    void Awake()
    {
        barrel = transform.Find("Barrel");
    }

    public IEnumerator Shoot()
    {
        if (canFire && bulletsFired < bulletLimit && Time.timeScale != 0)
        {
            canFire = false;
            bulletsFired++;

            Transform bulletClone = null;
            bulletClone = Instantiate(bullet, barrel.position + (barrel.Find("Anchor").forward * 2), Quaternion.LookRotation(barrel.Find("Anchor").forward), projectileParent);
            bulletClone.localScale = new Vector3(1, 1, 1);

            yield return new WaitWhile(() => bulletClone.GetComponent<BulletBehaviour>() == null);

            bulletClone.GetComponent<BulletBehaviour>().owner = transform;

            yield return new WaitForSeconds(fireCooldown);
            canFire = true;
        }
    }
}
