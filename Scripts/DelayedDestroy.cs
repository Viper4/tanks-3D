using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class DelayedDestroy : MonoBehaviour
{
    public float delay = 5;
    [SerializeField] bool multiplayer = false;

    // Start is called before the first frame Update
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
            Debug.Log(GetComponent<BulletBehaviour>().owner.GetComponent<FireControl>().bulletsFired);
        }

        if (multiplayer)
        {
            if (GetComponent<PhotonView>().IsMine)
            {
                PhotonNetwork.Destroy(gameObject);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
