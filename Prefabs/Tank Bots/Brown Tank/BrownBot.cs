using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;

    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 1, 3 };

    [SerializeField] float turretRotSpeed = 20;

    [SerializeField] Vector2 turretScanRange = new Vector2(8, 45);
    [SerializeField] float[] scanChangeDelay = { 3, 6 };
    float scanOffset = 0;
    float currentScanOffset = 0;

    bool shooting = false;

    [SerializeField] LayerMask targetLayerMask;

    // Start is called before the first frame Update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of BrownBot has been defaulted to the player");
            target = GameObject.Find("Player").transform;
        }

        barrel = transform.Find("Barrel");
        turret = transform.Find("Turret");
        
        StartCoroutine(ChangeScan());
    }

    // Update is called once per frame
    void Update()
    {
        dstToTarget = Vector3.Distance(transform.position, target.position);

        float angleX = Mathf.PingPong(Time.time * turretRotSpeed, turretScanRange.x * 2) - turretScanRange.x;
        float angleY = Mathf.PingPong(Time.time * turretRotSpeed, turretScanRange.y * 2) - turretScanRange.y;

        currentScanOffset += (scanOffset + angleY - currentScanOffset) * (Time.deltaTime * turretRotSpeed / 20);

        turret.localEulerAngles = new Vector3(0, currentScanOffset, 0);
        barrel.localEulerAngles = new Vector3(angleX, currentScanOffset, 0);

        // If target is in front of barrel then fire
        if (!shooting && Physics.Raycast(barrel.position + barrel.forward, barrel.forward, out RaycastHit hit, dstToTarget))
        {
            // If hit layer is in targetLayerMask
            if(targetLayerMask == (targetLayerMask | (1 << hit.transform.gameObject.layer)))
            {
                StartCoroutine(Shoot());
            }
        }
    }

    IEnumerator Shoot()
    {
        shooting = true;

        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());

        shooting = false;
    }
    
    IEnumerator ChangeScan()
    {
        yield return new WaitForSeconds(Random.Range(scanChangeDelay[0], scanChangeDelay[1]));
        scanOffset = Random.Range(-180.0f, 180.0f);
        
        StartCoroutine(ChangeScan());
    }
}
