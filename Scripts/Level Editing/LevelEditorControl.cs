using MyUnityAddons.Calculations;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Photon.Pun;
using UnityEngine.UI;
using System.Linq;

public class LevelEditorControl : MonoBehaviour
{
    [SerializeField] Rigidbody rb;
    BaseUIHandler baseUIHandler;

    string levelName = "Custom";
    string levelDescription = "A custom level.";
    string levelCreators = "";
    [SerializeField] RectTransform levelSlotTemplate;
    [SerializeField] RectTransform levelSlotContainer;
    string selectedLevelSlot;

    bool scrollForSpeed = true;
    [SerializeField] float movementSpeed = 6;
    [SerializeField] float speedLimit = 100;

    Vector3 rotationSmoothVelocity;
    Vector3 currentRotation;

    float yaw;
    float pitch;

    [SerializeField] float previewDistance = 20;
    [SerializeField] float[] previewDistanceLimit = { 2, 80 };

    [SerializeField] LayerMask ignoreLayers;
    [SerializeField] Transform previewObject;
    [SerializeField] Color previewColor = Color.yellow;
    Collider previewCollider;
    MeshRenderer previewRenderer;
    [SerializeField] Vector3Int cellSize;
    public int brushSize = 0;

    [SerializeField] List<string> prefabKeys = new List<string>();
    [SerializeField] List<GameObject> prefabValues = new List<GameObject>();
    Dictionary<string, GameObject> prefabDictionary = new Dictionary<string, GameObject>();

    Dictionary<Vector3Int, GameObject> placedBlocks = new Dictionary<Vector3Int, GameObject>();
    Dictionary<Vector3Int, string> destroyedBlocks = new Dictionary<Vector3Int, string>();

    enum UndoAction
    {
        Fill,
        Clear,
    }

    List<UndoAction> undoActions = new List<UndoAction>();
    List<object[]> undoObjects = new List<object[]>();

    List<UndoAction> redoActions = new List<UndoAction>();
    List<object[]> redoObjects = new List<object[]>();

    public bool Paused { get; set; }

    private void Start()
    {
        baseUIHandler = GetComponentInChildren<BaseUIHandler>();
        baseUIHandler.UIElements["PauseMenu"].Find("LabelBackground").GetChild(0).GetComponent<Text>().text = "Paused\nEditing " + GameManager.Instance.currentScene.name;

        levelSlotTemplate.gameObject.SetActive(false);

        levelDescription = "A custom level by " + PhotonNetwork.NickName + ".";

        for(int i = 0; i < prefabKeys.Count; i++)
        {
            prefabDictionary.Add(prefabKeys[i], prefabValues[i]);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Escape))
        {
            if(baseUIHandler.UIElements["PauseMenu"].gameObject.activeSelf || baseUIHandler.UIElements["EditorMenu"].gameObject.activeSelf)
            {
                Resume();
            }
            else
            {
                baseUIHandler.UIElements["PauseMenu"].gameObject.SetActive(true);
                Pause();
            }
        }
        else if(Input.GetKeyDown(KeyCode.Tab))
        {
            if(baseUIHandler.UIElements["EditorMenu"].gameObject.activeSelf)
            {
                Resume();
            }
            else
            {
                baseUIHandler.UIElements["EditorMenu"].gameObject.SetActive(true);
                Pause();
            }
        }

        if(Input.GetKey(KeyCode.LeftControl))
        {
            if(Input.GetKeyDown(KeyCode.S))
            {
                baseUIHandler.UIElements["SaveMenu"].gameObject.SetActive(true);
                baseUIHandler.UIElements["SaveMenu"].Find("Level Name").GetComponent<TMP_InputField>().SetTextWithoutNotify(levelName);
                baseUIHandler.UIElements["SaveMenu"].Find("Level Description").GetComponent<TMP_InputField>().SetTextWithoutNotify(levelDescription);
                baseUIHandler.UIElements["SaveMenu"].Find("Level Creators").GetComponent<TMP_InputField>().SetTextWithoutNotify(levelCreators);
                Pause();
            }
            else if(Input.GetKeyDown(KeyCode.L))
            {
                RefreshLevelSlots();
                baseUIHandler.UIElements["LoadMenu"].gameObject.SetActive(true);
                Pause();
            }
        }

