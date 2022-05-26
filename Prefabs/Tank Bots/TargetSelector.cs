using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TargetSelector : MonoBehaviour
{
    public Transform primaryTarget;
    [HideInInspector] public Transform currentTarget;
    [SerializeField] bool findTarget = false;

    [SerializeField] bool predictTargetPos;
    [SerializeField] float predictionScale = 1;
    [HideInInspector] public Vector3 predictedTargetPos;

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
            currentTarget = primaryTarget;
        }

        if (predictTargetPos)
        {
            Rigidbody targetRB;

            if (currentTarget.CompareTag("Tank"))
            {
                targetRB = currentTarget.parent.GetComponent<Rigidbody>();
            }
            else
            {
                targetRB = currentTarget.GetComponent<Rigidbody>();
            }

            if (targetRB != null)
            {
                predictedTargetPos = currentTarget.position + (currentTarget.forward + targetRB.velocity * predictionScale);
                Debug.DrawLine(transform.position, predictedTargetPos, Color.blue, 0.1f);
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
