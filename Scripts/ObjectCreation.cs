using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/* Cites
 * RandomExtension (class I created) 
 *  Variables: WeightedVector3, WeightedFloat
 *  Methods: Shuffle(), ChooseWeightedFloat(), ChooseWeightedVector3()
 * System.Collections.Generic (built-in library) https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic?view=net-6.0
 *  Variables: List<type> https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1?view=net-6.0
 * System.Linq (built-in library) https://docs.microsoft.com/en-us/dotnet/api/system.linq?view=net-6.0
 *  Methods: ToList() https://docs.microsoft.com/en-us/dotnet/api/system.linq.enumerable.tolist?view=net-6.0
 * UnityEngine (built-in library) https://docs.unity3d.com/ScriptReference/
 *  Variables: GameObject https://docs.unity3d.com/ScriptReference/GameObject-ctor.html, Transform https://docs.unity3d.com/ScriptReference/Transform.html, Vector3 https://docs.unity3d.com/ScriptReference/Vector3.html, Bounds https://docs.unity3d.com/ScriptReference/Bounds.html, RaycastHit https://docs.unity3d.com/ScriptReference/RaycastHit.html
 *  Classes: MonoBehaviour https://docs.unity3d.com/ScriptReference/MonoBehaviour.html, Debug https://docs.unity3d.com/ScriptReference/Debug.html, Physics https://docs.unity3d.com/ScriptReference/Physics.html, Mathf https://docs.unity3d.com/ScriptReference/Mathf.html
 */
public class ObjectCreation : MonoBehaviour
{
    // Declaring global variables
    List<Transform> extendedObjects = new List<Transform>();

    [SerializeField] Vector3 direction;
    [SerializeField] float distanceAway;
    [SerializeField] int times = 1;
    [SerializeField] Vector3 eulerAngles;
    [SerializeField] Vector3 scale = new Vector3(2, 2, 2);

    public void Extend()
    {
        // Iterate through all the times to clone this gameobject
        for (int i = 0; i < times; i++)
        {
            // Instantiate this object at the given direction and distance away, reset its name and scale, and add the clone to the extendedObjects list
            Transform clone = Instantiate(transform, transform.position + direction * (distanceAway * (i + 1)), Quaternion.Euler(eulerAngles), transform.parent);
            clone.name = transform.name;
            clone.localScale = scale;
            extendedObjects.Add(clone);
        }
    }

    public void Delete()
    {
        // Delete this game object
        DestroyImmediate(gameObject);
    }

    public void Undo()
    {
        // If the there are extended objects
        if (extendedObjects.Count != 0)
        {
            // If the most recent element in extendedObjects is not null delete it
            if (extendedObjects[extendedObjects.Count - 1] != null)
            {
                DestroyImmediate(extendedObjects[extendedObjects.Count - 1].gameObject);
            }
            // Remove most recent element in extendedObjects
            extendedObjects.RemoveAt(extendedObjects.Count - 1);
        }
    }

