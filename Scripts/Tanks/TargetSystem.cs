using MyUnityAddons.Calculations;
using Photon.Pun;
using System.Collections.Generic;
using UnityEngine;

public class TargetSystem : MonoBehaviour
{
    public Transform primaryTarget;
    public Transform currentTarget;
    [SerializeField] string preferredTargetArea = "Turret";
    public bool chooseTarget = false;

    Transform turret;
    Transform barrel;
    public LayerMask ignoreLayerMask;
    public Transform enemyParent;

    private void Start()
    {
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        if (GameManager.gameManager != null)
        {
            chooseTarget = !PhotonNetwork.OfflineMode || GameManager.autoPlay;
        }

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
                            if (hit.transform.CompareTag(target.transform.tag))
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
            else if (enemyParent.childCount == 1)
            {
                Transform enemyChild = enemyParent.GetChild(0);
                if (enemyChild.CompareTag("Player"))
                {
                    currentTarget = enemyChild.Find("Tank Origin").Find(preferredTargetArea);
                }
                else
                {
                    currentTarget = enemyChild.Find(preferredTargetArea);
                }
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
            return hit.transform.CompareTag(currentTarget.tag);
        }
        return false;
    }

    public bool TargetInLineOfFire(float maxDistance = Mathf.Infinity)
    {
        if (Physics.Raycast(barrel.position, barrel.forward, out RaycastHit barrelHit, maxDistance, ~ignoreLayerMask))
        {
            return barrelHit.transform.CompareTag(currentTarget.tag);
        }
        return false;
    }

    public Vector3 PredictedTargetPosition(float seconds)
    {
        if (currentTarget.parent != null && currentTarget.parent.TryGetComponent<Rigidbody>(out var rigidbody))
        {
            Vector3 futurePosition = CustomMath.FuturePosition(currentTarget.position, rigidbody, seconds);
            Vector3 futureDirection = futurePosition - currentTarget.position;
            if (Physics.Raycast(currentTarget.position, futureDirection, out RaycastHit hit, Vector3.Distance(currentTarget.position, futurePosition)))
            {
                return hit.point - futureDirection * 0.05f; // subtracting so the point returned isn't inside a collider
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
}
