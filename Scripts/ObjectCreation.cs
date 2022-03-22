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
    public Vector3 scale = new Vector3(2, 2, 2);

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
    
    public void RandomGeneration(GameObject boundingBoxRef, Vector3[] bounds, int tankLimit, List<Transform> tanks, int obstacleLimit, List<Transform> obstacles, float branchChance)
    {    
        // Generating bounding box between two points
        GameObject boundingBox = Instantiate(boundingBoxRef);
        Vector3 between = bounds[1] - bounds[0];
        float distance = between.magnitude;
        boundingBox.localScale.x = distance;
        boundingBox.position = bounds[0] + (between / 2.0);
        boundingBox.LookAt(bounds[1]);
        
        Collider boundsCollider = boundingBox.GetComponent<Collider>();

        // Checking if origin is within bounds
        if (boundsCollider.bounds.Contains(transform.position))
        {
            Transform targetObstacle = null;
            // Selecting a obstacle to instantiate in game
            foreach(Transform obstacle in obstacles)
            {
                // If the obstacle in list matches the selected obstacle then use it first for random generation
                if(obstacle.name = transform.name)
                {
                    targetObstacle = obstacle;
                }
            }
            // If targetObstacle has not been selected
            if(targetObstacle == null)
            {
                // If there is elements in the obstacle list select a random obstacle otherwise log error and stop the method
                if(obstacles.Length != 0)
                {
                    targetObstacle = obstacles[Random.Range(0, obstacles.Length)];
                }
                else
                {
                    Debug.LogError("No obstacles to instantiate!");
                }
            }
            // Generating level
            else 
            {
                Vector3[] validDirections;
                Vector3 dir = transform.forward;
                for(int i = 0; i < 5; i++)
                {
                    if(i < 4)
                    {
                        dir = Quaternion.AngleAxis(90, Vector3.up) * dir;
                    }
                    else if (i == 4) 
                    {
                        dir = transform.up;
                    }
                    else if (i == 5)
                    {
                        dir = -transform.up;
                    }
                    else 
                    {
                        Debug.Log("No directions from " + transform.name + " are valid");
                        break;
                    }
                    if(boundsCollider.bounds.Contains(transform.position + dir * distanceAway))
                    {
                        validDirections.Add(dir);
                    }
                }
            }
        }
        else
        {
            Debug.Log(transform.name + " out of bounds, choosing random origin");
            
        }
    }
}
