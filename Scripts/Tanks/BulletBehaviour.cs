using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class BulletBehaviour : MonoBehaviourPunCallbacks
{
    public int bulletID;

    Rigidbody rb;

    public Transform owner { get; set; }
    public PhotonView ownerPV { get; set; }
    [SerializeField] Transform explosionEffect;
    [SerializeField] Transform sparkEffect;

    List<Transform> collidedTransforms = new List<Transform>();

    public float speed { get; set; } = 32f;

    public int pierceLevel { get; set; } = 0;
    public int pierceLimit { get; set; } = 0;
    int pierces = 0;

    public int ricochetLevel { get; set; } = 1;
    int bounces = 0;

    bool removedSelf = false;

    // Start is called before the first frame Update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
        ResetVelocity();
    }

    void Update()
    {
        if (GameManager.frozen)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void OnEnable()
    {
        base.OnEnable();
        if (!GameManager.autoPlay)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }
    }

    private void OnDisable()
    {
        base.OnDisable();
        if (!GameManager.autoPlay)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }
    }

    void OnEvent(EventData eventData)
    {
        if (eventData.Code == GameManager.Instance.SetIDCode)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            Debug.Log("Try set ID");
            if (ownerPV != null && ownerPV.ViewID == (int)parameters["ViewID"])
            {
                Debug.Log("Set ID: " + (int)parameters["ID"]);
                bulletID = (int)parameters["ID"];
            }
        }
        else if (eventData.Code == GameManager.Instance.DestroyCode)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            Debug.Log("Try destroy");
            if ((int)parameters["ID"] == bulletID)
            {
                Debug.Log("Destroyed: " + (int)parameters["ID"]);
                SubtractBulletsFired();
                if (!(bool)parameters["Safe"])
                {
                    Instantiate(explosionEffect, transform.position, Quaternion.identity);
                }
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManager.frozen)
        {
            switch (other.tag)
            {
                case "Tank":
                    if (!collidedTransforms.Contains(other.transform.parent))
                    {
                        KillTarget(other.transform.parent);
                        collidedTransforms.Add(other.transform.parent);
                    }
                    break;
                case "Player":
                    Transform otherPlayer = other.transform.parent.parent;
                    if (!collidedTransforms.Contains(otherPlayer))
                    {
                        KillTarget(otherPlayer);
                        collidedTransforms.Add(otherPlayer);
                    }
                    break;
                case "AI Tank":
                    if (owner != null && !owner.CompareTag("AI Tank") && !collidedTransforms.Contains(other.transform.parent))
                    {
                        KillTarget(other.transform.parent);
                        collidedTransforms.Add(other.transform.parent);
                    }
                    break;
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!GameManager.frozen)
        {
            switch (other.transform.tag)
            {
                case "Tank":
                    if (!collidedTransforms.Contains(other.transform))
                    {
                        KillTarget(other.transform);
                        collidedTransforms.Add(other.transform);
                    }
                    break;
                case "Player":
                    Transform otherPlayer = other.transform.parent;
                    if (!collidedTransforms.Contains(otherPlayer))
                    {
                        KillTarget(otherPlayer);
                        collidedTransforms.Add(otherPlayer);
                    }
                    break;
                case "AI Tank":
                    if (!owner.CompareTag("AI Tank") && !collidedTransforms.Contains(other.transform))
                    {
                        KillTarget(other.transform);
                        collidedTransforms.Add(other.transform);
                    }
                    break;
                case "Destructable":
                    // If can pierce, destroy the hit object, otherwise bounce off
                    if (other.transform.parent.TryGetComponent<DestructableObject>(out var destructableObject))
                    {
                        if (pierceLevel >= destructableObject.destroyResistance)
                        {
                            destructableObject.DestroyObject();
                            if (pierces < pierceLimit)
                            {
                                // Resetting velocity
                                rb.velocity = transform.forward * speed;
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
                    // Destroy bullet
                    Destroy(other.gameObject);
                    
                    NormalDestroy();
                    break;
                case "Mine":
                    other.transform.parent.GetComponent<MineBehaviour>().ExplodeMine(new List<Transform>());

                    SafeDestroy();
                    break;
                default:
                    BounceOff(other);
                    break;
            }
        }
    }

    void MultiplayerAddKills()
    {
        DataManager.playerData.kills++;

        PhotonHashtable playerProperties = new PhotonHashtable
        {
            { "Kills", DataManager.playerData.kills }
        };
        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
    }
    
    void IncreaseKills(Transform other)
    {
        if (owner != null && owner != other)
        {
            if (owner.CompareTag("Player"))
            {
                if (!PhotonNetwork.OfflineMode)
                {
                    if (ownerPV != null && ownerPV.IsMine)
                    {
                        if (other.CompareTag("Tank"))
                        {
                            MultiplayerAddKills();
                        }
                        else if (other.CompareTag("Player"))
                        {
                            if (other.name.Contains("Team"))
                            {
                                if (other.name != owner.name)
                                {
                                    MultiplayerAddKills();
                                }
                            }
                            else
                            {
                                MultiplayerAddKills();
                            }
                        }
                    }
                }
                else
                {
                    DataManager.playerData.kills++;
                }
            }
            else if (owner.CompareTag("AI Tank"))
            {
                GeneticAlgorithmBot bot = owner.GetComponent<GeneticAlgorithmBot>();
                bot.Kills++;
            }
        }
    }

    void BounceOff(Collision collision)
    {
        if (bounces < ricochetLevel)
        {
            bounces++;
            Vector3 reflection = Vector3.Reflect(transform.forward, collision.GetContact(0).normal);

            Reflect(reflection);
            ResetVelocity();
        }
        else
        {
            NormalDestroy();
        }
    }

    void Reflect(Vector3 reflection)
    {
        Instantiate(sparkEffect, transform.position, Quaternion.identity);

        transform.forward = reflection;
    }

    public void ResetVelocity()
    {
        if (rb != null)
        {
            rb.velocity = transform.forward * speed;
        }
    }

    void KillTarget(Transform target)
    {
        if (transform.name != "Rocket Bullet" && target != null)
        {
            if (!PhotonNetwork.OfflineMode && !GameManager.autoPlay)
            {
                if (ownerPV != null && ownerPV.IsMine)
                {
                    target.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                }
            }
            else
            {
                if (target.TryGetComponent<BaseTankLogic>(out var baseTankLogic))
                {
                    baseTankLogic.ExplodeTank();
                }
            }
            IncreaseKills(target);
        }

        NormalDestroy();
    }

    void SubtractBulletsFired()
    {
        // Keeping track of how many bullets a tank has fired
        if (!removedSelf && owner != null)
        {
            owner.GetComponent<FireControl>().firedBullets.Remove(transform);
            removedSelf = true;
        }
    }

    void NormalDestroy()
    {
        SubtractBulletsFired();

        PhotonHashtable parameters = new PhotonHashtable
        {
            { "ID", bulletID },
            { "Safe", false },
        };
        PhotonNetwork.RaiseEvent(GameManager.Instance.DestroyCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void SafeDestroy()
    {
        SubtractBulletsFired();

        PhotonHashtable parameters = new PhotonHashtable
        {
            { "ID", bulletID },
            { "Safe", true },
        };
        PhotonNetwork.RaiseEvent(GameManager.Instance.DestroyCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        Destroy(gameObject);
    }
}