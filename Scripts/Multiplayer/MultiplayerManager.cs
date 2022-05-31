using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MultiplayerManager : MonoBehaviour
{
    public bool inMultiplayer = false;

    PhotonView view;

    // Start is called before the first frame update
    void Awake()
    {
        view = GetComponent<PhotonView>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool ViewIsMine()
    {
        return view.IsMine;
    }
}
