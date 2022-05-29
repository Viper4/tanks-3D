using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] float[] startDelay = {0, 0.2f};

    AudioSource audioSource;

    // Start is called before the first frame update
    void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.volume *= SaveSystem.currentSettings.masterVolume / 100;

        audioSource.PlayDelayed(Random.Range(startDelay[0], startDelay[1]));
    }
}
