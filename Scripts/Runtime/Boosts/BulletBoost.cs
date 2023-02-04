using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletBoost : MonoBehaviour
{
    [SerializeField] PhotonView photonView;
    [SerializeField] FireControl fireControl;
    [SerializeField] PlayerUI playerUI;

    Coroutine boostRoutine;

    [PunRPC]
    public void ApplyBulletBoost(float duration, int bulletIndex, float speed, int pierceLimit, int ricochetLevel, float explosionRadius)
    {
        if(boostRoutine != null)
        {
            StopCoroutine(boostRoutine);
        }

        boostRoutine = StartCoroutine(BoostRoutine(duration, bulletIndex, speed, pierceLimit, ricochetLevel, explosionRadius));
    }

    IEnumerator BoostRoutine(float duration, int bulletIndex, float speed, int pierceLimit, int ricochetLevel, float explosionRadius)
    {
        if(playerUI != null && (PhotonNetwork.OfflineMode || photonView.IsMine))
            playerUI.ChangeBulletIconIndex(bulletIndex);
        fireControl.bulletSettings.bulletIndex = bulletIndex;
        fireControl.bulletSettings.speed = speed;
        fireControl.bulletSettings.pierceLimit = pierceLimit;
        fireControl.bulletSettings.ricochetLevel = ricochetLevel;
        fireControl.bulletSettings.explosionRadius = explosionRadius;

        yield return new WaitForSeconds(duration);

        if(playerUI != null && (PhotonNetwork.OfflineMode || photonView.IsMine))
            playerUI.ChangeBulletIconIndex(fireControl.originalBulletSettings.bulletIndex);
        fireControl.bulletSettings.bulletIndex = fireControl.originalBulletSettings.bulletIndex;
        fireControl.bulletSettings.speed = fireControl.originalBulletSettings.speed;
        fireControl.bulletSettings.pierceLimit = fireControl.originalBulletSettings.pierceLimit;
        fireControl.bulletSettings.ricochetLevel = fireControl.originalBulletSettings.ricochetLevel;
        fireControl.bulletSettings.explosionRadius = fireControl.originalBulletSettings.explosionRadius;

        boostRoutine = null;
    }
}
