using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] LayerMask ignoreLayerMask;
    [SerializeField] Transform obstacleParent;
    [SerializeField] Transform tankParent;
    [SerializeField] Transform clearParent;

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
        foreach (Transform obstacleChild in obstacleParent)
        {
            Destroy(obstacleChild.gameObject);
        }
        foreach(Transform child in clearParent)
        {
            Destroy(child.gameObject);
        }

        GameObject obstacle = Instantiate(obstacles[0], CustomRandom.GetSpawnPointInCollider(boundingCollider, Vector3.down, ignoreLayerMask, obstacles[0].GetComponent<BoxCollider>(), obstacles[0].transform.rotation, true), obstacles[0].transform.rotation, obstacleParent);
        GenerateObstacles(obstacle.GetComponent<ObstacleGeneration>());
        FindObjectOfType<TankManager>().GenerateTanks(true);
    }

    public void GenerateObstacles(ObstacleGeneration selectedObject)
    {
        Dictionary<string, int> cloneAmounts = new Dictionary<string, int>();
        int[] distribution = CustomRandom.Distribute(obstacleLimit, obstacles.Count, switchChance, amountDeviationMin, amountDeviationMax);
        for (int i = 0; i < obstacles.Count; i++)
        {
            cloneAmounts[obstacles[i].name] = distribution[i];
        }

        selectedObject.RandomObstacleGeneration(obstacles, new List<GameObject>(), switchChance, branchChance, cloneAmounts, selectedObject.transform.position, possibleDirections, logicalStructure, possibleDistances, rangedDistance, boundingCollider);
    }
}
