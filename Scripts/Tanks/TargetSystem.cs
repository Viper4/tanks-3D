using MyUnityAddons.Math;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSystem : MonoBehaviour
{
    public Transform primaryTarget;
    public Transform currentTarget;
    [SerializeField] string preferredTargetArea = "Turret";
    bool chooseTarget = false;

    Transform turret;
    Transform barrel;
    public LayerMask ignoreLayerMask;
    public Transform enemyParent;

    private void Start()
    {
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        chooseTarget = !PhotonNetwork.OfflineMode || GameManager.autoPlay;

        if (!chooseTarget)
        {
            if (primaryTarget == null)
            {
                Debug.Log("The variable primaryTarget of " + transform.name + " has been defaulted to the player");
                primaryTarget = GameObject.Find("Player").transform;
            }
            else
            {
                currentTarget = primaryTarget;
            }
        }
        
        if (enemyParent == null)
        {
            enemyParent = transform.parent;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (chooseTarget)
        {
            if (enemyParent.childCount > 1)
            {
                List<Transform> visibleTargets = new List<Transform>();
                foreach (Transform tank in enemyParent)
                {
                    if (tank != transform)
                    {
                        Transform target;
                        if (tank.CompareTag("Player"))
                        {
                            target = tank.transform.Find("Tank Origin").Find(preferredTargetArea);
                        }
                        else
                        {
                            target = tank.transform.Find(preferredTargetArea);
                        }

                        if (Physics.Raycast(turret.position, target.position - turret.position, out RaycastHit hit, Mathf.Infinity, ~ignoreLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.transform.gameObject.layer == target.gameObject.layer)
                            {
                                visibleTargets.Add(target);
                            }
                        }
                    }
                }

                if (visibleTargets.Count != 0)
                {
                    currentTarget = transform.ClosestTransform(visibleTargets);
                }
                else
                {
                    currentTarget = transform.ClosestTransform(enemyParent);
                    if (currentTarget.CompareTag("Player"))
                    {
                        currentTarget = currentTarget.Find("Tank Origin").Find(preferredTargetArea);
                    }
                    else
                    {
                        currentTarget = currentTarget.Find(preferredTargetArea);
                    }
                }
            }
            else
            {
                currentTarget = enemyParent.GetChild(0).Find(preferredTargetArea);
            }
        }

        if (currentTarget == null)
        {
            if (primaryTarget != null)
            {
                currentTarget = primaryTarget;
            }
            else
            {
                currentTarget = enemyParent;
            }
        }
    }

    public bool TargetVisible()
    {
        if (Physics.Raycast(turret.position, currentTarget.position - turret.position, out RaycastHit hit, Mathf.Infinity, ~ignoreLayerMask))
        {
            return currentTarget.gameObject.layer == hit.transform.gameObject.layer;
        }
        return false;
    }

    public bool TargetInLineOfFire(float maxDistance = Mathf.Infinity)
    {
        if (Physics.Raycast(barrel.position, barrel.forward, out RaycastHit barrelHit, maxDistance, ~ignoreLayerMask))
        {
            return barrelHit.transform.gameObject.layer == currentTarget.gameObject.layer;
        }
        return false;
    }

    public Vector3 PredictedTargetPosition(float seconds)
    {
        if (currentTarget.parent.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            Vector3 futurePosition = CustomMath.FuturePosition(currentTarget.position, rigidbody, seconds);
            if (Physics.Raycast(currentTarget.position, futurePosition - currentTarget.position, out RaycastHit hit, Vector3.Distance(currentTarget.position, futurePosition)))
            {
                return hit.point;
            }
            else
            {
                return futurePosition;
            }
        }
        else
        {
            return currentTarget.position;
        }
    }

    Transform GetClosestTank(List<Transform> tanks)
    {
        Transform closestTank = null;
        float closestTankDst = Mathf.Infinity;
        foreach (Transform tank in tanks)
        {
            if (tank != transform)
            {
                float dstToTank = CustomMath.SqrDistance(tank.transform.position, transform.position);

                if (dstToTank < closestTankDst)
                {
                    closestTankDst = dstToTank;
                    closestTank = tank;
                }
            }
        }
        return closestTank.Find("Turret");
    }

    Transform GetClosestTank(Transform parent)
    {
        Transform closestTank = null;
        float closestTankDst = Mathf.Infinity;
        foreach (Transform tank in parent)
        {
            if (tank != transform)
            {
                float dstToTank = CustomMath.SqrDistance(tank.transform.position, transform.position);

                if (dstToTank < closestTankDst)
                {
                    closestTankDst = dstToTank;
                    closestTank = tank;
                }
            }
        }
        return closestTank.Find("Turret");
    }
}
