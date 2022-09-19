using UnityEngine;
using Photon.Pun;
using MyUnityAddons.Calculations;
using Photon.Realtime;

public class PhotonTankView : MonoBehaviourPunCallbacks, IPunObservable
{
    [SerializeField] bool player;
    [SerializeField] Behaviour[] ownerComponents;
    [SerializeField] Rigidbody rb;
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform turret;
    [SerializeField] Transform barrel;
    public string teamName = null;
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
        targetVelocity = Vector3.zero;
        targetPosition = tankOrigin.position;
        targetTankRotation = tankOrigin.rotation;
        targetTurretRotation = turret.rotation;
        targetBarrelRotation = barrel.rotation;
        if (!player)
        {
            transform.SetParent(TankManager.Instance.tankParent);
            if (!GameManager.Instance.inLobby)
            {
                baseTankLogic = GetComponent<BaseTankLogic>();

                if (PhotonNetwork.IsMasterClient)
                {
                    photonView.RequestOwnership();
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

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (!player && !GameManager.Instance.inLobby && PhotonNetwork.IsMasterClient)
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
