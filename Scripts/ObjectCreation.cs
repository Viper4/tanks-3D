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
    
    public void RandomGeneration(GameObject boundingBoxRef, Vector3[] bounds, int tankLimit, List<Transform> tanks, int obstacleLimit, List<Transform> obstacles, float branchChance, bool distribute)
    {    
        // Generating bounding box between two points
        // UnityEngine Transform, GameObject, and Vector3 documentation: https://docs.unity3d.com/ScriptReference/Transform.html, https://docs.unity3d.com/ScriptReference/GameObject.html, https://docs.unity3d.com/ScriptReference/Vector3.html
        // Instantiate method from UnityEngine library: https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
        
        // Generating stretched cube given two corner points: https://answers.unity.com/questions/52747/how-i-can-create-a-cube-with-specific-coordenates.html
        GameObject boundingBox = Instantiate(boundingBoxRef);
        Vector3 between = bounds[1] - bounds[0];
        float distance = between.magnitude;
        boundingBox.localScale.x = distance;
        boundingBox.position = bounds[0] + (between / 2.0);
        boundingBox.LookAt(bounds[1]);
        
        // UnityEngine Collider documentation: https://docs.unity3d.com/ScriptReference/Collider.html
        Collider boundsCollider = boundingBox.GetComponent<Collider>();

        // Checking if origin is within bounds
        // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
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
                // If there are still elements in the obstacle list select a random one otherwise log error and stop the method
                if(obstacles.Length != 0)
                {
                    targetObstacle = obstacles[Random.Range(0, obstacles.Length)];
                }
                else
                {
                    // Debug class from UnityEngine library: https://docs.unity3d.com/ScriptReference/Debug.html
                    Debug.LogError("No obstacles to instantiate!");
                }
            }
            // Generating level
            else 
            {
                // Getting list of valid directions to instantiate
                Vector3 direction;
                List<Vector3> validDirections = { transform.forward, transform.right, -transform.forward, -transform.right, transform.up, -transform.up };
                foreach(Vector3 direction in validDirections)
                {
                    // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
                    if(!boundsCollider.bounds.Contains(transform.position + direction * distanceAway))
                    {
                        validDirections.Remove(direction);
                    }
                }
                
                // Picking random direction from valid directions to instantiate
                if(validDirections.Length != 0)
                {
                    direction = validDirections[Random.Range(0, validDirections.Length)]
                    if (distribute)
                    {
                        int subtractA = Mathf.Floor(obstacleLimit / obstacles.Length);
                        for(int i = 0; i < obstacles.Length; i++)
                        {
                            GenerateObject(obstacle[i], boundsCollider, direction, distanceAway, , branchChance);
                        }
                    }
                    else
                    {
                        int times = obstacleLimit;
                        foreach(Transform obstacle in obstacles)
                        {
                            times -= Random.Range(0, times);
                            GenerateObject(obstacle, boundsCollider, direction, distanceAway, times, branchChance);
                        }
                    }
                }
                else
                {
                    Debug.log("No valid spawn position from " + transform.name);
                }
            }
        }
        else
        {
            Debug.Log(transform.name + " out of bounds, choosing random origin");
            // Pick random origin
        }
    }
    
    public void GenerateObject(GameObject prefab, Collider boundingCollider, Vector3 direction, float dst, int times, float branchChance)
    {
        GameObject clone = null;

        for (int i = 0; i < times; i++)
        {
            Vector3 clonePosition = transform.position + direction * ((i+1) * dst));
            // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
            if(boundsCollider.bounds.Contains(clonePosition)
            {
                // Instantiate method from UnityEngine library: https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
                clone = Instantiate(gameObject, clonePosition, transform.rotation, transform.parent);
                if (Random.value <= branchChance)
                {
                    clone.GetComponent<ObjectCreation>().GenerateObject(prefab, boundingCollider, dst, i);
                    break;
                }
            }
        }
        else
        {
            Debug.log("No valid spawn position from " + transform.name);
        }
    }
}
