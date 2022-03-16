using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectCreation : MonoBehaviour
{
    List<Transform> extendedObjects = new List<Transform>();

    public Vector3 direction;
    public float distanceAway;
    public int times = 1;
    public Vector3 eulerAngles;
    public Vector3 scale = new Vector3(1, 1, 1);

    public void Extend()
    {
        for(int i = 0; i < times; i++)
        {
            Transform clone = Instantiate(transform, transform.position + direction * (distanceAway * (i + 1)), Quaternion.Euler(eulerAngles), transform.parent);
            clone.name = transform.name;
            clone.localScale = scale;
            extendedObjects.Add(clone);
        }
    }
    
    public void Delete()
    {
        DestroyImmediate(gameObject);
    }

    public void Undo()
    {
        if(extendedObjects.Count != 0)
        {
            if(extendedObjects[extendedObjects.Count - 1] != null)
            {
                DestroyImmediate(extendedObjects[extendedObjects.Count - 1].gameObject);
            }

            extendedObjects.RemoveAt(extendedObjects.Count - 1);
        }
    }
}
