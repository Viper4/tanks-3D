using System.Collections;
using UnityEngine;

public class WhiteBot : MonoBehaviour
{
    [SerializeField] float offensePitch;
    [SerializeField] float defensePitch;

    [SerializeField] Transform disappearEffect;
    [SerializeField] Transform circleEffect;

    [SerializeField] private EngineSoundManager engineSoundManager;
    [SerializeField] private TrapBot trapBot;

    [SerializeField] private MeshRenderer bodyRenderer;
    [SerializeField] private MeshRenderer turretRenderer;
    [SerializeField] private MeshRenderer barrelRenderer;

    IEnumerator Start()
    {
        yield return new WaitUntil(() => !GameManager.Instance.frozen && Time.timeScale != 0);
        PoofEffect();
    }

    private void LateUpdate()
    {
        if (!GameManager.Instance.frozen && Time.timeScale != 0)
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

    public void PoofEffect()
    {
        Instantiate(disappearEffect, transform.position, transform.rotation);
        Instantiate(circleEffect, transform);
    }
}