using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MineControl : MonoBehaviourPun
{
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform mine;
    public Transform mineParent;

    BaseTankLogic baseTankLogic;

    public int mineLimit = 2;
    public List<Transform> laidMines { get; private set; } = new List<Transform>();
    public float[] layCooldown = { 2f, 4f };
    public float explosionRadius = 7f;
    public bool canLay { get; set; } = false;

    private IEnumerator Start()
    {
        baseTankLogic = GetComponent<BaseTankLogic>();
        if(GameManager.Instance.autoPlay)
        {
            mineParent = GameObject.Find("ToClear").transform;
        }

        yield return new WaitForSeconds(Random.Range(layCooldown[0], layCooldown[1]));
        canLay = true;
    }

    public IEnumerator LayMine()
    {
        if(!baseTankLogic.disabled && canLay && laidMines.Count < mineLimit)
        {
            canLay = false;

            InstantiateMine();

            yield return new WaitForSeconds(Random.Range(layCooldown[0], layCooldown[1]));
            canLay = true;
        }
    }

    [PunRPC]
    public void MultiplayerInstantiateMine(Vector3 position, int mineID)
    {
        Transform newMine = Instantiate(mine, position, Quaternion.identity, mineParent);
        laidMines.Add(newMine);
        StartCoroutine(InitializeMine(newMine, mineID));
    }

    Transform InstantiateMine()
    {
        Transform newMine = Instantiate(mine, tankOrigin.position, Quaternion.identity, mineParent);
        laidMines.Add(newMine);
        StartCoroutine(InitializeMine(newMine, newMine.GetInstanceID()));
        return newMine;
    }

    IEnumerator InitializeMine(Transform mine, int ID)
    {
        mine.gameObject.SetActive(false);
        yield return new WaitUntil(() => mine.GetComponent<MineBehaviour>() != null);
        if(mine != null)
        {
            mine.gameObject.SetActive(true);

            MineBehaviour mineBehaviour = mine.GetComponent<MineBehaviour>();
            Explosive explosive = mine.GetComponent<Explosive>();
            mineBehaviour.owner = transform;
            mineBehaviour.ownerPV = photonView;
            explosive.owner = transform;
            explosive.ownerPV = photonView;
            explosive.explosionRadius = explosionRadius;
            if(!PhotonNetwork.OfflineMode && !GameManager.Instance.inLobby)
            {
                mineBehaviour.mineID = ID;

                if(photonView.IsMine)
                {
                    photonView.RPC("MultiplayerInstantiateMine", RpcTarget.Others, new object[] { tankOrigin.position, ID });
                }
            }
        }
        else
        {
            laidMines.Remove(mine);
        }
    }
}
