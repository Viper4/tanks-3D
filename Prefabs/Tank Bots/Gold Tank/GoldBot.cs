using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;
using Photon.Pun;

public class GoldBot : MonoBehaviour
{
    TargetSystem targetSystem;
    BaseTankLogic baseTankLogic;
    AreaScanner areaScanner;
    RicochetCalculation bulletRicochet;

    Transform body;
    Transform turret;
    Transform barrel;

    [SerializeField] List<string> mineObjectTags = new List<string>() { "Destructable", "Untagged" };
    [SerializeField] LayerMask mineObjectLayerMask;

    [SerializeField] float maxShootAngle = 5;
    public float[] fireDelay = { 0.15f, 0.25f };
    [SerializeField] float layDistance = 2.5f;
    [SerializeField] float trapRadius = 5;
    public float[] layDelay = { 0.3f, 0.6f };
    [SerializeField] LayerMask highTierTanks;
    [SerializeField] LayerMask mineLayerMask;
    [SerializeField] float updateDelay = 0.25f;
    [SerializeField] int escapeSearchSteps = 20;

    FireControl fireControl;
    Coroutine fireRoutine = null;
    Coroutine firePatternRoutine = null;
    MineControl mineControl;
    Coroutine mineRoutine = null;
    bool shootAfterLay = false;
    Transform targetMine = null;

    Vector3 predictedTargetPosition;

    enum Mode
    {
        LayRoutine,
        Laying,
        Resetting,
        StraightFirePattern,
        RicochetFirePattern,
        Escape,
        None
    }
    Mode mode = Mode.None;

    Transform nearbyMine = null;
    Transform nearbyBullet = null;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();
        baseTankLogic = GetComponent<BaseTankLogic>();
        areaScanner = GetComponent<AreaScanner>();
        bulletRicochet = GetComponent<RicochetCalculation>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();

        InvokeRepeating(nameof(Loop), 0.1f, updateDelay);
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            if (mode != Mode.LayRoutine && mode != Mode.Laying && mode != Mode.Escape)
            {
                baseTankLogic.targetTankDir = transform.forward;
            }

            Vector3 targetDir = targetSystem.currentTarget.position - turret.position;
            predictedTargetPosition = targetSystem.PredictedTargetPosition(CustomMath.TravelTime(turret.position, targetSystem.currentTarget.position, fireControl.speed));
            bool predictedPositionVisible = false;
            if (mode != Mode.StraightFirePattern && mode != Mode.RicochetFirePattern)
            {
                if (!Physics.Linecast(turret.position, predictedTargetPosition, ~targetSystem.ignoreLayerMask))
                {
                    baseTankLogic.targetTurretDir = predictedTargetPosition - turret.position;
                    predictedPositionVisible = true;
                }
                else
                {
                    baseTankLogic.targetTurretDir = targetDir;
                }
            }

            if (targetSystem.TargetVisible() || predictedPositionVisible)
            {
                if (mode == Mode.LayRoutine)
                {
                    StopMineRoutine();
                }
                else if (mode == Mode.RicochetFirePattern)
                {
                    StopFireRoutine();
                }

                if (mode != Mode.StraightFirePattern && fireControl.canFire && Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle)
                {
                    firePatternRoutine = StartCoroutine(FireStraight(false));
                    StopMineRoutine();
                }
            }
            else
            {
                if (bulletRicochet.shootPositions.Count > 0)
                {
                    if (mode == Mode.StraightFirePattern)
                    {
                        StopFireRoutine();
                    }

                    if (nearbyMine == null && mode != Mode.RicochetFirePattern && fireControl.canFire && fireControl.BulletSpawnClear())
                    {
                        StartCoroutine(FireRicochet(false));
                    }
                }
                else if (targetMine != null)
                {
                    if (mode != Mode.RicochetFirePattern)
                    {
                        // Rotating turret and barrel towards mine
                        baseTankLogic.targetTurretDir = targetMine.position - turret.position;
                    }

                    if (Physics.Raycast(turret.position, targetMine.position - turret.position, out RaycastHit hit, Mathf.Infinity, ~targetSystem.ignoreLayerMask) && hit.transform.CompareTag(targetMine.tag))
                    {
                        if (fireControl.canFire && mode != Mode.RicochetFirePattern && mode != Mode.StraightFirePattern && !Physics.CheckSphere(targetMine.position, trapRadius, highTierTanks))
                        {
                            fireRoutine = StartCoroutine(FireStraight(true));
                        }
                    }
                    else
                    {
                        bulletRicochet.CalculateBulletRicochets(barrel, targetMine.position);
                        if (fireControl.canFire && mode != Mode.RicochetFirePattern && mode != Mode.StraightFirePattern && bulletRicochet.shootPositions.Count > 0 && !Physics.CheckSphere(targetMine.position, trapRadius, highTierTanks))
                        {
                            fireRoutine = StartCoroutine(FireRicochet(true));
                        }
                    }
                }
            }

