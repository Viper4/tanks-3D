using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TrailEmitter : MonoBehaviour
{
    private bool disabled;
    public bool Disabled 
    { 
        get
        {
            return disabled;
        }
        set
        {
            disabled = value;
            foreach (Transform trail in trails)
            {
                trail.GetComponent<TrailRenderer>().emitting = !disabled;
            }
        }
    }

    [SerializeField] Transform tankOrigin;

    [SerializeField] Transform trackMarks;
    [SerializeField] BaseTankLogic baseTankLogic;

    List<Transform> trails = new List<Transform>();

    private void Start()
    {
        foreach (Transform trail in trackMarks)
        {
            trails.Add(trail);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!Disabled)
        {
            foreach (Transform trail in trails)
            {
                trail.GetComponent<TrailRenderer>().emitting = baseTankLogic.IsGrounded();
            }
        }
    }

    [PunRPC]
    public void ResetTrails()
    {
        foreach (Transform trail in trails)
        {
            trail.GetComponent<TrailRenderer>().Clear();
        }
    }
}
