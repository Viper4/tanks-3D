using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownBot : MonoBehaviour
{
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 1, 3 };

    [SerializeField] float turretRotSpeed = 20;

    [SerializeField] Vector2 turretScanRange = new Vector2(8, 45);
    [SerializeField] float[] scanChangeDelay = { 3, 6 };
    float scanOffset = 0;
    float currentScanOffset = 0;

    bool shooting = false;

    TargetSystem targetSystem;

    // Start is called before the first frame Update
    void Start()
    {
        barrel = transform.Find("Barrel");
        turret = transform.Find("Turret");

        if (GetComponent<TargetSystem>() != null)
        {
            targetSystem = GetComponent<TargetSystem>();
        }

        StartCoroutine(ChangeScan());
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            float angleX = Mathf.PingPong(Time.time * turretRotSpeed, turretScanRange.x * 2) - turretScanRange.x;
            float angleY = Mathf.PingPong(Time.time * turretRotSpeed, turretScanRange.y * 2) - turretScanRange.y;

            currentScanOffset += (scanOffset + angleY - currentScanOffset) * (Time.deltaTime * turretRotSpeed / 30);

            turret.localEulerAngles = new Vector3(0, currentScanOffset, 0);
            barrel.localEulerAngles = new Vector3(angleX, currentScanOffset, 0);

            // If target is in front of barrel then fire
            if (!shooting && targetSystem.TargetInLineOfFire())
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
