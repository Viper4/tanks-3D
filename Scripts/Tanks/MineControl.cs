using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MineControl : MonoBehaviour
{
    [SerializeField] PhotonView PV;
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform mine;

    public int mineLimit = 2;
    public int minesLaid { get; set; } = 0;
    [SerializeField] float layCooldown = 2;
    public bool canLay = true;

    public IEnumerator LayMine()
    {
        if (canLay && minesLaid < mineLimit)
        {
            minesLaid++;

            canLay = false;

            Transform newMine = InstantiateMine();

            if (!PhotonNetwork.OfflineMode)
            {
                PV.RPC("InstantiateMine", RpcTarget.Others);
            }

            yield return new WaitWhile(() => newMine.GetComponent<MineBehaviour>() == null);

            MineBehaviour mineBehaviour = newMine.GetComponent<MineBehaviour>();
            mineBehaviour.owner = transform;
            mineBehaviour.ownerPV = PV;
            if (transform.CompareTag("Player"))
            {
                mineBehaviour.dataSystem = GetComponent<DataManager>();
            }

            yield return new WaitForSeconds(layCooldown);
            canLay = true;
        }
    }

    [PunRPC]
    Transform InstantiateMine()
    {
        Transform newMine = Instantiate(mine, tankOrigin.position, Quaternion.identity);

        return newMine;
    }

    [PunRPC]
    void DestroyMine(GameObject gameObject, Transform explosionEffect)
    {
        Instantiate(explosionEffect, gameObject.transform.position, Quaternion.Euler(-90, 0, 0));
        Destroy(gameObject);
    }
}
