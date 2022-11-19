using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boost : MonoBehaviourPun, IPunInstantiateMagicCallback
{
    [SerializeField] float value = 1;
    [SerializeField] float effectDuration = 10;
    public float[] duration = { 25, 40 };
    private float currentDuration = -1;
    [SerializeField] float flashDuration = 5;
    [SerializeField] float flashTime = 1;
    [SerializeField] Material[] flashMaterials;
    Material[] savedMaterials;
    bool flashing;
    [SerializeField] float respawnDelay = 5;

    [SerializeField] ParticleSystem particles;
    [SerializeField] AudioSource audioSource;

    [SerializeField] GameObject spinningObject;
    MeshRenderer spinningObjectRenderer;
    [SerializeField] float spinRate = 50;
    Collider triggerCollider;

    [SerializeField]
    enum Mode
    {
        Static,
        Dynamic,
    }
    [SerializeField] Mode mode = Mode.Static;
    [SerializeField] int useLimit = -1;
    int uses = 0;

    bool activated = false;

    [SerializeField]
    enum BoostType
    {
        Bullet,
        Mine,
        Bounce,
        Pierce,
        Speed,
        Rockets,
        Missiles,
        Grenades,
        Invisibility,
        Shields
    }
    [SerializeField] BoostType type = BoostType.Bullet;

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        object[] instantiationData = info.photonView.InstantiationData;
        currentDuration =(float)instantiationData[0];
        transform.SetParent(BoostGenerator.Instance.transform);
    }

    private void Start()
    {
        if(currentDuration < 0)
        {
            currentDuration = Random.Range(duration[0], duration[1]);
        }

        triggerCollider = GetComponent<Collider>();
        spinningObjectRenderer = spinningObject.GetComponent<MeshRenderer>();
        savedMaterials = spinningObjectRenderer.materials;
    }

    private void Update()
    {
        spinningObject.transform.rotation = Quaternion.AngleAxis(Time.deltaTime * spinRate, Vector3.up) * spinningObject.transform.rotation;
        if(!activated && mode != Mode.Static && Time.timeScale > 0 && !GameManager.Instance.frozen)
        {
            currentDuration -= Time.deltaTime;

            if(currentDuration <= flashDuration)
            {
                if(currentDuration > 0)
                {
                    if(!flashing)
                    {
                        StartCoroutine(Flash());
                    }
                }
                else if(PhotonNetwork.IsMasterClient)
                {
                    BoostGenerator.Instance.SpawnNewBoost();
                    if(PhotonNetwork.OfflineMode)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") &&(PhotonNetwork.OfflineMode || other.transform.parent.parent.GetComponent<PhotonView>().IsMine))
        {
            StartCoroutine(Activate(other.transform.parent.parent));
        }
    }

    IEnumerator Flash()
    {
        flashing = true;
        Material[] newMaterials = new Material[spinningObjectRenderer.materials.Length];
        for(int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = flashMaterials[i];
        }
        spinningObjectRenderer.materials = newMaterials;
        yield return new WaitForSeconds(flashTime * 0.5f);

        for(int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = savedMaterials[i];
        }
        spinningObjectRenderer.materials = newMaterials;
        yield return new WaitForSeconds(flashTime * 0.5f);

        flashing = false;
    }

    [PunRPC]
    public void PlayEffects()
    {
        particles.Play();
        audioSource.Play();
        spinningObject.SetActive(false);
        triggerCollider.enabled = false;
    }

    [PunRPC]
    public void Respawn()
    {
        spinningObject.SetActive(true);
        triggerCollider.enabled = true;
    }

    IEnumerator Activate(Transform player)
    {
        activated = true;
        if(!PhotonNetwork.OfflineMode)
        {
            photonView.RPC("PlayEffects", RpcTarget.All, null);
        }
        else
        {
            PlayEffects();
        }
        FireControl fireControl = player.GetComponent<FireControl>();
        MineControl mineControl = player.GetComponent<MineControl>();
        BaseTankLogic baseTankLogic = player.GetComponent<BaseTankLogic>();

        switch(type)
        {
            case BoostType.Bullet:
                fireControl.bulletLimit +=(int)value;
                player.Find("Player UI").GetComponent<PlayerUIHandler>().UpdateBulletIcons();

                yield return new WaitForSeconds(effectDuration);
                fireControl.bulletLimit -=(int)value;
                fireControl.transform.Find("Player UI").GetComponent<PlayerUIHandler>().UpdateBulletIcons();
                break;
            case BoostType.Mine:
                mineControl.mineLimit +=(int)value;
                player.Find("Player UI").GetComponent<PlayerUIHandler>().UpdateMineIcons();

                yield return new WaitForSeconds(effectDuration);
                mineControl.mineLimit -=(int)value;
                mineControl.transform.Find("Player UI").GetComponent<PlayerUIHandler>().UpdateMineIcons();
                break;
            case BoostType.Bounce:
                fireControl.bulletSettings.ricochetLevel +=(int)value;

                yield return new WaitForSeconds(effectDuration);
                fireControl.bulletSettings.ricochetLevel -=(int)value;
                break;
            case BoostType.Pierce:
                fireControl.bulletSettings.pierceLimit +=(int)value;

                yield return new WaitForSeconds(effectDuration);
                fireControl.bulletSettings.pierceLimit -=(int)value;
                break;
            case BoostType.Speed:
                baseTankLogic.normalSpeed += value;

                yield return new WaitForSeconds(effectDuration);
                baseTankLogic.normalSpeed -= value;
                break;
            case BoostType.Rockets:
                if(!PhotonNetwork.OfflineMode && player.TryGetComponent<PhotonView>(out var playerPV))
                {
                    playerPV.RPC("ApplyBulletBoost", RpcTarget.All, new object[] { effectDuration, 1, 26.0f, fireControl.bulletSettings.pierceLimit, fireControl.bulletSettings.ricochetLevel, 5.0f });
                }
                else
                {
                    player.GetComponent<BulletBoost>().ApplyBulletBoost(effectDuration, 1, 26.0f, fireControl.bulletSettings.pierceLimit, fireControl.bulletSettings.ricochetLevel, 5.0f);
                }

                yield return new WaitForSeconds(4); // Wait for particles to finish playing
                break;
            case BoostType.Missiles:
                if(!PhotonNetwork.OfflineMode && player.TryGetComponent(out playerPV))
                {
                    playerPV.RPC("ApplyBulletBoost", RpcTarget.All, new object[] { effectDuration, 2, 26.0f, fireControl.bulletSettings.pierceLimit, 0, 5.0f });
                }
                else
                {
                    player.GetComponent<BulletBoost>().ApplyBulletBoost(effectDuration, 2, 26.0f, fireControl.bulletSettings.pierceLimit, 0, 5.0f);
                }

                yield return new WaitForSeconds(4); // Wait for particles to finish playing
                break;
            case BoostType.Grenades:
                if(!PhotonNetwork.OfflineMode && player.TryGetComponent(out playerPV))
                {
                    playerPV.RPC("ApplyBulletBoost", RpcTarget.All, new object[] { effectDuration, 3, 14.0f, 0, 0, 5.0f });
                }
                else
                {
                    player.GetComponent<BulletBoost>().ApplyBulletBoost(effectDuration, 3, 14.0f, 0, 0, 5.0f);
                }

                yield return new WaitForSeconds(4); // Wait for particles to finish playing
                break;
            case BoostType.Invisibility:
                if(!PhotonNetwork.OfflineMode && player.TryGetComponent(out playerPV))
                {
                    playerPV.RPC("SetInvisible", RpcTarget.All, new object[] { effectDuration });
                }
                else
                {
                    player.GetComponent<Invisibility>().SetInvisible(effectDuration);
                }
                break;
            case BoostType.Shields:
                if(!PhotonNetwork.OfflineMode && player.TryGetComponent(out playerPV))
                {
                    playerPV.RPC("AddShields", RpcTarget.All, new object[] {(int)value });
                }
                else
                {
                    player.GetComponent<Shields>().AddShields((int)value);
                }

                yield return new WaitForSeconds(4); // Wait for particles to finish playing
                break;
        }

        switch(mode)
        {
            case Mode.Static:
                if(useLimit < 0 || uses < useLimit)
                {
                    uses++;
                    yield return new WaitForSeconds(respawnDelay);
                    Respawn();
                    if(!PhotonNetwork.OfflineMode)
                    {
                        photonView.RPC("Respawn", RpcTarget.Others, null);
                    }
                }
                else
                {
                    if(PhotonNetwork.OfflineMode)
                    {
                        Destroy(gameObject);
                    }
                    else
                    {
                        PhotonNetwork.Destroy(gameObject);
                    }
                }
                break;
            case Mode.Dynamic:
                BoostGenerator.Instance.SpawnNewBoost();
                if(PhotonNetwork.OfflineMode)
                {
                    Destroy(gameObject);
                }
                else
                {
                    PhotonNetwork.Destroy(gameObject);
                }
                break;
        }
        activated = false;
    }
}