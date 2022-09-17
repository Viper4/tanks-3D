using System.Collections;
using UnityEngine;

public class RedBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 0.28f, 0.425f };

    [SerializeField] float maxShootAngle = 5;
    [SerializeField] float maxTargetAngle = 100;

    FireControl fireControl;
    bool shooting = false;

    Transform nearbyMine = null;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            Vector3 targetDir = targetSystem.currentTarget.position - turret.position;
            baseTankLogic.targetTurretDir = targetDir;

            if (fireControl.canFire && fireControl.firedBullets.Count < fireControl.bulletLimit && !shooting && targetSystem.TargetVisible())
            {
                StartCoroutine(Shoot());
            }

            if (nearbyMine == null)
            {
                // Rotating towards target when target is getting behind this tank
                if (Mathf.Abs(Vector3.SignedAngle(transform.forward, targetDir, transform.up)) > maxTargetAngle)
                {
                    baseTankLogic.targetTankDir = targetDir;
                }
                else
                {
                    baseTankLogic.targetTankDir = transform.forward;
                }
            }
            else
            {
                baseTankLogic.AvoidMine(nearbyMine, 100);
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        switch (other.tag)
        {
            case "Mine":
                nearbyMine = other.transform;
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch (other.tag)
        {
            case "Mine":
                if (nearbyMine == other.transform)
                {
                    nearbyMine = null;
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than shootAngle, then stop and fire
        if (Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
        {
            shooting = true;

            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            // Stops moving and delay in firing
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            StartCoroutine(GetComponent<FireControl>().Shoot());
            shooting = false;
            baseTankLogic.stationary = false;
        }
    }
}
