using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSoundManager : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    AudioSource audioSource;

    [SerializeField] float scale;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();

        audioSource.Pause();
    }

    // Update is called once per frame
    void Update()
    {
        if (!SceneLoader.frozen && Time.timeScale > 0)
        {
            audioSource.UnPause();

            float targetVolume = rb.velocity.magnitude * scale * (SaveSystem.currentSettings.masterVolume / 100);
            audioSource.volume = targetVolume;
        }
        else
        {
            audioSource.Pause();
        }
    }
}
