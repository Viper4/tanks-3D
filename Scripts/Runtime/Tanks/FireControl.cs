using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class FireControl : MonoBehaviourPun
{
    [SerializeField] PlayerControl playerControl;
    BaseTankLogic baseTankLogic;

    [SerializeField] Transform barrel;

    [SerializeField] Transform[] bulletTypes;
    [SerializeField] Transform spawnPoint;
    [SerializeField] Transform shootEffect;
    public Transform bulletParent;

    public int bulletLimit = 5;
    [HideInInspector] public List<Transform> firedBullets = new List<Transform>();
    [SerializeField] float[] fireCooldown = { 2, 4 };
    public bool canFire = true;
    
    [SerializeField] LayerMask solidLayerMask;

    [HideInInspector] public BulletBehaviour.BulletSettings originalBulletSettings = new BulletBehaviour.BulletSettings();

    public BulletBehaviour.BulletSettings bulletSettings = new BulletBehaviour.BulletSettings()
    {
        bulletIndex = 0,
        speed = 16,
        pierceLevel = 0,
        pierceLimit = 0,
        ricochetLevel = 1,
        explosionRadius = 5
    };

    private void Start()
    {
        originalBulletSettings = bulletSettings;
        baseTankLogic = GetComponent<BaseTankLogic>();

        if(GameManager.Instance.autoPlay)
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
        if(!baseTankLogic.disabled && canFire && firedBullets.Count < bulletLimit && Time.timeScale != 0 && BulletSpawnClear())
        {
            canFire = false;

            if(transform.CompareTag("Player"))
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
        Transform bulletPrefab = bulletTypes[bulletSettings.bulletIndex];
        Transform bulletClone = Instantiate(bulletPrefab, position, rotation, bulletParent);
        bulletClone.name = bulletPrefab.name;
        firedBullets.Add(bulletClone);
        Instantiate(shootEffect, position, rotation, bulletParent);
        StartCoroutine(InitializeBullet(bulletClone, speed, pierceLimit, ricochetLevel, bulletID));
    }

    void InstantiateBullet(Vector3 position, Quaternion rotation)
    {
        Transform bulletPrefab = bulletTypes[bulletSettings.bulletIndex];
        Transform bulletClone = Instantiate(bulletPrefab, position, rotation, bulletParent);
        bulletClone.name = bulletPrefab.name;
        firedBullets.Add(bulletClone);
        Instantiate(shootEffect, position, rotation, bulletParent);
        StartCoroutine(InitializeBullet(bulletClone, bulletSettings.speed, bulletSettings.pierceLimit, bulletSettings.ricochetLevel, bulletClone.GetInstanceID()));
    }

    IEnumerator InitializeBullet(Transform bullet, float _speed, int _pierceLimit, int _ricochetLevel, int ID)
    {
        bullet.gameObject.SetActive(false);
        yield return new WaitUntil(() => bullet.GetComponent<BulletBehaviour>() != null);
        if(bullet != null)
        {
            bullet.gameObject.SetActive(true);

            BulletBehaviour bulletBehaviour = bullet.GetComponent<BulletBehaviour>();
            bulletBehaviour.owner = transform;
            bulletBehaviour.ownerPV = photonView;
            bulletBehaviour.settings.speed = _speed;
            bulletBehaviour.settings.pierceLevel = bulletSettings.pierceLevel;
            bulletBehaviour.settings.pierceLimit = _pierceLimit;
            bulletBehaviour.settings.ricochetLevel = _ricochetLevel;
            bulletBehaviour.ResetVelocity();
            if(bullet.TryGetComponent<Explosive>(out var explosive))
            {
                explosive.owner = transform;
                explosive.initiator = transform;
                explosive.ownerPV = photonView;
                explosive.explosionRadius = bulletSettings.explosionRadius;
            }
            if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
            {
                bulletBehaviour.bulletID = ID;

                if(photonView.IsMine)
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
