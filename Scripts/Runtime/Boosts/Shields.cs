using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shields : MonoBehaviour
{
    public int shieldAmount = 0;
    [SerializeField] int shieldLimit = 6;
    [SerializeField] Transform tankOrigin;
    [SerializeField] GameObject shieldPrefab;
    [SerializeField] float distanceFromTank = 1.8f;
    [SerializeField] float spinRate = 100;
    [SerializeField] Transform shieldParent;
    [SerializeField] AudioSource shieldAudio;
    int lastDamageID = 1;

    Quaternion lastTankRotation;

    // Start is called before the first frame update
    void Start()
    {
        lastTankRotation = tankOrigin.localRotation;
    }

    // Update is called once per frame
    void Update()
    {
        shieldParent.localRotation = Quaternion.Euler(0, lastTankRotation.eulerAngles.y - tankOrigin.localEulerAngles.y, 0) *(Quaternion.AngleAxis(Time.deltaTime * spinRate, Vector3.up) * shieldParent.localRotation);
        lastTankRotation = tankOrigin.localRotation;
    }

    [PunRPC]
    public void AddShields(int amount)
    {
        shieldAmount += amount;
        int newShields = amount;
        if(shieldAmount > shieldLimit)
        {
            newShields -= shieldAmount - shieldLimit;
            shieldAmount = shieldLimit;
        }

        for(int i = 0; i < newShields; i++)
        {
            Instantiate(shieldPrefab, shieldParent);
        }

        if(newShields > 0)
        {
            UpdateShields();
        }
    }

    [PunRPC]
    public void DamageShieldsRPC(int amount, int damageID)
    {
        if(damageID != lastDamageID)
        {
            DamageShields(amount);
            lastDamageID = damageID;
        }
    }

    [PunRPC]
    public void DamageShields(int amount)
    {
        shieldAudio.Play();
        int previousShieldAmount = shieldAmount; // shieldParent.childCount updates too slowly
        shieldAmount -= amount;
        int shieldsToRemove = amount;
        if (shieldAmount < 0)
        {
            shieldsToRemove += shieldAmount;
        }

        for (int i = previousShieldAmount - 1; i > previousShieldAmount - shieldsToRemove - 1; i--)
        {
            Destroy(shieldParent.GetChild(i).gameObject);
        }

        if (shieldAmount > 0)
        {
            UpdateShields();
        }
    }

    public void DeleteShields()
    {
        shieldAmount = 0;
        foreach(Transform child in shieldParent)
        {
            Destroy(child.gameObject);
        }
    }

    public void UpdateShields()
    {
        float angleBetween = 360 / shieldAmount;
        for(int i = 0; i < shieldAmount; i++)
        {
            Transform shield = shieldParent.GetChild(i);
            Quaternion shieldRotation = Quaternion.AngleAxis(angleBetween * (i + 1), shieldParent.up);
            Vector3 rotatedForward = shieldRotation * Vector3.forward * distanceFromTank;
            shield.SetPositionAndRotation(shieldParent.position + rotatedForward, shieldRotation);
        }
    }
}
