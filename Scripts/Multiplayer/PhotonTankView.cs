using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using MyUnityAddons.Calculations;
using Photon.Realtime;

public class PhotonTankView : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] bool player;
    [SerializeField] Behaviour[] ownerComponents;
    [SerializeField] Transform tankOrigin;
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform turret;
    [SerializeField] Transform barrel;
    BaseTankLogic baseTankLogic;

    [SerializeField] float teleportDistance = 10;
    [SerializeField] float rotateTowardsSpeed = 90;

    private void Start()
    {
        if (!player && !GameManager.autoPlay)
        {
            if (PhotonNetwork.IsMasterClient)
            {
                photonView.RequestOwnership();
            }
            else
            {
                baseTankLogic = GetComponent<BaseTankLogic>();

                baseTankLogic.disabled = true;
                foreach (Behaviour component in ownerComponents)
                {
                    component.enabled = false;
                }
            }
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (Time.timeScale > 0)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(tankOrigin.position);
                stream.SendNext(tankOrigin.rotation);
                stream.SendNext(rb.velocity);
                stream.SendNext(turret.rotation);
                stream.SendNext(barrel.rotation);

            }
            else if (stream.IsReading)
            {
                Vector3 targetPosition = (Vector3)stream.ReceiveNext();
                tankOrigin.rotation = Quaternion.RotateTowards(tankOrigin.rotation, (Quaternion)stream.ReceiveNext(), rotateTowardsSpeed);
                Vector3 targetVelocity = (Vector3)stream.ReceiveNext();
                if (CustomMath.SqrDistance(targetPosition, tankOrigin.position) < teleportDistance * teleportDistance)
                {
                    tankOrigin.position = Vector3.MoveTowards(tankOrigin.position, targetPosition, targetVelocity.magnitude);
                }
                else
                {
                    tankOrigin.position = targetPosition;
                }
                rb.velocity = targetVelocity;
                turret.rotation = Quaternion.RotateTowards(turret.rotation, (Quaternion)stream.ReceiveNext(), rotateTowardsSpeed);
                barrel.rotation = Quaternion.RotateTowards(barrel.rotation, (Quaternion)stream.ReceiveNext(), rotateTowardsSpeed);
            }
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RequestOwnership();
            baseTankLogic.disabled = false;
            foreach (Behaviour component in ownerComponents)
            {
                component.enabled = true;
            }
        }
    }
}
