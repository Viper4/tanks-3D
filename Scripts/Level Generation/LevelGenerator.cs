using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] LayerMask ignoreLayerMask;
    [SerializeField] Transform obstacleParent;
    [SerializeField] Transform tankParent;

    [SerializeField] BoxCollider boundingCollider;

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

    public void GenerateLevel()
    {
        GameObject obstacle = Instantiate(obstacles[0], CustomRandom.GetSpawnPointInCollider(boundingCollider, Vector3.down, ignoreLayerMask, obstacles[0].GetComponent<BoxCollider>()), obstacles[0].transform.rotation, obstacleParent);
        GenerateObstacles(obstacle.GetComponent<ObstacleGeneration>());
        GenerateTanks(tankParent.GetComponent<TankGeneration>());
    }

    public void GenerateObstacles(ObstacleGeneration selectedObject)
    {
        Dictionary<string, int> cloneAmounts = new Dictionary<string, int>();
        int[] distribution = CustomRandom.Distribute(obstacleLimit, obstacles.Count, amountDeviationMin, amountDeviationMax);
        for (int i = 0; i < obstacles.Count; i++)
        {
            cloneAmounts[obstacles[i].name] = distribution[i];
        }

        selectedObject.RandomObstacleGeneration(obstacles, new List<GameObject>(), switchChance, branchChance, cloneAmounts, selectedObject.transform.position, possibleDirections, logicalStructure, possibleDistances, rangedDistance, boundingCollider);
    }

    public void GenerateTanks(TankGeneration selectedObject)
    {
        Dictionary<string, int> cloneAmounts = new Dictionary<string, int>();
        int[] distribution = CustomRandom.Distribute(tankLimit, tanks.Count, amountDeviationMin, amountDeviationMax);
        for (int i = 0; i < tanks.Count; i++)
        {
            cloneAmounts[tanks[i].name] = distribution[i];
        }

        selectedObject.RandomTankGeneration(tanks, cloneAmounts, boundingCollider);
    }
}
