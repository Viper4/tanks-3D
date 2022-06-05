using Photon.Pun;
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

    [SerializeField] bool multiplayer = false;
    PhotonView view;

    // Start is called before the first frame Update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (multiplayer)
        {
            view = GetComponent<PhotonView>();

            view.RPC("ResetVelocity", RpcTarget.All);
        }
        else
        {
            ResetVelocity();
        }
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
                        KillTarget(other.transform.parent);
                    }
                    break;
                case "Player":
                    KillTarget(other.transform.root);
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
                case "Player":
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
                    if (multiplayer)
                    {
                        PhotonNetwork.Destroy(other.gameObject);
                    }
                    else
                    {
                        Destroy(other.gameObject);
                    }
                    DestroySelf();
                    break;
                default:
                    BounceOff(other);
                    break;
            }
        }
    }

    void IncreaseKills()
    {
        if (owner != null && owner.CompareTag("Player"))
        {
            Debug.Log("Increased kills");
            owner.GetComponent<DataSystem>().currentPlayerData.kills++;
        }
    }

    void BounceOff(Collision hit)
    {
        if (bounces < ricochetLevel)
        {
            bounces++;
            Vector3 reflection = Vector3.Reflect(transform.forward, hit.contacts[0].normal);

            if (multiplayer)
            {
                if (view != null && view.IsMine)
                {
                    PhotonNetwork.Instantiate(sparkEffect.name, transform.position, Quaternion.identity);

                    view.RPC("Reflect", RpcTarget.All, new object[] { reflection });
                    view.RPC("ResetVelocity", RpcTarget.All);
                }
            }
            else
            {
                Instantiate(sparkEffect, transform.position, Quaternion.identity);

                Reflect(reflection);
                ResetVelocity();
            }
        }
        else
        {
            DestroySelf();
        }
    }

    [PunRPC]
    void Reflect(Vector3 reflection)
    {
        transform.forward = reflection;
    }

    [PunRPC]
    void ResetVelocity()
    {
        rb.velocity = transform.forward * speed;
    }

    void KillTarget(Transform target)
    {
        if (transform.name != "Rocket Bullet" && target != null)
        {
            if (target != owner)
            {
                IncreaseKills();
            }
            if (multiplayer)
            {
                if (view.IsMine)
                {
                    Debug.Log("Started here");
                    target.GetComponent<PhotonView>().RPC("ExplodeTank", RpcTarget.All);
                }
            }
            else
            {
                BaseTankLogic baseTankLogic = target.GetComponent<BaseTankLogic>();
                if (baseTankLogic != null)
                {
                    baseTankLogic.ExplodeTank();
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

        if (multiplayer)
        {
            PhotonNetwork.Destroy(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void DestroySelf()
    {

        // Keeping track of how many bullets a tank has fired
        if (owner != null)
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
                            collider.transform.parent.GetComponent<BaseTankLogic>().ExplodeTank();
                            IncreaseKills();
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
                        collider.GetComponent<MineBehaviour>().ExplodeMine(new List<Transform>());
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

        if (multiplayer)
        {
            if (GetComponent<PhotonView>().IsMine)
            {
                PhotonNetwork.Instantiate(explosionEffect.name, transform.position, Quaternion.identity);
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            Instantiate(explosionEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}