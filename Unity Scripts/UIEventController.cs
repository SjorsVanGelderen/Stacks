/*
  Controller for event screens
*/

using System;
using UnityEngine;
using UnityEngine.UI;

using ActivityType = Communications.ActivityType;

public class UIEventController : MonoBehaviour
{
    private Option<UIController> controllerUI      = new None<UIController>();
    private Option<Canvas>       canvasMain        = new None<Canvas>();
    private Option<Canvas>       canvasEvent       = new None<Canvas>();
    private Option<Text>         textDescription   = new None<Text>();
    private Option<Button>       buttonAccept      = new None<Button>();
    private Option<Button>       buttonDecline     = new None<Button>();
    private Option<Button>       buttonInvestigate = new None<Button>();

    //Trigger an event screen
    public void TriggerEvent(string _description, bool _mandatory, bool _investigable, Option<ActivityType> _kind)
    {
	canvasMain.Visit<Unit>(
	    a  => canvasEvent.Visit<Unit>(
	    b  => textDescription.Visit<Unit>(
	    c  => buttonAccept.Visit<Unit>(
	    d  => buttonDecline.Visit<Unit>(
	    e  => buttonInvestigate.Visit<Unit>(
	    f  => { a.enabled      = false;
		    b.enabled      = true;
		    c.text         = _description;
		    //Something with d could be added here
		    e.interactable = !_mandatory;
		    f.interactable = _investigable;
		    
		    //See if there is an associated activity to be scheduled
		    _kind.Visit<Unit>(
			x  => //Schedule event here
			Unit.Instance,
			() => //Nothing to schedule
			Unit.Instance);
		
	            return Unit.Instance; },
	    () => { Debug.LogError("Failed to access investigate button!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access decline button!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access accept button!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access description text!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access event canvas!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access main canvas!");
		    return Unit.Instance; });
    }
    
    //Triggered by event buttons
    public void PokeEventController(string _whichButton)
    {
	switch(_whichButton)
	{
	case "accept":
	    //Temporary; switch back to the game screen
	    controllerUI.Visit<Unit>(
		x  => canvasMain.Visit<Unit>(
		y  => canvasEvent.Visit<Unit>(
		z  => { x.RequestResume();
			y.enabled = true;
			z.enabled = false;
			return Unit.Instance; },
		() => { Debug.LogError("Failed to access UI controller!");
			return Unit.Instance; }),
		() => { Debug.LogError("Failed to access event canvas!");
			return Unit.Instance; }),
		() => { Debug.LogError("Failed to access main canvas!");
			return Unit.Instance; });
	    break;

	case "decline":
	    break;

	case "investigate":
	    break;

	default:
	    Debug.LogError("Failed to identify button type for identifier: " + _whichButton + "!");
	    break;
	}
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
	
	//Set up references to event UI elements
	var canvasMainObject = transform.Find("CanvasMain");
	if(canvasMainObject == null)
	{
	    Debug.LogError("Failed to access main canvas object!");
	}
	else
	{
	    var canvasMainComponent = canvasMainObject.GetComponent<Canvas>();
	    if(canvasMainComponent == null)
	    {
		Debug.LogError("Failed to access main canvas component!");
	    }
	    else
	    {
		canvasMain = new Some<Canvas>(canvasMainComponent);
	    }
	}

	var canvasEventObject = transform.Find("CanvasEvent");
	if(canvasEventObject == null)
	{
	    Debug.LogError("Failed to access event canvas object!");
	}
	else
	{
	    var canvasEventComponent = canvasEventObject.GetComponent<Canvas>();
	    if(canvasEventComponent == null)
	    {
		Debug.LogError("Failed to access event canvas component!");
	    }
	    else
	    {
		canvasEvent = new Some<Canvas>(canvasEventComponent);
	    }
	}

	var textDescriptionObject = transform.Find("CanvasEvent/Panel/TextDescription");
	if(textDescriptionObject == null)
	{
	    Debug.LogError("Failed to access text description object!");
	}
	else
	{
	    var textDescriptionComponent = textDescriptionObject.GetComponent<Text>();
	    if(textDescriptionComponent == null)
	    {
		Debug.LogError("Failed to access text description component!");
	    }
	    else
	    {
		textDescription = new Some<Text>(textDescriptionComponent);
	    }
	}

	var buttonAcceptObject = transform.Find("CanvasEvent/Panel/ButtonAccept");
	if(buttonAcceptObject == null)
	{
	    Debug.LogError("Failed to access accept button object!");
	}
	else
	{
	    var buttonAcceptComponent = buttonAcceptObject.GetComponent<Button>();
	    if(buttonAcceptComponent == null)
	    {
		Debug.LogError("Failed to access accept button component!");
	    }
	    else
	    {
		buttonAccept = new Some<Button>(buttonAcceptComponent);
	    }
	}
	
	var buttonDeclineObject = transform.Find("CanvasEvent/Panel/ButtonDecline");
	if(buttonDeclineObject == null)
	{
	    Debug.LogError("Failed to access decline button object!");
	}
	else
	{
	    var buttonDeclineComponent = buttonDeclineObject.GetComponent<Button>();
	    if(buttonDeclineComponent == null)
	    {
		Debug.LogError("Failed to access decline button component!");
	    }
	    else
	    {
		buttonDecline = new Some<Button>(buttonDeclineComponent);
	    }
	}
	
	var buttonInvestigateObject = transform.Find("CanvasEvent/Panel/ButtonInvestigate");
	if(buttonInvestigateObject == null)
	{
	    Debug.LogError("Failed to access investigate button object!");
	}
	else
	{
	    var buttonInvestigateComponent = buttonInvestigateObject.GetComponent<Button>();
	    if(buttonInvestigateComponent == null)
	    {
		Debug.LogError("Failed to access investigate button component!");
	    }
	    else
	    {
		buttonInvestigate = new Some<Button>(buttonInvestigateComponent);
	    }
	}
    }
}
