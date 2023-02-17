using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomLevel : MonoBehaviourPunCallbacks
{
    [SerializeField] PlayerManager playerManager;
    [SerializeField] TankManager tankManager;

    // Start is called before the first frame update
    void Start()
    {
        LevelInfo levelInfo;
        if (PhotonNetwork.IsMasterClient)
        {
            levelInfo = SaveSystem.LoadLevel(DataManager.roomSettings.map);
        }
        else
        {
            levelInfo = SaveSystem.LoadTempLevel();
        }
        LoadLevel(levelInfo);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void LoadLevel(LevelInfo info)
    {
        foreach (LevelObjectInfo levelObjectInfo in info.levelObjects)
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
                switch (levelObjectInfo.spawnType)
                {
                    case 0:
                        levelObject.transform.SetParent(playerManager.defaultSpawnParent);
                        break;
                    case 1:
                        levelObject.transform.SetParent(tankManager.spawnParent);
                        break;
                    case 2:
                        levelObject.transform.SetParent(playerManager.teamSpawnParent);
                        break;
                }
                levelObjectRenderer.enabled = false;
            }

            if (levelObject.TryGetComponent<DestructableObject>(out var destructableObject))
            {
                destructableObject.destructableID = levelObjectInfo.ID;
            }
        }
    }
}
