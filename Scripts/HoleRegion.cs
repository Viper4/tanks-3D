using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoleRegion : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Tank") && other.name == "Body")
        {
            Debug.Log(other.name);
            if(other.transform.root.name == "Enemies")
            {
                Debug.Log("Enter");
                other.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
            else if(other.transform.root.name == "Player")
            {
                Debug.Log("Enter");
                other.transform.root.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotation;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Tank") && other.name == "Body")
        {
            Debug.Log(other.name);
            if (other.transform.root.name == "Enemies")
            {
                Debug.Log("Exit");

                other.transform.parent.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            }
            else if (other.transform.root.name == "Player")
            {
                Debug.Log("Exit");

                other.transform.root.GetComponent<Rigidbody>().constraints = RigidbodyConstraints.FreezeRotation;
            }
        }
    }
}
