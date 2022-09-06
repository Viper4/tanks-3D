using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;
using Photon.Pun;

public class OrangeBot : MonoBehaviour
{
    TargetSystem targetSystem;
    BaseTankLogic baseTankLogic;
    AreaScanner areaScanner;

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

    FireControl fireControl;
    bool shooting = false;
    MineControl mineControl;
    Coroutine mineRoutine = null;

    enum Mode
    {
        LayRoutine,
        Resetting,
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

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            if (fireControl.canFire && !shooting && targetSystem.TargetVisible())
            {
                StartCoroutine(Shoot());
            }

            if (nearbyMine != null)
            {
                baseTankLogic.AvoidMine(nearbyMine, 100);
                if (mineRoutine != null)
                {
                    StartCoroutine(ResetLay());
                    StopCoroutine(mineRoutine);
                    baseTankLogic.stationary = false;
                    mineRoutine = null;
                }
            }
            else if (mode != Mode.LayRoutine)
            {
                baseTankLogic.targetTankDir = transform.forward;
                if (mode == Mode.None && mineControl.canLay && mineControl.laidMines.Count < mineControl.mineLimit)
                {
                    foreach (string tag in mineObjectTags)
                    {
                        List<Transform> visibleObjects = areaScanner.GetVisibleObjects(turret, mineObjectLayerMask, tag);
                        areaScanner.SelectObject(targetSystem.currentTarget, visibleObjects);
                        if (areaScanner.selectedObject != null)
                        {
                            mineRoutine = StartCoroutine(LayMine());
                            break;
                        }
                    }
                }
            }

            if (nearbyBullet != null)
            {
                baseTankLogic.AvoidBullet(nearbyBullet);
            }

            if (!mineControl.laidMines.Contains(targetSystem.currentTarget.parent))
            {
                if (!shooting && !targetSystem.TargetVisible() && mineControl.laidMines.Count > 0)
                {
                    foreach (Transform mine in mineControl.laidMines)
                    {
                        if (Physics.Raycast(turret.position, mine.position - turret.position, out RaycastHit hit, Mathf.Infinity, ~targetSystem.ignoreLayerMask))
                        {
                            if (hit.transform.CompareTag("Mine") && Physics.CheckSphere(mine.position, trapRadius, 1 << targetSystem.currentTarget.gameObject.layer))
                            {
                                targetSystem.currentTarget = mine.GetChild(0);
                                targetSystem.chooseTarget = false;
                                break;
                            }
                        }
                    }
                }
            }
            else
            {
                if (!targetSystem.TargetVisible())
                {
                    targetSystem.currentTarget = targetSystem.primaryTarget;
                    targetSystem.chooseTarget = GameManager.gameManager != null && (!PhotonNetwork.OfflineMode || GameManager.autoPlay);
                }
            }

            // Rotating turret and barrel towards target
            baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        switch (other.tag)
        {
            case "Mine":
                nearbyMine = other.transform;
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

    IEnumerator Shoot()
    {
        // When angle between barrel and target is less than maxShootAngle, then stop and fire
        shooting = true;
        yield return new WaitUntil(() => Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);

        // Stops moving and delay in firing
        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());

        if (targetSystem.currentTarget != targetSystem.primaryTarget)
        {
            targetSystem.currentTarget = targetSystem.primaryTarget;
            targetSystem.chooseTarget = GameManager.gameManager != null && (!PhotonNetwork.OfflineMode || GameManager.autoPlay);
        }
        shooting = false;
        baseTankLogic.stationary = false;
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

        Vector3 layPositionCenter;
        if (areaScanner.selectedObject.TryGetComponent(out Collider collider))
        {
            layPositionCenter = collider.ClosestPoint(transform.position);
        }
        else
        {
            layPositionCenter = areaScanner.selectedObject.position;
        }
        layPositionCenter = new Vector3(layPositionCenter.x, body.position.y, layPositionCenter.z);

        while (CustomMath.SqrDistance(layPositionCenter, body.position) > layDistance * layDistance)
        {
            if (areaScanner.selectedObject != null)
            {
                baseTankLogic.targetTankDir = layPositionCenter - body.position;
            }
            else
            {
                mode = Mode.None;
                yield break;
            }
            yield return new WaitForEndOfFrame();
        }

        baseTankLogic.stationary = true;
        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
        StartCoroutine(mineControl.LayMine());
        transform.position -= transform.forward * 0.1f;
        baseTankLogic.stationary = false;
        mode = Mode.None;
        mineRoutine = null;
    }
}