        if (!Paused && Time.timeScale != 0)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
            bool leftShiftDown = Input.GetKey(KeyCode.LeftShift);
            float scrollRate = leftShiftDown ? DataManager.playerSettings.slowZoomSpeed : DataManager.playerSettings.fastZoomSpeed;

            Vector3 inputDir = new Vector3(GetInputAxis("x"), GetInputAxis("y"), GetInputAxis("z")).normalized;

            float targetSpeed = movementSpeed / 2 * inputDir.magnitude;

            if(Input.GetKeyDown(KeyCode.LeftAlt))
            {
                scrollForSpeed = !scrollForSpeed;
            }

            // Speed up/down with scroll
            if(scrollForSpeed)
            {
                if(Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                {
                    movementSpeed = Mathf.Clamp(movementSpeed - scrollRate, 0, speedLimit);
                }
                else if(Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                {
                    movementSpeed = Mathf.Clamp(movementSpeed + scrollRate, 0, speedLimit);
                }
            }
            else
            {
                if(Input.GetAxisRaw("Mouse ScrollWheel") < 0)
                {
                    previewDistance = Mathf.Clamp(previewDistance - scrollRate, previewDistanceLimit[0], previewDistanceLimit[1]);
                }
                else if(Input.GetAxisRaw("Mouse ScrollWheel") > 0)
                {
                    previewDistance = Mathf.Clamp(previewDistance + scrollRate, previewDistanceLimit[0], previewDistanceLimit[1]);
                }
            }

            MouseCameraRotation();

            Vector3 velocity = targetSpeed *(Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * inputDir);
            rb.velocity = velocity;

            if(previewObject != null)
            {
                Vector3Int previewGridPosition;
                RaycastHit hit = new RaycastHit()
                {
                    point = Vector3.zero,
                    normal = Vector3.zero,
                };
                if(!leftShiftDown && Physics.Raycast(transform.position, transform.forward, out hit, Mathf.Infinity, ~ignoreLayers))
                {
                    hit.normal.Scale(previewCollider.bounds.extents);
                    previewGridPosition = WorldToGrid(hit.point.Round(2) + hit.normal);
                }
                else
                {
                    previewGridPosition = WorldToGrid(transform.position + transform.forward * previewDistance);
                }
                previewObject.position = previewGridPosition;

                if(Input.GetMouseButtonDown(0)) // Object destruction
                {
                    List<Vector3Int> destroyedCells = new List<Vector3Int>();
                    if(brushSize > 1)
                    {
                        int halfBrushSize = brushSize / 2;
                        if(brushSize % 2 == 0)
                        {
                            for(int i = -halfBrushSize + 1; i <= halfBrushSize; i++)
                            {
                                int x = previewGridPosition.x + (i * cellSize.x);
                                for(int j = -halfBrushSize + 1; j <= halfBrushSize; j++)
                                {
                                    int y = previewGridPosition.y + (j * cellSize.y);
                                    for(int k = -halfBrushSize + 1; k <= halfBrushSize; k++)
                                    {
                                        int z = previewGridPosition.z + (k * cellSize.z);
                                        Vector3Int gridPosition = new Vector3Int(x, y, z) - (hit.normal.FloorToInt() * cellSize);
                                        if(DestroyCell(gridPosition))
                                        {
                                            destroyedCells.Add(gridPosition);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for(int i = -halfBrushSize; i <= halfBrushSize; i++)
                            {
                                int x = previewGridPosition.x + (i * cellSize.x);
                                for(int j = -halfBrushSize; j <= halfBrushSize; j++)
                                {
                                    int y = previewGridPosition.y + (j * cellSize.y);
                                    for(int k = -halfBrushSize; k <= halfBrushSize; k++)
                                    {
                                        int z = previewGridPosition.z + (k * cellSize.z);
                                        Vector3Int gridPosition = new Vector3Int(x, y, z) - (hit.normal.FloorToInt() * cellSize);
                                        if(DestroyCell(gridPosition))
                                        {
                                            destroyedCells.Add(gridPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        Vector3Int gridPosition = previewGridPosition - (hit.normal.FloorToInt() * cellSize);
                        if(DestroyCell(gridPosition))
                        {
                            destroyedCells.Add(gridPosition);
                        }
                    }

                    if(destroyedCells.Count > 0)
                    {
                        AddUndoAction(UndoAction.Clear, new object[] { destroyedCells });
                    }
                }
                else if(Input.GetMouseButtonDown(1)) // Object placement
                {
                    List<Vector3Int> filledCells = new List<Vector3Int>();
                    if(brushSize > 1)
                    {
                        int halfBrushSize = brushSize / 2;
                        if(brushSize % 2 == 0)
                        {
                            for(int i = -halfBrushSize + 1; i <= halfBrushSize; i++)
                            {
                                int x = previewGridPosition.x + (i * cellSize.x);
                                for(int j = -halfBrushSize + 1; j <= halfBrushSize; j++)
                                {
                                    int y = previewGridPosition.y + (j * cellSize.y);
                                    for(int k = -halfBrushSize + 1; k <= halfBrushSize; k++)
                                    {
                                        int z = previewGridPosition.z + (k * cellSize.z);
                                        Vector3Int gridPosition = new Vector3Int(x, y, z);
                                        if(FillCell(gridPosition, prefabDictionary[previewObject.name]))
                                        {
                                            filledCells.Add(gridPosition);
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            for(int i = -halfBrushSize; i <= halfBrushSize; i++)
                            {
                                int x = previewGridPosition.x + (i * cellSize.x);
                                for(int j = -halfBrushSize; j <= halfBrushSize; j++)
                                {
                                    int y = previewGridPosition.y + (j * cellSize.y);
                                    for(int k = -halfBrushSize; k <= halfBrushSize; k++)
                                    {
                                        int z = previewGridPosition.z + (k * cellSize.z);
                                        Vector3Int gridPosition = new Vector3Int(x, y, z);
                                        if(FillCell(gridPosition, prefabDictionary[previewObject.name]))
                                        {
                                            filledCells.Add(gridPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        if(FillCell(previewGridPosition, prefabDictionary[previewObject.name]))
                        {
                            filledCells.Add(previewGridPosition);
                        }
                    }

                    if(filledCells.Count > 0)
                    {
                        AddUndoAction(UndoAction.Fill, new object[] { filledCells });
                    }
                }

                if(Input.GetKey(KeyCode.LeftControl))
                {
                    if(Input.GetKeyDown(KeyCode.Z)) // Undo
                    {
                        if(undoActions.Count > 0)
                        {
                            object[] undoData = undoObjects[^1];
                            switch(undoActions[^1])
                            {
                                case UndoAction.Fill:
                                    List<Vector3Int> cells = (List<Vector3Int>)undoData[0];
                                    foreach(Vector3Int cell in cells)
                                    {
                                        DestroyCell(cell);
                                    }
                                    AddRedoAction(UndoAction.Fill, new object[] { cells });
                                    break;
                                case UndoAction.Clear:
                                    cells = (List<Vector3Int>)undoData[0];
                                    foreach(Vector3Int cell in cells)
                                    {
                                        FillCell(cell, prefabDictionary[destroyedBlocks[cell]]);
                                    }
                                    AddRedoAction(UndoAction.Clear, new object[] { cells });
                                    break;
                            }
                            undoActions.RemoveAt(undoActions.Count - 1);
                            undoObjects.RemoveAt(undoObjects.Count - 1);
                        }
                    }
                    else if(Input.GetKeyDown(KeyCode.Y)) // Redo
                    {
                        if(redoActions.Count > 0)
                        {
                            object[] redoData = redoObjects[^1];
                            switch(redoActions[^1])
                            {
                                case UndoAction.Fill:
                                    List<Vector3Int> cells = (List<Vector3Int>)redoData[0];
                                    foreach(Vector3Int cell in cells)
                                    {
                                        FillCell(cell, prefabDictionary[destroyedBlocks[cell]]);
                                    }
                                    AddUndoAction(UndoAction.Fill, new object[] { cells });
                                    break;
                                case UndoAction.Clear:
                                    cells = (List<Vector3Int>)redoData[0];
                                    foreach(Vector3Int cell in cells)
                                    {
                                        DestroyCell(cell);
                                    }
                                    AddUndoAction(UndoAction.Clear, new object[] { cells });
                                    break;
                            }
                            redoActions.RemoveAt(redoActions.Count - 1);
                            redoObjects.RemoveAt(redoObjects.Count - 1);
                        }
                    }
                }
                else
                {
                    if(Input.GetKeyDown(KeyCode.R))
                    {
                        AddUndoAction(UndoAction.Clear, new object[] { placedBlocks.Keys });

                        foreach(Vector3Int cell in placedBlocks.Keys.ToList())
                        {
                            DestroyCell(cell);
                        }
                    }
                }
            }
        }
    }

    private Vector3Int WorldToGrid(Vector3 worldPosition)
    {
        return new Vector3Int(Mathf.FloorToInt(worldPosition.x / cellSize.x) * cellSize.x, Mathf.FloorToInt(worldPosition.y / cellSize.y) * cellSize.y, Mathf.FloorToInt(worldPosition.z / cellSize.z) * cellSize.z) + Vector3Int.one;
    }

    private bool DestroyCell(Vector3Int gridPosition)
    {
        if(placedBlocks.TryGetValue(gridPosition, out GameObject placedBlock))
        {
            destroyedBlocks.Add(gridPosition, placedBlock.name);
            Destroy(placedBlock);
            placedBlocks.Remove(gridPosition);
            return true;
        }
        return false;
    }

    private bool FillCell(Vector3Int gridPosition, GameObject withObject)
    {
        if(!placedBlocks.ContainsKey(gridPosition))
        {
            GameObject newObject = Instantiate(withObject, gridPosition, Quaternion.identity);
            newObject.name = previewObject.name;
            placedBlocks.Add(gridPosition, newObject);
            destroyedBlocks.Remove(gridPosition);
            return true;
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

        currentRotation = Vector3.SmoothDamp(currentRotation, new Vector3(pitch, yaw), ref rotationSmoothVelocity, DataManager.playerSettings.cameraSmoothing);
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

    public void Resume()
    {
        foreach(Transform element in baseUIHandler.UIElements.Values)
        {
            element.gameObject.SetActive(false);
        }

        Paused = false;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    public void Pause()
    {
        Paused = true;
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
        SaveSystem.SaveLevel(levelName, levelDescription, levelCreators, FindObjectsOfType<SaveableLevelObject>());
        Resume();
    }

    public void Load()
    {
        foreach(Vector3Int cell in placedBlocks.Keys.ToList())
        {
            DestroyCell(cell);
        }
        destroyedBlocks.Clear();
        undoActions.Clear();
        undoObjects.Clear();
        redoActions.Clear();
        redoObjects.Clear();

        LevelInfo levelInfo = SaveSystem.LoadLevel(selectedLevelSlot);
        levelName = levelInfo.name;
        levelDescription = levelInfo.description;
        levelCreators = levelInfo.creators;

        foreach(LevelInfo.LevelObjectInfo levelObjectInfo in levelInfo.levelObjects)
        {
            Vector3 levelObjectPosition = new Vector3(levelObjectInfo.posX, levelObjectInfo.posY, levelObjectInfo.posZ);
            GameObject levelObject = Instantiate(prefabDictionary[levelObjectInfo.prefabName], levelObjectPosition, Quaternion.Euler(new Vector3(levelObjectInfo.eulerX, levelObjectInfo.eulerY, levelObjectInfo.eulerZ)));
            levelObject.transform.localScale = new Vector3(levelObjectInfo.scaleX, levelObjectInfo.scaleY, levelObjectInfo.scaleZ);
            levelObject.name = levelObjectInfo.name;
            levelObject.tag = levelObjectInfo.tag;
            levelObject.layer = levelObjectInfo.layer;
            placedBlocks.Add(WorldToGrid(levelObjectPosition), levelObject);
        }
        Resume();
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

    public void SetBrushSize(TMP_InputField inputField)
    {
        int.TryParse(inputField.text, out int result);
        if(result < 1)
        {
            inputField.SetTextWithoutNotify("0");
            brushSize = 1;
        }
        else
        {
            brushSize = result;
        }
    }

    public void SetPreviewObject(Transform button)
    {
        if(previewObject != null)
        {
            Destroy(previewObject.gameObject);
        }
        previewObject = Instantiate(prefabValues[prefabKeys.IndexOf(button.name)], Vector3.zero, Quaternion.identity).transform;

        if(previewObject.TryGetComponent<SaveableLevelObject>(out var saveableLevelObject))
        {
            Destroy(saveableLevelObject);
        }

        previewRenderer = previewObject.GetComponentInChildren<MeshRenderer>();
        previewRenderer.material.color = previewColor;

        if(previewRenderer.TryGetComponent<Collider>(out var collider))
        {
            previewCollider = collider;
        }
        else
        {
            previewCollider = previewObject.GetComponentInChildren<Collider>();
        }

        previewRenderer.gameObject.layer = 2;
        previewObject.name = button.name;
    }
}
