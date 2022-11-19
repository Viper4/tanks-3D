using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cell : MonoBehaviour
{
    Grid3D parentGrid;

    public GameObject occupiedObject;
    public Vector3Int gridPosition;

    [SerializeField] MeshRenderer meshRenderer;
    [SerializeField] GameObject highlightObject;

    private void OnEnable()
    {
        highlightObject.SetActive(false);
    }

    public void Init(Grid3D parent, Vector3Int position, bool offset, Color baseColor, Color offsetColor)
    {
        meshRenderer.material.color = offset ? offsetColor : baseColor;
        parentGrid = parent;
        gridPosition = position;
    }

    public bool Fill(GameObject withObject)
    {
        if(occupiedObject == null)
        {
            occupiedObject = Instantiate(withObject, transform.position, Quaternion.identity);
            occupiedObject.name = withObject.name;
            meshRenderer.enabled = false;
            return true;
        }
        return false;
    }

    public bool Clear()
    {
        if(occupiedObject != null)
        {
            Destroy(occupiedObject);
            meshRenderer.enabled = true;
            return true;
        }
        return false;
    }

    public void SetHighlight(bool value)
    {
        highlightObject.SetActive(value);
    }

    private void OnMouseEnter()
    {
        highlightObject.SetActive(true);
    }

    private void OnMouseExit()
    {
        highlightObject.SetActive(false);
    }
}
