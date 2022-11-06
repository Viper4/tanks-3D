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
    public List<Transform> enemyParents;

    PhotonTankView myPTV;

    private void Start()
    {
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        if (GameManager.Instance != null)
        {
            chooseTarget = !PhotonNetwork.OfflineMode || GameManager.Instance.autoPlay;
        }

        if (!chooseTarget)
        {
            if (primaryTarget == null)
            {
                primaryTarget = GameObject.Find("Player").transform.Find("Tank Origin").Find(preferredTargetArea);
            }

            currentTarget = primaryTarget;
        }

        if (enemyParents == null)
        {
            enemyParents.Add(transform.parent);
        }

        myPTV = GetComponent<PhotonTankView>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.timeScale != 0 && !GameManager.Instance.frozen)
        {
            if (chooseTarget)
            {
                if (enemyParents.Count > 0)
                {
                    List<Transform> allTargets = new List<Transform>();
                    List<Transform> visibleTargets = new List<Transform>();
                    for (int i = 0; i < enemyParents.Count; i++)
                    {
                        Transform enemyParent = enemyParents[i];
                        foreach (Transform tank in enemyParent)
                        {
                            if (tank != transform && (myPTV.teamName == "FFA" || tank.GetComponent<PhotonTankView>().teamName != myPTV.teamName))
                            {
                                Transform target = GetTargetArea(tank);

                                allTargets.Add(target);
                                if (Physics.Raycast(turret.position, target.position - turret.position, out RaycastHit hit, Mathf.Infinity, ~ignoreLayerMask, QueryTriggerInteraction.Ignore))
                                {
                                    if (hit.transform.CompareTag(target.tag))
                                    {
                                        visibleTargets.Add(target);
                                    }
                                }
                            }
                        }
                    }

                    if (visibleTargets.Count > 0)
                    {
                        currentTarget = transform.ClosestTransform(visibleTargets);
                    }
                    else
                    {
                        currentTarget = transform.ClosestTransform(allTargets);
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
                    currentTarget = enemyParents[0];
                }
            }
        }
    }

    public Transform GetTargetArea(Transform target)
    {
        Transform targetArea = target;
        if (target.CompareTag("Player"))
        {
            targetArea = target.Find("Tank Origin").Find(preferredTargetArea);
        }
        else
        {
            targetArea = target.Find(preferredTargetArea);
        }
        return targetArea;
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
