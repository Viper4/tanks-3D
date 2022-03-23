using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] GameObject boundingBox;

    [SerializeField] int tankLimit;
    [SerializeField] List<GameObject> tanks;

    [SerializeField] int obstacleLimit;
    [SerializeField] List<GameObject> obstacles;

    [SerializeField] float branchChance;
    [SerializeField] bool distribute;

    public void Generate(ObjectCreation selectedObject)
    {
        selectedObject.RandomGeneration(boundingBox, tankLimit, tanks, obstacleLimit, obstacles, branchChance, distribute); 
    }
}
