using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructableObject : MonoBehaviour
{
    Collider objectCollider;
    public ParticleSystem particles;
    public int destroyResistance = 1;
    [SerializeField] LayerMask overlapLayerMask;
    [SerializeField] float respawnDelay = 0;
    [SerializeField] GameObject solidObject;
    [SerializeField] float[] pitchRange = { 0.9f, 1.1f };
    AudioSource audioSource;

    bool respawning = false;

    private void Start()
    {
        objectCollider = transform.GetChild(0).GetComponent<Collider>();
        audioSource = GetComponent<AudioSource>();
        if (TryGetComponent<Collider>(out var rootCollider))
        {
            Destroy(rootCollider);
        }
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

        if (!respawning && respawnDelay > 0)
        {
            StartCoroutine(Respawn());
        }
    }

    IEnumerator Respawn()
    {
        respawning = true;
        yield return new WaitForSeconds(respawnDelay);
        while (Physics.CheckBox(objectCollider.bounds.center, objectCollider.bounds.extents * 0.49f, transform.rotation, overlapLayerMask))
        {
            yield return new WaitForSeconds(1f);
        }
        solidObject.SetActive(true);
        respawning = false;
    }
}