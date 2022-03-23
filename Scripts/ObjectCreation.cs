using System.Collections.Generic;
using System.Linq;
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

    // UnityEngine Transform, GameObject, and Vector3 documentation: https://docs.unity3d.com/ScriptReference/Transform.html, https://docs.unity3d.com/ScriptReference/GameObject.html, https://docs.unity3d.com/ScriptReference/Vector3.html
    public void RandomGeneration(GameObject boundingBox, int tankLimit, List<GameObject> tankList, int obstacleLimit, List<GameObject> obstacleList, float branchChance, bool distribute)
    {
        // Cloning lists to not affect original lists
        List<GameObject> tanks = tankList.ToList();
        List<GameObject> obstacles = obstacleList.ToList();

        // UnityEngine Collider documentation: https://docs.unity3d.com/ScriptReference/Collider.html
        BoxCollider boundsCollider = boundingBox.GetComponent<BoxCollider>();

        // Checking if origin is within bounds
        // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
        if (boundsCollider.bounds.Contains(transform.position))
        {
            GameObject targetObstacle = null;
            // Selecting a obstacle to instantiate in game
            foreach(GameObject obstacle in obstacles.ToList())
            {
                // If the obstacle in list matches the selected obstacle then use it first for random generation
                if(obstacle.name == transform.name)
                {
                    targetObstacle = obstacle;
                }
            }
            // If targetObstacle has not been selected
            if(targetObstacle == null)
            {
                // If there are still elements in the obstacle list select a random one otherwise log error and stop the method
                if(obstacles.Count != 0)
                {
                    targetObstacle = obstacles[Random.Range(0, obstacles.Count)];
                }
                else
                {
                    // Debug class from UnityEngine library: https://docs.unity3d.com/ScriptReference/Debug.html
                    Debug.Log("No obstacles found to instantiate");
                    return;
                }
            }
            obstacles.Remove(targetObstacle);

            // Generating level
            // Getting list of valid directions to instantiate
            Vector3 direction;
            List<Vector3> validDirections = new List<Vector3>{ transform.forward, transform.right, -transform.forward, -transform.right, transform.up, -transform.up };
            foreach(Vector3 dir in validDirections.ToList())
            {
                // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
                if(!boundsCollider.bounds.Contains(transform.position + dir * distanceAway))
                {
                    validDirections.Remove(dir);
                }
            }
                
            // Picking random direction from valid directions to instantiate
            if(validDirections.Count != 0)
            {
                direction = validDirections[Random.Range(0, validDirections.Count)];
                // Generating first targetted obstacle
                GenerateObject(targetObstacle, boundsCollider, direction, distanceAway, Mathf.FloorToInt(obstacleLimit / obstacles.Count), branchChance);

                if (distribute)
                {
                    foreach(GameObject obstacle in obstacles)
                    {
                        GenerateObject(obstacle, boundsCollider, direction, distanceAway, Mathf.FloorToInt(obstacleLimit / obstacles.Count), branchChance);
                    }
                }
                else
                {
                    int times = obstacleLimit;
                    foreach(GameObject obstacle in obstacles)
                    {
                        times -= Random.Range(0, times);
                        GenerateObject(obstacle, boundsCollider, direction, distanceAway, times, branchChance);
                    }
                }
            }
            else
            {
                Debug.Log("No valid spawn position from " + transform.name);
            }
            
        }
        else
        {
            Debug.Log(transform.name + " out of bounds, choosing random origin");
            // Pick random origin
        }
    }
    
    public void GenerateObject(GameObject prefab, Collider boundsCollider, Vector3 direction, float dst, int times, float branchChance)
    {
        GameObject clone = null;

        for (int i = 0; i < times; i++)
        {
            Vector3 clonePosition = transform.position + direction * ((i+1) * dst);
            // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
            if(boundsCollider.bounds.Contains(clonePosition))
            {
                // Instantiate method from UnityEngine library: https://docs.unity3d.com/ScriptReference/Object.Instantiate.html
                clone = Instantiate(prefab, clonePosition, transform.rotation, transform.parent);
                if (Random.value <= branchChance)
                {
                    Vector3 newDir = direction;

                    List<Vector3> validDirections = new List<Vector3> { clone.transform.forward, clone.transform.right, -clone.transform.forward, -clone.transform.right, clone.transform.up, -clone.transform.up };
                    validDirections.Remove(direction);
                    foreach (Vector3 dir in validDirections.ToList())
                    {
                        // Bounds.Contains method from UnityEngine lib: https://docs.unity3d.com/ScriptReference/Bounds.Contains.html
                        if (!boundsCollider.bounds.Contains(transform.position + dir * distanceAway))
                        {
                            validDirections.Remove(dir);
                        }
                    }
                    if(validDirections.Count != 0)
                    {
                        newDir = validDirections[Random.Range(0, validDirections.Count)];
                    }
                    clone.GetComponent<ObjectCreation>().GenerateObject(prefab, boundsCollider, newDir, dst, times - i, branchChance);

                    return;
                }
            }
            else if (clone != null)
            {
                Debug.Log("No valid spawn position from " + clone.name);
                return;
            }
        }
    }
}
