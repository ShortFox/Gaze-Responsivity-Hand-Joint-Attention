using System.Collections;
using Tobii.G2OM;
using UnityEngine;

//Monobehaviour which implements the "IGazeFocusable" interface, meaning it will be called on when the object receives focus
public class HighlightAtGaze : MonoBehaviour, IGazeFocusable
{
    //Animation time of the highlight
    private const float AnimationTime = 0.1f;

    //The method of the "IGazeFocusable" interface, which will be called when this object receives or loses focus
    public void GazeFocusChanged(bool hasFocus)
    {
        //Stop the current animation
        StopAllCoroutines();

        //If this object received focus, fade the object's color to red
        if (hasFocus)
        {
            StartCoroutine(FadeTo(Color.red));
        }
        //If this object lost focus, fade the object's color to white
        else
        {
            StartCoroutine(FadeTo(Color.white));
        }
    }

    //Coroutine which will fade the color of the object
    private IEnumerator FadeTo(Color color)
    {
        var material = GetComponent<Renderer>().material;
        var startColor = material.color;

        var progress = 0f; // 0 - 1
        while (progress < 1f)
        {
            progress += Time.deltaTime * (1f / AnimationTime);
            material.color = Color.Lerp(startColor, color, progress);
            yield return null;
        }
    }
}