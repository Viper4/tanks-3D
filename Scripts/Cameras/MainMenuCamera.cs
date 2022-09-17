using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainMenuCamera : MonoBehaviour
{
    [SerializeField] Transform tankParent;
    [SerializeField] Transform target;
    int targetIndex = -1;

    [SerializeField] float dstFromTarget = 4;
    [SerializeField] Vector2 targetDstMinMaxFar = new Vector2(30, 50);
    [SerializeField] Vector2 targetDstMinMaxTank = new Vector2(0, 40);

    Vector2 targetDstLimit = new Vector2(30, 50);

    [SerializeField] float rotationSpeed = 15;

    [SerializeField] GameObject mainMenuObject;

    void SwitchTarget()
    {
        target.gameObject.SetActive(true);
        if (targetIndex > tankParent.childCount - 1)
        {
            targetDstLimit = targetDstMinMaxFar;
            targetIndex = -1;
            target = tankParent;
        }
        else if (targetIndex < 0)
        {
            targetDstLimit = targetDstMinMaxFar;
            targetIndex = tankParent.childCount;
            target = tankParent;
        }
        else
        {
            targetDstLimit = targetDstMinMaxTank;
            target = tankParent.GetChild(targetIndex).Find("Barrel");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (target == null)
        {
            targetDstLimit = targetDstMinMaxFar;
            targetIndex = -1;
            target = tankParent;
        }
        if (Input.GetMouseButtonDown(0))
        {
            targetIndex++;
            SwitchTarget();
        }
        else if (Input.GetMouseButtonDown(1))
        {
            targetIndex--;
            SwitchTarget();
        }
        else if (Input.GetKeyDown(DataManager.playerSettings.keyBinds["Toggle HUD"]))
        {
            mainMenuObject.SetActive(!mainMenuObject.activeSelf);
        }

        float zoomRate = Input.GetKey(KeyCode.LeftShift) ? 0.5f : 5f;
        // Zoom with scroll
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
        {
            dstFromTarget = Mathf.Clamp(dstFromTarget - zoomRate, targetDstLimit.x, targetDstLimit.y);
        }
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
        {
            dstFromTarget = Mathf.Clamp(dstFromTarget + zoomRate, targetDstLimit.x, targetDstLimit.y);
            transform.eulerAngles = new Vector3(60, transform.eulerAngles.y, 0);
        }

        Quaternion rotation;
        if (dstFromTarget == 0)
        {
            if (target == tankParent)
            {
                dstFromTarget = targetDstMinMaxFar[0];
                rotation = Quaternion.Euler(new Vector3(60, transform.eulerAngles.y, 0));
            }
            else
            {
                rotation = target.rotation;
                target.gameObject.SetActive(false);
            }
        }
        else
        {
            rotation = Quaternion.AngleAxis(Time.deltaTime * rotationSpeed, Vector3.up) * transform.rotation;
            target.gameObject.SetActive(true);
        }
        transform.SetPositionAndRotation(target.position - transform.forward * dstFromTarget, rotation);
    }
}
