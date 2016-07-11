/*
  Simple controller for the pause toggling button
*/

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UIPauseController : MonoBehaviour
{
    private Option<UIController> controllerUI = new None<UIController>();
    private Option<Text>         text         = new None<Text>();
    private Option<Transform>    textPaused   = new None<Transform>();
    private bool                 pause        = false;
    
    //Reset the button status
    public void Reset()
    {
        text.Visit<Unit>(
            x  => textPaused.Visit<Unit>(
            y  => { pause  = true;
                    x.text = "Pause";
                    y.gameObject.SetActive(false);
                    return Unit.Instance; },
            () => { Debug.LogError("Failed to access paused text object!");
                    return Unit.Instance; }),
            () => { Debug.LogError("Failed to access text component!");
                    return Unit.Instance; });
    }
    
    //Associated button was clicked, toggle pause state
    public void PokePauseController()
    {
        controllerUI.Visit<Unit>(
            x  => text.Visit<Unit>(
            y  => textPaused.Visit<Unit>(
            z  => { if(pause)
                    {
                        x.RequestPause();
                        y.text = "Resume";
                    }
                    else
                    {
                        x.RequestResume();
                        y.text = "Pause";
                    }
                    
                    z.gameObject.SetActive(!z.gameObject.activeSelf);
                    pause = !pause;
                    return Unit.Instance; },
            () => { Debug.LogError("Failed to access paused text object!");
                    return Unit.Instance; }),
            () => { Debug.LogError("Failed to access text component!");
                    return Unit.Instance; }),
            () => { Debug.LogError("Failed to access UI controller!");
                    return Unit.Instance; });
    }
    
    void Awake()
    {
        //Set up reference to the UI controller
        var controllerUIComponent = GetComponent<UIController>();
        if(controllerUIComponent == null)
        {
            Debug.LogError("Failed to access UI controller component!");
        }
        else
        {
            controllerUI = new Some<UIController>(controllerUIComponent);
        }
        
        //Set up reference to the text component
        var textObject = transform.Find("CanvasMain/ContainerActions/Viewport/Content/ButtonPause/Text");
        if(textObject == null)
        {
            Debug.LogError("Failed to access text object!");
        }
        else
        {
            var textComponent = textObject.GetComponent<Text>();
            if(textComponent == null)
            {
                Debug.LogError("Failed to access text component!");
            }
            else
            {
                text = new Some<Text>(textComponent);
            }
        }

        //Set up reference to the paused text object
        var textPausedObject = transform.Find("CanvasMain/TextPaused");
        if(textPausedObject == null)
        {
            Debug.LogError("Failed to access paused text object!");
        }
        else
        {
            textPaused = new Some<Transform>(textPausedObject);
        }
    }
}
