using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BaseUIHandler : MonoBehaviour
{
    public static Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();

    [SerializeField] List<Transform> activeElements = new List<Transform>();

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            if (!activeElements.Contains(child))
            {
                child.gameObject.SetActive(false);
            }
        }
        try
        {
            UIElements["HUD"] = UIElements["InGame"].Find("HUD");
        }
        catch
        {

        }
    }

    public static bool PauseUIActive()
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

    public void LoadScene(int index)
    {
        SceneLoader.sceneLoader.LoadScene(false, index);
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
