using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class BulletBehaviour : MonoBehaviour
{
    Rigidbody rb;

    public DataManager dataSystem { get; set; }

    public Transform owner { get; set; }
    public PhotonView ownerPV { get; set; }
    [SerializeField] Transform explosionEffect;
    [SerializeField] Transform sparkEffect;

    public float speed { get; set; } = 32f;
    public float explosionRadius { get; set; } = 0;

    public int pierceLevel { get; set; } = 0;
    int pierces = 0;

    public int ricochetLevel { get; set; } = 1;
    int bounces = 0;

    // Start is called before the first frame Update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (GameManager.frozen)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!GameManager.frozen)
        {
            switch (other.tag)
            {
                case "Tank":
                    if (other.transform.parent.name != "Tanks")
                    {
                        KillTarget(other.transform.parent, true);
                    }
                    break;
                case "Player":
                    Transform otherPlayer = other.transform.parent.parent;
                    KillTarget(otherPlayer, !(owner.name.Contains("Team") && owner.name == otherPlayer.name));
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
                    KillTarget(other.transform, true);
                    break;
                case "Destructable":
                    // If can pierce, destroy the hit object, otherwise bounce off
                    if (pierceLevel > 0)
                    {
                        if (pierces < pierceLevel)
                        {
                            pierces++;
                            // Resetting velocity
                            rb.velocity = transform.forward * speed;
                        }
                        else
                        {
                            NormalDestroy();
                        }
                        // Playing destroy particles for hit object and hiding it
                        other.transform.parent.GetComponent<DestructableObject>().DestroyObject();
                    }
                    else
                    {
                        BounceOff(other);
                    }
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
                default:
                    BounceOff(other);
                    break;
            }
        }
    }
    
    void IncreaseKills()
    {
        if (owner != null && owner.CompareTag("Player"))
        {
            if (!PhotonNetwork.OfflineMode)
            {
                if (ownerPV != null && ownerPV.IsMine)
                {
                    dataSystem.currentPlayerData.kills++;

                    PhotonHashtable playerProperties = PhotonNetwork.LocalPlayer.CustomProperties;
                    playerProperties["Kills"] = dataSystem.currentPlayerData.kills;
                    PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
                }
            }
            else
            {
                dataSystem.currentPlayerData.kills++;
            }
        }
    }

    void BounceOff(Collision hit)
    {
        if (bounces < ricochetLevel)
        {
            bounces++;
            Vector3 reflection = Vector3.Reflect(transform.forward, hit.GetContact(0).normal);

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
        rb.velocity = transform.forward * speed;
    }

    void KillTarget(Transform target, bool increaseKills)
    {
        if (transform.name != "Rocket Bullet" && target != null)
        {
            if (!PhotonNetwork.OfflineMode)
            {
                if (ownerPV != null && ownerPV.IsMine)
                {
                    target.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                    if (target != owner && increaseKills)
                    {
                        IncreaseKills();
                    }
                }
            }
            else
            {
                if (target != owner && increaseKills)
                {
                    IncreaseKills();
                }
                BaseTankLogic baseTankLogic = target.GetComponent<BaseTankLogic>();
                if (baseTankLogic != null)
                {
                    baseTankLogic.ExplodeTank();
                }
            }
        }

        NormalDestroy();
    }

    void NormalDestroy()
    {
        // Keeping track of how many bullets a tank has fired
        if (owner != null)
        {
            owner.GetComponent<FireControl>().bulletsFired -= 1;
        }

        // Same explosion system as mines
        if (explosionRadius > 0)
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            List<Transform> explodedTanks = new List<Transform>();
            foreach (Collider collider in colliders)
            {
                switch (collider.tag)
                {
                    case "Tank":
                        if (collider != null && collider.transform.parent.name != "Tanks" && !explodedTanks.Contains(collider.transform.parent))
                        {
                            explodedTanks.Add(collider.transform.parent);

                            if (!PhotonNetwork.OfflineMode)
                            {
                                if (ownerPV.IsMine)
                                {
                                    collider.transform.parent.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                                    IncreaseKills();
                                }
                            }
                            else
                            {
                                collider.transform.parent.GetComponent<BaseTankLogic>().ExplodeTank();
                                IncreaseKills();
                            }
                        }
                        break;
                    case "Player":
                        if (collider != null && !explodedTanks.Contains(collider.transform.parent))
                        {
                            explodedTanks.Add(collider.transform.parent);
                            if (!PhotonNetwork.OfflineMode)
                            {
                                if (ownerPV.IsMine)
                                {
                                    collider.transform.parent.parent.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                                    if (owner != collider.transform.parent.parent && !(owner.name.Contains("Team") && owner.name == collider.transform.parent.parent.name))
                                    {
                                        IncreaseKills();
                                    }
                                }
                            }
                            else
                            {
                                collider.transform.parent.parent.GetComponent<BaseTankLogic>().ExplodeTank();
                            }
                        }
                        break;
                    case "Destructable":
                        collider.transform.parent.GetComponent<DestructableObject>().DestroyObject();
                        break;
                    case "Bullet":
                        // Destroying bullets in explosion
                        collider.GetComponent<BulletBehaviour>().SafeDestroy();
                        break;
                    case "Mine":
                        collider.GetComponent<MineBehaviour>().ExplodeMine(new List<Transform>());
                        break;
                }

                Rigidbody rb = collider.GetComponent<Rigidbody>();
                // Applying explosion force to rigid bodies of hit colliders
                if (rb != null)
                {
                    rb.AddExplosionForce(8, transform.position, explosionRadius, 3);
                }
            }
        }

        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    public void SafeDestroy()
    {
        // Keeping track of how many bullets a tank has fired
        if (owner != null)
        {
            owner.GetComponent<FireControl>().bulletsFired -= 1;
        }

        Destroy(gameObject);
    }
}