using System.Collections;
using UnityEngine;

public class BrownBot : MonoBehaviour
{
    Transform turret;
    Transform barrel;

    [SerializeField] float turretRotSpeed = 20;

    [SerializeField] float visibilityDistance = 3;
    [SerializeField] float[] fireDelay = { 1.25f, 3f };
    [SerializeField] Vector2 turretScanRange = new Vector2(8, 45);
    [SerializeField] float[] scanChangeDelay = { 3, 6 };
    float scanOffset = 0;
    float currentScanOffset = 0;

    bool shooting = false;

    TargetSystem targetSystem;
    FireControl fireControl;
    BaseTankLogic baseTankLogic;

    // Start is called before the first frame Update
    void Start()
    {
        barrel = transform.Find("Barrel");
        turret = transform.Find("Turret");


        targetSystem = GetComponent<TargetSystem>();
        fireControl = GetComponent<FireControl>();
        baseTankLogic = GetComponent<BaseTankLogic>();

        StartCoroutine(ChangeScan());
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            float angleX = Mathf.PingPong(Time.time * turretRotSpeed, turretScanRange.x * 2) - turretScanRange.x;
            float angleY = Mathf.PingPong(Time.time * turretRotSpeed, turretScanRange.y * 2) - turretScanRange.y;

            currentScanOffset +=(scanOffset + angleY - currentScanOffset) *(Time.deltaTime * turretRotSpeed / 30);

            turret.localEulerAngles = new Vector3(0, currentScanOffset, 0);
            barrel.localEulerAngles = new Vector3(angleX, currentScanOffset, 0);

            // If target is in front of barrel then fire
            if(fireControl.canFire && !shooting && targetSystem.TargetInLineOfFire(visibilityDistance))
            {
                StartCoroutine(Shoot());
            }
        }
    }
    
    IEnumerator Shoot()
    {
        shooting = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(fireControl.Shoot());
        shooting = false;
    }

    IEnumerator ChangeScan()
    {
        yield return new WaitForSeconds(Random.Range(scanChangeDelay[0], scanChangeDelay[1]));
        scanOffset = Random.Range(-180.0f, 180.0f);
        
        StartCoroutine(ChangeScan());
    }
}
