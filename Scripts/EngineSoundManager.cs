using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineSoundManager : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    AudioSource audioSource;
    float masterVolume = 100;

    [SerializeField] float scale;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();

        UpdateMasterVolume(FindObjectOfType<DataSystem>().currentSettings.masterVolume);

        audioSource.Pause();
    }

    // Update is called once per frame
    void Update()
    {
        if (!SceneLoader.frozen && Time.timeScale > 0)
        {
            audioSource.UnPause();

            float targetVolume = rb.velocity.magnitude * scale * (masterVolume / 100);
            audioSource.volume = targetVolume;
        }
        else
        {
            audioSource.Pause();
        }
    }

    public void UpdateMasterVolume(float targetVolume)
    {
        masterVolume = targetVolume;
    }
}
