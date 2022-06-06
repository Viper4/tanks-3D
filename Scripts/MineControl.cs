using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class MineControl : MonoBehaviour
{
    [SerializeField] PlayerControl playerControl;
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

            Transform newMine;

            if (playerControl != null && playerControl.ClientManager.inMultiplayer)
            {
                newMine = PhotonNetwork.Instantiate(mine.name, tankOrigin.position, Quaternion.identity).transform;
            }
            else
            {
                newMine = Instantiate(mine, tankOrigin.position, Quaternion.identity);
            }

            newMine.GetComponent<MineBehaviour>().owner = transform;

            yield return new WaitForSeconds(layCooldown);
            canLay = true;
        }
    }
}
