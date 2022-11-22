using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;

public class SaveableLevelObject : MonoBehaviour
{
    public string prefabName;
    public MeshRenderer thisRenderer;
    public Collider thisCollider;

    private void Start()
    {
        if (!GameManager.Instance.editingMode)
        {
            Destroy(this);
        }
        else
        {
            if(thisRenderer == null)
            {
                thisRenderer = GetComponentInChildren<MeshRenderer>();
            }
            if(thisCollider == null && !thisRenderer.TryGetComponent<Collider>(out thisCollider))
            {
                thisCollider = GetComponentInChildren<Collider>();
            }
        }
    }

    private void LateUpdate()
    {
        thisCollider.enabled = thisRenderer.isVisible;
    }
}
