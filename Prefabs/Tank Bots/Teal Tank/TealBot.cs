using System.Collections;
using UnityEngine;

public class TealBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;

    public float[] fireDelay = { 0.3f, 0.45f };
    
    FireControl fireControl;
    bool shooting = false;

    Transform nearbyMine = null;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");

        fireControl = GetComponent<FireControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null && !baseTankLogic.disabled)
        {
            if(fireControl.canFire && !shooting && targetSystem.TargetInLineOfFire())
            {
                StartCoroutine(Shoot());
            }

            if(nearbyMine == null)
            {
                baseTankLogic.targetTankDir = transform.forward;
            }
            else
            {
                baseTankLogic.AvoidMine(nearbyMine, 100);
            }

            // Rotating turret and barrel towards player
            baseTankLogic.targetTurretDir = targetSystem.currentTarget.position - turret.position;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        switch(other.tag)
        {
            case "Mine":
                nearbyMine = other.transform;
                break;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        switch(other.tag)
        {
            case "Mine":
                if(nearbyMine == other.transform)
                {
                    nearbyMine = null;
                }
                break;
        }
    }

    IEnumerator Shoot()
    {
        shooting = true;
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        baseTankLogic.stationary = true;
        // Stops moving and delay in firing
        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());

        shooting = false;
        baseTankLogic.stationary = false;
    }
}
