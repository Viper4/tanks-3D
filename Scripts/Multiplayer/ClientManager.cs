using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using MyUnityAddons.Calculations;

public class ClientManager : MonoBehaviourPunCallbacks
{
    public bool deleteOnMultiplayer = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!PhotonNetwork.OfflineMode && deleteOnMultiplayer)
        {
            Destroy(gameObject);
        }
    }

    [PunRPC]
    void InitializePlayer(float[] primaryColorArray, float[] secondaryColorArray)
    {
        transform.SetParent(FindObjectOfType<PlayerManager>().playerParent);

        Color primaryColor = new Color(primaryColorArray[0], primaryColorArray[1], primaryColorArray[2], primaryColorArray[3]);
        Color secondaryColor = new Color(secondaryColorArray[0], secondaryColorArray[1], secondaryColorArray[2], secondaryColorArray[3]);

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
