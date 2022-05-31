using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] float[] startDelay = {0, 0.2f};

    AudioSource audioSource;
    float originalVolume = 1;

    // Start is called before the first frame update
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        originalVolume = audioSource.volume;
        UpdateVolume();

        audioSource.PlayDelayed(Random.Range(startDelay[0], startDelay[1]));
    }

    public void UpdateVolume()
    {
        audioSource.volume = originalVolume * FindObjectOfType<DataSystem>().currentSettings.masterVolume / 100;
    }
}
