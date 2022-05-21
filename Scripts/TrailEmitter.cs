using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrailEmitter : MonoBehaviour
{
    [SerializeField] BaseTankLogic baseTankLogic;

    [SerializeField] Transform trackMarks;

    // Start is called before the first frame update
    void Awake()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        foreach (Transform trail in trackMarks)
        {
            trail.GetComponent<TrailRenderer>().emitting = baseTankLogic.IsGrounded();
        }
    }
}
