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
        for (int i = 0; i < times; i++)
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
        if (extendedObjects.Count != 0)
        {
            if (extendedObjects[extendedObjects.Count - 1] != null)
            {
                DestroyImmediate(extendedObjects[extendedObjects.Count - 1].gameObject);
            }

            extendedObjects.RemoveAt(extendedObjects.Count - 1);
        }
    }

    public void GenerateRandomObstacle(Collider boundingBox, int obstacleLimit, List<GameObject> obstacleList, Vector3 inputDir, float branchChance, bool distribute)
    {
        if (obstacleList.Count != 0)
        {
            List<GameObject> obstacles = obstacleList.ToList();
            int[] cloneAmounts = new int[obstacleList.Count];

            // Determining cloneAmounts for each obstacle
            if (distribute)
            {
                // distributing obstacleLimit evenly among each obstacle
                cloneAmounts = DivideEvenly(obstacleLimit, obstacleList.Count);
            }
            else
            {
                // Randomly distributing amounts of each obstacle
                for(int i = 0; i < obstacleList.Count; i++)
                {
                    cloneAmounts[i] = Random.Range(0, obstacleLimit);
                }
            }

            Vector3 lastPosition = transform.position;
            List<Vector3> validDirections;
            Vector3 targetDirection = inputDir;

            for (int i = 0; i < obstacles.Count; i++)
            {
                // Cloning the obstacle
                for (int j = 0; j < cloneAmounts[i]; j++)
                {
                    if (Random.value <= branchChance)
                    {
                        // Testing directions to the right, left, and back of inputDir
                        validDirections = TestValidDirections(lastPosition, new List<Vector3>() { Quaternion.AngleAxis(90, Vector3.up) * inputDir, Quaternion.AngleAxis(-90, Vector3.up) * inputDir, Quaternion.AngleAxis(180, Vector3.up) * inputDir }, boundingBox);

                        targetDirection = validDirections[Random.Range(0, validDirections.Count)];
                    }

                    GameObject newClone = Instantiate(obstacles[i], lastPosition + targetDirection * distanceAway, transform.rotation);
                    lastPosition = newClone.transform.position;
                }
            }
        }
        else
        {
            Debug.LogWarning("No obstacles in obstacleList to instantiate");
        }
    }

    private List<Vector3> TestValidDirections(Vector3 origin, List<Vector3> testDirections, Collider boundingBox)
    {
        List<Vector3> validDirections = testDirections.ToList();
        // Testing directions to the right, left, and back of inputDir
        foreach(Vector3 testDirection in testDirections.ToList())
        {
            Vector3 testPosition = origin + testDirection * distanceAway;
            if (!boundingBox.bounds.Contains(testPosition) || Physics.CheckBox(testPosition, new Vector3(0.5f, 0.5f, 0.5f), Quaternion.identity))
            {
                validDirections.Remove(testDirection);
            }
        }
        return validDirections;
    }

    static int[] DivideEvenly(int numerator, int denominator)
    {
        int[] result = new int[denominator];

        int rem = numerator % denominator;
        int div = numerator / denominator;

        for (int i = 0; i < denominator; i++)
        {
            result[i] = i < rem ? div + 1 : div;
        }
        return result;
    }
}
