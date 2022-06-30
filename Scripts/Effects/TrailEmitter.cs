using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class TrailEmitter : MonoBehaviour
{
    public bool disabled = false;

    [SerializeField] Transform trackMarks;
    [SerializeField] BaseTankLogic baseTankLogic;

    // Update is called once per frame
    void Update()
    {
        if (!disabled)
        {
            foreach (Transform trail in trackMarks)
            {
                trail.GetComponent<TrailRenderer>().emitting = baseTankLogic.IsGrounded();
            }
        }
        else
        {
            foreach (Transform trail in trackMarks)
            {
                trail.GetComponent<TrailRenderer>().emitting = false;
            }
        }
    }

    [PunRPC]
    public void ResetTrails()
    {
        foreach(Transform trail in trackMarks)
        {
            trail.GetComponent<TrailRenderer>().Clear();
        }
    }
}
