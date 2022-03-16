using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DelayedDestroy : MonoBehaviour
{
    public float delay = 5;

    // Start is called before the first frame update
    void Awake()
    {
        // Start timer to destroy this gameObject
        StartCoroutine(KillTimer());
    }

    IEnumerator KillTimer()
    {
        yield return new WaitForSeconds(delay);
        if (transform.CompareTag("Bullet"))
        {
            GetComponent<BulletBehaviour>().owner.GetComponent<FireControl>().bulletsFired--;
        }
        Destroy(gameObject);
    }
}
