using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DelayedDestroy : MonoBehaviour
{
    public float delay = 5;

    // Start is called before the first frame Update
    void Start()
    {
        // Start timer to destroy this gameObject
        StartCoroutine(KillTimer());
    }

    IEnumerator KillTimer()
    {
        yield return new WaitForSeconds(delay);
        if(transform.CompareTag("Bullet"))
        {
            if(GetComponent<BulletBehaviour>().owner != null)
            {
                GetComponent<BulletBehaviour>().owner.GetComponent<FireControl>().firedBullets.Remove(transform);
            }
        }

        Destroy(gameObject);
    }
}
