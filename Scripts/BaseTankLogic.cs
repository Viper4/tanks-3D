using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseTankLogic : MonoBehaviour
{
    [SerializeField] Transform explosionEffect;

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
}
