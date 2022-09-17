using System.Collections;
using UnityEngine;

public class GreyBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 0.7f, 1.25f };

    [SerializeField] float maxShootAngle = 30;

    FireControl fireControl;

    bool shooting = false;

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
            if (fireControl.canFire && !shooting && targetSystem.TargetVisible())
            {
                StartCoroutine(Shoot());
            }

            baseTankLogic.targetTankDir = body.forward;
            // Rotating turret and barrel towards target
            baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;
        }
    }

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than maxShootAngle, then stop and fire
        float angle = Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir);
        if (angle < maxShootAngle)
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
