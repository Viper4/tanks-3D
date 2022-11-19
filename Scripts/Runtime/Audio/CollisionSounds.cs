using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CollisionSounds : MonoBehaviour
{
    [SerializeField] Transform audioSourceTransform;
    [SerializeField] float strengthThreshold = 2;
    [SerializeField] float[] pitchRange = {0.8f, 1.2f};
    [SerializeField] float volumeMultiplier = 0.1f;
    AudioSource audioSource;

    // Start is called before the first frame update
    void Start()
    {
        audioSource = audioSourceTransform.GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if(!collision.transform.CompareTag("Bullet"))
        {
            ContactPoint contact = collision.GetContact(0);
            float collisionStrength = Vector3.Dot(contact.normal, collision.relativeVelocity);
            if(collision.rigidbody != null)
            {
                collisionStrength *= collision.rigidbody.mass;
            }
            if(collisionStrength >= strengthThreshold)
            {
                audioSourceTransform.position = contact.point;
                audioSource.pitch = Random.Range(pitchRange[0], pitchRange[1]);
                audioSource.volume = collisionStrength * volumeMultiplier;
                audioSource.Play();
            }
        }
    }
}
