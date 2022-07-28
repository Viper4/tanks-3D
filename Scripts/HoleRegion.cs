using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleRegion : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.name == "Body")
        {
            if (other.CompareTag("Tank") || other.CompareTag("Player"))
            {
                other.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name == "Body")
        {
            if (other.CompareTag("Tank") || other.CompareTag("Player"))
            {
                other.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
    }
}
