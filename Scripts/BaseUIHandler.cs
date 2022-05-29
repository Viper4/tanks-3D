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
        if(UIElements["InGame"] != null)
        {
            UIElements["HUD"] = UIElements["InGame"].Find("HUD");
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
