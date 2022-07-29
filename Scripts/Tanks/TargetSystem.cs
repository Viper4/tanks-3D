using MyUnityAddons.Math;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSystem : MonoBehaviour
{
    public Transform primaryTarget;
    public Transform currentTarget;
    public bool chooseTarget = false;

    [SerializeField] bool predictTargetPos;
    [SerializeField] float predictionScale = 1;
    public Vector3 predictedTargetPos { get; set; }

    Transform turret;
    Transform barrel;
    public LayerMask ignoreLayerMask;
    [SerializeField] Transform tankParent;

    private void Start()
    {
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        if (!chooseTarget && !GameManager.autoPlay)
        {
            if (primaryTarget == null)
            {
                Debug.Log("The variable primaryTarget of BrownBot has been defaulted to the player");
                primaryTarget = GameObject.Find("Player").transform;
            }
            else
            {
                currentTarget = primaryTarget;
            }
        }

        if (tankParent == null)
        {
            tankParent = transform.parent;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (chooseTarget || GameManager.autoPlay)
        {
            if (tankParent.childCount > 1)
            {
                List<Transform> visibleTanks = new List<Transform>();
                foreach (Transform tank in tankParent)
                {
                    if (tank != transform)
                    {
                        Transform tankTurret = tank.transform.Find("Turret");
                        if (Physics.Raycast(tankTurret.position, turret.position - tankTurret.position, out RaycastHit hit, Mathf.Infinity, ~ignoreLayerMask, QueryTriggerInteraction.Ignore))
                        {
                            if (hit.transform == transform)
                            {
                                visibleTanks.Add(tank);
                            }
                        }
                    }
                }

                if (visibleTanks.Count != 0)
                {
                    currentTarget = GetClosestTank(visibleTanks);
                }
                else
                {
                    currentTarget = GetClosestTank(tankParent);
                }
            }
            else
            {
                currentTarget = tankParent;
            }
        }

        if (currentTarget == null)
        {
            try
            {
                currentTarget = primaryTarget;
            }
            catch
            {
                currentTarget = tankParent;
            }
        }

        if (predictTargetPos)
        {
            if (currentTarget.parent.TryGetComponent(out Rigidbody targetRB))
            {
                predictedTargetPos = currentTarget.position + (targetRB.velocity * predictionScale);
                Debug.DrawLine(turret.position, predictedTargetPos, Color.blue, 0.1f);
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

    public bool TargetInLineOfFire()
    {
        if (Physics.Raycast(barrel.position, barrel.forward, out RaycastHit barrelHit, Mathf.Infinity, ~ignoreLayerMask))
        {
            return barrelHit.transform.gameObject.layer == currentTarget.gameObject.layer;
        }
        return false;
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
