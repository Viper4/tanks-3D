using UnityEngine;
using Photon.Pun;
using TMPro;

public class UsernameSystem : MonoBehaviour
{
    [SerializeField] int fontScaler = 4;

    [SerializeField] PhotonView PV;
    [SerializeField] TextMeshPro textMesh;

    Transform mainCamera;
    CameraControl cameraControl;

    // Start is called before the first frame update
    void Start()
    {
        if(PV != null && PV.Owner != null)
            textMesh.text = PV.Owner.NickName;

        UpdateMainCamera();
    }

    private void Update()
    {
        if (DataManager.playerSettings.silhouettes || DataManager.roomSettings.mode == "Co-Op")
        {
            if (!textMesh.enabled)
                textMesh.enabled = true;
            if (cameraControl == null)
            {
                UpdateTextMeshTo(mainCamera, false);
            }
            else
            {
                UpdateTextMeshTo(mainCamera, cameraControl.alternateCamera);
            }
        }
        else
        {
            if(textMesh.enabled)
                textMesh.enabled = false;
        }
    }

    public void UpdateMainCamera()
    {
        mainCamera = Camera.main.transform;
        if (mainCamera.TryGetComponent<CameraControl>(out var camControl))
        {
            cameraControl = camControl;
        }
    }

    void UpdateTextMeshTo(Transform camera, bool altCam)
    {
        transform.rotation = camera.rotation;
        textMesh.fontSize = altCam ? (int)Mathf.Abs(camera.position.y - transform.position.y) * fontScaler / 2 : (int)Vector3.Distance(camera.position, transform.position) * fontScaler;
    }
}
