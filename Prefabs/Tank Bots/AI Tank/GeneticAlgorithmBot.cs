using System.Collections;
using UnityEngine;
using MyUnityAddons.Calculations;
using System.Collections.Generic;

public class GeneticAlgorithmBot : MonoBehaviour
{
    [SerializeField] string modelPath;
    [SerializeField] int[] layers;

    public Trainer trainer;

    TargetSystem targetSystem;
    BaseTankLogic baseTankLogic;
    AreaScanner areaScanner;

    Transform body;
    Transform turret;
    Transform barrel;

    FireControl fireControl;
    MineControl mineControl;

    public NeuralNetwork neuralNetwork;

    public int score;

    float timeAlive = 0;
    public bool Dead
    {
        get { return dead; }
        set { SetDeath(value); }
    }
    private bool dead = false;
    public int Kills { get; set; }

    float[] input;

    [SerializeField] float cellSize = 2;
    [SerializeField] int gridLength = 32;
    private int halfGridLength;
    [SerializeField] int gridWidth = 32;
    private int halfGridWidth;
    [SerializeField] int gridHeight = 16;
    private int halfGridHeight;

    [SerializeField] int miniGridLength = 8;
    private int halfMiniLength;
    [SerializeField] int miniGridWidth = 8;
    private int halfMiniWidth;
    [SerializeField] int miniGridHeight = 4;
    private int halfMiniHeight;
    float[,] map;
    float[,] dynamicMap;
    [SerializeField] LayerMask dynamicObstacleLayerMask;
    [SerializeField] LayerMask staticObstacleLayerMask;
    [SerializeField] LayerMask dangerLayerMask;
    [SerializeField] LayerMask tankLayerMask;

    List<Transform> passedCheckpoints = new List<Transform>();

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();
        baseTankLogic = GetComponent<BaseTankLogic>();
        areaScanner = GetComponent<AreaScanner>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();

        if (trainer == null)
        {
            neuralNetwork = new NeuralNetwork(layers, NeuralNetwork.Activations.Tanh);
            neuralNetwork.Load(modelPath);
        }
        halfGridLength = gridLength / 2;
        halfGridWidth = gridWidth / 2;
        halfMiniLength = miniGridLength / 2;
        halfMiniWidth = miniGridWidth / 2;
        input = new float[1 + miniGridWidth * miniGridLength];
        StaticMap();

