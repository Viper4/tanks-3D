using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSoundManager : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    [HideInInspector] public AudioSource audioSource;

    [SerializeField] float scale;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.Pause();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale > 0)
        {
            audioSource.UnPause();

            float targetVolume = rb.velocity.magnitude * scale;
            audioSource.volume = targetVolume;
        }
        else
        {
            audioSource.Pause();
        }
    }
}
