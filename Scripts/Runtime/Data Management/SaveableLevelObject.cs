using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;

public class SaveableLevelObject : MonoBehaviour
{
    public int prefabIndex;
    public MeshRenderer thisRenderer;
    public Collider thisCollider;
    TransformInfo originalTransform;
    [SerializeField] Collider[] colliderVisibility;
    [SerializeField] Renderer[] rendererVisibility;
    [SerializeField] GameObject[] gameObjectVisibility;

    private void Start()
    {
        originalTransform = new TransformInfo()
        {
            position = transform.position,
            rotation = transform.rotation,
        };
        if (!GameManager.Instance.editing)
        {
            if (transform.CompareTag("Spawnpoint") || transform.CompareTag("Barrier"))
            {
                thisRenderer.enabled = false;
            }
            Destroy(this);
        }
    }

    private void LateUpdate()
    {
        
    }

    private void OnBecameInvisible()
    {
        if(GameManager.Instance.editing && !GameManager.Instance.playMode)
        {
            foreach (Collider collider in colliderVisibility)
            {
                collider.enabled = false;
            }
        }

        foreach(Renderer renderer in rendererVisibility)
        {
            renderer.enabled = false;
        }
        foreach(GameObject GO in gameObjectVisibility)
        {
            GO.SetActive(false);
        }
    }
     
    private void OnBecameVisible()
    {
        if (GameManager.Instance.editing && !GameManager.Instance.playMode)
        {
            foreach (Collider collider in colliderVisibility)
            {
                collider.enabled = true;
            }
        }

        foreach(Renderer renderer in rendererVisibility)
        {
            renderer.enabled = true;
        }
        foreach(GameObject GO in gameObjectVisibility)
        {
            GO.SetActive(true);
        }
    }

    public void ResetTransform()
    {
        transform.SetPositionAndRotation(originalTransform.position, originalTransform.rotation);
    }

    public struct TransformInfo
    {
        public Vector3 position;
        public Quaternion rotation;
    }
}
