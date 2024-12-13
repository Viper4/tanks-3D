using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using System.Linq;
public class CloudRegionUI : MonoBehaviour
{
    private string[] regions = { null, "asia", "au", "cae", "eu", "hk", "in", "jp", "za", "sa", "kr", "tr", "uae", "us", "usw", "ussc" };
    [SerializeField] private TMP_Dropdown regionDropdown;
    private void Start()
    {
        regionDropdown.value = regions.ToList().IndexOf(PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion);
    }
    public void SetCloudRegion(int index)
    {
        PhotonNetwork.PhotonServerSettings.AppSettings.FixedRegion = regions[index];
    }
}