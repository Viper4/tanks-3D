using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructableObject : MonoBehaviour
{
    public ParticleSystem particles;
    public int destroyResistance = 1;
    [SerializeField] float respawnDelay = 0;
    [SerializeField] GameObject solidObject;
    [SerializeField] float[] pitchRange = { 0.9f, 1.1f };
    AudioSource audioSource;

    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public void DestroyObject()
    {
        solidObject.SetActive(false);
        particles.Play();
        if (audioSource != null)
        {
            audioSource.pitch = Random.Range(pitchRange[0], pitchRange[1]);
            audioSource.Play();
        }

        if (respawnDelay > 0)
        {
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);
        solidObject.SetActive(true);
    }
}