    public void RandomObstacleGeneration(List<GameObject> obstacles, List<GameObject> clonedObstacles, float switchChance, float branchChance, Dictionary<string,int> cloneAmounts, 
        Vector3 lastPosition, List<WeightedVector3> directions, bool logicalStructure, List<WeightedFloat> distances, bool rangedDst, Collider boundingCollider)
    {
        // If the user input obstacles to generate
        if(obstacles.Count != 0)
        {
            // Select random direction from input possible directions
            WeightedVector3 targetDirection = RandomExtension.ChooseWeightedVector3(directions);

            // Iterating through each obstacle in input obstacles
            foreach (GameObject obstacle in obstacles.ToList())
            {
                // Iterating through the amount of times to clone this obstacle
                for (int i = 0; i < cloneAmounts[obstacle.name]; i++)
                {
                    float dstAway;

                    // If the user input distances away to generate each obstacle
                    if (distances.Count > 0)
                    {
                        // If the user wants a range of distances
                        if (rangedDst && distances.Count > 1)
                        {
                            // Picks random startVal, picks random endVal from  startVal to the end of the values, and sets dstAway to a random value between startVal and endVal (from my RandomExtension class)
                            float startVal = RandomExtension.ChooseWeightedFloat(distances).value;
                            float endVal = RandomExtension.ChooseWeightedFloat(distances, startVal).value;

                            dstAway = RandomExtension.ChooseWeightedFloat(distances, startVal, endVal).value;
                        }
                        // If the user wants to select a single distance based on weights (from my RandomExtension class)
                        else
                        {
                            dstAway = RandomExtension.ChooseWeightedFloat(distances).value;
                        }
                    }
                    // If the user did not input distances away to generate each obstacle set it to the global variable distanceAway
                    else
                    {
                        dstAway = distanceAway;
                    }

                    // Setting newPosition to instantiate at
                    Vector3 newPosition = lastPosition + targetDirection.value * dstAway;

                    // If a random float from 0 to 1 is less than switchChance, then update this obstacle's amount left to clone, recall this function with a shuffled obstacles list, and stop this method
                    if (Random.value < switchChance)
                    {
                        cloneAmounts[obstacle.name] -= i;

                        RandomObstacleGeneration(obstacles.Shuffle(), clonedObstacles, switchChance, branchChance, cloneAmounts, lastPosition, directions, logicalStructure, distances, rangedDst, boundingCollider);
                        return;
                    }

                    // If a random float from 0 to 1 is less than branchChance, or new position is out of bounds, or new position is obstructed then pick another direction
                    if (Random.value < branchChance || !boundingCollider.bounds.Contains(newPosition) || Physics.CheckSphere(newPosition, 0.1f))
                    {
                        List<WeightedVector3> validDirections = new List<WeightedVector3>();

                        // Going through previously cloned obstacles starting at the latest clone to find a valid spawn position
                        for (int k = clonedObstacles.Count - 1; k > -1; k--)
                        {
                            // Testing valid directions in possible directions dstAway from this clone's position
                            validDirections = TestValidDirections(boundingCollider, clonedObstacles[k].transform.position, directions, dstAway).ToList();

                            // If a valid direction(s) has been found, randomely pick one based on weights (from my RandomExtension class), set the position to instantiate at, and break out of the loop
                            if (validDirections.Count != 0)
                            {
                                targetDirection = RandomExtension.ChooseWeightedVector3(validDirections);
                                
                                newPosition = clonedObstacles[k].transform.position + targetDirection.value * dstAway;
                                break;
                            }
                        }

                        // If no valid direction has been found, log a warning and stop the method
                        if (validDirections.Count == 0)
                        {
                            Debug.LogWarning("No unobstructed clone position found from " + lastPosition);
                            return;
                        }
                    }

                    // If user wants to generate obstacles with gravity in mind
                    if (logicalStructure)
                    {
                        // Checking if the new position is above ground
                        if(Physics.Raycast(newPosition, -Vector3.up, out RaycastHit groundHit, Mathf.Infinity))
                        {
                            // Setting new position to the distance to ground, instantiating the object, and updating last position
                            newPosition -= Vector3.up * (groundHit.distance - obstacle.GetComponent<Renderer>().bounds.size.y * 0.5f);
                            clonedObstacles.Add(Instantiate(obstacle, newPosition, transform.rotation, transform.parent));
                            lastPosition = newPosition;
                        }
                        else // When new position is not above ground then log warning and stop the recursion
                        {
                            Debug.LogWarning("No grounded clone position found from " + newPosition);
                            return;
                        }
                    }
                    else
                    {
                        // Instantiating obstacle at new position and updating last position
                        clonedObstacles.Add(Instantiate(obstacle, newPosition, transform.rotation, transform.parent));
                        lastPosition = newPosition;
                    }
                }

                // When the iteration through the amount of this obstacle is finished set the amount to 0
                cloneAmounts[obstacle.name] = 0;
            }
        }
        // If the user didn't input any obstacles then log a warning
        else
        {
            Debug.LogWarning("No obstacles in obstacleList to instantiate");
        }
    }

    private List<WeightedVector3> TestValidDirections(Collider boundingCollider, Vector3 origin, List<WeightedVector3> testDirections, float dst, bool logicalStructure)
    {
        List<WeightedVector3> validDirections = new List<WeightedVector3>();
        // Iterating through each testDirection in the input directions
        foreach (WeightedVector3 testDirection in testDirections.ToList())
        {
            Vector3 testPosition = origin + testDirection.value * dst;
            // If testPosition is within bounds and unobstructed
            if (boundingCollider.bounds.Contains(testPosition) && !Physics.CheckSphere(testPosition, 0.1f))
            {
                // If user wants to generate objects with gravity in mind
                if (logicalStructure) 
                {
                    // If testPosition is above ground then add testDirection to validDirections
                    if(Physics.Raycast(testPosition, -Vector3.up, out RaycastHit groundHit, Mathf.Infinity))
                    {
                        validDirections.Add(testDirection);
                    }
                }
                else
                {
                    validDirections.Add(testDirection);
                }
            }
        }
        return validDirections;
    }
}
