using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class UsernameSystem : MonoBehaviour
{
    [SerializeField] int fontScaler = 4;

    [SerializeField] PlayerControl playerControl;
    [SerializeField] TextMesh textMesh;

    // Start is called before the first frame update
    void Start()
    {
        textMesh.text = playerControl.ClientManager.photonView.Owner.NickName;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void UpdateTextMeshTo(Transform fromCamera)
    {
        transform.rotation = fromCamera.rotation;
        textMesh.fontSize = (int)Vector3.Distance(fromCamera.position, transform.position) * fontScaler;
    }
}
