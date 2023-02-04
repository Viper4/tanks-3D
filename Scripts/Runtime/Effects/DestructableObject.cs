using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using PhotonHashtable = ExitGames.Client.Photon.Hashtable;

public class DestructableObject : MonoBehaviourPunCallbacks
{
    public int destructableID = 0;

    Collider objectCollider;
    public ParticleSystem particles;
    public int destroyResistance = 1;
    [SerializeField] LayerMask overlapLayerMask;
    [SerializeField] float respawnDelay = 0;
    public GameObject solidObject;
    [SerializeField] float[] pitchRange = { 0.9f, 1.1f };
    AudioSource audioSource;

    bool respawning = false;

    private void Start()
    {
        objectCollider = transform.GetChild(0).GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if(TryGetComponent<Collider>(out var rootCollider))
        {
            Destroy(rootCollider);
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
        if (eventData.Code == EventCodes.Destroy)
        {
            PhotonHashtable parameters = (PhotonHashtable)eventData.Parameters[ParameterCode.Data];
            if ((int)parameters["ID"] == destructableID)
            {
                DestroyObject();
            }
        }
    }

    public void DestroyObject(bool raiseEvent = false)
    {
        if (raiseEvent)
        {
            PhotonHashtable parameters = new PhotonHashtable()
            {
                { "ID", destructableID }
            };
            PhotonNetwork.RaiseEvent(EventCodes.Destroy, parameters, RaiseEventOptions.Default, SendOptions.SendUnreliable);
        }
        solidObject.SetActive(false);
        particles.Play();
        if(audioSource != null)
        {
            audioSource.pitch = Random.Range(pitchRange[0], pitchRange[1]);
            audioSource.Play();
        }

        if(!respawning && respawnDelay > 0)
        {
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);
        while(Physics.CheckBox(objectCollider.bounds.center, objectCollider.bounds.extents, transform.rotation, overlapLayerMask))
        {
            yield return new WaitForSeconds(1f);
        }
        solidObject.SetActive(true);
        respawning = false;
    }
}
