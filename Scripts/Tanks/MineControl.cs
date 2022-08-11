using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MineControl : MonoBehaviour
{
    [SerializeField] PhotonView PV;
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform mine;
    [SerializeField] Transform mineParent;

    public int mineLimit = 2;
    public int minesLaid { get; set; } = 0;
    public float[] layCooldown = { 2f, 4f };
    public float explosionRadius = 7f;
    public bool canLay { get; set; } = false;

    private IEnumerator Start()
    {
        if (GameManager.autoPlay)
        {
            mineParent = GameObject.Find("ToClear").transform;
        }

        yield return new WaitForSeconds(Random.Range(layCooldown[0], layCooldown[1]));
        canLay = true;
    }

    public IEnumerator LayMine()
    {
        if (canLay && minesLaid < mineLimit)
        {
            minesLaid++;

            canLay = false;

            Transform newMine = InstantiateMine();

            if (!PhotonNetwork.OfflineMode && !GameManager.autoPlay)
            {
                PV.RPC("InstantiateMine", RpcTarget.Others);
            }

            yield return new WaitWhile(() => newMine.GetComponent<MineBehaviour>() == null);

            MineBehaviour mineBehaviour = newMine.GetComponent<MineBehaviour>();
            mineBehaviour.owner = transform;
            mineBehaviour.ownerPV = PV;
            mineBehaviour.explosionRadius = explosionRadius;
            if (transform.CompareTag("Player"))
            {
                mineBehaviour.dataSystem = GetComponent<DataManager>();
            }

            yield return new WaitForSeconds(Random.Range(layCooldown[0], layCooldown[1]));
            canLay = true;
        }
    }

    [PunRPC]
    Transform InstantiateMine()
    {
        Transform newMine = Instantiate(mine, tankOrigin.position, Quaternion.identity, mineParent);

        return newMine;
    }
}
