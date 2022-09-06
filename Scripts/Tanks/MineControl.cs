using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MineControl : MonoBehaviour
{
    [SerializeField] PhotonView PV;
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform mine;
    public Transform mineParent;

    public int mineLimit = 2;
    public List<Transform> laidMines { get; private set; } = new List<Transform>();
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
        if (canLay && laidMines.Count < mineLimit)
        {
            canLay = false;

            InstantiateMine();

            if (!PhotonNetwork.OfflineMode && !GameManager.autoPlay)
            {
                PV.RPC("InstantiateMine", RpcTarget.Others);
            }

            yield return new WaitForSeconds(Random.Range(layCooldown[0], layCooldown[1]));
            canLay = true;
        }
    }

    [PunRPC]
    Transform InstantiateMine()
    {
        Transform newMine = Instantiate(mine, tankOrigin.position, Quaternion.identity, mineParent);
        laidMines.Add(newMine);
        StartCoroutine(InitializeMine(newMine));
        return newMine;
    }

    IEnumerator InitializeMine(Transform mine)
    {
        mine.gameObject.SetActive(false);
        yield return new WaitUntil(() => mine.GetComponent<MineBehaviour>() != null);
        if (mine != null)
        {
            mine.gameObject.SetActive(true);

            MineBehaviour mineBehaviour = mine.GetComponent<MineBehaviour>();
            mineBehaviour.owner = transform;
            mineBehaviour.ownerPV = PV;
            mineBehaviour.explosionRadius = explosionRadius;
        }
        else
        {
            laidMines.Remove(mine);
        }
    }
}
