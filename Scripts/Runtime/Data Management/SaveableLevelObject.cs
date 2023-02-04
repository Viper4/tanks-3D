using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Calculations;

public class SaveableLevelObject : MonoBehaviour
{
    public int prefabIndex;
    public MeshRenderer thisRenderer;
    public Collider thisCollider;
    [SerializeField] bool overrideColliderHiding = false;
    TransformInfo originalTransform;

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
        if (GameManager.Instance.editing && !overrideColliderHiding && !GameManager.Instance.playMode && thisRenderer != null)
            thisCollider.enabled = thisRenderer.isVisible;
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
