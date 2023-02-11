using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class Explosive : MonoBehaviourPun
{
    public Transform initiator { get; set; } // tank/player that exploded this
    public Transform owner { get; set; }
    public PhotonView ownerPV { get; set; }

    public Transform explosionEffect;
    [SerializeField] LayerMask overlapInteract;
    public float explosionForce = 12f;
    public float explosionRadius = 7f;
    [SerializeField] float upwardsModifier = 2.5f;

    void KillTank(Transform tank)
    {
        if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
        {
            if(ownerPV.IsMine)
            {
                tank.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
            }
        }
        else
        {
            tank.GetComponent<BaseTankLogic>().ExplodeTank();
        }

        if(initiator != null && initiator != tank)
        {
            if(initiator.CompareTag("Player"))
            {
                if (PhotonNetwork.OfflineMode)
                {
                    DataManager.playerData.kills++;
                }
                else
                {
                    if(tank.CompareTag("Tank"))
                    {
                        DataManager.playerData.kills++;
                    }
                    else if(tank.CompareTag("Player"))
                    {
                        if(tank.name.Contains("Team"))
                        {
                            if(tank.name != initiator.name)
                            {
                                DataManager.playerData.kills++;
                            }
                        }
                        else
                        {
                            DataManager.playerData.kills++;
                        }
                    }
                }
            }
            else if(initiator.CompareTag("AI Tank"))
            {
                GeneticAlgorithmBot bot = owner.GetComponent<GeneticAlgorithmBot>();
                bot.Kills++;
            }
        }
    }

    public void Explode(List<Transform> chain)
    {
        Instantiate(explosionEffect, transform.position, Quaternion.Euler(-90, 0, 0), transform.parent);

        chain.Add(transform);

        // Getting all colliders within explosionRadius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, overlapInteract);
        foreach(Collider collider in colliders)
        {
            if(collider != null && !chain.Contains(collider.transform))
            {
                switch(collider.tag)
                {
                    case "Tank":
                        chain.Add(collider.transform);

                        KillTank(collider.transform);
                        break;
                    case "Player":
                        if(PhotonNetwork.OfflineMode || initiator.GetComponent<PhotonView>().IsMine)
                        {
                            Transform otherPlayer = collider.transform.parent;

                            if(!chain.Contains(otherPlayer))
                            {
                                if(!otherPlayer.TryGetComponent<Shields>(out var shields))
                                {
                                    KillTank(otherPlayer);
                                    chain.Add(otherPlayer);
                                    break;
                                }

                                if(shields.shieldAmount < 3)
                                {
                                    KillTank(otherPlayer);
                                    chain.Add(otherPlayer);
                                }
                                else
                                {
                                    if(otherPlayer.TryGetComponent<PhotonView>(out var otherPV))
                                    {
                                        otherPV.RPC("DamageShields", RpcTarget.All, new object[] { 3 });
                                    }
                                    else
                                    {
                                        shields.DamageShields(3);
                                    }
                                }
                            }
                        }
                        break;
                    case "AI Tank":
                        chain.Add(collider.transform);

                        KillTank(collider.transform);
                        break;
                    case "Destructable":
                        collider.transform.parent.GetComponent<DestructableObject>().DestroyObject();
                        break;
                    case "Bullet":
                        // Destroying bullets in explosion
                        if(collider.TryGetComponent<Explosive>(out var explosive) && !chain.Contains(collider.transform))
                        {
                            explosive.Explode(chain);
                        }

                        collider.GetComponent<BulletBehaviour>().SilentDestroy();
                        break;
                    case "Mine":
                        // Explode other mines not in mine chain
                        if(!chain.Contains(collider.transform.parent))
                        {
                            Explosive otherExplosive = collider.transform.parent.GetComponent<Explosive>();
                            otherExplosive.initiator = initiator;
                            otherExplosive.Explode(chain);
                            collider.transform.parent.GetComponent<MineBehaviour>().DestroyMine();
                        }
                        break;
                }
            }

            // Applying explosion force to rigid bodies of hit colliders
            if(collider.TryGetComponent<Rigidbody>(out var rb))
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, upwardsModifier);
            }
        }

        if(!PhotonNetwork.OfflineMode && ownerPV != null && ownerPV.IsMine)
        {
            PhotonHashtable playerProperties = new PhotonHashtable
            {
                { "kills", DataManager.playerData.kills }
            };
            PhotonNetwork.LocalPlayer.SetCustomProperties(playerProperties);
        }
    }
}
