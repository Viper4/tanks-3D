using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleRegion : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tank") && other.name == "Body")
        {
            if (other.transform.root.name == "Tanks")
            {
                other.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
            else if (other.transform.root.name == "Player")
            {
                other.transform.root.Find("Tank Origin").GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Tank") && other.name == "Body")
        {
            if (other.transform.root.name == "Tanks")
            {
                other.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            }
            else if (other.transform.root.name == "Player")
            {
                other.transform.root.Find("Tank Origin").GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
    }
}
