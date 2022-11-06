using System.Collections;
using UnityEngine;
using MyUnityAddons.Calculations;

public class GreenBot : MonoBehaviour
{
    BaseTankLogic baseTankLogic;
    FireControl fireControl;
    RicochetCalculation bulletRicochet;

    Transform turret;
    Transform barrel;

    [SerializeField] float[] fireDelay = { 0.2f, 0.45f };
    [SerializeField] float[] indexChangeDelay = { 0.5f, 1.5f };

    [SerializeField] float maxShootAngle = 2;
    [SerializeField] float updateDelay = 1;
    [SerializeField] float predictionScale = 0.8f;

    bool shooting = false;

    TargetSystem targetSystem;

    Vector3 shootPosition;
    Vector3 lookDirection;
    int lookIndex = 0;
    Vector3 lastPosition;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();
        fireControl = GetComponent<FireControl>();
        bulletRicochet = GetComponent<RicochetCalculation>();

        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        lastPosition = transform.position + transform.forward;

        InvokeRepeating(nameof(Loop), 0.1f, updateDelay);
        StartCoroutine(SwitchLookIndex());
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            if (bulletRicochet.shootPositions.Count > 0)
            {
                baseTankLogic.targetTurretDir = shootPosition - turret.position;

                if (!shooting && fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
                {
                    StartCoroutine(Shoot());
                }
            }
            else if (bulletRicochet.lookPositions.Count > 0)
            {
                if (lookIndex >= bulletRicochet.lookPositions.Count)
                {
                    lookIndex = 0;
                }
                lookDirection = bulletRicochet.lookPositions[lookIndex] - turret.position;
                baseTankLogic.targetTurretDir = lookDirection;
            }
            else
            {
                baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;
            }
        }
    }

    void Loop()
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            if ((lastPosition - transform.position).sqrMagnitude > 0.099f)
            {
                lastPosition = transform.position;
                bulletRicochet.ScanArea(turret.position);
            }

            Vector3 predictedPos = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.bulletSettings.speed * predictionScale));
            bulletRicochet.CalculateBulletRicochets(barrel, predictedPos);

            if (bulletRicochet.shootPositions.Count > 0)
            {
                shootPosition = bulletRicochet.SelectShootPosition(barrel, RicochetCalculation.SelectionMode.Closest);
                predictedPos = targetSystem.PredictedTargetPosition(bulletRicochet.shootPositions[shootPosition] / fireControl.bulletSettings.speed);
                bulletRicochet.CalculateBulletRicochets(barrel, predictedPos);
                if (bulletRicochet.shootPositions.Count > 0)
                {
                    shootPosition = bulletRicochet.SelectShootPosition(barrel, RicochetCalculation.SelectionMode.Closest);
                }
            }
        }
    }

    IEnumerator Shoot()
    {
        shooting = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(fireControl.Shoot());
        bulletRicochet.SelectShootPosition(barrel, bulletRicochet.selectionMode);

        shooting = false;
    }

    IEnumerator SwitchLookIndex()
    {
        yield return new WaitUntil(() => lookDirection != null && Vector3.Angle(barrel.forward, lookDirection) < maxShootAngle);
        lookIndex = Random.Range(0, bulletRicochet.lookPositions.Count);
        yield return new WaitForSeconds(Random.Range(indexChangeDelay[0], indexChangeDelay[1]));

        StartCoroutine(SwitchLookIndex());
    }
}
