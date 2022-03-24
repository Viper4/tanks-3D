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
    
    public void GenerateRandomObstacle(Collider boundingBox, int obstacleLimit, List<GameObject> obstacleList, Vector3 inputDir, float branchChance, bool distribute, int times)
    {
        if(obstacleList.Count != 0)
        {
            List<GameObject> obstacles = obstacleList.ToList();
            int[] cloneAmounts;
            Vector3 lastPosition = transform.position;
            Vector3 targetDirection = inputDir;
            
            foreach (GameObject obstacle in obstacles)
            {
                // Determining cloneAmounts for each obstacle
                if(distribute)
                {
                    // distributing obstacleLimit as evenly as possible among each obstacle
                    cloneAmount = Mathf.FloorToInt(obstacleLimit / obstacleList.Count);
                }
                else
                {
                    // Choosing random amount to clone from 0 to cloneAmount - 1
                    cloneAmount -= Random.Range(0, cloneAmount - 1);
                }
                // Cloning the obstacle
                for (int i = 0; i < cloneAmount; i++) 
                {
                    if (Random.value <= branchChance)
                    {
                        // Testing directions to the right, left, and back of inputDir
                        List<Vector3> validDirections = new List<Vector3>();
                        validDirections = TestValidDirections(lastPosition, {Quaternion.AngleAxis(90, Vector3.up) * inputDir, Quaternion.AngleAxis(-90, Vector3.up) * inputDir, Quaternion.AngleAxis(180, Vector3.up) * inputDir});

                        targetDirection = validDirections[Random.Range(0, validDirections.Count)];
                    }
                    GameObject newClone = Instantiate(obstacle, lastPosition + targetDirection * distanceAway, transform.rotation);
                    lastPosition = newClone.position;
                }
            }                 
        }
        else
        {
            Debug.LogWarning("No obstacles in obstacleList to instantiate");
        }
    }
    
    private List<Vector3> TestValidDirections(Vector3 origin, List<Vector3> testDirections)
    {
        List<Vector3> validDirections = testDirections.ToList();
        // Testing directions to the right, left, and back of inputDir
        foreach(Vector3 testDirection in testDirections.ToList())
        {
            Vector3 testPosition = origin + testDirection * distanceAway;
            if (!boundingBox.bounds.Contains(testPosition) || Physics.CheckBox(testPosition, targetObstacle.localScale * 0.25f, Quaternion.identity))
            {
                validDirections.Remove(testDirection);
            }
        }
        return validDirections;
    }
}
