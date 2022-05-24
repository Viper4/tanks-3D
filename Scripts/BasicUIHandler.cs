using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BasicUIHandler : MonoBehaviour
{
    [SerializeField] bool mainMenu = false;
    public Dictionary<string, Transform> UIElements = new Dictionary<string, Transform>();

    private void Awake()
    {
        foreach (Transform child in transform)
        {
            UIElements[child.name] = child;

            child.gameObject.SetActive(false);
        }
        
        if(mainMenu)
        {
            UIElements["MainMenu"].gameObject.SetActive(true);
        }
    }
    
    public void LoadNextScene()
    {
        SceneLoader.sceneLoader.LoadNextScene();
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
