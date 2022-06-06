using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FireControl : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;

    [SerializeField] Transform owner;

    [SerializeField] Transform bullet;
    [SerializeField] Transform shootEffect;

    [SerializeField] Transform barrel;

    public int bulletLimit = 5;
    public int bulletsFired { get; set; } = 0;
    [SerializeField] float fireCooldown = 4f;
    public bool canFire = true;
    
    [SerializeField] LayerMask solidLayerMask;

    public IEnumerator Shoot()
    {
        if (transform.CompareTag("Player"))
        {
            playerControl.dataSystem.currentPlayerData.shots++;
        }

        bulletsFired = Mathf.Clamp(bulletsFired, 0, bulletLimit);
        if (canFire && bulletsFired < bulletLimit && Time.timeScale != 0)
        {
            canFire = false;

            Transform bulletClone = null;
            Vector3 clonePosition = barrel.position + barrel.forward * 2.25f;
            Quaternion cloneRotation = Quaternion.LookRotation(barrel.forward, Vector3.up);
            // Checking if the clone spot is not blocked
            if (!Physics.CheckBox(clonePosition, bullet.GetComponent<Collider>().bounds.size, cloneRotation, solidLayerMask))
            {
                bulletsFired++;
                if (playerControl != null && playerControl.ClientManager.inMultiplayer)
                {
                    bulletClone = PhotonNetwork.Instantiate(bullet.name, clonePosition, cloneRotation).transform;
                    PhotonNetwork.Instantiate(shootEffect.name, clonePosition, cloneRotation);
                }
                else
                {
                    bulletClone = Instantiate(bullet, clonePosition, cloneRotation);
                    Instantiate(shootEffect, clonePosition, cloneRotation);
                }
                bulletClone.transform.localScale = new Vector3(1, 1, 1);

                yield return new WaitWhile(() => bulletClone.GetComponent<BulletBehaviour>() == null);

                if (bulletClone != null)
                {
                    bulletClone.GetComponent<BulletBehaviour>().owner = owner;
                    if (transform.CompareTag("Player"))
                    {
                        bulletClone.GetComponent<BulletBehaviour>().dataSystem = owner.GetComponent<DataSystem>();
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
        }
    }
}
