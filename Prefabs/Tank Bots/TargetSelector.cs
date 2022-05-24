using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    public Transform target;
    [SerializeField] bool findTarget = false;

    [SerializeField] Transform turret;
    [SerializeField] LayerMask ignoreLayerMask;
    [SerializeField] Transform tankParent;

    private void Awake()
    {
        if (turret == null)
        {
            turret = transform.Find("Turret");
        }

        if (!findTarget && !SceneLoader.autoPlay)
        {
            if (target == null)
            {
                Debug.Log("The variable target of BrownBot has been defaulted to the player");
                target = GameObject.Find("Player").transform;
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
        if (findTarget || SceneLoader.autoPlay)
        {
            if(tankParent.childCount > 1)
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
                    target = GetClosestTank(visibleTanks);
                }
                else
                {
                    target = GetClosestTank(tankParent);
                }
            }
            else
            {
                target = tankParent;
            }
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
                float dstToTank = Vector3.Distance(tank.transform.position, transform.position);

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
                float dstToTank = Vector3.Distance(tank.transform.position, transform.position);

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
