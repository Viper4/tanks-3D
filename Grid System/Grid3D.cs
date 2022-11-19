using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grid3D : MonoBehaviour
{
    int[,,] grid;
    public bool visible = true;
    public Dictionary<Vector3Int, Cell> cellDictionary = new Dictionary<Vector3Int, Cell>();

    public List<Cell> selectedCells = new List<Cell>();

    [SerializeField] Cell cellPrefab;
    [SerializeField] Color baseCellColor = Color.white;
    [SerializeField] Color offsetCellColor = Color.black;

    public Vector3 cellSize = new Vector3(1, 1, 1);

    public int width; // x
    public int height; // y
    public int length; // z

    int halfWidth;
    int halfHeight;
    int halfLength;

    // Start is called before the first frame update
    void Start()
    {
        halfWidth = width / 2;
        halfHeight = height / 2;
        halfLength = length / 2;
        GenerateGrid();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void OnValidate()
    {
        grid = new int[width, height, length];
    }

    public void GenerateGrid()
    {
        grid = new int[width, height, length];
        foreach(Cell cell in cellDictionary.Values)
        {
            Destroy(cell.gameObject);
        }
        cellDictionary.Clear();

        if(visible)
        {
            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        Vector3Int cellPosition = new Vector3Int(x, y, z);
                        Vector3 cellWorldPosition = CellToWorld(cellPosition);
                        bool offset =(x % 2 == 0 && z % 2 != 0) ||(x % 2 != 0 && z % 2 == 0);
                        if(y % 2 != 0)
                        {
                            offset = !offset;
                        }
                        Cell newCell = Instantiate(cellPrefab,(Vector3)cellWorldPosition, Quaternion.identity, transform);
                        cellDictionary.Add(cellPosition, newCell);
                        newCell.name = $"Cell({x}, {y}, {z})";
                        newCell.transform.localScale = cellSize;
                        newCell.Init(this, cellPosition, offset, baseCellColor, offsetCellColor);
                    }
                }
            }
        }
    }

    public int DimensionLength(int dimension)
    {
        return grid.GetLength(dimension);
    }

    bool CellInBounds(Vector3Int cellPosition)
    {
        return cellPosition.x > -1 && cellPosition.x < width && cellPosition.y > -1 && cellPosition.y < height && cellPosition.z > -1 && cellPosition.z < length;
    }

    public Vector3 CellToWorld(Vector3Int cellPosition)
    {
        return transform.position + new Vector3((cellPosition.x - halfWidth) * cellSize.x,(cellPosition.y - halfHeight) * cellSize.y,(cellPosition.z - halfLength) * cellSize.z);
    }

    public Vector3Int WorldToCell(Vector3 worldPosition)
    {
        return new Vector3Int(Mathf.FloorToInt((worldPosition.x - transform.position.x) / cellSize.x) + halfWidth, Mathf.FloorToInt((worldPosition.y - transform.position.y) / cellSize.y) + halfHeight, Mathf.FloorToInt((worldPosition.z - transform.position.z) / cellSize.z) + halfLength);
    }

    public Cell GetCell(Vector3Int cellPosition)
    {
        if(cellDictionary.TryGetValue(cellPosition, out Cell cell))
        {
            return cell;
        }
        return null;
    }

    public Cell GetCell(Vector3 worldPosition)
    {
        Vector3Int cellPosition = WorldToCell(worldPosition);
        if(CellInBounds(cellPosition) && cellDictionary.TryGetValue(cellPosition, out Cell cell))
        {
            return cell;
        }
        return null;
    }

    public void SetAllActive(bool value)
    {
        foreach(Cell cell in cellDictionary.Values)
        {
            cell.gameObject.SetActive(value);
        }
    }

    public void SetLayerActive(int dimension, int layer, bool value)
    {
        switch(dimension)
        {
            case 0: // x
                for(int y = 0; y < height; y++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        GetCell(new Vector3Int(layer, y, z)).gameObject.SetActive(value);
                    }
                }
                break;
            case 1: // y
                for(int x = 0; x < width; x++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        GetCell(new Vector3Int(x, layer, z)).gameObject.SetActive(value);
                    }
                }
                break;
            case 2: // z
                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        GetCell(new Vector3Int(x, y, layer)).gameObject.SetActive(value);
                    }
                }
                break;
        }
    }

    public List<Cell> FillAll(GameObject withObject)
    {
        List<Cell> filledCells = new List<Cell>();
        foreach(Cell cell in cellDictionary.Values)
        {
            if(cell.Fill(withObject))
            {
                filledCells.Add(cell);
            }
        }
        return filledCells;
    }

    public List<Cell> FillLayer(int dimension, int layer, GameObject withObject)
    {
        List<Cell> filledCells = new List<Cell>();
        switch(dimension)
        {
            case 0: // x
                for(int y = 0; y < height; y++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        Cell cell = GetCell(new Vector3Int(layer, y, z));
                        if(cell.Fill(withObject))
                        {
                            filledCells.Add(cell);
                        }
                    }
                }
                break;
            case 1: // y
                for(int x = 0; x < width; x++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        Cell cell = GetCell(new Vector3Int(x, layer, z));
                        if(cell.Fill(withObject))
                        {
                            filledCells.Add(cell);
                        }
                    }
                }
                break;
            case 2: // z
                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Cell cell = GetCell(new Vector3Int(x, y, layer));
                        if(cell.Fill(withObject))
                        {
                            filledCells.Add(cell);
                        }
                    }
                }
                break;
        }
        return filledCells;
    }

    public List<Cell> FillVolume(Cell centerCell, int size, GameObject withObject)
    {
        List<Cell> filledCells = new List<Cell>();
        foreach(Cell cell in GetVolumeCells(centerCell, size))
        {
            if(cell.Fill(withObject))
            {
                filledCells.Add(cell);
            }
        }
        return filledCells;
    }

    public Dictionary<Cell, string> ClearAll()
    {
        Dictionary<Cell, string> clearedCells = new Dictionary<Cell, string>();
        foreach(Cell cell in cellDictionary.Values)
        {
            if(cell.occupiedObject != null)
            {
                clearedCells.Add(cell, cell.occupiedObject.name);
            }
        }
        return clearedCells;
    }

    public Dictionary<Cell, string> ClearLayer(int dimension, int layer)
    {
        Dictionary<Cell, string> clearedCells = new Dictionary<Cell, string>();
        switch(dimension)
        {
            case 0: // x
                for(int y = 0; y < height; y++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        Cell cell = GetCell(new Vector3Int(layer, y, z));
                        if(cell.occupiedObject != null)
                        {
                            clearedCells.Add(cell, cell.occupiedObject.name);
                        }
                    }
                }
                break;
            case 1: // y
                for(int x = 0; x < width; x++)
                {
                    for(int z = 0; z < length; z++)
                    {
                        Cell cell = GetCell(new Vector3Int(x, layer, z));
                        if(cell.occupiedObject != null)
                        {
                            clearedCells.Add(cell, cell.occupiedObject.name);
                        }
                    }
                }
                break;
            case 2: // z
                for(int x = 0; x < width; x++)
                {
                    for(int y = 0; y < height; y++)
                    {
                        Cell cell = GetCell(new Vector3Int(x, y, layer));
                        if(cell.occupiedObject != null)
                        {
                            clearedCells.Add(cell, cell.occupiedObject.name);
                        }
                    }
                }
                break;
        }
        return clearedCells;
    }

    public Dictionary<Cell, string> ClearVolume(Cell centerCell, int size)
    {
        Dictionary<Cell, string> clearedCells = new Dictionary<Cell, string>();
        foreach(Cell cell in GetVolumeCells(centerCell, size))
        {
            if(cell.occupiedObject != null)
            {
                clearedCells.Add(cell, cell.occupiedObject.name);
                cell.Clear();
            }
        }
        return clearedCells;
    }
    
    private List<Cell> GetVolumeCells(Cell centerCell, int size)
    {
        List<Cell> volumeCells = new List<Cell>();
        volumeCells.Add(centerCell);
        if(size > 0)
        {
            for(int i = -size; i <= size; i++)
            {
                int x = centerCell.gridPosition.x + i;
                if(x > -1 && x < width)
                {
                    for(int j = -size; j <= size; j++)
                    {
                        int y = centerCell.gridPosition.y + j;
                        if(y > -1 && y < height)
                        {
                            for(int k = -size; k <= size; k++)
                            {
                                int z = centerCell.gridPosition.z + k;
                                if(z > -1 && z < length)
                                {
                                    Cell cell = GetCell(new Vector3Int(x, y, z));
                                    if(cell != centerCell && cell.gameObject.activeSelf)
                                    {
                                        volumeCells.Add(cell);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        return volumeCells;
    }

    public void VolumeHighlight(Cell centerCell, int size, bool value)
    {
        selectedCells.Clear();
        selectedCells.Add(centerCell);
        if(size > 0)
        {
            for(int i = -size; i <= size; i++)
            {
                int x = centerCell.gridPosition.x + i;
                if(x > -1 && x < width)
                {
                    for(int j = -size; j <= size; j++)
                    {
                        int y = centerCell.gridPosition.y + j;
                        if(y > -1 && y < height)
                        {
                            for(int k = -size; k <= size; k++)
                            {
                                int z = centerCell.gridPosition.z + k;
                                if(z > -1 && z < length)
                                {
                                    Cell cell = GetCell(new Vector3Int(x, y, z));
                                    if(cell != centerCell && cell.gameObject.activeSelf)
                                    {
                                        cell.SetHighlight(value);
                                        selectedCells.Add(cell);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
