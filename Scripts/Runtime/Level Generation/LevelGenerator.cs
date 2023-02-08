using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

public class LevelGenerator : MonoBehaviour
{
    [SerializeField] Vector3Int mazeDimensions = new Vector3Int() { x = 10, y = 1, z = 10 };
    [SerializeField] float cellSize = 2;
    [SerializeField] bool randomSeed = true; 
    [SerializeField] int offsetX;
    [SerializeField] int offsetY;

    [SerializeField] Vector2 noiseScale = new Vector2(5, 5);
    [SerializeField] Cell[] cellsToInstantiate;

    [SerializeField] Transform toClearParent;

    [Serializable]
    struct Cell
    {
        public GameObject[] prefabs;
        [Range(0, 1)] public float minNoise;
        [Range(0, 1)] public float maxNoise;
        [Range(0, 4)] public int minNeighbors;
        [Range(0, 4)] public int maxNeighbors;
        public uint height;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Generate()
    {
        if (randomSeed)
        {
            offsetX = Random.Range(-999, 999);
            offsetY = Random.Range(-999, 999);
        }
        foreach (Transform child in toClearParent)
        {
            Destroy(child.gameObject);
        }
        int width = mazeDimensions.x;
        int height = mazeDimensions.z;

        bool[,] filledCells = new bool[width, height];
        foreach (Cell cell in cellsToInstantiate)
        {
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    int x = i + offsetX;
                    int y = j + offsetY;
                    if(!filledCells[i, j])
                    {
                        float noise = Mathf.PerlinNoise((float)x / width * noiseScale.x, (float)y / height * noiseScale.y);
                        if (noise > cell.minNoise && noise < cell.maxNoise)
                        {
                            int neighbors = 0;
                            float right = Mathf.PerlinNoise((float)(x + 1) / width * noiseScale.x, (float)y / height * noiseScale.y);
                            if (right > cell.minNoise && right < cell.maxNoise)
                                neighbors++;
                            float left = Mathf.PerlinNoise((float)(x - 1) / width * noiseScale.x, (float)y / height * noiseScale.y);
                            if (left > cell.minNoise && left < cell.maxNoise)
                                neighbors++;
                            float up = Mathf.PerlinNoise((float)x / width * noiseScale.x, (float)(y + 1) / height * noiseScale.y);
                            if (up > cell.minNoise && up < cell.maxNoise)
                                neighbors++;
                            float down = Mathf.PerlinNoise((float)x / width * noiseScale.x, (float)(y - 1) / height * noiseScale.y);
                            if (down > cell.minNoise && down < cell.maxNoise)
                                neighbors++;

                            if(neighbors >= cell.minNeighbors && neighbors <= cell.maxNeighbors)
                            {
                                for(int cellY = 0; cellY <= cell.height; cellY++)
                                {
                                    Instantiate(cell.prefabs[Random.Range(0, cell.prefabs.Length)], transform.position + new Vector3(i - mazeDimensions.x * 0.5f, cellY, j - mazeDimensions.z * 0.5f) * cellSize, Quaternion.identity, toClearParent);
                                }
                                filledCells[i, j] = true;
                            }
                        }
                    }
                }
            }
        }
    }
}
