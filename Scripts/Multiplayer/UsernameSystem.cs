using UnityEngine;
using Photon.Pun;

public class UsernameSystem : MonoBehaviour
{
    [SerializeField] int fontScaler = 4;

    [SerializeField] PhotonView PV;
    [SerializeField] TextMesh textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh.text = PV.Owner.NickName;
    }

    public void UpdateTextMeshTo(Transform camera, bool altCam)
    {
        transform.rotation = camera.rotation;
        textMesh.fontSize = altCam ? (int)Mathf.Abs(camera.position.y - transform.position.y) * fontScaler / 2: (int)Vector3.Distance(camera.position, transform.position) * fontScaler;
    }
}