            if (nearbyMine != null && mode != Mode.Escape)
            {
                baseTankLogic.AvoidMine(nearbyMine, 100);
                StopMineRoutine();
            }

            if (nearbyBullet != null)
            {
                baseTankLogic.AvoidBullet(nearbyBullet);
            }
        }
    }

    void Loop()
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            targetSystem.chooseTarget = GameManager.Instance != null && (!PhotonNetwork.OfflineMode || GameManager.Instance.autoPlay);

            bulletRicochet.ScanArea(turret.position);
            bulletRicochet.CalculateBulletRicochets(barrel, predictedTargetPosition);

            if (targetMine == null)
            {
                Collider[] allMines = Physics.OverlapSphere(turret.position, 50, mineLayerMask);
                List<Collider> checkedMines = new List<Collider>();
                foreach (Collider collider in allMines)
                {
                    if (!checkedMines.Contains(collider))
                    {
                        if (!Physics.CheckSphere(collider.bounds.center, trapRadius, highTierTanks) && Physics.CheckSphere(collider.bounds.center, trapRadius, 1 << targetSystem.currentTarget.gameObject.layer))
                        {
                            targetMine = collider.transform;
                            baseTankLogic.targetTurretDir = collider.transform.position - turret.position;
                            break;
                        }
                        else
                        {
                            Collider[] overlappingMines = Physics.OverlapSphere(collider.bounds.center, mineControl.explosionRadius, mineLayerMask);
                            foreach (Collider overlappingMine in overlappingMines)
                            {
                                if (!Physics.CheckSphere(overlappingMine.bounds.center, trapRadius, highTierTanks) && Physics.CheckSphere(overlappingMine.bounds.center, trapRadius, 1 << targetSystem.currentTarget.gameObject.layer))
                                {
                                    targetMine = collider.transform;
                                    baseTankLogic.targetTurretDir = collider.transform.position - turret.position;
                                    goto LoopEnd;
                                }
                                checkedMines.Add(overlappingMine);
                            }
                        }
                    }
                }
                LoopEnd:;

                if (!targetSystem.TargetVisible() && bulletRicochet.shootPositions.Count <= 0 && mode != Mode.LayRoutine && mode != Mode.Laying && mode != Mode.Escape && mode != Mode.Resetting && nearbyMine == null && mineControl.canLay && mineControl.laidMines.Count < mineControl.mineLimit)
                {
                    SearchForMinePositions();
                }
            }
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0 && baseTankLogic != null && !baseTankLogic.disabled)
        {
            switch (other.tag)
            {
                case "Mine":
                    nearbyMine = other.transform;
                    if (targetMine == null && mode != Mode.Escape)
                    {
                        targetMine = nearbyMine;
                    }
                    break;
                case "Bullet":
                    if (other.TryGetComponent<BulletBehaviour>(out var bulletBehaviour))
                    {
                        if (bulletBehaviour.owner != null && bulletBehaviour.owner != transform)
                        {
                            nearbyBullet = other.transform;
                        }
                    }
                    break;
            }
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
            case "Bullet":
                if (nearbyBullet == other.transform)
                {
                    nearbyBullet = null;
                }
                break;
        }
    }

    void SearchForMinePositions()
    {
        Dictionary<string, List<Transform>> visibleObjects = new Dictionary<string, List<Transform>>();
        List<Transform> bothVisibleObjects = new List<Transform>();

        foreach (string tag in mineObjectTags)
        {
            List<Transform> myVisibleObjects = areaScanner.GetVisibleObjectsNotNear(turret, mineObjectLayerMask, tag, highTierTanks | mineLayerMask, trapRadius);
            visibleObjects.Add(tag, myVisibleObjects);

            foreach (Transform visibleObject in myVisibleObjects)
            {
                if (Physics.Raycast(targetSystem.currentTarget.position, visibleObject.position - targetSystem.currentTarget.position, out RaycastHit hit, Mathf.Infinity, ~targetSystem.ignoreLayerMask))
                {
                    if (hit.transform == visibleObject)
                    {
                        bothVisibleObjects.Add(visibleObject);
                    }
                }
            }
        }

        if (bothVisibleObjects.Count > 0)
        {
            areaScanner.SelectObject(targetSystem.currentTarget, bothVisibleObjects);
            StopMineRoutine();
            shootAfterLay = false;
            mineRoutine = StartCoroutine(LayMine());
        }
        else
        {
            if (visibleObjects["Destructable"].Count > 0)
            {
                areaScanner.SelectObject(targetSystem.currentTarget, visibleObjects["Destructable"]);
                StopMineRoutine();
                shootAfterLay = true;
                mineRoutine = StartCoroutine(LayMine());
            }
        }
    }

    void StopFireRoutine()
    {
        if (firePatternRoutine != null)
        {
            StopCoroutine(firePatternRoutine);
            if (fireRoutine != null)
            {
                StopCoroutine(fireRoutine);
            }
            baseTankLogic.stationary = false;
            fireRoutine = null;
            mode = Mode.None;
        }
    }

    void StopMineRoutine()
    {
        if (mineRoutine != null)
        {
            StartCoroutine(ResetLay());
            StopCoroutine(mineRoutine);
            baseTankLogic.stationary = false;
            mineRoutine = null;
            mode = Mode.None;
        }
    }

    IEnumerator FireStraight(bool mine)
    {
        mode = Mode.StraightFirePattern;

        // Firing bullet straight to target
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        yield return new WaitUntil(() => Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);
        StartCoroutine(fireControl.Shoot());
        baseTankLogic.stationary = false;
        if (mine && fireControl.firedBullets[^1] != null)
        {
            Transform bullet = fireControl.firedBullets[^1];
            yield return new WaitUntil(() => targetMine == null || bullet == null);
        }

        mode = Mode.None;
        fireRoutine = null;
    }

    IEnumerator FireRicochet(bool mine)
    {
        mode = Mode.RicochetFirePattern;

        bulletRicochet.ScanArea(turret.position);
        if (!mine)
        {
            bulletRicochet.CalculateBulletRicochets(barrel, predictedTargetPosition);
        }
        else
        {
            bulletRicochet.CalculateBulletRicochets(barrel, targetMine.position);
        }

        if (bulletRicochet.shootPositions.Count > 0)
        {
            Vector3 shootPosition = bulletRicochet.SelectShootPosition(barrel, bulletRicochet.selectionMode);

            // Firing ricochet bullet to target
            Vector3 shootDirection = shootPosition - turret.position;
            baseTankLogic.targetTurretDir = shootDirection;
            baseTankLogic.stationary = true;
            yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
            yield return new WaitUntil(() => fireControl.canFire && Vector3.Angle(barrel.forward, shootDirection) < maxShootAngle);
            StartCoroutine(fireControl.Shoot());
            baseTankLogic.stationary = false;
            if (mine && fireControl.firedBullets.Count > 0)
            {
                Transform bullet = fireControl.firedBullets[^1];
                yield return new WaitUntil(() => targetMine == null || bullet == null);
            }
        }

        mode = Mode.None;
        fireRoutine = null;
    }

    IEnumerator ResetLay()
    {
        mode = Mode.Resetting;
        yield return new WaitForSeconds(Random.Range(mineControl.layCooldown[0], mineControl.layCooldown[1]));
        mode = Mode.None;
    }

    IEnumerator LayMine()
    {
        mode = Mode.LayRoutine;

        baseTankLogic.stationary = false;
        Vector3 layPosition;
        if (areaScanner.selectedObject.TryGetComponent(out Collider collider))
        {
            layPosition = collider.ClosestPoint(transform.position);
        }
        else
        {
            layPosition = areaScanner.selectedObject.position;
        }
        layPosition = new Vector3(layPosition.x, body.position.y, layPosition.z);

        while (CustomMath.SqrDistance(layPosition, body.position) > layDistance * layDistance)
        {
            if (areaScanner.selectedObject != null)
            {
                baseTankLogic.targetTankDir = layPosition - body.position;
            }
            else
            {
                mode = Mode.None;
                StopMineRoutine();
            }
            yield return new WaitForEndOfFrame();
        }

        mode = Mode.Laying;
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));

        float angleOffset = 360 / escapeSearchSteps;
        baseTankLogic.stationary = false;
        for (int i = 0; i < escapeSearchSteps; i++)
        {
            Vector3 testDirection = Quaternion.AngleAxis(i * angleOffset, turret.up) * transform.forward;
            Vector3 escapePosition = transform.position + testDirection * (trapRadius * 1.5f);
            if (!Physics.Raycast(turret.position, testDirection, trapRadius * 1.5f, baseTankLogic.barrierLayers) && !Physics.CheckSphere(turret.position + testDirection * (trapRadius * 1.5f), trapRadius, mineLayerMask))
            {
                StartCoroutine(mineControl.LayMine());
                if (shootAfterLay)
                {
                    targetMine = mineControl.laidMines[^1].GetChild(0);
                }
                mode = Mode.Escape;
                Debug.DrawLine(turret.position, turret.position + testDirection * (trapRadius * 1.5f), Color.green, 10);
                while (CustomMath.SqrDistance(transform.position, escapePosition) > 4)
                {
                    baseTankLogic.targetTankDir = escapePosition - transform.position;
                    yield return new WaitForEndOfFrame();
                }
                break;
            }
        }

        mode = Mode.None;
        mineRoutine = null;
    }
}
