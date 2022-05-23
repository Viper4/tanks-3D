using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineControl : MonoBehaviour
{
    [SerializeField] Transform tankOrigin;
    [SerializeField] Transform mine;

    [SerializeField] int mineLimit = 2;
    public int minesLaid { get; set; } = 0;
    [SerializeField] float layCooldown = 2;
    public bool canLay = true;

    public IEnumerator LayMine()
    {
        if (canLay && minesLaid < mineLimit)
        {
            minesLaid++;

            canLay = false;

            Transform newMine = Instantiate(mine, tankOrigin.position, Quaternion.identity);
            newMine.GetComponent<MineBehaviour>().owner = transform;

            yield return new WaitForSeconds(layCooldown);
            canLay = true;
        }
    }
}
