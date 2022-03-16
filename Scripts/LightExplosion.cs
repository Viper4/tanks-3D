using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightExplosion : MonoBehaviour
{
    Light thisLight;

    public float[] intensityRange = { 1, 0 };
    public float intensityPerSecond = -0.1f;

    void Awake()
    {
        thisLight = transform.GetComponent<Light>();
        thisLight.intensity = intensityRange[0];
    }

    // Update is called once per frame
    void Update()
    {
        if (thisLight.intensity > intensityRange[1])
        {
            thisLight.intensity -= Time.deltaTime * intensityPerSecond;
        }
    }
}
