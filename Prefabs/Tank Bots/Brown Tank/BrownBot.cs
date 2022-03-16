using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BrownBot : MonoBehaviour
{
    public Transform target;
    float dstToTarget;

    Transform anchor;
    Transform turret;
    Transform barrel;

    public float[] fireDelay = { 1, 3 };

    public float rotateSpeed = 20;

    public Vector2 rotateRange = new Vector2(15, 45);

    Vector3 turretStartEulers;
    Vector3 barrelStartEulers;

    bool shooting = false;

    // Start is called before the first frame update
    void Awake()
    {
        if (target == null)
        {
            Debug.Log("The variable target of BrownBot has been defaulted to player's Camera Target");
            target = GameObject.Find("Player").transform.Find("Camera Target");
        }

        anchor = transform.Find("Anchor");
        barrel = transform.Find("Barrel");
        turret = transform.Find("Turret");

        barrelStartEulers = barrel.eulerAngles;
        turretStartEulers = turret.eulerAngles;
    }

    // Update is called once per frame
    void Update()
    {
        dstToTarget = Vector3.Distance(transform.position, target.position);

        float angleX = Mathf.PingPong(Time.time * rotateSpeed, rotateRange.x * 2) - rotateRange.x;
        float angleY = Mathf.PingPong(Time.time * rotateSpeed, rotateRange.y * 2) - rotateRange.y;

        turret.eulerAngles = new Vector3(-90, turretStartEulers.y + angleY, 0);
        barrel.eulerAngles = anchor.eulerAngles = new Vector3(barrelStartEulers.x + angleX, barrelStartEulers.y + angleY, 0);
        anchor.rotation *= Quaternion.Euler(90, 0, 0);

        // origin is offset forward by 1.7 to prevent ray from hitting this tank
        Vector3 origin = anchor.position + anchor.forward * 1.7f;
        // If player is in front of turret then fire
        RaycastHit hit;
        if (!shooting && Physics.Raycast(origin, anchor.forward, out hit, dstToTarget))
        {
            if(hit.transform.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                StartCoroutine(Shoot());
            }
        }
    }

    IEnumerator Shoot()
    {
        shooting = true;

        yield return new WaitForSeconds(Random.Range(fireDelay[0], fireDelay[1]));
        StartCoroutine(GetComponent<FireControl>().Shoot());

        shooting = false;
    }
}
