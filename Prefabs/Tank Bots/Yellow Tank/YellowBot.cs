using System.Collections;
using UnityEngine;

public class YellowBot : MonoBehaviour
{
    TargetSystem targetSystem;

    BaseTankLogic baseTankLogic;

    Transform body;
    Transform turret;
    Transform barrel;

    [SerializeField] float maxShootAngle = 30;
    public float[] fireDelay = { 0.3f, 0.45f };
    public float[] layDelay = { 0.3f, 0.6f };

    FireControl fireControl;
    bool shooting = false;
    MineControl mineControl;
    bool layingMine = false;

    Transform nearbyMine = null;

    // Start is called before the first frame Update
    void Start()
    {
        targetSystem = GetComponent<TargetSystem>();

        baseTankLogic = GetComponent<BaseTankLogic>();

        body = transform.Find("Body");
        turret = transform.Find("Turret");
        barrel = transform.Find("Barrel");

        fireControl = GetComponent<FireControl>();
        mineControl = GetComponent<MineControl>();
    }

    // Update is called once per frame
    void Update()
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0 && targetSystem.currentTarget != null)
        {
            if(fireControl.canFire && !shooting && targetSystem.TargetVisible())
            {
                StartCoroutine(Shoot());
            }

            if(mineControl.canLay && !layingMine)
            {
                StartCoroutine(LayMine());
            }

            if(!layingMine)
            {
                if(nearbyMine != null)
                {
                    baseTankLogic.AvoidMine(nearbyMine, 100);
                }
                else
                {
                    baseTankLogic.targetTankDir = transform.forward;
                }
            }
            
            // Rotating turret and barrel towards target
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
        // When angle between barrel and target is less than maxShootAngle, then stop and fire
        shooting = true;
        yield return new WaitUntil(() => Vector3.Angle(barrel.forward, baseTankLogic.targetTurretDir) < maxShootAngle);

        // Stops moving and delay in firing
        baseTankLogic.stationary = true;

        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());

        shooting = false;
        baseTankLogic.stationary = false;
    }

    IEnumerator LayMine()
    {
        layingMine = true;
        baseTankLogic.stationary = true;

        yield return new WaitForSeconds(Random.Range(layDelay[0], layDelay[1]));
        StartCoroutine(GetComponent<MineControl>().LayMine());
        transform.position += Quaternion.AngleAxis(Random.Range(-180, 180), transform.up) * transform.forward * 0.1f;
        baseTankLogic.stationary = false;

        layingMine = false;
    }
}
