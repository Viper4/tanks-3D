using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BreakParticleSystem : MonoBehaviour
{
    public Transform particles;
    public Material material;

    public void PlayParticles()
    {
        Transform newParticles = Instantiate(particles, transform.position, Quaternion.Euler(-90, 0, 0));
        newParticles.GetComponent<Renderer>().material = material;
    }
}
