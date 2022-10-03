using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class MineBehaviour : MonoBehaviourPunCallbacks
{
    readonly byte StartTimerCode = 9;
    public int mineID = 0;

    public Transform owner { get; set; }
    public PhotonView ownerPV { get; set; }

    [SerializeField] LayerMask overlapInteract;
    public Transform explosionEffect;

    public Material normalMaterial;
    public Material flashMaterial;

    public float activateDelay = 1;
    public float timer = 30;
    public float explosionForce = 12f;
    public float explosionRadius = 7f;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip tickOne;
    [SerializeField] AudioClip tickTwo;

    bool canFlash = true;

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale > 0 && !GameManager.Instance.frozen)
        {
            activateDelay -= Time.deltaTime;

            if (activateDelay <= 0)
            {
                timer -= Time.deltaTime;

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

    public override void OnEnable()
    {
        base.OnEnable();
        if (!GameManager.Instance.inLobby)
        {
            PhotonNetwork.NetworkingClient.EventReceived += OnEvent;
        }
    }

    public override void OnDisable()
    {
        base.OnDisable();
        if (!GameManager.Instance.inLobby)
        {
            PhotonNetwork.NetworkingClient.EventReceived -= OnEvent;
        }
    }

    void OnEvent(EventData eventData)
    {
        if (eventData.Code == GameManager.Instance.DestroyCode)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if ((int)parameters["ID"] == mineID)
            {
                ExplodeMine(new List<Transform>());
            }
        }
        else if (eventData.Code == StartTimerCode)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if ((int)parameters["ID"] == mineID)
            {
                timer = 2;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if ((ownerPV == null || ownerPV.IsMine) && (other.CompareTag("Tank") || other.CompareTag("Player") || other.CompareTag("AI Tank")))
        {
            if (activateDelay <= 0 && timer > 1.5f)
            {
                if (timer > 2)
                {
                    PhotonHashtable parameters = new PhotonHashtable()
                    {
                        { "ID", mineID }
                    };
                    PhotonNetwork.RaiseEvent(StartTimerCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
                    timer = 2;
                }
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

    IEnumerator Flash(float timeLeft)
    {
        audioSource.pitch += 0.01f;
        // Alternating between normal and flash materials
        canFlash = false;
        GetComponent<Renderer>().material = flashMaterial;
        audioSource.clip = tickOne;
        audioSource.Play();

        yield return new WaitForSeconds(timeLeft * 0.1f);

        GetComponent<Renderer>().material = normalMaterial;
        audioSource.clip = tickTwo;
        audioSource.Play();

        yield return new WaitForSeconds(timeLeft * 0.1f);
        canFlash = true;
    }

    public void ExplodeMine(List<Transform> chain)
    {
        if (!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby && ownerPV.IsMine)
        {
            PhotonHashtable parameters = new PhotonHashtable
            {
                { "ID", mineID },
            };
            PhotonNetwork.RaiseEvent(GameManager.Instance.DestroyCode, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
        chain.Add(transform);

        // Getting all colliders within explosionRadius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius, overlapInteract);
        foreach (Collider collider in colliders)
        {
            if (collider != null && !chain.Contains(collider.transform))
            {
                switch (collider.tag)
                {
                    case "Tank":
                        chain.Add(collider.transform);

                        if (!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
                        {
                            if (ownerPV.IsMine)
                            {
                                collider.transform.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                            }
                        }
                        else
                        {
                            collider.transform.GetComponent<BaseTankLogic>().ExplodeTank();
                        }
                        IncreaseKills(collider.transform);
                        
                        break;
                    case "Player":
                        Transform otherPlayer = collider.transform.parent;
                        if (!chain.Contains(otherPlayer))
                        {
                            chain.Add(otherPlayer);
                            if (!PhotonNetwork.OfflineMode)
                            {
                                if (ownerPV.IsMine)
                                {
                                    otherPlayer.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                                }
                            }
                            else
                            {
                                otherPlayer.GetComponent<BaseTankLogic>().ExplodeTank();
                            }
                            IncreaseKills(otherPlayer);
                        }
                        break;
                    case "AI Tank":
                        chain.Add(collider.transform);

                        if (!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
                        {
                            if (ownerPV.IsMine)
                            {
                                collider.transform.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                            }
                        }
                        else
                        {
                            collider.transform.GetComponent<BaseTankLogic>().ExplodeTank();
                        }
                        IncreaseKills(collider.transform);
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
                        if (!chain.Contains(collider.transform.parent))
                        {
                            collider.transform.parent.GetComponent<MineBehaviour>().ExplodeMine(chain);
                        }
                        break;
                }
            }

            // Applying explosion force to rigid bodies of hit colliders
            if (collider.TryGetComponent<Rigidbody>(out var rb))
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
            owner.GetComponent<MineControl>().laidMines.Remove(transform);
        }

        Instantiate(explosionEffect, transform.position, Quaternion.Euler(-90, 0, 0), transform.parent);
        Destroy(gameObject);
    }
}