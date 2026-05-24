using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class FadeCanvas : MonoBehaviour
{
    //public Image fadeImage;
    CanvasGroup group;

    private void OnEnable()
    {
        if (!group)
        { 
            group = GetComponent<CanvasGroup>();
        }

        EventCenter.Instance.AddEventListener<float>("FadeIn",OnFadeInEvent);
        EventCenter.Instance.AddEventListener<float>("FadeOut",OnFadeOutEvent);
    }
    private void OnDisable()
    {
        EventCenter.Instance.RemoveEventListener<float>("FadeIn", OnFadeInEvent);
        EventCenter.Instance.RemoveEventListener<float>("FadeOut", OnFadeOutEvent);
    }


    private void OnFadeInEvent(float duration)
    {
        //fadeImage.DOBlendableColor(new Color(1,1,1,1), duration);
        group.DOFade(1f, duration);
    }

    private void OnFadeOutEvent(float duration)
    {
        //fadeImage.DOBlendableColor(new Color(1, 1, 1, 0), duration);
        group.DOFade(0f, duration);
    }
}
