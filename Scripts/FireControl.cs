using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FireControl : MonoBehaviour
{
    [SerializeField] Transform owner;

    [SerializeField] Transform bullet;
    [SerializeField] Transform shootEffect;

    Transform barrel;

    public int bulletLimit = 5;
    public int bulletsFired { get; set; } = 0;
    [SerializeField] float fireCooldown = 4f;
    public bool canFire = true;
    
    [SerializeField] LayerMask solidLayerMask;

    // Start is called before the first frame Update
    void Awake()
    {
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

            Transform bulletClone = null;
            Vector3 clonePosition = barrel.position + barrel.forward * 2;
            Quaternion cloneRotation = Quaternion.LookRotation(barrel.forward, Vector3.up);
            // Checking if the clone spot is not blocked
            if (!Physics.CheckBox(clonePosition, bullet.GetComponent<Collider>().bounds.size, cloneRotation, solidLayerMask))
            {
                bulletsFired++;
                bulletClone = Instantiate(bullet, clonePosition, cloneRotation);
                bulletClone.localScale = new Vector3(1, 1, 1);
                Instantiate(shootEffect, clonePosition, cloneRotation);

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
            else
            {
                canFire = true;
                yield return null;
            }
        }
    }
}
