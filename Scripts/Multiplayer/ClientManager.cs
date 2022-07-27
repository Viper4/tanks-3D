using UnityEngine;
using Photon.Pun;

public class ClientManager : MonoBehaviourPunCallbacks
{
    public PhotonView PV;
    [SerializeField] DataManager clientData;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.OfflineMode)
        {
            EngineSoundManager[] allEngineSounds = FindObjectsOfType<EngineSoundManager>();
            foreach (EngineSoundManager engineSound in allEngineSounds)
            {
                engineSound.UpdateMasterVolume(clientData.currentPlayerSettings.masterVolume);
            }
        }
    }

    public void UpdateSoundManagerOnClient(SoundManager soundManager)
    {
        soundManager.UpdateVolume(clientData.currentPlayerSettings.masterVolume);
    }

    [PunRPC]
    void RandomizeMaterialColors()
    {
        Color primaryColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
        Color secondaryColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));

        Transform tankOrigin = transform.Find("Tank Origin");
        MeshRenderer bodyRenderer = tankOrigin.Find("Body").GetComponent<MeshRenderer>();
        MeshRenderer turretRenderer = tankOrigin.Find("Turret").GetComponent<MeshRenderer>();
        MeshRenderer barrelRenderer = tankOrigin.Find("Barrel").GetComponent<MeshRenderer>();
        bodyRenderer.materials[2].color = primaryColor;
        bodyRenderer.materials[0].color = secondaryColor;

        turretRenderer.materials[0].color = secondaryColor;

        barrelRenderer.materials[1].color = primaryColor;
        barrelRenderer.materials[0].color = secondaryColor;
    }
}
