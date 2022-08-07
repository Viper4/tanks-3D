using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MyUnityAddons.Math;
using System.Linq;

public class WhiteBot : MonoBehaviour
{
    [SerializeField] float offensePitch;
    [SerializeField] float defensePitch;

    [SerializeField] Transform disappearEffect;
    EngineSoundManager engineSoundManager;
    TrapBot trapBot;

    MeshRenderer bodyRenderer;
    MeshRenderer turretRenderer;
    MeshRenderer barrelRenderer;

    IEnumerator Start()
    {
        engineSoundManager = transform.Find("Engine Sounds").GetComponent<EngineSoundManager>();
        trapBot = GetComponent<TrapBot>();
        bodyRenderer = transform.Find("Body").GetComponent<MeshRenderer>();
        turretRenderer = transform.Find("Turret").GetComponent<MeshRenderer>();
        barrelRenderer = transform.Find("Barrel").GetComponent<MeshRenderer>();
        yield return new WaitUntil(() => !GameManager.frozen && Time.timeScale != 0);
        Instantiate(disappearEffect, transform.position, transform.rotation);
    }

    private void Update()
    {
        if (!GameManager.frozen && Time.timeScale != 0)
        {
            bodyRenderer.enabled = turretRenderer.enabled = barrelRenderer.enabled = false;

            switch (trapBot.mode)
            {
                case TrapBot.Mode.Offense:
                    engineSoundManager.audioSource.pitch = offensePitch;
                    break;
                case TrapBot.Mode.Pincer:
                    engineSoundManager.audioSource.pitch = offensePitch;
                    break;
                case TrapBot.Mode.Defense:
                    engineSoundManager.audioSource.pitch = defensePitch;
                    break;
            }
        }
        else
        {
            bodyRenderer.enabled = turretRenderer.enabled = barrelRenderer.enabled = true;
        }
    }
}
