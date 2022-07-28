using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Pun.UtilityScripts;

public class FireControl : MonoBehaviour
{
    [SerializeField] PhotonView PV;
    [SerializeField] PlayerControl playerControl;

    [SerializeField] Transform barrel;

    [SerializeField] Transform bullet;
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform shootEffect;

    [SerializeField] float speed = 32;
    [SerializeField] float explosionRadius = 0;
    [SerializeField] int pierceLevel = 0;
    [SerializeField] int ricochetLevel = 1;

    public int bulletLimit = 5;
    public int bulletsFired { get; set; } = 0;
    [SerializeField] float fireCooldown = 4f;
    public bool canFire = true;
    
    [SerializeField] LayerMask solidLayerMask;

    public IEnumerator Shoot()
    {
        if (canFire && bulletsFired < bulletLimit && Time.timeScale != 0)
        {
            canFire = false;

            if (transform.CompareTag("Player"))
            {
                playerControl.myData.currentPlayerData.shots++;
            }

            // Checking if the clone spot is not blocked
            if (!Physics.CheckBox(spawnPoint.position, bullet.GetComponent<Collider>().bounds.size, spawnPoint.rotation, solidLayerMask))
            {
                bulletsFired++;

                Transform bulletClone = InstantiateBullet(spawnPoint.position, spawnPoint.rotation);

                if (!PhotonNetwork.OfflineMode)
                {
                    PV.RPC("InstantiateBullet", RpcTarget.Others, new object[] { spawnPoint.position, spawnPoint.rotation });
                }
                
                yield return new WaitWhile(() => bulletClone.GetComponent<BulletBehaviour>() == null);

                if (bulletClone != null)
                {
                    BulletBehaviour bulletBehaviour = bulletClone.GetComponent<BulletBehaviour>();
                    bulletBehaviour.owner = transform;
                    bulletBehaviour.ownerPV = PV;
                    bulletBehaviour.speed = speed;
                    bulletBehaviour.explosionRadius = explosionRadius;
                    bulletBehaviour.pierceLevel = pierceLevel;
                    bulletBehaviour.ricochetLevel = ricochetLevel;
                    bulletBehaviour.ResetVelocity();

                    if (transform.CompareTag("Player"))
                    {
                        bulletBehaviour.dataSystem = GetComponent<DataManager>();
                    }
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

            bulletsFired = Mathf.Clamp(bulletsFired, 0, bulletLimit);
        }
    }

    [PunRPC]
    Transform InstantiateBullet(Vector3 position, Quaternion rotation)
    {
        Transform bulletClone = Instantiate(bullet, position, rotation);
        Instantiate(shootEffect, position, rotation);

        return bulletClone;
    }

    [PunRPC]
    void ReflectBullet(Transform bullet, Vector3 reflection, Transform sparkEffect)
    {
        Instantiate(sparkEffect, transform.position, Quaternion.identity);

        bullet.forward = reflection;
    }

    [PunRPC]
    void ResetBulletVelocity(Rigidbody bulletRB, float speed)
    {
        bulletRB.velocity = bulletRB.transform.forward * speed;
    }

    [PunRPC]
    void BulletNormalDestroy(GameObject gameObject, Transform explosionEffect)
    {
        Instantiate(explosionEffect, gameObject.transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    [PunRPC]
    void BulletSafeDestroy(GameObject gameObject)
    {
        Destroy(gameObject);
    }
}