        InvokeRepeating(nameof(Loop), 0.1f, 0.05f);
    }

    private void Update()
    {
        timeAlive += Time.deltaTime;
    }

    void Loop()
    {
        if (!dead && !GameManager.frozen && Time.timeScale != 0)
        {
            // Feed mini map of obstacles (-1), tanks (0.5), and dangers (1) in an area of length * width * cellSize of full map
            MiniMap();
            int index = 0;
            Vector3Int miniMapOrigin = WorldToCell(transform.position);
            int rowStart = miniMapOrigin.z - halfMiniLength;
            int columnStart = miniMapOrigin.x - halfMiniWidth;
            if (trainer == null)
            {
                string mapVisual = "";
                for (int i = rowStart; i < rowStart + miniGridLength; i++)
                {
                    for (int j = columnStart; j < columnStart + miniGridWidth; j++)
                    {
                        input[index] = dynamicMap[i, j];
                        mapVisual += string.Format("{0} ", dynamicMap[i, j]);
                        index++;
                    }
                    mapVisual += "\n";
                }
                Debug.Log("Mini Map\n" + mapVisual + "---");
            }
            else
            {
                for (int i = rowStart; i < rowStart + miniGridLength; i++)
                {
                    for (int j = columnStart; j < columnStart + miniGridWidth; j++)
                    {
                        if (i >= 0 && j >= 0 && i < gridLength && j < gridWidth)
                        {
                            input[index] = dynamicMap[i, j];
                            index++;
                        }
                    }
                }
            }
            input[index] = transform.eulerAngles.y / 180;

            float[] output = neuralNetwork.Forward(input);

            // Tank
            baseTankLogic.targetTankDir = Quaternion.AngleAxis(output[0] * 2, turret.up) * transform.forward;
            baseTankLogic.normalSpeed = output[1] * 6.5f;
            // Turret
            //baseTankLogic.targetTurretDir = Quaternion.AngleAxis(output[2] * 2, turret.up) * barrel.forward;
            
            /*// Firing
            if (output[3] > 0.75f && fireControl.canFire)
            {
                StartCoroutine(fireControl.Shoot());
            }

            // Laying
            if (output[4] > 0.5f && mineControl.canLay)
            {
                StartCoroutine(mineControl.LayMine());
            }*/
        }
        else
        {
            baseTankLogic.stationary = true;
        }
    }

    Vector3 CellToWorld(Vector3Int cell)
    {
        return new Vector3((cell.x - halfGridWidth) * cellSize, (cell.y - halfGridHeight) * cellSize, (Mathf.Abs(cell.z - (gridLength - 1)) - halfGridLength) * cellSize);
    }

    Vector3Int WorldToCell(Vector3 position)
    {
        return new Vector3Int(Mathf.FloorToInt((position.x / cellSize) + halfGridWidth), Mathf.FloorToInt(position.y / cellSize) + halfGridHeight, Mathf.FloorToInt((-position.z / cellSize) + halfGridLength));
    }

    void SetCell(Vector3Int cell, float value)
    {
        if (cell.z >= 0 && cell.x >= 0 && cell.z < gridLength && cell.x < gridWidth)
        {
            if (value != -0.5f || dynamicMap[cell.z, cell.x] != -1)
            {
                dynamicMap[cell.z, cell.x] = value;
            }
        }
        else
        {
            Debug.LogWarning("Failed to set cell " + cell + "; Cell is out of range of map");
        }
    }

    void StaticMap()
    {
        map = new float[gridLength, gridWidth];
        // Checking every cell is slow, but is the easiest way to get all the cells a collider overlaps
        int rowLength = map.GetLength(0);
        int columnLength = map.GetLength(1);
        float halfCellSize = cellSize * 0.5f;
        Vector3 halfCell = new Vector3(halfCellSize, halfCellSize, halfCellSize);
        if (trainer == null)
        {
            string mapVisual = "";
            for (int i = 0; i < rowLength; i++)
            {
                for (int j = 0; j < columnLength; j++)
                {
                    if (Physics.CheckBox(CellToWorld(new Vector3Int(j, 0, i)) + halfCell, halfCell * 0.99f, Quaternion.identity, staticObstacleLayerMask))
                    {
                        map[i, j] = -1;
                    }
                    mapVisual += string.Format("{0} ", map[i, j]);
                }
                mapVisual += "\n";
            }
            Debug.Log("Static Map:\n" + mapVisual + "---");
        }
        else
        {
            for (int i = 0; i < rowLength; i++)
            {
                for (int j = 0; j < columnLength; j++)
                {
                    if (Physics.CheckBox(CellToWorld(new Vector3Int(j, 0, i)) + halfCell, halfCell * 0.99f, Quaternion.identity, staticObstacleLayerMask))
                    {
                        map[i, j] = -1;
                    }
                }
            }
        }
    }

    void MiniMap()
    {
        dynamicMap = (float[,])map.Clone();

        Collider[] allObstacles = Physics.OverlapBox(transform.position, new Vector3(miniGridWidth * cellSize, miniGridHeight * cellSize, miniGridLength * cellSize) * 0.5f, Quaternion.identity, dynamicObstacleLayerMask);
        Collider[] allTanks = Physics.OverlapBox(transform.position, new Vector3(miniGridWidth * cellSize, miniGridHeight * cellSize, miniGridLength * cellSize) * 0.5f, Quaternion.identity, tankLayerMask);
        Collider[] allDangers = Physics.OverlapBox(transform.position, new Vector3(miniGridWidth * cellSize, miniGridHeight * cellSize, miniGridLength * cellSize) * 0.5f, Quaternion.identity, dangerLayerMask);
        /* TODO: Map all cells that the collider overlaps
         * 0. Test all vertices, vertices on the edge of cells are a problem
         * 1. Start from min then test every cellSize to max
         * 2. Create box collider from intersecting colliders and test points in that collider
         * 3. Iterate through every cell and test cell for each layermask
         */
        foreach (Collider obstacle in allObstacles)
        {
            SetCell(WorldToCell(obstacle.bounds.center), -0.5f);
            foreach (Vector3 vertex in obstacle.Vertices())
            {
                SetCell(WorldToCell(vertex), -0.5f);
            }
        }
        foreach (Collider tank in allTanks)
        {
            SetCell(WorldToCell(tank.bounds.center), 0.5f);
        }
        foreach (Collider danger in allDangers)
        {
            SetCell(WorldToCell(danger.bounds.center), 1.0f);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.transform.name != "Floor")
        {
            Dead = true;
        }
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.transform.CompareTag("Checkpoint"))
        {
            if (passedCheckpoints.Contains(collider.transform))
            {
                score--;
                passedCheckpoints.Remove(collider.transform);
            }
            else
            {
                score++;
                passedCheckpoints.Add(collider.transform);
            }
        }
    }

    void SetDeath(bool value)
    {
        dead = value;

        if (dead && trainer != null)
        {
            trainer.OnBotDeath();
        }
    }

    public void UpdateFitness()
    {
        score += Kills * 20;
        neuralNetwork.fitness = score;
    }
}
