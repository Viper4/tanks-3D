using MyUnityAddons.Calculations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;
using System.Linq;
using Photon.Pun.UtilityScripts;
using MyUnityAddons.CustomPhoton;

public class LevelEditorControl : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    BaseUI baseUI;
    Camera editCamera;
    Camera playCamera;
    GameObject player;

    [SerializeField] TextMeshProUGUI errorMessage;
    string levelName = "Custom";
    string levelDescription = "A custom level.";
    string levelCreators = "";
    [SerializeField] RectTransform levelSlotTemplate;
    [SerializeField] RectTransform levelSlotContainer;
    string selectedLevelSlot;

    bool scrollForSpeed = true;
    [SerializeField] float movementSpeed = 6;
    [SerializeField] float speedLimit = 100;

    [SerializeField] float rotationSmoothing = 0.05f;
    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    float yaw;
    float pitch;

    [SerializeField] float previewDistance = 20;
    [SerializeField] float[] previewDistanceLimit = { 2, 80 };

    [SerializeField] LayerMask ignoreLayers;
    [SerializeField] Transform previewObject;
    Collider previewCollider;
    MeshRenderer previewRenderer;
    [SerializeField] Vector3Int cellSize;
    public int brushSize = 0;
    int halfBrushSize;

    Vector3 lastEulerAngles;
    Vector3 rotationAxis;

    bool hollowBrush = false;
    enum BrushType
    {
        Cube,
        Sphere,
        Cylinder,
        Square,
        Circle,
    }
    BrushType brushType = BrushType.Cube;

    private struct CellInfo
    {
        public string name;
        public Vector3 eulerAngles;
        public Vector3 scale;
    }

    Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    Dictionary<Vector3Int, GameObject> placedSpawnpoints = new Dictionary<Vector3Int, GameObject>();
    Dictionary<Vector3Int, GameObject> placedBlocks = new Dictionary<Vector3Int, GameObject>();
    Dictionary<Vector3Int, GameObject> placedHoles = new Dictionary<Vector3Int, GameObject>();
    Dictionary<Vector3Int, CellInfo> destroyedBlocks = new Dictionary<Vector3Int, CellInfo>();
    Dictionary<Vector3Int, CellInfo> destroyedHoles = new Dictionary<Vector3Int, CellInfo>();
    float destroyHoldTimer = 0.75f;
    float placeHoldTimer = 0.75f;

    enum SpawnpointType
    {
        Players,
        Bots,
        Team1,
        Team2,
        Team3,
        Team4,
    }
    SpawnpointType spawnpointType = SpawnpointType.Players;

    enum UndoAction
    {
        Fill,
        Clear,
        Instantiate,
        DestroySpawnpoint,
    }

    Vector3 objectScale;
    Vector3 objectRotation;

    List<UndoAction> undoActions = new List<UndoAction>();
    List<object[]> undoObjects = new List<object[]>();

    List<UndoAction> redoActions = new List<UndoAction>();
    List<object[]> redoObjects = new List<object[]>();

    private Vector3Int hitGridPoint;
    private RaycastHit hit;
    Vector3 hitRight;
    Vector3 hitUp;

    List<DestructableObject> destructables = new List<DestructableObject>();
    List<SaveableLevelObject> dynamicObjects = new List<SaveableLevelObject>();

    private void Start()
    {
        baseUI = GetComponentInChildren<BaseUI>();
        editCamera = GetComponent<Camera>();
        baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\nEditing " + levelName;

        levelSlotTemplate.gameObject.SetActive(false);
        levelDescription = "A custom level by " + PhotonNetwork.NickName + ".";

        objectScale = cellSize;

        for (int i = 0; i < GameManager.Instance.editorPrefabs.Count; i++)
        {
            prefabDictionary.Add(GameManager.Instance.editorNames[i], GameManager.Instance.editorPrefabs[i]);
        }
        rotationAxis = Vector3.up;

        InvokeRepeating(nameof(MouseHoldLoop), 0, 0.125f);
        PhotonChatController.Instance.Resume(false);
        Resume();
    }

    // Update is called once per frame
    void Update()
    {
        if (editCamera.enabled)
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (baseUI.UIElements["PauseMenu"].gameObject.activeSelf || baseUI.UIElements["EditorMenu"].gameObject.activeSelf || baseUI.UIElements["LoadMenu"].gameObject.activeSelf || baseUI.UIElements["SaveMenu"].gameObject.activeSelf)
                {
                    Resume();
                }
                else
                {
                    baseUI.UIElements["PauseMenu"].gameObject.SetActive(true);
                    Pause();
                }
            }

            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.S))
                {
                    baseUI.UIElements["SaveMenu"].gameObject.SetActive(true);
                    baseUI.UIElements["SaveMenu"].Find("Level Name").GetComponent<TMP_InputField>().SetTextWithoutNotify(levelName);
                    baseUI.UIElements["SaveMenu"].Find("Level Description").GetComponent<TMP_InputField>().SetTextWithoutNotify(levelDescription);
                    baseUI.UIElements["SaveMenu"].Find("Level Creators").GetComponent<TMP_InputField>().SetTextWithoutNotify(levelCreators);
                    Pause();
                }
                else if (Input.GetKeyDown(KeyCode.L))
                {
                    RefreshLevelSlots();
                    baseUI.UIElements["LoadMenu"].gameObject.SetActive(true);
                    Pause();
                }
                else if (Input.GetKeyDown(KeyCode.P))
                {
                    if (!GameManager.Instance.playMode)
                    {
                        baseUI.UIElements["PlayMenu"].gameObject.SetActive(true);
                        Pause();
                    }
                    else
                    {
                        ExitPlayMode();
                    }
                }
                else if (Input.GetKeyDown(KeyCode.E) && GameManager.Instance.playMode)
                {
                    SwitchPlayCamera();
                }
            }

            if (!GameManager.Instance.paused && Time.timeScale != 0)
            {
                if (Input.GetKeyDown(KeyCode.Tab))
                {
                    if (baseUI.UIElements["EditorMenu"].gameObject.activeSelf)
                    {
                        Resume();
                    }
                    else
                    {
                        baseUI.UIElements["EditorMenu"].gameObject.SetActive(true);
                        Pause();
                    }
                }
                else if (Input.GetKeyDown(KeyCode.R) && previewObject != null)
                {
                    previewObject.Rotate(rotationAxis, 90);
                    objectRotation = previewObject.eulerAngles;
                }
                else if (Input.GetKeyDown(KeyCode.X))
                {
                    rotationAxis = Vector3.right;
                }
                else if (Input.GetKeyDown(KeyCode.Y))
                {
                    rotationAxis = Vector3.up;
                }
                else if (Input.GetKeyDown(KeyCode.Z))
                {
                    rotationAxis = Vector3.forward;
                }
                else if (Input.GetKeyDown(KeyCode.C))
                {
                    List<Vector3Int> placedCells = placedBlocks.Keys.ToList();
                    placedCells.AddRange(placedHoles.Keys.ToList());

                    AddUndoAction(UndoAction.Clear, new object[] { placedCells });
                    foreach (Vector3Int cell in placedCells)
                    {
                        DestroyCell(cell);
                    }
                    foreach (Vector3Int collider in placedSpawnpoints.Keys.ToList())
                    {
                        DestroySpawnpoint(collider);
                    }
                }

                bool leftShiftDown = Input.GetKey(KeyCode.LeftShift);
                if (Input.GetKeyDown(KeyCode.LeftShift))
                {
                    UpdatePreviewObject(true);
                }
                else if (Input.GetKeyUp(KeyCode.LeftShift))
                {
                    UpdatePreviewObject(false);
                }
                float scrollRate = Input.GetKey(DataManager.playerSettings.keyBinds["Zoom Control"]) ? DataManager.playerSettings.slowZoomSpeed : DataManager.playerSettings.fastZoomSpeed;

                Vector3 inputDir = new Vector3(GetInputAxis("x"), GetInputAxis("y"), GetInputAxis("z")).normalized;

                float targetSpeed = movementSpeed / 2 * inputDir.magnitude;

                if (Input.GetKeyDown(KeyCode.LeftAlt))
                {
                    scrollForSpeed = !scrollForSpeed;
                }

                // Speed up/down with scroll
                if (scrollForSpeed)
                {
                    if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                    {
                        movementSpeed = Mathf.Clamp(movementSpeed - scrollRate, 0, speedLimit);
                    }
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                    {
                        movementSpeed = Mathf.Clamp(movementSpeed + scrollRate, 0, speedLimit);
                    }
                }
                else
                {
                    if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                    {
                        previewDistance = Mathf.Clamp(previewDistance - scrollRate, previewDistanceLimit[0], previewDistanceLimit[1]);
                        UpdatePreviewObject(leftShiftDown);
                    }
                    else if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                    {
                        previewDistance = Mathf.Clamp(previewDistance + scrollRate, previewDistanceLimit[0], previewDistanceLimit[1]);
                        UpdatePreviewObject(leftShiftDown);
                    }
                }

                MouseCameraRotation();

                Vector3 velocity = targetSpeed * (Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * inputDir);
                rb.velocity = velocity;

                if (previewObject != null)
                {
                    if(targetSpeed != 0 || lastEulerAngles != transform.eulerAngles)
                    {
                        UpdatePreviewObject(leftShiftDown);
                    }
                    lastEulerAngles = transform.eulerAngles;

                    if (Input.GetMouseButtonDown(0)) // Object destruction
                    {
                        OnMouseOne();
                    }
                    else if (Input.GetMouseButtonDown(1)) // Object placement
                    {
                        OnMouseTwo();
                    }

                    if (Input.GetMouseButton(0))
                    {
                        destroyHoldTimer -= Time.unscaledDeltaTime;
                        UpdatePreviewObject(leftShiftDown);
                    }
                    else
                    {
                        destroyHoldTimer = 0.75f;
                    }
                    if (Input.GetMouseButton(1))
                    {
                        placeHoldTimer -= Time.unscaledDeltaTime;
                        UpdatePreviewObject(leftShiftDown);
                    }
                    else
                    {
                        placeHoldTimer = 0.75f;
                    }

                    if (Input.GetKey(KeyCode.LeftControl))
                    {
                        if (Input.GetKeyDown(KeyCode.Z)) // Undo
                        {
                            if (undoActions.Count > 0)
                            {
                                object[] undoData = undoObjects[^1];
                                switch (undoActions[^1])
                                {
                                    case UndoAction.Fill:
                                        List<Vector3Int> cells = (List<Vector3Int>)undoData[0];
                                        foreach (Vector3Int cell in cells)
                                        {
                                            DestroyCell(cell);
                                        }
                                        AddRedoAction(UndoAction.Fill, new object[] { cells });
                                        break;
                                    case UndoAction.Clear:
                                        cells = (List<Vector3Int>)undoData[0];
                                        foreach(Vector3Int cell in cells)
                                        {
                                            if (!destroyedBlocks.TryGetValue(cell, out CellInfo cellInfo))
                                            {
                                                destroyedHoles.TryGetValue(cell, out cellInfo);
                                            }
                                            FillCell(cell, prefabDictionary[cellInfo.name], cellInfo);
                                        }
                                        AddRedoAction(UndoAction.Clear, new object[] { cells });
                                        break;
                                    case UndoAction.Instantiate:
                                        GameObject undoGO = (GameObject)undoData[0];
                                        AddRedoAction(UndoAction.Instantiate, new object[] { undoGO.name, undoGO.transform.position, undoGO.transform.localScale, undoGO.transform.eulerAngles, undoGO.GetComponentInChildren<MeshRenderer>().material.color, (int)undoData[1] });
                                        Destroy(undoGO);
                                        break;
                                    case UndoAction.DestroySpawnpoint:
                                        GameObject prefab = prefabDictionary["Spawnpoint"];
                                        GameObject undoSpawnpoint = Instantiate(prefab, (Vector3)undoData[1], Quaternion.Euler((Vector3)undoData[3]));
                                        undoSpawnpoint.name = (string)undoData[0];
                                        undoSpawnpoint.transform.localScale = (Vector3)undoData[2];
                                        Color undoColor = (Color)undoData[4];
                                        MeshRenderer undoRenderer = undoSpawnpoint.GetComponentInChildren<MeshRenderer>();
                                        undoRenderer.material.color = undoColor;
                                        undoRenderer.material.SetColor("_EmissionColor", undoColor);
                                        SpawnpointType spawnType = (SpawnpointType)undoData[5];
                                        switch (spawnType)
                                        {
                                            case SpawnpointType.Players:
                                                undoSpawnpoint.transform.SetParent(PlayerManager.Instance.defaultSpawnParent);
                                                break;
                                            case SpawnpointType.Bots:
                                                undoSpawnpoint.transform.SetParent(TankManager.Instance.spawnParent);
                                                break;
                                            default:
                                                undoSpawnpoint.transform.SetParent(PlayerManager.Instance.teamSpawnParent);
                                                break;
                                        }

                                        AddRedoAction(UndoAction.DestroySpawnpoint, new object[] { undoSpawnpoint, spawnType });
                                        break;
                                }
                                undoActions.RemoveAt(undoActions.Count - 1);
                                undoObjects.RemoveAt(undoObjects.Count - 1);
                            }
                        }
                        else if (Input.GetKeyDown(KeyCode.Y)) // Redo
                        {
                            if (redoActions.Count > 0)
                            {
                                object[] redoData = redoObjects[^1];
                                switch (redoActions[^1])
                                {
                                    case UndoAction.Fill:
                                        List<Vector3Int> cells = (List<Vector3Int>)redoData[0];
                                        foreach (Vector3Int cell in cells)
                                        {
                                            if (!destroyedBlocks.TryGetValue(cell, out CellInfo cellInfo))
                                            {
                                                destroyedHoles.TryGetValue(cell, out cellInfo);
                                            }
                                            FillCell(cell, prefabDictionary[cellInfo.name], cellInfo);
                                        }
                                        AddUndoAction(UndoAction.Fill, new object[] { cells });
                                        break;
                                    case UndoAction.Clear:
                                        cells = (List<Vector3Int>)redoData[0];
                                        foreach (Vector3Int cell in cells)
                                        {
                                            DestroyCell(cell);
                                        }
                                        AddUndoAction(UndoAction.Clear, new object[] { cells });
                                        break;
                                    case UndoAction.Instantiate:
                                        GameObject prefab = prefabDictionary["Spawnpoint"];
                                        GameObject undoSpawnpoint = Instantiate(prefab, (Vector3)redoData[1], Quaternion.Euler((Vector3)redoData[3]));
                                        undoSpawnpoint.name = (string)redoData[0];
                                        undoSpawnpoint.transform.localScale = (Vector3)redoData[2];
                                        Color redoColor = (Color)redoData[4];
                                        MeshRenderer redoRenderer = undoSpawnpoint.GetComponentInChildren<MeshRenderer>();
                                        redoRenderer.material.color = redoColor;
                                        redoRenderer.material.SetColor("_EmissionColor", redoColor);
                                        SpawnpointType spawnType = (SpawnpointType)redoData[5];
                                        switch (spawnType)
                                        {
                                            case SpawnpointType.Players:
                                                undoSpawnpoint.transform.SetParent(PlayerManager.Instance.defaultSpawnParent);
                                                break;
                                            case SpawnpointType.Bots:
                                                undoSpawnpoint.transform.SetParent(TankManager.Instance.spawnParent);
                                                break;
                                            default:
                                                undoSpawnpoint.transform.SetParent(PlayerManager.Instance.teamSpawnParent);
                                                break;
                                        }

                                        AddUndoAction(UndoAction.Instantiate, new object[] { undoSpawnpoint, spawnType });
                                        break;
                                    case UndoAction.DestroySpawnpoint:
                                        GameObject redoGO = (GameObject)redoData[0];
                                        AddUndoAction(UndoAction.DestroySpawnpoint, new object[] { redoGO.name, redoGO.transform.position, redoGO.transform.localScale, redoGO.transform.eulerAngles, redoGO.GetComponentInChildren<MeshRenderer>().material.color, (int)redoData[1] });
                                        Destroy(redoGO);
                                        break;
                                }
                                redoActions.RemoveAt(redoActions.Count - 1);
                                redoObjects.RemoveAt(redoObjects.Count - 1);
                            }
                        }
                    }
                }
            }
            else
            {
                rb.velocity = Vector3.zero;
            }
        }
        else
        {
            rb.velocity = Vector3.zero;

            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.P))
                {
                    ExitPlayMode();
                }
                else if (Input.GetKeyDown(KeyCode.E))
                {
                    SwitchPlayCamera();
                }
            }
        }
    }

    void UpdatePreviewObject(bool leftShiftDown)
    {
        if(previewObject != null)
        {
            if (!leftShiftDown && Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ~ignoreLayers))
            {
                hit.normal = previewObject.name == "Hole" ? -hit.normal : hit.normal.Multiply(previewCollider.bounds.extents);

                hitGridPoint = WorldToGrid(hit.point.Round(2) + hit.normal);

                if (hit.normal == Vector3.forward || hit.normal == Vector3.back)
                {
                    hitUp = Vector3.Cross(hit.normal, Vector3.right);
                }
                else
                {
                    hitUp = Vector3.Cross(hit.normal, Vector3.forward);
                }
                hitRight = Vector3.Cross(hit.normal, hitUp);
            }
            else
            {
                hitGridPoint = WorldToGrid(transform.position + transform.forward * previewDistance);
                hit.normal = transform.forward;
                hitRight = transform.right;
                hitUp = transform.up;
            }

            if(previewObject.name == "Hole")
            {
                if (!previewObject.CompareTag("Spawnpoint"))
                {
                    if (placedHoles.ContainsKey(hitGridPoint))
                    {
                        previewRenderer.material.color = Color.red;
                    }
                    else
                    {
                        previewRenderer.material.color = Color.green;
                    }
                }
                previewObject.position = new Vector3(hitGridPoint.x, hitGridPoint.y + 0.01f, hitGridPoint.z);
            }
            else
            {
                if (!previewObject.CompareTag("Spawnpoint") && !previewObject.CompareTag("Tank"))
                {
                    if (placedBlocks.ContainsKey(hitGridPoint))
                    {
                        previewRenderer.material.color = Color.red;
                    }
                    else
                    {
                        previewRenderer.material.color = Color.green;
                    }
                }
                previewObject.position = hitGridPoint;
            }
        }
    }

    void DestroySolidCircle(Vector3 center, List<Vector3Int> destroyedCells, int evenOffset)
    {
        float radius = brushSize * 0.5f * cellSize.x;
        int offsetRight, offsetUp;
        for (int i = -halfBrushSize; i <= halfBrushSize; i++)
        {
            offsetRight = i * cellSize.x;

            for (int j = -halfBrushSize; j <= halfBrushSize; j++)
            {
                offsetUp = j * cellSize.y;
                Vector3Int gridPosition = WorldToGrid(center + offsetRight * hitRight.ToNormal() + offsetUp * hitUp.ToNormal());
                if (Vector3.Distance(center + (0.5f * evenOffset * (Vector3)cellSize - hit.normal), gridPosition) <= radius)
                {
                    RaycastDestroyCell(gridPosition, hit.normal, destroyedCells);
                }
            }
        }
    }

    void DestroyHollowCircle(Vector3 center, List<Vector3Int> destroyedCells, int evenOffset)
    {
        float radius = brushSize * 0.5f * cellSize.x;
        int offsetRight, offsetUp;
        for (int i = -halfBrushSize; i <= halfBrushSize; i++)
        {
            offsetRight = i * cellSize.x;

            for (int j = -halfBrushSize; j <= halfBrushSize; j++)
            {
                offsetUp = j * cellSize.y;
                Vector3Int gridPosition = WorldToGrid(center + offsetRight * hitRight.ToNormal() + offsetUp * hitUp.ToNormal());
                float dst = Vector3.Distance(center + (0.5f * evenOffset * (Vector3)cellSize - hit.normal), gridPosition);
                if (dst <= radius && dst > radius - cellSize.x)
                {
                    RaycastDestroyCell(gridPosition, hit.normal, destroyedCells);
                }
            }
        }
    }

    void OnMouseOne()
    {
        List<Vector3Int> destroyedCells = new List<Vector3Int>();
        if (brushSize > 1)
        {
            halfBrushSize = brushSize / 2;
            int evenOffset = brushSize % 2 == 0 ? 1 : 0;
            switch (brushType)
            {
                case BrushType.Cube:
                    int x, y, z, i_y;
                    if (hollowBrush)
                    {
                        for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                        {
                            x = hitGridPoint.x + (i * cellSize.x);
                            i_y = hitGridPoint.y + (i * cellSize.y);

                            for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                            {
                                // x side
                                z = hitGridPoint.z + (j * cellSize.z);
                                RaycastDestroyCell(new Vector3Int(hitGridPoint.x - ((halfBrushSize - evenOffset) * cellSize.x), i_y, z), hit.normal, destroyedCells);
                                RaycastDestroyCell(new Vector3Int(hitGridPoint.x + (halfBrushSize * cellSize.x), i_y, z), hit.normal, destroyedCells);

                                // y side
                                y = hitGridPoint.y + (j * cellSize.y);
                                RaycastDestroyCell(new Vector3Int(x, hitGridPoint.y - ((halfBrushSize - evenOffset) * cellSize.y), z), hit.normal, destroyedCells);
                                RaycastDestroyCell(new Vector3Int(x, hitGridPoint.y + (halfBrushSize * cellSize.y), z), hit.normal, destroyedCells);

                                // z side
                                RaycastDestroyCell(new Vector3Int(x, y, hitGridPoint.z - ((halfBrushSize - evenOffset) * cellSize.z)), hit.normal, destroyedCells);
                                RaycastDestroyCell(new Vector3Int(x, y, hitGridPoint.z + (halfBrushSize * cellSize.z)), hit.normal, destroyedCells);
                            }
                        }
                    }
                    else
                    {
                        for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                        {
                            x = hitGridPoint.x + (i * cellSize.x);
                            for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                            {
                                y = hitGridPoint.y + (j * cellSize.y);
                                for (int k = -halfBrushSize + evenOffset; k <= halfBrushSize; k++)
                                {
                                    z = hitGridPoint.z + (k * cellSize.z);
                                    RaycastDestroyCell(new Vector3Int(x, y, z), hit.normal, destroyedCells);
                                }
                            }
                        }
                    }
                    break;
                case BrushType.Sphere:
                    float radius = brushSize * 0.5f * cellSize.x;
                    if (hollowBrush)
                    {
                        for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                        {
                            x = hitGridPoint.x + (i * cellSize.x);
                            for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                            {
                                y = hitGridPoint.y + (j * cellSize.y);
                                for (int k = -halfBrushSize + evenOffset; k <= halfBrushSize; k++)
                                {
                                    z = hitGridPoint.z + (k * cellSize.z);
                                    Vector3Int gridPosition = new Vector3Int(x, y, z);
                                    float dst = Vector3.Distance(hitGridPoint + (0.5f * evenOffset * (Vector3)cellSize), gridPosition);
                                    if (dst <= radius && dst > radius - cellSize.x)
                                    {
                                        RaycastDestroyCell(gridPosition, hit.normal, destroyedCells);
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                        {
                            x = hitGridPoint.x + (i * cellSize.x);
                            for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                            {
                                y = hitGridPoint.y + (j * cellSize.y);
                                for (int k = -halfBrushSize + evenOffset; k <= halfBrushSize; k++)
                                {
                                    z = hitGridPoint.z + (k * cellSize.z);
                                    Vector3Int gridPosition = new Vector3Int(x, y, z);
                                    if (Vector3.Distance(hitGridPoint + (0.5f * evenOffset * (Vector3)cellSize), gridPosition) <= radius)
                                    {
                                        RaycastDestroyCell(gridPosition, hit.normal, destroyedCells);
                                    }
                                }
                            }
                        }
                    }
                    break;
                case BrushType.Cylinder:
                    int negativeHalfBrush = -halfBrushSize + evenOffset;
                    hit.normal = hit.normal.ToNormal();
                    if (hollowBrush)
                    {
                        DestroySolidCircle(hitGridPoint + negativeHalfBrush * cellSize.x * hit.normal, destroyedCells, evenOffset);
                        DestroySolidCircle(hitGridPoint + halfBrushSize * cellSize.x * hit.normal, destroyedCells, evenOffset);
                        for (int i = negativeHalfBrush + 1; i < halfBrushSize; i++)
                        {
                            DestroyHollowCircle(hitGridPoint + i * cellSize.x * hit.normal, destroyedCells, evenOffset);
                        }
                    }
                    else
                    {
                        for (int i = negativeHalfBrush; i <= halfBrushSize; i++)
                        {
                            DestroySolidCircle(hitGridPoint + i * cellSize.x * hit.normal, destroyedCells, evenOffset);
                        }
                    }
                    break;
                case BrushType.Square:
                    int offsetRight, offsetUp;
                    hitRight = hitRight.ToNormal();
                    hitUp = hitUp.ToNormal();
                    if (hollowBrush)
                    {
                        int offset;
                        for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                        {
                            offset = i * cellSize.x;
                            RaycastDestroyCell(WorldToGrid(hitGridPoint + (((cellSize.x * -halfBrushSize + evenOffset) * hitRight) + offset * hitUp)), hit.normal, destroyedCells);
                            RaycastDestroyCell(WorldToGrid(hitGridPoint + ((cellSize.x * halfBrushSize * hitRight) + offset * hitUp)), hit.normal, destroyedCells);

                            RaycastDestroyCell(WorldToGrid(hitGridPoint + (((cellSize.x * -halfBrushSize + evenOffset) * hitUp) + offset * hitRight)), hit.normal, destroyedCells);
                            RaycastDestroyCell(WorldToGrid(hitGridPoint + ((cellSize.x * halfBrushSize * hitUp) + offset * hitRight)), hit.normal, destroyedCells);
                        }
                    }
                    else
                    {
                        for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                        {
                            offsetRight = i * cellSize.x;

                            for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                            {
                                offsetUp = j * cellSize.y;
                                Vector3Int gridPosition = WorldToGrid(hitGridPoint + offsetRight * hitRight + offsetUp * hitUp);
                                RaycastDestroyCell(gridPosition, hit.normal, destroyedCells);
                            }
                        }
                    }
                    break;
                case BrushType.Circle:
                    if (hollowBrush)
                    {
                        DestroyHollowCircle(hitGridPoint, destroyedCells, evenOffset);
                    }
                    else
                    {
                        DestroySolidCircle(hitGridPoint, destroyedCells, evenOffset);
                    }
                    break;
            }
        }
        else if(hit.transform != null && hit.transform.name != "Floor")
        {
            Vector3Int hitCell = WorldToGrid(hit.transform.position);
            if (DestroyCell(hitCell))
                destroyedCells.Add(hitCell);
        }
        else
        {
            if (DestroyCell(hitGridPoint))
                destroyedCells.Add(hitGridPoint);
        }

        if (destroyedCells.Count > 0)
        {
            AddUndoAction(UndoAction.Clear, new object[] { destroyedCells });
        }
    }

    void FillSolidCircle(Vector3 center, List<Vector3Int> filledCells, int evenOffset)
    {
        float radius = brushSize * 0.5f * cellSize.x;
        float offsetRight, offsetUp;
        for (int i = -halfBrushSize; i <= halfBrushSize; i++)
        {
            offsetRight = i * cellSize.x;

            for (int j = -halfBrushSize; j <= halfBrushSize; j++)
            {
                offsetUp = j * cellSize.y;
                Vector3Int gridPosition = WorldToGrid(center + offsetRight * hitRight.ToNormal() + offsetUp * hitUp.ToNormal());
                if (Vector3.Distance(center + (0.5f * evenOffset * (Vector3)cellSize - hit.normal), gridPosition) <= radius)
                {
                    RaycastFillCell(gridPosition, filledCells);
                }
            }
        }
    }

    void FillHollowCircle(Vector3 center, List<Vector3Int> filledCells, int evenOffset)
    {
        float radius = brushSize * 0.5f * cellSize.x;
        int offsetRight, offsetUp;
        for (int i = -halfBrushSize; i <= halfBrushSize; i++)
        {
            offsetRight = i * cellSize.x;

            for (int j = -halfBrushSize; j <= halfBrushSize; j++)
            {
                offsetUp = j * cellSize.y;
                Vector3Int gridPosition = WorldToGrid(center + offsetRight * hitRight.ToNormal() + offsetUp * hitUp.ToNormal());
                float dst = Vector3.Distance(center + (0.5f * evenOffset * (Vector3)cellSize - hit.normal), gridPosition);
                if (dst <= radius && dst > radius - cellSize.x)
                {
                    RaycastFillCell(gridPosition, filledCells);
                }
            }
        }
    }

    void OnMouseTwo()
    {
        if (previewObject.CompareTag("Spawnpoint"))
        {
            if (!placedSpawnpoints.ContainsKey(hitGridPoint))
            {
                GameObject newSpawnpoint = Instantiate(prefabDictionary[previewObject.name], hitGridPoint, Quaternion.identity);
                switch (spawnpointType)
                {
                    case SpawnpointType.Players:
                        newSpawnpoint.transform.SetParent(PlayerManager.Instance.defaultSpawnParent);
                        break;
                    case SpawnpointType.Bots:
                        newSpawnpoint.transform.SetParent(TankManager.Instance.spawnParent);
                        break;
                    default:
                        newSpawnpoint.transform.SetParent(PlayerManager.Instance.teamSpawnParent);
                        break;
                }
                newSpawnpoint.transform.rotation = previewObject.rotation;
                newSpawnpoint.name = spawnpointType.ToString();
                newSpawnpoint.transform.localScale = objectScale;
                MeshRenderer newRenderer = newSpawnpoint.GetComponentInChildren<MeshRenderer>();
                newRenderer.material.color = previewRenderer.material.color;
                newRenderer.material.SetColor("_EmissionColor", previewRenderer.material.color);
                placedSpawnpoints.Add(hitGridPoint, newSpawnpoint);

                AddUndoAction(UndoAction.Instantiate, new object[] { newSpawnpoint, spawnpointType, previewObject.eulerAngles });
            }
        }
        else
        {
            List<Vector3Int> filledCells = new List<Vector3Int>();
            if (brushSize > 1)
            {
                halfBrushSize = brushSize / 2;
                int evenOffset = brushSize % 2 == 0 ? 1 : 0;
                switch (brushType)
                {
                    case BrushType.Cube:
                        int x, y, z, i_y;
                        if (hollowBrush)
                        {
                            for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                            {
                                x = hitGridPoint.x + (i * cellSize.x);
                                i_y = hitGridPoint.y + (i * cellSize.y);

                                for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                                {
                                    // x side
                                    z = hitGridPoint.z + (j * cellSize.z);
                                    RaycastFillCell(new Vector3Int(hitGridPoint.x - ((halfBrushSize - evenOffset) * cellSize.x), i_y, z), filledCells);
                                    RaycastFillCell(new Vector3Int(hitGridPoint.x + (halfBrushSize * cellSize.x), i_y, z), filledCells);

                                    // y side
                                    y = hitGridPoint.y + (j * cellSize.y);
                                    RaycastFillCell(new Vector3Int(x, hitGridPoint.y - ((halfBrushSize - evenOffset) * cellSize.y), z), filledCells);
                                    RaycastFillCell(new Vector3Int(x, hitGridPoint.y + (halfBrushSize * cellSize.y), z), filledCells);

                                    // z side
                                    RaycastFillCell(new Vector3Int(x, y, hitGridPoint.z - ((halfBrushSize - evenOffset) * cellSize.z)), filledCells);
                                    RaycastFillCell(new Vector3Int(x, y, hitGridPoint.z + (halfBrushSize * cellSize.z)), filledCells);
                                }
                            }
                        }
                        else
                        {
                            for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                            {
                                x = hitGridPoint.x + (i * cellSize.x);
                                for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                                {
                                    y = hitGridPoint.y + (j * cellSize.y);
                                    for (int k = -halfBrushSize + evenOffset; k <= halfBrushSize; k++)
                                    {
                                        z = hitGridPoint.z + (k * cellSize.z);
                                        RaycastFillCell(new Vector3Int(x, y, z), filledCells);
                                    }
                                }
                            }
                        }
                        break;
                    case BrushType.Sphere:
                        float radius = brushSize * 0.5f * cellSize.x;
                        if (hollowBrush)
                        {
                            for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                            {
                                x = hitGridPoint.x + (i * cellSize.x);
                                for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                                {
                                    y = hitGridPoint.y + (j * cellSize.y);
                                    for (int k = -halfBrushSize + evenOffset; k <= halfBrushSize; k++)
                                    {
                                        z = hitGridPoint.z + (k * cellSize.z);
                                        Vector3Int gridPosition = new Vector3Int(x, y, z);
                                        float dst = Vector3.Distance(hitGridPoint + (0.5f * evenOffset * (Vector3)cellSize), gridPosition);
                                        if (dst <= radius && dst >= radius - cellSize.x)
                                        {
                                            RaycastFillCell(gridPosition, filledCells);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                            {
                                x = hitGridPoint.x + (i * cellSize.x);
                                for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                                {
                                    y = hitGridPoint.y + (j * cellSize.y);
                                    for (int k = -halfBrushSize + evenOffset; k <= halfBrushSize; k++)
                                    {
                                        z = hitGridPoint.z + (k * cellSize.z);
                                        Vector3Int gridPosition = new Vector3Int(x, y, z);
                                        if (Vector3.Distance(hitGridPoint + (0.5f * evenOffset * (Vector3)cellSize), gridPosition) <= radius)
                                        {
                                            RaycastFillCell(gridPosition, filledCells);
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case BrushType.Cylinder:
                        int negativeHalfBrush = -halfBrushSize + evenOffset;
                        hit.normal = hit.normal.ToNormal();
                        if (hollowBrush)
                        {
                            FillSolidCircle(hitGridPoint + negativeHalfBrush * cellSize.x * hit.normal, filledCells, evenOffset);
                            FillSolidCircle(hitGridPoint + halfBrushSize * cellSize.x * hit.normal, filledCells, evenOffset);
                            for (int i = negativeHalfBrush + 1; i < halfBrushSize; i++)
                            {
                                FillHollowCircle(hitGridPoint + i * cellSize.x * hit.normal, filledCells, evenOffset);
                            }
                        }
                        else
                        {
                            for (int i = negativeHalfBrush; i <= halfBrushSize; i++)
                            {
                                FillSolidCircle(hitGridPoint + i * cellSize.x * hit.normal, filledCells, evenOffset);
                            }
                        }
                        break;
                    case BrushType.Square:
                        int offsetRight, offsetUp;
                        hitRight = hitRight.ToNormal();
                        hitUp = hitUp.ToNormal();
                        if (hollowBrush)
                        {
                            int offset;
                            for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                            {
                                offset = i * cellSize.x;
                                RaycastFillCell(WorldToGrid(hitGridPoint + (((cellSize.x * -halfBrushSize + evenOffset) * hitRight) + offset * hitUp)), filledCells);
                                RaycastFillCell(WorldToGrid(hitGridPoint + ((cellSize.x * halfBrushSize * hitRight) + offset * hitUp)), filledCells);

                                RaycastFillCell(WorldToGrid(hitGridPoint + (((cellSize.x * -halfBrushSize + evenOffset) * hitUp) + offset * hitRight)), filledCells);
                                RaycastFillCell(WorldToGrid(hitGridPoint + ((cellSize.x * halfBrushSize * hitUp) + offset * hitRight)), filledCells);
                            }
                        }
                        else
                        {
                            for (int i = -halfBrushSize + evenOffset; i <= halfBrushSize; i++)
                            {
                                offsetRight = i * cellSize.x;

                                for (int j = -halfBrushSize + evenOffset; j <= halfBrushSize; j++)
                                {
                                    offsetUp = j * cellSize.y;
                                    Vector3Int gridPosition = WorldToGrid(hitGridPoint + offsetRight * hitRight + offsetUp * hitUp);
                                    RaycastFillCell(gridPosition, filledCells);
                                }
                            }
                        }
                        break;
                    case BrushType.Circle:
                        if (hollowBrush)
                        {
                            FillHollowCircle(hitGridPoint, filledCells, evenOffset);
                        }
                        else
                        {
                            FillSolidCircle(hitGridPoint, filledCells, evenOffset);
                        }
                        break;
                }
            }
            else
            {
                RaycastFillCell(hitGridPoint, filledCells);
            }

            if (filledCells.Count > 0)
            {
                AddUndoAction(UndoAction.Fill, new object[] { filledCells });
            }
        }
    }

    void MouseHoldLoop()
    {
        if (destroyHoldTimer <= 0)
        {
            OnMouseOne();
        }
        if (placeHoldTimer <= 0)
        {
            OnMouseTwo();
        }
    } 

    private Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector3Int(Mathf.FloorToInt(worldPosition.x / cellSize.x) * cellSize.x, Mathf.FloorToInt(worldPosition.y / cellSize.y) * cellSize.y, Mathf.FloorToInt(worldPosition.z / cellSize.z) * cellSize.z) + (cellSize / 2);
    }

    void RaycastDestroyCell(Vector3Int gridPosition, Vector3 normal, List<Vector3Int> destroyedCells)
    {
        gridPosition -= normal.FloorToInt() * cellSize;
        if (previewObject.CompareTag("Spawnpoint"))
        {
            DestroySpawnpoint(gridPosition);
        }
        else
        {
            if (DestroyCell(gridPosition))
                destroyedCells.Add(gridPosition);
        }
    }

    void RaycastFillCell(Vector3Int gridPosition, List<Vector3Int> filledCells)
    {
        if(FillCell(gridPosition, prefabDictionary[previewObject.name]))
        {
            filledCells.Add(gridPosition);
        }
    }

    void DestroySpawnpoint(Vector3Int gridPosition)
    {
        if (placedSpawnpoints.TryGetValue(gridPosition, out GameObject placedCollider))
        {
            Destroy(placedCollider);
            placedSpawnpoints.Remove(gridPosition);
        }
    }

    private bool DestroyCell(Vector3Int gridPosition)
    {
        if(placedHoles.TryGetValue(gridPosition, out GameObject placedHole))
        {
            destroyedHoles.Add(gridPosition, new CellInfo() { name = placedHole.name, eulerAngles = placedHole.transform.eulerAngles, scale = placedHole.transform.localScale });
            Destroy(placedHole);
            placedHoles.Remove(gridPosition);
            return true;
        }
        else if(placedBlocks.TryGetValue(gridPosition, out GameObject placedBlock))
        {
            if (placedBlock.GetComponentInChildren<Rigidbody>() != null && placedBlock.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
            {
                dynamicObjects.Remove(saveableLevelObject);
            }
            if (placedBlock.TryGetComponent<DestructableObject>(out var destructableScript))
            {
                destructables.Remove(destructableScript);
            }

            destroyedBlocks.Add(gridPosition, new CellInfo() { name = placedBlock.name, eulerAngles = placedBlock.transform.eulerAngles, scale = placedBlock.transform.localScale });
            Destroy(placedBlock);
            placedBlocks.Remove(gridPosition);
            return true;
        }

        return false;
    }

    private bool FillCell(Vector3Int gridPosition, GameObject withObject)
    {
        if(withObject.name == "CylinderHole" || withObject.name == "Hole")
        {
            if (!placedHoles.ContainsKey(gridPosition))
            {
                GameObject newObject = Instantiate(withObject, new Vector3(gridPosition.x, gridPosition.y + 0.01f, gridPosition.z), Quaternion.identity);
                newObject.transform.rotation = previewObject.rotation;
                newObject.transform.localScale = objectScale;
                if(newObject.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
                {
                    newObject.name = GameManager.Instance.editorNames[saveableLevelObject.prefabIndex];
                }
                placedHoles.Add(gridPosition, newObject);
                destroyedHoles.Remove(gridPosition);
                return true;
            }
        }
        else
        {
            if (!placedBlocks.ContainsKey(gridPosition))
            {
                GameObject newObject = Instantiate(withObject, gridPosition, Quaternion.identity);
                newObject.transform.rotation = previewObject.rotation;
                newObject.transform.localScale = objectScale;
                if (newObject.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
                {
                    newObject.name = GameManager.Instance.editorNames[saveableLevelObject.prefabIndex];
                }
                Rigidbody newRigidbody = newObject.GetComponentInChildren<Rigidbody>();
                if (newRigidbody != null)
                {
                    if (!GameManager.Instance.playMode)
                        newRigidbody.isKinematic = true;
                    dynamicObjects.Add(saveableLevelObject);
                }
                if (newObject.TryGetComponent<DestructableObject>(out var destructableScript))
                {
                    destructables.Add(destructableScript);
                }

                placedBlocks.Add(gridPosition, newObject);
                destroyedBlocks.Remove(gridPosition);
                return true;
            }
        }

        return false;
    }

    private bool FillCell(Vector3Int gridPosition, GameObject withObject, CellInfo cellInfo)
    {
        if (withObject.name == "CylinderHole" || withObject.name == "Hole")
        {
            if (!placedHoles.ContainsKey(gridPosition))
            {
                GameObject newObject = Instantiate(withObject, new Vector3(gridPosition.x, gridPosition.y + 0.01f, gridPosition.z), Quaternion.identity);
                newObject.transform.eulerAngles = cellInfo.eulerAngles;
                newObject.transform.localScale = cellInfo.scale;
                if (newObject.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
                {
                    newObject.name = GameManager.Instance.editorNames[saveableLevelObject.prefabIndex];
                }
                placedHoles.Add(gridPosition, newObject);
                destroyedHoles.Remove(gridPosition);
                return true;
            }
        }
        else
        {
            if (!placedBlocks.ContainsKey(gridPosition))
            {
                GameObject newObject = Instantiate(withObject, gridPosition, Quaternion.identity);
                if (newObject.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
                {
                    newObject.name = GameManager.Instance.editorNames[saveableLevelObject.prefabIndex];
                }
                newObject.transform.eulerAngles = cellInfo.eulerAngles;
                newObject.transform.localScale = cellInfo.scale;
                Rigidbody newRigidbody = newObject.GetComponentInChildren<Rigidbody>();
                if (newRigidbody != null)
                {
                    if (!GameManager.Instance.playMode)
                        newRigidbody.isKinematic = true;
                    dynamicObjects.Add(saveableLevelObject);
                }
                if (newObject.TryGetComponent<DestructableObject>(out var destructableScript))
                {
                    destructables.Add(destructableScript);
                }

                placedBlocks.Add(gridPosition, newObject);
                destroyedBlocks.Remove(gridPosition);
                return true;
            }
        }

        return false;
    }

    private float GetInputAxis(string axis)
    {
        switch(axis)
        {
            case "x":
                float x = 0;
                if(Input.GetKey(DataManager.playerSettings.keyBinds["Right"]))
                {
                    x += 1;
                }
                if(Input.GetKey(DataManager.playerSettings.keyBinds["Left"]))
                {
                    x -= 1;
                }
                return x;
            case "y":
                float y = 0;
                if(Input.GetKey(DataManager.playerSettings.keyBinds["Up"]))
                {
                    y += 1;
                }
                if(Input.GetKey(DataManager.playerSettings.keyBinds["Down"]))
                {
                    y -= 1;
                }
                return y;
            case "z":
                float z = 0;
                if(Input.GetKey(DataManager.playerSettings.keyBinds["Forward"]))
                {
                    z += 1;
                }
                if(Input.GetKey(DataManager.playerSettings.keyBinds["Backward"]))
                {
                    z -= 1;
                }
                return z;
        }

        return 0;
    }

    private void MouseCameraRotation()
    {
        // Translating inputs from mouse into smoothed rotation of camera
        yaw += Input.GetAxis("Mouse X") * DataManager.playerSettings.sensitivity / 4;
        pitch -= Input.GetAxis("Mouse Y") * DataManager.playerSettings.sensitivity / 4;
        pitch = Mathf.Clamp(pitch, -90, 90);

        if (DataManager.playerSettings.cameraSmoothing)
        {
            currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, rotationSmoothing);
        }
        else
        {
            currentRotation = new Vector3(pitch, yaw, currentRotation.z);
        }
        // Setting rotation and position of camera on previous params and target and dstFromTarget
        transform.eulerAngles = currentRotation;
    }

    void AddUndoAction(UndoAction action, object[] undoData)
    {
        undoActions.Add(action);
        undoObjects.Add(undoData);
        if(undoActions.Count > 50)
        {
            undoActions.RemoveAt(0);
            undoObjects.RemoveAt(0);
        }
    }

    void AddRedoAction(UndoAction action, object[] redoData)
    {
        redoActions.Add(action);
        redoObjects.Add(redoData);
        if(redoActions.Count > 25)
        {
            redoActions.RemoveAt(0);
            redoObjects.RemoveAt(0);
        }
    }

    public void Resume(bool changeCursor = true)
    {
        foreach(Transform element in baseUI.UIElements.Values)
        {
            element.gameObject.SetActive(false);
        }

        GameManager.Instance.paused = false;
        if (changeCursor)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    public void Pause()
    {
        GameManager.Instance.paused = true;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Leave()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void RefreshLevelSlots()
    {
        foreach(RectTransform child in levelSlotContainer)
        {
            if(child != levelSlotTemplate)
            {
                Destroy(child.gameObject);
            }
        }

        IEnumerable<string> allSaveFiles = SaveSystem.FilesInSaveFolder(false, ".level");
        foreach(string fileName in allSaveFiles)
        {
            InstantiateLevelSlot(fileName);
        }
    }

    void InstantiateLevelSlot(string fileName)
    {
        RectTransform newLevelSlot = Instantiate(levelSlotTemplate, levelSlotContainer);
        newLevelSlot.name = newLevelSlot.Find("Label").GetComponent<TextMeshProUGUI>().text = fileName;
        newLevelSlot.gameObject.SetActive(true);
    }

    public void SelectLevelSlot(Button levelSlot)
    {
        selectedLevelSlot = levelSlot.name;
    }

    public void Save()
    {
        if (placedSpawnpoints.Count > 0)
        {
            SaveSystem.SaveLevel(levelName, levelDescription, levelCreators, FindObjectsOfType<SaveableLevelObject>());
            Resume();
        }
        else
        {
            errorMessage.text = "Failed to save. Place at least one player spawnpoint";
            errorMessage.gameObject.SetActive(true);
            Invoke(nameof(HideErrorMessage), 2.5f);
        }
    }

    void HideErrorMessage()
    {
        errorMessage.gameObject.SetActive(false);
    }

    public void Load()
    {
        if(selectedLevelSlot != null)
        {
            foreach (Vector3Int hole in placedHoles.Keys.ToList())
            {
                DestroyCell(hole);
            }
            foreach (Vector3Int block in placedBlocks.Keys.ToList())
            {
                DestroyCell(block);
            }
            foreach(Vector3Int spawnpoint in placedSpawnpoints.Keys.ToList())
            {
                DestroySpawnpoint(spawnpoint);
            }
            destroyedBlocks.Clear();
            destroyedHoles.Clear();
            undoActions.Clear();
            undoObjects.Clear();
            redoActions.Clear();
            redoObjects.Clear();
            destructables.Clear();
            dynamicObjects.Clear();

            LevelInfo levelInfo = SaveSystem.LoadLevel(selectedLevelSlot);
            levelName = levelInfo.name;
            levelDescription = levelInfo.description;
            levelCreators = levelInfo.creators;
            baseUI.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\nEditing " + levelName;

            foreach (LevelObjectInfo levelObjectInfo in levelInfo.levelObjects)
            {
                Vector3 levelObjectPosition = new Vector3(levelObjectInfo.posX, levelObjectInfo.posY, levelObjectInfo.posZ);
                GameObject levelObject = Instantiate(GameManager.Instance.editorPrefabs[levelObjectInfo.prefabIndex], levelObjectPosition, Quaternion.Euler(new Vector3(levelObjectInfo.eulerX, levelObjectInfo.eulerY, levelObjectInfo.eulerZ)));
                levelObject.transform.localScale = new Vector3(levelObjectInfo.scaleX, levelObjectInfo.scaleY, levelObjectInfo.scaleZ);
                levelObject.name = levelObjectInfo.name;
                levelObject.tag = levelObjectInfo.tag;
                levelObject.layer = levelObjectInfo.layer;
                if (levelObjectInfo.tag == "Spawnpoint")
                {
                    MeshRenderer levelObjectRenderer = levelObject.GetComponent<MeshRenderer>();
                    Color savedColor = new Color(levelObjectInfo.colorR, levelObjectInfo.colorG, levelObjectInfo.colorB, levelObjectInfo.colorA);
                    levelObjectRenderer.material.color = savedColor;
                    levelObjectRenderer.material.SetColor("_EmissionColor", savedColor);
                    switch (levelObjectInfo.spawnType)
                    {
                        case 0:
                            levelObject.transform.SetParent(PlayerManager.Instance.defaultSpawnParent);
                            break;
                        case 1:
                            levelObject.transform.SetParent(TankManager.Instance.spawnParent);
                            break;
                        case 2:
                            levelObject.transform.SetParent(PlayerManager.Instance.teamSpawnParent);
                            break;
                    }

                    placedSpawnpoints.Add(WorldToGrid(levelObjectPosition), levelObject);
                }
                else
                {
                    if(levelObjectInfo.name == "CylinderHole" || levelObjectInfo.name == "Hole")
                    {
                        placedHoles.Add(WorldToGrid(levelObjectPosition), levelObject);
                    }
                    else
                    {
                        if (levelObject.TryGetComponent<DestructableObject>(out var destructableScript))
                        {
                            destructables.Add(destructableScript);
                        }
                        Rigidbody attachedRigidbody = levelObject.GetComponentInChildren<Rigidbody>();
                        if (attachedRigidbody != null)
                        {
                            if (!GameManager.Instance.playMode)
                                attachedRigidbody.isKinematic = true;
                            if (attachedRigidbody.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
                            {
                                dynamicObjects.Add(saveableLevelObject);
                            }
                        }
                        placedBlocks.Add(WorldToGrid(levelObjectPosition), levelObject);
                    }
                }
            }
            Resume();
        }
    }

    public void SetLevelName(TMP_InputField inputField)
    {
        levelName = inputField.text;
    }

    public void SetLevelDescription(TMP_InputField inputField)
    {
        levelDescription = inputField.text;
    }

    public void SetLevelCreators(TMP_InputField inputField)
    {
        levelCreators = inputField.text;
    }

    public void SetPreviewObject(Transform button)
    {
        if (previewObject != null)
        {
            Destroy(previewObject.gameObject);
        }
        previewObject = Instantiate(prefabDictionary[button.name], Vector3.zero, Quaternion.identity).transform;
        previewObject.name = button.name;
        previewObject.eulerAngles = objectRotation;

        if (previewObject.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
        {
            previewRenderer = saveableLevelObject.thisRenderer;
            previewCollider = saveableLevelObject.thisCollider;
            Destroy(saveableLevelObject);
        }
        else
        {
            previewRenderer = previewObject.GetComponentInChildren<MeshRenderer>();

            if (previewRenderer.TryGetComponent<Collider>(out var collider))
            {
                previewCollider = collider;
            }
            else
            {
                previewCollider = previewObject.GetComponentInChildren<Collider>();
            }
        }
        if(previewCollider.TryGetComponent<MeshCollider>(out var meshCollider))
        {
            meshCollider.convex = true;
        }
        previewCollider.isTrigger = true;

        Rigidbody previewRigidbody = previewObject.GetComponentInChildren<Rigidbody>();
        if (previewRigidbody != null)
        {
            previewRigidbody.isKinematic = true;
        }

        if (previewRenderer != null)
        {
            if(!previewObject.CompareTag("Spawnpoint"))
                previewRenderer.material.color = Color.green;
            previewRenderer.gameObject.layer = 2;
        }

        if (previewObject.CompareTag("Tank"))
        {
            foreach (Transform child in previewObject)
            {
                child.gameObject.layer = 2;
            }
        }
        else
        {
            previewObject.localScale = objectScale;
            if (previewObject.CompareTag("Spawnpoint"))
            {
                switch (spawnpointType)
                {
                    case SpawnpointType.Players:
                        previewRenderer.material.color = Color.magenta;
                        previewRenderer.material.SetColor("_EmissionColor", Color.magenta);
                        break;
                    case SpawnpointType.Bots:
                        previewRenderer.material.color = Color.cyan;
                        previewRenderer.material.SetColor("_EmissionColor", Color.cyan);
                        break;
                    case SpawnpointType.Team1:
                        previewRenderer.material.color = Color.red;
                        previewRenderer.material.SetColor("_EmissionColor", Color.red);
                        break;
                    case SpawnpointType.Team2:
                        previewRenderer.material.color = Color.green;
                        previewRenderer.material.SetColor("_EmissionColor", Color.green);
                        break;
                    case SpawnpointType.Team3:
                        previewRenderer.material.color = Color.blue;
                        previewRenderer.material.SetColor("_EmissionColor", Color.blue);
                        break;
                    case SpawnpointType.Team4:
                        previewRenderer.material.color = Color.yellow;
                        previewRenderer.material.SetColor("_EmissionColor", Color.yellow);
                        break;
                }
            }
        }
        UpdatePreviewObject(false);
    }

    public void ChangeBrushType(Button button)
    {
        switch (button.name)
        {
            case "Cube":
                brushType = BrushType.Cube;
                break;
            case "Sphere":
                brushType = BrushType.Sphere;
                break;
            case "Cylinder":
                brushType = BrushType.Cylinder;
                break;
            case "Square":
                brushType = BrushType.Square;
                break;
            case "Circle":
                brushType = BrushType.Circle;
                break;
        }
    }

    public void SetBrushSize(TMP_InputField inputField)
    {
        int.TryParse(inputField.text, out int result);
        if(result < 1)
        {
            inputField.SetTextWithoutNotify("1");
            brushSize = 1;
        }
        else
        {
            brushSize = result;
        }
    }

    public void ToggleHollowBrush(Toggle toggle)
    {
        hollowBrush = toggle.isOn;
    }

    public void SetSpawnpointType(TMP_Dropdown dropdown)
    {
        switch (dropdown.value)
        {
            case 0:
                spawnpointType = SpawnpointType.Players;
                previewRenderer.material.color = Color.magenta;
                previewRenderer.material.SetColor("_EmissionColor", Color.magenta);
                break;
            case 1:
                spawnpointType = SpawnpointType.Bots;
                previewRenderer.material.color = Color.cyan;
                previewRenderer.material.SetColor("_EmissionColor", Color.cyan);
                break;
            case 2:
                spawnpointType = SpawnpointType.Team1;
                previewRenderer.material.color = Color.red;
                previewRenderer.material.SetColor("_EmissionColor", Color.red);
                break;
            case 3:
                spawnpointType = SpawnpointType.Team2;
                previewRenderer.material.color = Color.green;
                previewRenderer.material.SetColor("_EmissionColor", Color.green);
                break;
            case 4:
                spawnpointType = SpawnpointType.Team3;
                previewRenderer.material.color = Color.blue;
                previewRenderer.material.SetColor("_EmissionColor", Color.blue);
                break;
            case 5:
                spawnpointType = SpawnpointType.Team4;
                previewRenderer.material.color = Color.yellow;
                previewRenderer.material.SetColor("_EmissionColor", Color.yellow);
                break;
        }
    }

    void UpdateObjectScale()
    {
        if (previewObject != null && !previewObject.CompareTag("Tank"))
        {
            previewObject.localScale = objectScale;
        }
        cellSize = Vector3Int.FloorToInt(objectScale);
    }

    public void SetObjectScaleX(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            objectScale.x = 2;
        }
        else
        {
            float.TryParse(inputField.text, out objectScale.x);
            objectScale.x = Mathf.Clamp(objectScale.x, 0f, 100f);
            inputField.SetTextWithoutNotify(objectScale.x.ToString());
        }
        UpdateObjectScale();
    }

    public void SetObjectScaleY(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            objectScale.y = 2;
        }
        else
        {
            float.TryParse(inputField.text, out objectScale.y);
            objectScale.y = Mathf.Clamp(objectScale.y, 0f, 100f);
            inputField.SetTextWithoutNotify(objectScale.y.ToString());
        }
        UpdateObjectScale();
    }

    public void SetObjectScaleZ(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            objectScale.z = 2;
        }
        else
        {
            float.TryParse(inputField.text, out objectScale.z);
            objectScale.z = Mathf.Clamp(objectScale.z, 0f, 100f);
            inputField.SetTextWithoutNotify(objectScale.z.ToString());
        }
        UpdateObjectScale();
    }

    void UpdateRotation()
    {
        if (previewObject != null && !previewObject.CompareTag("Tank"))
        {
            previewObject.eulerAngles = objectRotation;
        }
    }

    public void SetRotationX(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            objectRotation.x = 0;
        }
        else
        {
            float.TryParse(inputField.text, out objectRotation.x);
            objectRotation.x = Mathf.Clamp(objectRotation.x, -180f, 180f);
            inputField.SetTextWithoutNotify(objectRotation.x.ToString());
        }
        UpdateRotation();
    }

    public void SetRotationY(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            objectRotation.y = 0;
        }
        else
        {
            float.TryParse(inputField.text, out objectRotation.y);
            objectRotation.y = Mathf.Clamp(objectRotation.y, -180f, 180f);
            inputField.SetTextWithoutNotify(objectRotation.y.ToString());
        }
        UpdateRotation();
    }

    public void SetRotationZ(TMP_InputField inputField)
    {
        if (string.IsNullOrEmpty(inputField.text))
        {
            objectRotation.z = 0;
        }
        else
        {
            float.TryParse(inputField.text, out objectRotation.z);
            objectRotation.z = Mathf.Clamp(objectRotation.z, -180f, 180f);
            inputField.SetTextWithoutNotify(objectRotation.z.ToString());
        }
        UpdateRotation();
    }

    public void ChangePlayMode(TMP_Dropdown dropdown)
    {
        DataManager.roomSettings.mode = dropdown.options[dropdown.value].text;
    }

    void ResetLevelObjects()
    {
        foreach(DestructableObject destructable in destructables)
        {
            destructable.solidObject.SetActive(true);
        }
        foreach(SaveableLevelObject dynamicObject in dynamicObjects)
        {
            Rigidbody dynamicRigidbody = dynamicObject.GetComponentInChildren<Rigidbody>();
            if(dynamicRigidbody != null)
            {
                dynamicRigidbody.isKinematic = true;
            }
            dynamicObject.ResetTransform();
        }
    }

    public void EnterPlayMode()
    {
        GameManager.Instance.playMode = true;
        editCamera.enabled = GetComponent<AudioListener>().enabled = false;
        if (previewObject != null)
        {
            previewObject.gameObject.SetActive(false);
        }
        Resume();

        SaveableLevelObject[] saveableLevelObjects = FindObjectsOfType<SaveableLevelObject>();
        foreach(SaveableLevelObject saveableLevelObject in saveableLevelObjects)
        {
            Rigidbody thisRigidbody = saveableLevelObject.GetComponentInChildren<Rigidbody>();
            if (thisRigidbody != null)
                thisRigidbody.isKinematic = false;
            if (saveableLevelObject.thisCollider != null)
                saveableLevelObject.thisCollider.enabled = true;
        }
        PlayerManager.Instance.Init();
        PhotonTeam team;
        switch (DataManager.roomSettings.mode)
        {
            case "Teams":
                PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Team 1");
                PhotonTeamsManager.Instance.TryGetTeamByName("Team 1", out team);
                break;
            default:
                PhotonNetwork.LocalPlayer.JoinOrSwitchTeam("Players");
                PhotonTeamsManager.Instance.TryGetTeamByName("Players", out team);
                break;
        }
        player = PlayerManager.Instance.SpawnInLocalPlayer(team);

        playCamera = player.transform.Find("Camera").GetComponent<Camera>();
        TankManager.Instance.Init();
        BoostGenerator.Instance.Init();
        GameManager.Instance.frozen = false;
    }

    public void ExitPlayMode()
    {
        GameManager.Instance.playMode = false;
        editCamera.enabled = true;
        if (previewObject != null)
        {
            previewObject.gameObject.SetActive(true);
        }
        GameManager.Instance.frozen = true;
        TankManager.Instance.ResetTanks();
        BoostGenerator.Instance.ResetBoosts();
        ResetLevelObjects();

        if (PhotonNetwork.OfflineMode)
        {
            Destroy(player);
        }
        else
        {
            PhotonNetwork.Destroy(player);
        }
    }

    public void SwitchPlayCamera()
    {
        if (editCamera.enabled)
        {
            editCamera.enabled = GetComponent<AudioListener>().enabled = false;
            playCamera.gameObject.SetActive(true);
            if (previewObject != null)
            {
                previewObject.gameObject.SetActive(false);
            }
            player.GetComponent<PlayerControl>().enabled = true;
            player.transform.Find("Player UI").gameObject.SetActive(true);
            GameManager.Instance.frozen = false;
        }
        else
        {
            editCamera.enabled = GetComponent<AudioListener>().enabled = true;
            playCamera.gameObject.SetActive(false);
            if (previewObject != null)
            {
                previewObject.gameObject.SetActive(true);
            }
            player.GetComponent<PlayerControl>().enabled = false;
            player.transform.Find("Player UI").gameObject.SetActive(false);
            GameManager.Instance.frozen = true;
        }
    }
}
