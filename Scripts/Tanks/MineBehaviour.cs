using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class MineBehaviour : MonoBehaviour
{
    public DataManager dataSystem { get; set; }

    public Transform owner { get; set; }
    public PhotonView ownerPV { get; set; }

    public Transform explosionEffect;

    public Material normalMaterial;
    public Material flashMaterial;

    public float activateDelay = 1;
    public float timer = 30;
    public float explosionForce = 12f;
    public float explosionRadius = 4.5f;

    bool canFlash = true;

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen)
        {
            activateDelay -= Time.deltaTime * 1;

            if (activateDelay <= 0)
            {
                timer -= Time.deltaTime * 1;

                // Explodes at 0 seconds
                if (timer <= 0)
                {
                    ExplodeMine(new List<Transform>());
                }
                // At less than 5 seconds, mine starts to flash
                else if (timer < 5)
                {
                    if (canFlash)
                    {
                        StartCoroutine(Flash(timer));
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        switch (other.tag)
        {
            case "Tank":
                if (activateDelay <= 0 && timer > 1.5f) 
                {
                    if (timer > 2)
                    {
                        timer = 2;
                    }
                }
                break;
            case "Player":
                if (activateDelay <= 0 && timer > 1.5f)
                {
                    if (timer > 2)
                    {
                        timer = 2;
                    }
                }
                break;
            case "Bullet":
                // Exploding if bullet hits the mine
                if (Vector3.Distance(transform.position, other.transform.position) <= GetComponent<SphereCollider>().radius)
                {
                    ExplodeMine(new List<Transform>());
                }
                break;
        }
    }

    void IncreaseKills()
    {
        if (owner != null && owner.CompareTag("Player"))
        {
            if (!PhotonNetwork.OfflineMode)
            {
                if (ownerPV.IsMine)
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

    IEnumerator Flash(float timeLeft)
    {
        // Alternating between normal and flash materials
        canFlash = false;
        GetComponent<Renderer>().material = flashMaterial;

        yield return new WaitForSeconds(timeLeft * 0.1f);

        GetComponent<Renderer>().material = normalMaterial;

        yield return new WaitForSeconds(timeLeft * 0.1f);
        canFlash = true;
    }

    public void ExplodeMine(List<Transform> chain)
    {
        chain.Add(transform);

        // Getting all colliders within explosionRadius
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
                    // Explode other mines not in mine chain
                    if (!chain.Contains(collider.transform))
                    {
                        collider.GetComponent<MineBehaviour>().ExplodeMine(chain);
                    }
                    break;
            }

            Rigidbody rb = collider.GetComponent<Rigidbody>();
            // Applying explosion force to rigid bodies of hit colliders
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 3);
            }
        }

        DestroyMine();
    }

    void DestroyMine()
    {
        if (owner != null)
        {
            owner.GetComponent<MineControl>().minesLaid -= 1;
        }

        Instantiate(explosionEffect, transform.position, Quaternion.Euler(-90, 0, 0));
        Destroy(gameObject);
    }
}