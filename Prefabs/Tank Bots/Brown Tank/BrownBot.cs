using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 1, 3 };

    [SerializeField] float turretRotSpeed = 20;

    [SerializeField] Vector2 turretScanRange = new Vector2(8, 45);
    [SerializeField] float[] scanChangeDelay = { 3, 6 };
    float scanOffset = 0;

    Vector3 turretStartEulers;
    Vector3 barrelStartEulers;

    bool shooting = false;

    [SerializeField] LayerMask ignoreLayerMask;

    // Start is called before the first frame Update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of BrownBot has been defaulted to the player");
            target = GameObject.Find("Player").transform;
        }

        body = transform.Find("Body");
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

        float targetEulerY = Mathf.Lerp(turret.localEulerAngles.y, scanOffset + angleY, turretRotSpeed * Time.deltaTime);

        turret.localEulerAngles = new Vector3(0, targetEulerY, 0);
        barrel.localEulerAngles = new Vector3(angleX, targetEulerY, 0);

        // If nothing blocking the player from barrel then fire
        if (!shooting && !Physics.Raycast(origin, barrel.position, dstToTarget, ~ignoreLayerMask))
        {
            StartCoroutine(Shoot());
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
        scanOffset = Random.Range(0, 360.0f)
        
        StartCoroutine(ChangeScan());
    }
}
