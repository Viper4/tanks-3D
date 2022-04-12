using System.Collections.Generic;
using System.Collections;
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

    Dictionary<string, int> directionsChoosed = new Dictionary<string, int>();

    public void RandomObstacleGeneration(List<GameObject> obstacles, List<GameObject> clonedObstacles, float switchChance, float branchChance, Dictionary<string, int> cloneAmounts, Vector3 lastPosition, List<WeightedVector3> directions, bool logicalStructure, List<WeightedFloat> distances, bool rangedDst, Collider boundingCollider)
    {
        if(obstacles.Count != 0)
        {
            WeightedVector3 targetDirection = RandomExtension.ChooseWeightedVector3(directions);

            foreach (GameObject obstacle in obstacles.ToList())
            {
                // Cloning the obstacle
                for (int i = 0; i < cloneAmounts[obstacle.name]; i++)
                {
                    float dstAway;

                    if (distances.Count > 0)
                    {
                        if (rangedDst && distances.Count > 1)
                        {
                            float startVal = RandomExtension.ChooseWeightedFloat(distances).value;
                            float endVal = RandomExtension.ChooseWeightedFloat(distances, startVal).value;

                            dstAway = RandomExtension.ChooseWeightedFloat(distances, startVal, endVal).value;
                        }
                        else
                        {
                            dstAway = RandomExtension.ChooseWeightedFloat(distances).value;
                        }
                    }
                    else
                    {
                        dstAway = distanceAway;
                    }

                    Vector3 newPosition = lastPosition + targetDirection.value * dstAway;

                    if (Random.value < switchChance)
                    {
                        cloneAmounts[obstacle.name] -= i;

                        RandomObstacleGeneration(obstacles.Shuffle(), clonedObstacles, switchChance, branchChance, cloneAmounts, lastPosition, directions, logicalStructure, distances, rangedDst, boundingCollider);
                        return;
                    }

                    if (Random.value < branchChance || !boundingCollider.bounds.Contains(newPosition) || Physics.CheckSphere(newPosition, 0.1f))
                    {
                        List<WeightedVector3> validDirections = new List<WeightedVector3>();

                        if (clonedObstacles.Count == 0)
                        {
                            validDirections = TestValidDirections(boundingCollider, lastPosition, directions, dstAway, logicalStructure);
                        }
                        else
                        {
                            // Going through previously cloned obstacles starting at the latest clone to find a valid spawn position
                            for (int k = clonedObstacles.Count - 1; k > -1; k--)
                            {
                                validDirections = TestValidDirections(boundingCollider, clonedObstacles[k].transform.position, directions, dstAway, logicalStructure).ToList();

                                if (validDirections.Count != 0)
                                {
                                    targetDirection = RandomExtension.ChooseWeightedVector3(validDirections);
                                    if (directionsChoosed.ContainsKey(targetDirection.value.ToString()))
                                    {
                                        directionsChoosed[targetDirection.value.ToString()] += 1;
                                    }
                                    else
                                    {
                                        directionsChoosed[targetDirection.value.ToString()] = 1;
                                    }
                                    newPosition = clonedObstacles[k].transform.position + targetDirection.value * dstAway;
                                    break;
                                }
                            }
                        }

                        if (validDirections.Count == 0)
                        {
                            Debug.LogWarning("No unobstructed clone position found from " + lastPosition);
                            return;
                        }
                    }

                    if (logicalStructure)
                    {
                        if(Physics.Raycast(newPosition, -Vector3.up, out RaycastHit groundHit, Mathf.Infinity))
                        {
                            newPosition -= Vector3.up * (groundHit.distance - obstacle.GetComponent<Renderer>().bounds.size.y * 0.5f);
                            clonedObstacles.Add(Instantiate(obstacle, newPosition, transform.rotation, transform.parent));
                            lastPosition = newPosition;
                        }
                        else
                        {
                            Debug.LogWarning("No grounded clone position found from " + lastPosition);
                        }
                    }
                    else
                    {
                        clonedObstacles.Add(Instantiate(obstacle, newPosition, transform.rotation, transform.parent));
                        lastPosition = newPosition;
                    }
                }

                cloneAmounts[obstacle.name] = 0;
            }
        }
        else
        {
            Debug.LogWarning("No obstacles in obstacleList to instantiate");
        }
        foreach(string key in directionsChoosed.Keys)
        {
            Debug.Log(key + ": " + directionsChoosed[key]);
        }
    }

    private List<WeightedVector3> TestValidDirections(Collider boundingCollider, Vector3 origin, List<WeightedVector3> testDirections, float dst, bool logicalStructure)
    {
        List<WeightedVector3> validDirections = new List<WeightedVector3>();
        // Testing directions to the right, left, and back of inputDir
        foreach (WeightedVector3 testDirection in testDirections.ToList())
        {
            Vector3 testPosition = origin + testDirection.value * dst;
            // If testPosition is within bounds and unobstructed
            if (boundingCollider.bounds.Contains(testPosition) && !Physics.CheckSphere(testPosition, 0.1f))
            {
                if (logicalStructure)
                {
                    if (Physics.Raycast(origin, -Vector3.up, Mathf.Infinity))
                    {
                        validDirections.Add(testDirection);
                        break;
                    }
                }
                else
                {
                    validDirections.Add(testDirection);
                    break;
                }
            }
        }
        return validDirections;
    }
}
