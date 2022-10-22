using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class FireControl : MonoBehaviourPun
{
    [SerializeField] PlayerControl playerControl;

    [SerializeField] Transform barrel;

    [SerializeField] Transform bullet;
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform shootEffect;
    public Transform bulletParent;

    public float speed = 32;
    [SerializeField] int pierceLevel = 0;
    public int pierceLimit = 0;
    public int ricochetLevel = 1;

    public int bulletLimit = 5;
    public List<Transform> firedBullets { get; set; } = new List<Transform>();
    [SerializeField] float[] fireCooldown = { 2, 4 };
    public bool canFire = true;
    
    [SerializeField] LayerMask solidLayerMask;

    private void Start()
    {
        if (GameManager.Instance.autoPlay)
        {
            bulletParent = GameObject.Find("ToClear").transform;
        }
    }

    public bool BulletSpawnClear()
    {
        return !Physics.Raycast(barrel.position, spawnPoint.position - barrel.position, Vector3.Distance(spawnPoint.position, barrel.position), solidLayerMask);
    }

    public IEnumerator Shoot()
    {
        if (canFire && firedBullets.Count < bulletLimit && Time.timeScale != 0 && BulletSpawnClear())
        {
            canFire = false;

            if (transform.CompareTag("Player"))
            {
                DataManager.playerData.shots++;
            }

            InstantiateBullet(spawnPoint.position, spawnPoint.rotation);

            yield return new WaitForSeconds(Random.Range(fireCooldown[0], fireCooldown[1]));

            canFire = true;
        }
        else
        {
            canFire = true;
            yield return null;
        }
    }

    [PunRPC]
    public void MultiplayerInstantiateBullet(Vector3 position, Quaternion rotation, float speed, int pierceLimit, int ricochetLevel, int bulletID)
    {
        Transform bulletClone = Instantiate(bullet, position, rotation, bulletParent);
        firedBullets.Add(bulletClone);
        Instantiate(shootEffect, position, rotation, bulletParent);
        StartCoroutine(InitializeBullet(bulletClone, speed, pierceLimit, ricochetLevel, bulletID));
    }

    void InstantiateBullet(Vector3 position, Quaternion rotation)
    {
        Transform bulletClone = Instantiate(bullet, position, rotation, bulletParent);
        firedBullets.Add(bulletClone);
        Instantiate(shootEffect, position, rotation, bulletParent);
        StartCoroutine(InitializeBullet(bulletClone, speed, pierceLimit, ricochetLevel, bulletClone.GetInstanceID()));
    }

    IEnumerator InitializeBullet(Transform bullet, float _speed, int _pierceLimit, int _ricochetLevel, int ID)
    {
        bullet.gameObject.SetActive(false);
        yield return new WaitUntil(() => bullet.GetComponent<BulletBehaviour>() != null);
        if (bullet != null)
        {
            bullet.gameObject.SetActive(true);

            BulletBehaviour bulletBehaviour = bullet.GetComponent<BulletBehaviour>();
            bulletBehaviour.owner = transform;
            bulletBehaviour.ownerPV = photonView;
            bulletBehaviour.speed = _speed;
            bulletBehaviour.pierceLevel = pierceLevel;
            bulletBehaviour.pierceLimit = _pierceLimit;
            bulletBehaviour.ricochetLevel = _ricochetLevel;
            bulletBehaviour.ResetVelocity();
            if (!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
            {
                bulletBehaviour.bulletID = ID;

                if (photonView.IsMine)
                {
                    photonView.RPC("MultiplayerInstantiateBullet", RpcTarget.Others, new object[] { spawnPoint.position, spawnPoint.rotation, _speed, _pierceLimit, _ricochetLevel, ID });
                }
            }
        }
        else
        {
            firedBullets.Remove(bullet);
        }
    }
}
