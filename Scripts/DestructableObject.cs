using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestructableObject : MonoBehaviour
{
    public Transform particles;
    public Material material;
    [SerializeField] float respawnDelay = 0;
    [SerializeField] GameObject solidObject;

    public void PlayParticles()
    {
        Transform newParticles = Instantiate(particles, transform.position, Quaternion.Euler(-90, 0, 0));
        newParticles.GetComponent<Renderer>().material = material;
    }
    
    public void DestroyObject()
    {
        solidObject.SetActive(false);
        PlayParticles();

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