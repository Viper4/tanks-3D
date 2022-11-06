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

    [SerializeField] Material normalMaterial;
    [SerializeField] Material flashMaterial;
    Renderer thisRenderer;

    public float activateDelay = 1;
    public float timer = 30;
    public Explosive explosive;

    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip tickOne;
    [SerializeField] AudioClip tickTwo;

    bool canFlash = true;

    private void Start()
    {
        thisRenderer = GetComponent<Renderer>();
    }

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
                    explosive.Explode(new List<Transform>());
                    DestroyMine();
                }
                // At less than 5 seconds, mine starts to flash
                else if (timer < 5)
                {
                    if (canFlash)
                    {
                        canFlash = false;
                        StartCoroutine(FlashLoop());
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
                explosive.Explode(new List<Transform>());
                DestroyMine();
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

    IEnumerator FlashLoop()
    {
        audioSource.pitch += 0.01f;
        // Alternating between normal and flash materials
        canFlash = false;
        thisRenderer.material = flashMaterial;
        audioSource.clip = tickOne;
        audioSource.Play();

        yield return new WaitForSeconds(timer * 0.1f);

        thisRenderer.material = normalMaterial;
        audioSource.clip = tickTwo;
        audioSource.Play();

        yield return new WaitForSeconds(timer * 0.1f);
        StartCoroutine(FlashLoop());
    }

    public void DestroyMine()
    {
        if (owner != null)
        {
            owner.GetComponent<MineControl>().laidMines.Remove(transform);
        }

        Destroy(gameObject);
    }
}