using UnityEngine;
using Photon.Pun;
using MyUnityAddons.Calculations;
using Photon.Realtime;
using MyUnityAddons.CustomPhoton;
using System.Collections;

public class PhotonTankView : MonoBehaviourPunCallbacks, IPunObservable, IPunOwnershipCallbacks, IPunInstantiateMagicCallback
{
    [SerializeField] bool player;
    [SerializeField] Behaviour[] ownerComponents;
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform turret;
    [SerializeField] Transform barrel;
    public string teamName = "FFA";
    BaseTankLogic baseTankLogic;

    [SerializeField] float teleportDistance = 10;
    [SerializeField] float rotateTowardsSpeed = 180;
    [SerializeField] float setRotationMinAngle = 90;

    Vector3 targetVelocity;
    Vector3 targetPosition;
    Quaternion targetTankRotation;
    Quaternion targetTurretRotation;
    Quaternion targetBarrelRotation;

    private void Start()
    {
        baseTankLogic = GetComponent<BaseTankLogic>();

        targetVelocity = Vector3.zero;
        targetPosition = tankOrigin.position;
        targetTankRotation = tankOrigin.rotation;
        targetTurretRotation = turret.rotation;
        targetBarrelRotation = barrel.rotation;
        if (!player)
        {
            transform.SetParent(TankManager.Instance.tankParent);
        }
    }

    private void Update()
    {
        if (!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby && !photonView.IsMine)
        {
            rb.velocity = targetVelocity;
            tankOrigin.position = Vector3.MoveTowards(tankOrigin.position, targetPosition, (targetPosition - tankOrigin.position).magnitude * PhotonNetwork.SerializationRate * Time.deltaTime);
            tankOrigin.rotation = Quaternion.RotateTowards(tankOrigin.rotation, targetTankRotation, rotateTowardsSpeed * Time.deltaTime);
            turret.rotation = Quaternion.RotateTowards(turret.rotation, targetTurretRotation, rotateTowardsSpeed * Time.deltaTime);
            barrel.rotation = Quaternion.RotateTowards(barrel.rotation, targetBarrelRotation, rotateTowardsSpeed * Time.deltaTime);
        }
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (Time.timeScale > 0)
        {
            if (stream.IsWriting)
            {
                stream.SendNext(rb.velocity);
                stream.SendNext(tankOrigin.position);
                stream.SendNext(tankOrigin.rotation);
                stream.SendNext(turret.rotation);
                stream.SendNext(barrel.rotation);
                stream.SendNext(teamName);
            }
            else if (stream.IsReading)
            {
                targetVelocity = (Vector3)stream.ReceiveNext();
                targetPosition = (Vector3)stream.ReceiveNext();
                if (CustomMath.SqrDistance(targetPosition, tankOrigin.position) > teleportDistance * teleportDistance)
                {
                    tankOrigin.position = targetPosition;
                }
                targetTankRotation = (Quaternion)stream.ReceiveNext();
                if (Quaternion.Angle(targetTankRotation, tankOrigin.rotation) > setRotationMinAngle)
                {
                    tankOrigin.rotation = targetTankRotation;
                }
                targetTurretRotation = (Quaternion)stream.ReceiveNext();
                if (Quaternion.Angle(targetTurretRotation, turret.rotation) > setRotationMinAngle)
                {
                    turret.rotation = targetTurretRotation;
                }
                targetBarrelRotation = (Quaternion)stream.ReceiveNext();
                if (Quaternion.Angle(targetBarrelRotation, barrel.rotation) > setRotationMinAngle)
                {
                    barrel.rotation = targetBarrelRotation;
                }
                teamName = (string)stream.ReceiveNext();
            }
        }
    }

    public void OnOwnershipRequest(PhotonView targetView, Player requestingPlayer)
    {
        if (targetView != photonView)
            return;

        photonView.TransferOwnership(requestingPlayer);
    }

    public void OnOwnershipTransfered(PhotonView targetView, Player previousOwner)
    {
        if (targetView != photonView)
            return;

        StartCoroutine(OwnershipChange());
    }

    IEnumerator OwnershipChange()
    {
        yield return new WaitUntil(() => baseTankLogic != null);

        if (photonView.IsMine)
        {
            baseTankLogic.disabled = false;
            foreach (Behaviour component in ownerComponents)
            {
                component.enabled = true;
            }
        }
        else
        {
            baseTankLogic.disabled = true;
            foreach (Behaviour component in ownerComponents)
            {
                component.enabled = false;
            }
        }
    }

    public void OnOwnershipTransferFailed(PhotonView targetView, Player senderOfFailedRequest)
    {
        Debug.LogWarning("Ownership transfer failed on " + targetView.ViewID + " from " + senderOfFailedRequest.NickName);
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        if (!player)
        {
            object[] instantiationData = info.photonView.InstantiationData;

            if (TryGetComponent<TargetSystem>(out var targetSystem))
            {
                if ((bool)instantiationData[0])
                {
                    targetSystem.enemyParents.Add(TankManager.Instance.tankParent);
                }
                if ((bool)instantiationData[1])
                {
                    targetSystem.enemyParents.Add(PlayerManager.Instance.playerParent);
                }
            }

            transform.SetParent(TankManager.Instance.tankParent);
        }
    }
}
