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
    private Option<Text>              textDate        = new None<Text>();
    private Option<Text>              textPlayer      = new None<Text>();
    private DateTime                  lastDate        = new DateTime(2016, 1, 1);
    private bool                      dayComplete     = false;
    
    //Ask the game controller to create a scheduled activity
    public Option<int> EmergeScheduledActivity(ActivityType _kind, DateTime _from, DateTime _to)
    {
	return controllerGame.Visit<Option<int>>(
	    x  => { int id = x.EmergeScheduledActivity(_kind, _from, _to);
		    return new Some<int>(id); },
	    () => { Debug.LogError("Failed to access game logic controller!");
		    return new None<int>(); });
    }

    //Ask the game controller to create a continuous activity
    public Option<int> EmergeContinuousActivity(ActivityType _kind)
    {
	return controllerGame.Visit<Option<int>>(
	    x  => { int id = x.EmergeContinuousActivity(_kind);
		    return new Some<int>(id); },
	    () => { Debug.LogError("Failed to access game logic controller!");
		    return new None<int>(); });
    }

    //Ask the game controller to remove a continuous activity
    public void CleanseActivity(int _id)
    {
	controllerGame.Visit<Unit>(
	    x  => { x.CleanseActivity(_id);
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access game logic controller!");
		    return Unit.Instance; });
    }
    
    //Ask the game to tell us the start date of a given activity
    public DateTime QueryActivityStartDate(int _id)
    {
        return controllerGame.Visit<DateTime>(
            x  => x.QueryActivityStartDate(_id),
            () => { Debug.LogError("Failed to access game logic controller! Using dummy date!");
                    return new DateTime(2016, 1, 1); });
    }
    
    //Ask the game to tell us the end date of a given activity
    public DateTime QueryActivityEndDate(int _id)
    {
        return controllerGame.Visit<DateTime>(
            x  => x.QueryActivityEndDate(_id),
            () => { Debug.LogError("Failed to access game logic controller! Using dummy date!");
                    return new DateTime(2016, 1, 1); });
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
    
    //Ask the game to process and flush event data
    public void ProcessEvent()
    {
	controllerGame.Visit<Unit>(
	    x  => { x.ProcessEvent();
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access game logic controller!");
		    return Unit.Instance; });
    }

    //Ask the game to flush event data
    public void FlushEvent()
    {
	controllerGame.Visit<Unit>(
	    x  => { x.FlushEvent();
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

        //Set up reference to the date text object
        var textDateObject = transform.Find("CanvasMain/TextDate");
        if(textDateObject == null)
        {
            Debug.LogError("Failed to access date text object!");
        }
        else
        {
            var textDateComponent = textDateObject.GetComponent<Text>();
            if(textDateComponent == null)
            {
                Debug.LogError("Failed to access date text component!");
            }
            else
            {
                textDate = new Some<Text>(textDateComponent);
            }
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

	var textPlayerObject = transform.Find("CanvasMain/PanelPlayerStatus/TextPlayerStatus");
	if(textPlayerObject == null)
	{
	    Debug.LogError("Failed to access player status text object!");
	}
	else
	{
	    var textPlayerComponent = textPlayerObject.GetComponent<Text>();
	    if(textPlayerComponent == null)
	    {
		Debug.LogError("Failed to access player status text component!");
	    }
	    else
	    {
		textPlayer = new Some<Text>(textPlayerComponent);
	    }
	}
    }
    
    void Update()
    {
	//Ideally these would be called by the logic component(more of a push architecture)
	//For now the performance seems to be sufficient anyway

        if(lastDate >= (new DateTime(2017, 1, 1)).AddDays(-1))
        {
            //End of the year, stop updating
            //Not tested
            return;
        }
        
	UpdateDate();
	UpdatePlayerStatus();

	controllerGame.Visit<Unit>(
	    x  => { UpdateActivities(new Some<IDictionary<int, float>>(x.QueryActivityProgress()));
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access game controller!");
		    return Unit.Instance; });
	
	if(dayComplete) //This might miss, causing an infinite loop of days that never complete, please fix
	{            
	    scheduler.Visit<Unit>(
		x  => { x.CompleteDay();
			return Unit.Instance; },
		() => { Debug.LogError("Failed to access scheduler!");
			return Unit.Instance; });
            
	    //Check if an event should be triggered
	    controllerGame.Visit<Unit>(
		x  => controllerEvent.Visit<Unit>(
		y  => { if(x.QueryEvent()) //Request roll for random event
		        {
			    //Read all event data
			    string               description    = x.QueryEventDescription();
			    bool                 mandatory      = x.QueryEventMandatory();
			    bool                 investigable   = x.QueryEventInvestigable();
			    ActivityType         activity       = x.QueryEventActivity();
			    Option<ActivityType> activityOption = new None<ActivityType>();
			    
			    if(activity != ActivityType.Default)
			    {
				activityOption = new Some<ActivityType>(activity);
			    }
			    
			    y.TriggerEvent(description,
					   mandatory,
					   investigable,
					   activityOption);
		        }

			return Unit.Instance; },
		() => { Debug.LogError("Failed to access event controller!");
			return Unit.Instance; }),
		() => { Debug.LogError("Failed to access game logic controller!");
			return Unit.Instance; });
	    
	    UpdateActivities(new None<IDictionary<int, float>>());
	    dayComplete = false;
	}

	//Temporary escape
	if(Input.GetKeyDown(KeyCode.Escape))
	{
	    Application.Quit();
	}
    }

    //Update the representation of the daytime slider
    void UpdateDate()
    {
        controllerGame.Visit<Unit>(
            x  => textDate.Visit<Unit>(
            y  => sliderDay.Visit<Unit>(
	    z  => { var currentDate = x.QueryDateTime();
                    if(currentDate.Year > 2016)
                    {
                        //End of the year, stop updating
                        //Not tested
                        return Unit.Instance;
                    }
                    
		    if(lastDate.Day < currentDate.Day || lastDate.Month < currentDate.Month)
		    {
                        y.text      = currentDate.ToShortDateString();
			lastDate    = currentDate;
                        dayComplete = true;
		    }
		    
		    z.value = (currentDate.Hour + currentDate.Minute / 60.0f) / 24.0f;
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access day slider!");
		    return Unit.Instance; }),
            () => { Debug.LogError("Failed to access date text!");
                    return Unit.Instance; }),
            () => { Debug.LogError("Failed to access game logic controller!");
                    return Unit.Instance; });
    }

    //Update player status text
    void UpdatePlayerStatus()
    {
	controllerGame.Visit<Unit>(
	    x  => textPlayer.Visit<Unit>(
	    y  => { y.text = x.QueryPlayerStatus();
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access player status text!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access game logic controller!");
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
