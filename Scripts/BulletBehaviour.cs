using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBehaviour : MonoBehaviour
{
    Rigidbody rb;

    public Transform owner { get; set; }

    [SerializeField] Transform explosionEffect;
    [SerializeField] Transform sparkEffect;

    public float speed = 32;
    public float explosionRadius = 0;

    public int pierceLevel = 0;
    int pierces = 0;

    public int ricochetLevel = 1;
    int bounces = 0;

    // Start is called before the first frame Update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        rb.velocity = transform.forward * speed;
    }

    void Update()
    {
        if (SceneLoader.frozen)
        {
            rb.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!SceneLoader.frozen)
        {
            switch (other.tag)
            {
                case "Tank":
                    if (other.transform.parent.name != "Tanks")
                    {
                        if (other.transform.root.name != "Player")
                        {
                            KillTarget(other.transform.parent);
                        }
                        else
                        {
                            KillTarget(other.transform.root);
                        }
                    }
                    break;
            }
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        if (!SceneLoader.frozen)
        {
            switch (other.transform.tag)
            {
                case "Tank":
                    KillTarget(other.transform);
                    break;
                case "Penetrable":
                    // If can pierce, destroy the hit object, otherwise bounce off
                    if (pierceLevel != 0)
                    {
                        if (pierces < pierceLevel)
                        {
                            pierces++;
                            // Resetting velocity
                            rb.velocity = transform.forward * speed;
                        }
                        else
                        {
                            DestroySelf();
                        }
                        // Playing destroy particles for hit object and destroying it
                        other.transform.GetComponent<BreakParticleSystem>().PlayParticles();
                        Destroy(other.gameObject);
                    }
                    else
                    {
                        BounceOff(other);
                    }
                    break;
                case "Kill Boundary":
                    // Kill self
                    DestroySelf();
                    break;
                case "Bullet":
                    // Destroy bullet
                    Destroy(other.gameObject);
                    DestroySelf();
                    break;
                default:
                    BounceOff(other);
                    break;
            }
        }
    }

    void BounceOff(Collision hit)
    {
        if (bounces < ricochetLevel)
        {
            Instantiate(sparkEffect, transform.position, Quaternion.identity);
            // Reflecting bullet across perpendicular vector of contact point
            bounces++;
            Vector3 reflection = Vector3.Reflect(transform.forward, hit.contacts[0].normal);
            transform.forward = reflection;
            // Resetting velocity
            rb.velocity = transform.forward * speed;
        }
        else
        {
            DestroySelf();
        }
    }

    void KillTarget(Transform target)
    {
        if (transform.name != "Rocket Bullet" && target != null && target.CompareTag("Tank"))
        {
            BaseTankLogic baseTankLogic = target.GetComponent<BaseTankLogic>();
            if (baseTankLogic != null)
            {
                baseTankLogic.Explode();
                if (owner != null && owner.name == "Player")
                {
                    owner.GetComponent<PlayerControl>().kills++;
                }
            }
        }

        DestroySelf();
    }

    public void SafeDestroy()
    {
        // Keeping track of how many bullets a tank has fired
        if (owner != null)
        {
            owner.GetComponent<FireControl>().bulletsFired -= 1;
        }

        Destroy(gameObject);
    }

    void DestroySelf()
    {
        // Keeping track of how many bullets a tank has fired
        if(owner != null)
        {
            owner.GetComponent<FireControl>().bulletsFired -= 1;
        }

        // Rockets have same explosion system as mines
        if (transform.name == "Rocket Bullet")
        {
            // Getting all colliders within explosionRadius
            Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
            // Iterating through every collider in explosionRadius
            foreach (Collider collider in colliders)
            {
                // Checking tags of colliders
                switch (collider.tag)
                {
                    case "Tank":
                        // Blowing up tanks
                        if (collider.transform.parent != null)
                        {
                            collider.transform.parent.GetComponent<BaseTankLogic>().Explode();
                            if (owner != null && owner.name == "Player")
                            {
                                owner.GetComponent<PlayerControl>().kills++;
                            }
                        }
                        break;
                    case "Penetrable":
                        // Playing destroy particles for hit object and destroying it
                        collider.transform.GetComponent<BreakParticleSystem>().PlayParticles();
                        Destroy(collider.gameObject);
                        break;
                    case "Bullet":
                        // Destroying bullets in explosion
                        collider.GetComponent<BulletBehaviour>().SafeDestroy();
                        break;
                    case "Mine":
                        // Explode mines
                        collider.GetComponent<MineBehaviour>().Explode(new List<Transform>());
                        break;
                }

                Rigidbody rb = collider.GetComponent<Rigidbody>();
                // Applying explosion force if collider has rigid body
                if (rb != null)
                {
                    rb.AddExplosionForce(8, transform.position, explosionRadius, 3);
                }
            }
        }

        Instantiate(explosionEffect, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
