using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] float[] startDelay = {0, 0.2f};

    AudioSource audioSource;
    float originalVolume = 1;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;
        UpdateVolume(FindObjectOfType<DataSystem>().currentSettings.masterVolume);

        ClientManager[] allClients = FindObjectsOfType<ClientManager>();
        foreach(ClientManager ClientManager in allClients)
        {
            ClientManager.UpdateSoundManagerOnClient(this);
        }

        audioSource.PlayDelayed(Random.Range(startDelay[0], startDelay[1]));
    }

    public void UpdateVolume(float masterVolume)
    {
        audioSource.volume = originalVolume * masterVolume / 100;
    }
}
