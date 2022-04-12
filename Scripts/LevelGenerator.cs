using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] Collider boundingCollider;

    [SerializeField] int tankLimit;
    [SerializeField] List<GameObject> tanks;

    [SerializeField] int obstacleLimit;
    [SerializeField] List<GameObject> obstacles;

    [SerializeField] List<WeightedFloat> possibleDistances;
    [SerializeField] bool rangedDistance = false;

    [SerializeField] List<WeightedVector3> possibleDirections;
    [SerializeField] bool logicalStructure;

    [SerializeField] float branchChance;
    [SerializeField] float switchChance;
    [SerializeField] int amountDeviationMin = 0;
    [SerializeField] int amountDeviationMax = 0;

    public void Generate(ObjectCreation selectedObject)
    {
        Dictionary<string, int> cloneAmounts = new Dictionary<string, int>();
        int[] distribution = RandomExtension.Distribute(obstacleLimit, obstacles.Count, amountDeviationMin, amountDeviationMax);
        for (int i = 0; i < obstacles.Count; i++)
        {
            cloneAmounts[obstacles[i].name] = distribution[i];
        }

        selectedObject.RandomObstacleGeneration(obstacles, new List<GameObject>(), switchChance, branchChance, cloneAmounts, selectedObject.transform.position, possibleDirections, logicalStructure, possibleDistances, rangedDistance, boundingCollider);
    }
}
