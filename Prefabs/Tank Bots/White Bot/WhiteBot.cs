using System.Collections;
using UnityEngine;

public class WhiteBot : MonoBehaviour
{
    [SerializeField] float offensePitch;
    [SerializeField] float defensePitch;

    [SerializeField] Transform disappearEffect;
    [SerializeField] Transform circleEffect;

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
        yield return new WaitUntil(() => !GameManager.Instance.frozen && Time.timeScale != 0);
        PoofEffect();
    }

    private void LateUpdate()
    {
        if(!GameManager.Instance.frozen && Time.timeScale != 0)
        {
            bodyRenderer.enabled = turretRenderer.enabled = barrelRenderer.enabled = false;

            switch(trapBot.mode)
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
