using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineBehaviour : MonoBehaviour
{
    public Transform owner { get; set; }

    public Transform explosionEffect;

    public Material normalMaterial;
    public Material flashMaterial;

    public float activateDelay = 1;
    public float timer = 30;
    public float explosionForce = 8f;
    public float explosionRadius = 4.5f;

    bool canFlash = true;

    // Update is called once per frame
    void Update()
    {
        if (!SceneLoader.frozen)
        {
            activateDelay -= Time.deltaTime * 1;

            if (activateDelay <= 0)
            {
                timer -= Time.deltaTime * 1;

                // Explodes at 0 seconds
                if (timer <= 0)
                {
                    Explode(new List<Transform>());
                }
                // At less than 5 seconds, mine starts to flash
                else if (timer < 5)
                {
                    if (canFlash)
                    {
                        StartCoroutine(Flash(timer));
                    }
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (activateDelay <= 0 && timer > 1.5f)
        {
            switch (other.tag)
            {
                case "Tank":
                    if (timer > 2)
                    {
                        timer = 2;
                    }
                    break;
                case "Bullet":
                    // Exploding if bullet hits the mine
                    if (Vector3.Distance(transform.position, other.transform.position) <= GetComponent<SphereCollider>().radius)
                    {
                        Explode(new List<Transform>());
                    }
                    break;
            }
        }
    }

    void IncreaseKills()
    {
        if (owner != null && owner.name == "Player")
        {
            SaveSystem.currentPlayerData.kills++;
            Debug.Log("Added kills");
        }
    }

    IEnumerator Flash(float timeLeft)
    {
        // Alternating between normal and flash materials
        canFlash = false;
        GetComponent<Renderer>().material = flashMaterial;

        yield return new WaitForSeconds(timeLeft * 0.1f);

        GetComponent<Renderer>().material = normalMaterial;

        yield return new WaitForSeconds(timeLeft * 0.1f);
        canFlash = true;
    }

    public void Explode(List<Transform> chain)
    {
        chain.Add(transform);

        // Getting all colliders within explosionRadius
        Collider[] colliders = Physics.OverlapSphere(transform.position, explosionRadius);
        List<Transform> explodedTanks = new List<Transform>();
        foreach (Collider collider in colliders)
        {
            switch (collider.tag)
            {
                case "Tank":
                    if (collider != null && collider.transform.parent.name != "Tanks" && !explodedTanks.Contains(collider.transform.parent))
                    {
                        explodedTanks.Add(collider.transform.parent);

                        // Blowing up tanks
                        if (collider.transform.root.name != "Player")
                        {
                            collider.transform.parent.GetComponent<BaseTankLogic>().Explode();
                            IncreaseKills();
                        }
                        else
                        {
                            collider.transform.root.GetComponent<BaseTankLogic>().Explode();
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
                    // Explode other mines not in mine chain
                    if(!chain.Contains(collider.transform))
                    {
                        collider.GetComponent<MineBehaviour>().Explode(chain);
                    }
                    break;
            }

            Rigidbody rb = collider.GetComponent<Rigidbody>();
            // Applying explosion force to rigid bodies of hit colliders
            if (rb != null)
            {
                rb.AddExplosionForce(explosionForce, transform.position, explosionRadius, 3);
            }
        }
        Instantiate(explosionEffect, transform.position, Quaternion.Euler(-90, 0, 0));
        DestroySelf();
    }

    void DestroySelf()
    {
        if(owner != null)
        {
            owner.GetComponent<MineControl>().minesLaid -= 1;
        }
        Destroy(gameObject);
    }
}
