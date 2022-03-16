using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MineControl : MonoBehaviour
{
    public Transform mine;

    public int mineLimit = 2;
    public int minesLaid { get; set; } = 0;
    public float layCooldown = 2;
    bool canLay = true;

    public IEnumerator LayMine()
    {
        if (canLay && minesLaid < mineLimit)
        {
            minesLaid++;

            canLay = false;

            Transform newMine = Instantiate(mine, transform.position, Quaternion.identity);
            newMine.GetComponent<MineBehaviour>().owner = transform;

            yield return new WaitForSeconds(layCooldown);
            canLay = true;
        }
    }
}
