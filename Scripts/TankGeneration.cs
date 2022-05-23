using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TankGeneration : MonoBehaviour
{
    List<GameObject> clonedObjects = new List<GameObject>();

    [SerializeField] LayerMask ignoreLayerMask;

    [SerializeField] Transform tankParent;
    [SerializeField] int loopTimeout;
    [SerializeField] bool randomYRotation = true;

    public void Clear()
    {
        Debug.Log(transform.name + " cleared " + clonedObjects.Count + " tanks.");

        // If there are cloned objects
        if (clonedObjects.Count != 0)
        {
            // Iterate through each clonedObject and delete them
            foreach (GameObject clonedObject in clonedObjects)
            {
                DestroyImmediate(clonedObject);
            }

            // Clear the list referencing the clonedObjects
            clonedObjects.Clear();
        }
    }

    public void RandomTankGeneration(List<GameObject> tanks, Dictionary<string, int> cloneAmounts, Collider boundingCollider)
    {
        foreach(GameObject tank in tanks)
        {
            for (int i = 0; i < cloneAmounts[tank.name]; i++)
            {
                for (int j = 0; j < loopTimeout; j++)
                {
                    if (Physics.Raycast(RandomExtensions.RandomPointInCollider(boundingCollider), Vector3.down, out RaycastHit hit, Mathf.Infinity, ~ignoreLayerMask))
                    {
                        Quaternion rotation = tank.transform.rotation;
                        if (randomYRotation)
                        {
                            rotation = Quaternion.AngleAxis(Random.Range(-180.0f, 180.0f), Vector3.up);
                        }

                        if (!Physics.CheckBox(hit.point + Vector3.up * (tank.transform.Find("Body").localPosition.y + 0.1f), tank.transform.Find("Body").GetComponent<BoxCollider>().size / 2, rotation))
                        {
                            clonedObjects.Add(Instantiate(tank, hit.point, rotation, tankParent));
                            break;
                        }
                    }
                }
            }
        }
    }
}
