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
    [SerializeField] Transform bulletParent;

    public float speed = 32;
    [SerializeField] float explosionRadius = 0;
    [SerializeField] int pierceLevel = 0;
    [SerializeField] int ricochetLevel = 1;

    public int bulletLimit = 5;
    public int bulletsFired { get; set; } = 0;
    [SerializeField] float[] fireCooldown = { 2, 4 };
    public bool canFire = true;
    
    [SerializeField] LayerMask solidLayerMask;

    private void Start()
    {
        if (GameManager.autoPlay)
        {
            bulletParent = GameObject.Find("ToClear").transform;
        }
    }

    public IEnumerator Shoot()
    {
        if (canFire && bulletsFired < bulletLimit && Time.timeScale != 0)
        {
            canFire = false;

            // Checking if the clone spot is not blocked
            if (!Physics.CheckBox(spawnPoint.position, bullet.GetComponent<Collider>().bounds.size, spawnPoint.rotation, solidLayerMask))
            {
                if (transform.CompareTag("Player"))
                {
                    playerControl.myData.currentPlayerData.shots++;
                }

                bulletsFired++;

                Transform bulletClone = InstantiateBullet(spawnPoint.position, spawnPoint.rotation);

                if (!PhotonNetwork.OfflineMode && !GameManager.autoPlay)
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
                yield return new WaitForSeconds(Random.Range(fireCooldown[0], fireCooldown[1]));

                canFire = true;
            }
            else
            {
                canFire = true;
                yield return null;
            }
        }
    }

    [PunRPC]
    Transform InstantiateBullet(Vector3 position, Quaternion rotation)
    {
        Transform bulletClone = Instantiate(bullet, position, rotation, bulletParent);
        Instantiate(shootEffect, position, rotation, bulletParent);

        return bulletClone;
    }
}
