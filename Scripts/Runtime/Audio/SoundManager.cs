using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] float[] startDelay = {0, 0.2f};
    [SerializeField] float[] pitchRange = {0.8f, 1.2f};

    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        audioSource.pitch = Random.Range(pitchRange[0], pitchRange[1]);

        audioSource.PlayDelayed(Random.Range(startDelay[0], startDelay[1]));
    }
}
