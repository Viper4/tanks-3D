using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTankLogic : MonoBehaviour
{
    [SerializeField] Transform explosionEffect;

    [SerializeField] float[] pitchRange = { -45, 45 };
    [SerializeField] float[] rollRange = { -45, 45 };

    Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        if(rb != null)
        {
            rb.rotation = Quaternion.Euler(new Vector3(Clamping.ClampAngle(rb.rotation.eulerAngles.x, pitchRange[0], pitchRange[1]), rb.rotation.eulerAngles.y, Clamping.ClampAngle(rb.rotation.eulerAngles.z, rollRange[0], rollRange[1])));
        }
    }

    public void Explode()
    {
        Instantiate(explosionEffect, transform.position, Quaternion.Euler(-90, 0, 0));

        if (transform.name == "Player")
        {
            PlayerControl playerControl = GetComponent<PlayerControl>();

            Camera.main.GetComponent<CameraControl>().dead = true;
            playerControl.dead = true;
            playerControl.lives--;
            playerControl.deaths++;
            StartCoroutine(playerControl.Restart());

            GetComponent<CharacterController>().enabled = false;
            transform.Find("Barrel").gameObject.SetActive(false);
            transform.Find("Turret").gameObject.SetActive(false);
            transform.Find("Body").gameObject.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public bool IsGrounded(Vector3 origin)
    {
        return Physics.Raycast(origin, -Vector3.up, 0.05f, ~LayerMask.NameToLayer("Tank"));
    }
}
