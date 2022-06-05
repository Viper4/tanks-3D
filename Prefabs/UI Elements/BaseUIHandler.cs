using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUIHandler : MonoBehaviour
{
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();

    [SerializeField] List<Transform> activeElements = new List<Transform>();

    private void Start()
    {
        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            if (!activeElements.Contains(child))
            {
                child.gameObject.SetActive(false);
            }
            else
            {
                child.gameObject.SetActive(true);
            }
        }
        if (UIElements.ContainsKey("InGame"))
        {
            UIElements["HUD"] = UIElements["InGame"].Find("HUD");
        }
    }

    public bool PauseUIActive()
    {
        try
        {
            if (UIElements["PauseMenu"].gameObject.activeSelf || UIElements["Settings"].gameObject.activeSelf)
            {
                return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    public void LoadNextScene(float delay)
    {
        SceneLoader.sceneLoader.LoadNextScene(delay);
    }

    public void LoadScene(string sceneName)
    {
        SceneLoader.sceneLoader.LoadScene(sceneName);
    }

    public void Exit()
    {
        Application.Quit();
    }

    public void ActivateElement(Transform element)
    {
        element.gameObject.SetActive(true);
    }

    public void DeactivateElement(Transform element)
    {
        element.gameObject.SetActive(false);
    }
}
