using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class BulletBehaviour : MonoBehaviourPunCallbacks
{
    [System.Serializable]
    public struct BulletSettings
    {
        public int bulletIndex;
        public float speed;
        public int pierceLevel;
        public int pierceLimit;
        public int ricochetLevel;
        public float explosionRadius;
    }

    public int bulletID = 0;

    Rigidbody rb;
    [SerializeField] Explosive explosive;
    [SerializeField] float timer = 3;
    [SerializeField] float flashTime = 0.5f;
    Renderer thisRenderer;
    [SerializeField] Material[] flashMaterials;
    Material[] savedMaterials;
    [SerializeField] AudioSource audioSource;

    public Transform owner { get; set; }
    public PhotonView ownerPV { get; set; }
    [SerializeField] Transform explosionEffect;
    [SerializeField] Transform sparkEffect;

    List<Transform> collidedTransforms = new List<Transform>();
    [SerializeField] List<string> explodeTags = new List<string>() { "Tank", "Player", "AI Tank", "Bullet", "Mine" };

    public BulletSettings settings = new BulletSettings()
    {
        bulletIndex = 0,
        speed = 16,
        pierceLevel = 0,
        pierceLimit = 0,
        ricochetLevel = 1,
    };

    int pierces = 0;
    int bounces = 0;

    bool removedSelf = false;

    // Start is called before the first frame Update
    IEnumerator Start()
    {
        thisRenderer = GetComponent<Renderer>();
        rb = GetComponent<Rigidbody>();
        ResetVelocity();
        savedMaterials = thisRenderer.materials;

        if(settings.bulletIndex == 3)
        {
            yield return new WaitForSeconds(0.5f);
            StartCoroutine(FlashLoop());
        }
    }

    void Update()
    {
        if(GameManager.Instance.frozen)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
        else if(settings.bulletIndex == 3 && Time.timeScale > 0)
        {
            timer -= Time.deltaTime;
            // Explodes at 0 seconds
            if(timer <= 0)
            {
                explosive.Explode(new List<Transform>());
                SilentDestroy();
            }
        }
    }

    public override void OnEnable()
    {
        base.OnEnable();
        if(!GameManager.Instance.inLobby)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if(!GameManager.Instance.inLobby)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }
    }

    void OnEvent(EventData eventData)
    {
        if(eventData.Code == GameManager.Instance.DestroyCode)
        {
            PhotonHashtable parameters =(PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if((int)parameters["ID"] == bulletID)
            {
                SubtractBulletsFired();
                if(!(bool)parameters["Safe"])
                {
                    Instantiate(explosionEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }

    IEnumerator FlashLoop()
    {
        Material[] newMaterials = new Material[thisRenderer.materials.Length];
        for(int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = flashMaterials[i];
        }
        thisRenderer.materials = newMaterials;

        yield return new WaitForSeconds(flashTime);
        for(int i = 0; i < newMaterials.Length; i++)
        {
            newMaterials[i] = savedMaterials[i];
        }
        thisRenderer.materials = newMaterials;

        yield return new WaitForSeconds(flashTime);

        StartCoroutine(FlashLoop());
    }

    private void OnTriggerEnter(Collider other)
    {
        if(!GameManager.Instance.frozen && !removedSelf)
        {
            if(explosive != null)
            {
                if(explodeTags.Contains(other.transform.tag))
                {
                    explosive.Explode(new List<Transform>());
                    SilentDestroy();
                }
            }
            else
            {
                switch(other.tag)
                {
                    case "Tank":
                        if(!collidedTransforms.Contains(other.transform.parent))
                        {
                            KillTarget(other.transform.parent);
                            collidedTransforms.Add(other.transform.parent);
                        }
                        break;
                    case "Player":
                        Transform otherPlayer = other.transform.parent.parent;
                        if(!collidedTransforms.Contains(otherPlayer))
                        {
                            collidedTransforms.Add(otherPlayer);

                            if(!otherPlayer.TryGetComponent<Shields>(out var shields))
                            {
                                KillTarget(otherPlayer);
                                break;
                            }

                            int damageAmount = settings.pierceLimit - pierces + 1;
                            if(damageAmount > shields.shieldAmount)
                            {
                                KillTarget(otherPlayer);
                            }
                            else
                            {
                                if(otherPlayer.TryGetComponent<PhotonView>(out var otherPV))
                                {
                                    if(otherPV.IsMine)
                                    {
                                        otherPV.RPC("DamageShields", RpcTarget.All, new object[] { damageAmount });
                                    }
                                }
                                else
                                {
                                    shields.DamageShields(damageAmount);
                                }
                                NormalDestroy();
                            }
                        }
                        break;
                    case "AI Tank":
                        if(owner != null && !owner.CompareTag("AI Tank") && !collidedTransforms.Contains(other.transform.parent))
                        {
                            KillTarget(other.transform.parent);
                            collidedTransforms.Add(other.transform.parent);
                        }
                        break;
                }
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if(!GameManager.Instance.frozen && !removedSelf)
        {
            if(explosive != null)
            {
                if(explodeTags.Contains(other.transform.tag))
                {
                    explosive.Explode(new List<Transform>());
                    SilentDestroy();
                }
                else
                {
                    if(settings.bulletIndex == 3)
                    {
                        ContactPoint contact = other.GetContact(0);
                        float collisionStrength = Vector3.Dot(contact.normal, other.relativeVelocity);
                        audioSource.volume = collisionStrength * 0.1f;
                        audioSource.Play();
                    }
                    else
                    {
                        BounceOff(other);
                    }
                }
            }
            else
            {
                switch(other.transform.tag)
                {
                    case "Tank":
                        if(!collidedTransforms.Contains(other.transform))
                        {
                            KillTarget(other.transform);
                            collidedTransforms.Add(other.transform);
                        }
                        break;
                    case "Player":
                        Transform otherPlayer = other.transform.parent;
                        if(!collidedTransforms.Contains(otherPlayer))
                        {
                            collidedTransforms.Add(otherPlayer);

                            if(!otherPlayer.TryGetComponent<Shields>(out var shields))
                            {
                                KillTarget(otherPlayer);
                                break;
                            }

                            int damageAmount = settings.pierceLimit - pierces + 1;
                            if(damageAmount > shields.shieldAmount)
                            {
                                KillTarget(otherPlayer);
                            }
                            else
                            {
                                if(otherPlayer.TryGetComponent<PhotonView>(out var otherPV))
                                {
                                    if(otherPV.IsMine)
                                    {
                                        otherPV.RPC("DamageShields", RpcTarget.All, new object[] { damageAmount });
                                    }
                                }
                                else
                                {
                                    shields.DamageShields(damageAmount);
                                }
                                NormalDestroy();
                            }
                        }
                        break;
                    case "AI Tank":
                        if(!owner.CompareTag("AI Tank") && !collidedTransforms.Contains(other.transform))
                        {
                            KillTarget(other.transform);
                            collidedTransforms.Add(other.transform);
                        }
                        break;
                    case "Destructable":
                        // If can pierce, destroy the hit object, otherwise bounce off
                        if(other.transform.parent.TryGetComponent<DestructableObject>(out var destructableObject))
                        {
                            if(settings.pierceLevel >= destructableObject.destroyResistance)
                            {
                                destructableObject.DestroyObject();
                                if(pierces < settings.pierceLimit)
                                {
                                    // Resetting velocity
                                    rb.velocity = transform.forward * settings.speed;
                                    pierces++;
                                    break;
                                }
                                else
                                {
                                    NormalDestroy();
                                    break;
                                }
                            }
                        }

                        BounceOff(other);
                        break;
                    case "Kill Boundary":
                        // Kill self
                        NormalDestroy();
                        break;
                    case "Bullet":
                        NormalDestroy();

                        //Destroy(other.gameObject); // Other bullet also gets triggered so use destroy instead of normal or safe destroy to prevent excessive calls
                        break;
                    case "Mine":
                        other.transform.parent.GetComponent<Explosive>().Explode(new List<Transform>());
                        other.transform.parent.GetComponent<MineBehaviour>().DestroyMine();

                        SilentDestroy();
                        break;
                    default:
                        BounceOff(other);
                        break;
                }
            }
        }
    }
    
    void BounceOff(Collision collision)
    {
        if(bounces < settings.ricochetLevel)
        {
            bounces++;
            Vector3 reflection = Vector3.Reflect(transform.forward, collision.GetContact(0).normal);

            Reflect(reflection);
            ResetVelocity();
        }
        else
        {
            if(explosive != null)
            {
                explosive.Explode(new List<Transform>());
                SilentDestroy();
            }
            else
            {
                NormalDestroy();
            }
        }
    }

    void Reflect(Vector3 reflection)
    {
        Instantiate(sparkEffect, transform.position, Quaternion.identity);

        transform.forward = reflection;
    }

    public void ResetVelocity()
    {
        if(rb != null)
        {
            rb.velocity = transform.forward * settings.speed;
        }
    }

    void KillTarget(Transform target)
    {
        if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
        {
            if(ownerPV != null && ownerPV.IsMine)
            {
                target.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                NormalDestroy();
            }
        }
        else
        {
            if(target.TryGetComponent<BaseTankLogic>(out var baseTankLogic))
            {
                baseTankLogic.ExplodeTank();
            }
            NormalDestroy();
        }

        if(owner != null && owner != target)
        {
            if(owner.CompareTag("Player"))
            {
                if(PhotonNetwork.OfflineMode)
                {
                    DataManager.playerData.kills++;
                }
                else
                {
                    if(target.CompareTag("Tank"))
                    {
                        DataManager.playerData.kills++;
                    }
                    else if(target.CompareTag("Player"))
                    {
                        if(target.name.Contains("Team"))
                        {
                            if(target.name != owner.name)
                            {
                                DataManager.playerData.kills++;
                            }
                        }
                        else
                        {
                            DataManager.playerData.kills++;
                        }
                    }
                    PhotonHashtable playerProperties = new PhotonHashtable
                    {
                        { "Kills", DataManager.playerData.kills }
                    };
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                }
            }
            else if(owner.CompareTag("AI Tank"))
            {
                GeneticAlgorithmBot bot = owner.GetComponent<GeneticAlgorithmBot>();
                bot.Kills++;
            }
        }
    }

    void SubtractBulletsFired()
    {
        // Keeping track of how many bullets a tank has fired
        if(!removedSelf && owner != null)
        {
            owner.GetComponent<FireControl>().firedBullets.Remove(transform);
            removedSelf = true;
        }
    }

    void NormalDestroy()
    {
        SubtractBulletsFired();

        if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby && ownerPV.IsMine)
        {
            PhotonHashtable parameters = new PhotonHashtable
            {
                { "ID", bulletID },
                { "Safe", false },
            };
            PhotonNetwork.RaiseEvent(GameManager.Instance.DestroyCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void SilentDestroy()
    {
        SubtractBulletsFired();

        if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby && ownerPV.IsMine)
        {
            PhotonHashtable parameters = new PhotonHashtable
            {
                { "ID", bulletID },
                { "Safe", true },
            };
            PhotonNetwork.RaiseEvent(GameManager.Instance.DestroyCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
        Destroy(gameObject);
    }
}