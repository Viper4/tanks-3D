using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class SaveFileSelection : MonoBehaviour
{
    [SerializeField] RectTransform template;
    [SerializeField] RectTransform container;

    [SerializeField] Text popupText;
    Coroutine popupCoroutine;

    RectTransform selectedSaveSlot;

    private void Start()
    {
        template.gameObject.SetActive(false);
    }

    public void RefreshSaveSlots()
    {
        foreach(RectTransform child in container)
        {
            if(child != template)
            {
                Destroy(child.gameObject);
            }
        }

        IEnumerable<string> allSaveFiles = SaveSystem.FilesInSaveFolder(false, ".roomsettings");
        foreach(string fileName in allSaveFiles)
        {
            InstantiateSaveSlot(fileName);
        }
    }

    void InstantiateSaveSlot(string fileName)
    {
        RectTransform newSaveSlot = Instantiate(template, container);
        newSaveSlot.Find("Label").GetComponent<Text>().text = fileName;
        newSaveSlot.gameObject.SetActive(true);
    }

    public void SelectSaveSlot(RectTransform saveSlot)
    {
        selectedSaveSlot = saveSlot;
    }

    public void CreateRoomSettings(InputField input)
    {
        string fileName = input.text;
        IEnumerable<string> allSaveFiles = SaveSystem.FilesInSaveFolder(false, ".roomsettings");

        if(fileName != null && fileName.Length != 0)
        {
            if(!allSaveFiles.Contains(fileName))
            {
                DataManager.roomSettings.SaveRoomSettings(fileName);

                InstantiateSaveSlot(fileName);
            }
            else
            {
                ShowPopup(2.5f, "File already exists");
            }
        }
        else
        {
            ShowPopup(2.5f, "No file name entered");
        }
    }

    public void SaveSelected()
    {
        if(selectedSaveSlot != null)
        {
            DataManager.roomSettings.SaveRoomSettings(selectedSaveSlot.Find("Label").GetComponent<Text>().text);
        }
        else
        {
            ShowPopup(2.5f, "No file selected");
        }
    }

    public void LoadSelected()
    {
        if(selectedSaveSlot != null)
        {
            DataManager.roomSettings = SaveSystem.LoadRoomSettings(selectedSaveSlot.Find("Label").GetComponent<Text>().text);
        }
        else if(popupText.gameObject.activeInHierarchy)
        {
            ShowPopup(2.5f, "No file selected");
        }
    }

    public void DeleteSelected()
    {
        if(selectedSaveSlot != null)
        {
            SaveSystem.DeleteFile("Settings/" + selectedSaveSlot.Find("Label").GetComponent<Text>().text + ".roomsettings");

            Destroy(selectedSaveSlot.gameObject);
        }
        else
        {
            ShowPopup(2.5f, "No file selected");
        }
    }

    private void ShowPopup(float time, string text)
    {
        if(popupCoroutine != null)
        {
            StopCoroutine(popupCoroutine);
        }
        popupCoroutine = StartCoroutine(ShowPopupRoutine(time, text));
    }

    IEnumerator ShowPopupRoutine(float time, string text)
    {
        popupText.text = text;
        popupText.gameObject.SetActive(true);
        yield return new WaitForSecondsRealtime(time);
        popupText.gameObject.SetActive(false);
    }
}
