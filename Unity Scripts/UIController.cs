/*
    Controller for managing the UI at large
    This is the only component that can interact directly with the Monaco Logic controller
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Monaco;

using ActivityType = Communications.ActivityType;

public class UIController : MonoBehaviour
{
    private Option<MonacoLogic>       controllerGame  = new None<MonacoLogic>();
    private Option<UIScheduler>       scheduler       = new None<UIScheduler>();
    private Option<UIEventController> controllerEvent = new None<UIEventController>();
    private Option<Slider>            sliderDay       = new None<Slider>();
    private DateTime                  lastDate        = new DateTime(2016, 1, 1);
    private bool                      dayComplete     = false;
    
    //Ask the game controller to create a scheduled activity
    public Option<int> EmergeScheduledActivity(ActivityType _kind, DateTime _from, DateTime _to)
    {
	return controllerGame.Visit<Option<int>>(
	    x  => { int id = x.EmergeScheduledActivity(_kind, _from, _to);
		    return new Some<int>(id); },
	    () => { Debug.LogError("Failed to access game controller!");
		    return new None<int>(); });
    }

    //Ask the game controller to create a continuous activity
    public Option<int> EmergeContinuousActivity(ActivityType _kind)
    {
	return controllerGame.Visit<Option<int>>(
	    x  => { int id = x.EmergeContinuousActivity(_kind);
		    return new Some<int>(id); },
	    () => { Debug.LogError("Failed to access game controller!");
		    return new None<int>(); });
    }

    //Ask the game to pause
    public void RequestPause()
    {
	controllerGame.Visit<Unit>(
	    x  => { x.UIRequest("pause");
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access game logic controller!");
		    return Unit.Instance; });
    }

    //Ask the game to resume
    public void RequestResume()
    {
	controllerGame.Visit<Unit>(
	    x  => { x.UIRequest("resume");
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access game logic controller!");
		    return Unit.Instance; });
    }
    
    void Awake()
    {
	//Set up reference to the game logic controller
	var controllerGameObject = GameObject.FindWithTag("GameController");
	if(controllerGameObject == null)
	{
	    Debug.LogError("Failed to access game controller object!");
	}
	else
	{
	    var controllerGameComponent = controllerGameObject.GetComponent<MonacoLogic>();
	    if(controllerGameComponent == null)
	    {
		Debug.LogError("Failed to access game controller component!");
	    }
	    else
	    {
		controllerGame = new Some<MonacoLogic>(controllerGameComponent);
	    }
	}

	//Set up reference to the schedule controller
	var schedulerComponent = GetComponent<UIScheduler>();
	if(schedulerComponent == null)
	{
            Debug.LogError("Failed to access scheduler component!");
        }
        else
        {
            scheduler = new Some<UIScheduler>(schedulerComponent);
        }

	//Set up reference to the event controller
	var controllerEventComponent = GetComponent<UIEventController>();
	if(controllerEventComponent == null)
	{
	    Debug.LogError("Failed to access event controller component!");
	}
	else
	{
	    controllerEvent = new Some<UIEventController>(controllerEventComponent);
	}

	//Set up reference to the day progress slider
	var sliderDayObject = transform.Find("CanvasMain/SliderDay");
        if(sliderDayObject == null)
        {
            Debug.LogError ("Failed to access day slider object!");
        }
        else
        {
	    var sliderDayComponent = sliderDayObject.GetComponent<Slider>();
	    if(sliderDayComponent == null)
	    {
		Debug.LogError("Failed to access day slider component!");
	    }
	    else
	    {
		sliderDay = new Some<Slider>(sliderDayComponent);
	    }
        }
    }
    
    void Update()
    {
	//Ideally these would be called by the logic component(more of a push architecture)
	//For now the performance seems to be sufficient anyway
	UpdateSliderDay();

	controllerGame.Visit<Unit>(
	    x  => { UpdateActivities(new Some<IDictionary<int, float>>(x.QueryActivityProgress()));
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access game controller!");
		    return Unit.Instance; });
	
	if(dayComplete)
	{
	    scheduler.Visit<Unit>(
		x  => { x.CompleteDay();
			return Unit.Instance; },
		() => { Debug.LogError("Failed to access scheduler!");
			return Unit.Instance; });

	    //Check if an event should be triggered
	    controllerGame.Visit<Unit>(
		x  => controllerEvent.Visit<Unit>(
		y  => { if(x.QueryEvent())
		        {
			    y.TriggerEvent("Dummy event description",
					   false,
					   true,
					   new None<ActivityType>());
		        }

			return Unit.Instance; },
		() => { Debug.LogError("Failed to access event controller!");
			return Unit.Instance; }),
		() => { Debug.LogError("Failed to access game controller!");
			return Unit.Instance; });
	    
	    UpdateActivities(new None<IDictionary<int, float>>());
	    dayComplete = false;
	}
    }

    //Update the representation of the daytime slider
    void UpdateSliderDay()
    {
        controllerGame.Visit<Unit>(
            x  => sliderDay.Visit<Unit>(
	    y  => { var currentDate = x.QueryDateTime();
		    if(lastDate.Day < currentDate.Day)
		    {
			dayComplete = true;
			lastDate = currentDate;
		    }
		    
		    y.value = currentDate.Hour / 24.0f;
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access day slider!");
		    return Unit.Instance; }),
            () => { Debug.LogError ("Failed to access game controller!");
                    return Unit.Instance; });
    }
    
    //Update all activity UI objects
    void UpdateActivities(Option<IDictionary<int, float>> _progressDict)
    {	
	controllerGame.Visit<Unit>(
	    x  => scheduler.Visit<Unit>(
            y  => { y.UpdateActivities(_progressDict);
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access scheduler!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access game controller!");
		    return Unit.Instance; });
    }
}
