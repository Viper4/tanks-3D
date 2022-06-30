using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class CrosshairManager : MonoBehaviour
{
    [SerializeField] Image reticleImage;

    public static readonly Color[] crosshairColors = { Color.white, Color.black, Color.gray, Color.red, Color.green, Color.blue, Color.yellow, Color.cyan, Color.magenta };

    public void UpdateReticleSprite(Sprite newSprite, int colorIndex, float scale)
    {
        reticleImage.sprite = newSprite;
        reticleImage.color = crosshairColors[colorIndex];
        transform.localScale = new Vector3(scale, scale, scale);
    }
}
