/*
    Controller for the scheduler of UI activities
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ContainerDict      = System.Collections.Generic.Dictionary<System.DateTime, UIActivityContainer>;
using ActivityType       = Communications.ActivityType;
using ActivitySpriteDict = System.Collections.Generic.Dictionary<Communications.ActivityType, Option<UnityEngine.Sprite>>;

public class UIScheduler : MonoBehaviour
{
    private Option<UIController>              controllerUI         = new None<UIController>();
    private Option<GameObject>                containerPrefab      = new None<GameObject>();
    private Option<Button>                    buttonStudio         = new None<Button>();
    private Option<ContainerDict>             scheduleContainers   = new None<ContainerDict>();
    private Option<List<UIActivityContainer>> previewContainers    = new None<List<UIActivityContainer>>();
    private Option<List<UIActivityContainer>> continuousContainers = new None<List<UIActivityContainer>>();
    private Option<ActivityType>              activityToSchedule   = new None<ActivityType>();
    private Option<int>                       daysToSchedule       = new None<int>();
    private ActivitySpriteDict                activitySprites      = new ActivitySpriteDict();
    private DateTime                          time                 = new DateTime(2016, 1, 1);
    
    //Load a single activity sprite
    //Helper function for populating the activity sprite dictionary
    Option<Sprite> LoadActivitySprite(string _name)
    {
	var sprite = Resources.Load<Sprite>("Sprites/Activities/" + _name);
	if(sprite == null)
	{
	    Debug.LogError("Failed to load activity sprite for " + _name + "!");
	    return new None<Sprite>();
	}
	else
	{
	    return new Some<Sprite>(sprite);
	}
    }
    
    //Loop through days between dates
    //Thanks to the user 'mquander', found at
    //http://stackoverflow.com/questions/1847580/how-do-i-loop-through-a-date-range
    public IEnumerable<DateTime> EachDay(DateTime _from, DateTime _to)
    {
	for(var day = _from.Date; day.Date < _to.Date; day = day.AddDays(1))
	{
	    yield return day;
	}
    }
    
    //Initiate scheduling process
    public void StartScheduling(ActivityType _kind, int _days, bool _mandatory)
    {
	activityToSchedule = new Some<ActivityType>(_kind);
	daysToSchedule     = new Some<int>(_days);
	
	if(_mandatory)
	{
	    buttonStudio.Visit<Unit>(
		x  => { x.interactable = false;
			return Unit.Instance; },
		() => { Debug.LogError("Failed to access studio button!");
			return Unit.Instance; });
	}
    }

    //Triggered when containers are clicked
    public void ContainerInteraction(DateTime _time)
    {
	activityToSchedule.Visit<Unit>(
	    x  => daysToSchedule.Visit<Unit>(
		y  => { AttemptSchedule(x, y, _time);
			return Unit.Instance; },
		() => { Debug.LogError("Failed to access number of days!");
			return Unit.Instance; }),
	    () => { return Unit.Instance; });
    }

    //Update the all activity UI states
    //Quite messy due to poor planning of the code's internal structure
    public void UpdateActivities(Option<IDictionary<int, float>> _progressDict)
    {
	previewContainers.Visit<Unit>(
	    x  => scheduleContainers.Visit<Unit>(
	    y  => continuousContainers.Visit<Unit>(
	    z  => { //Update preview bar
		    if(x.Count > 0)
		    {
			for(int i = 0; i < x.Count; i++)
                        {
			    var keyTime = time.AddDays(i - x.Count / 2);
			    if(y.ContainsKey(keyTime))
			    {
				//Match the preview container images to those in the schedule
				x[i].SetSprite(y[keyTime].GetSprite());
				
				//Assign the matching ID
				x[i].SetID(y[keyTime].GetID());
				
				//Reset the pie progress for anything that isn't currently in focus
				if(i < (int)(x.Count / 2))
				{
				    x[i].SetProgress(1);
				}
				else if(i > (int)(x.Count / 2))
				{
				    x[i].SetProgress(0);
				}
			    }
			}
			
			//Set the pie progress of each activity
			//Not very elegant at the moment, but sufficiently performant
			var centralContainer = x[(int)(x.Count / 2)];
			_progressDict.Visit<Unit>(
			    q  => { //Update preview containers
				    var id = centralContainer.GetID();
				    id.Visit<Unit>(
					w  => { foreach(KeyValuePair<int, float> entry in q)
					        {
						    if(entry.Key == w)
						    {
							centralContainer.SetProgress(entry.Value);
						    }
						}
						
						return Unit.Instance; },
					() => { centralContainer.SetProgress(0);
						return Unit.Instance; });

				    //Update continuous containers
				    foreach(var container in z)
				    {
					var entryID = container.GetID();
					entryID.Visit<Unit>(
					    w  => { foreach(KeyValuePair<int, float> progressEntry in q)
						    {
							if(w == progressEntry.Key)
							{
							    container.SetProgress(progressEntry.Value);
							}
						    }

						    return Unit.Instance; },
					    () => { Debug.LogError("Failed to access continuous activity ID! None set?");
						    return Unit.Instance; });
						
				    }
				    
				    return Unit.Instance; },
			    () => { //No progress to record
			            return Unit.Instance; });
		    }
		    
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access continuous containers!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access schedule containers!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access preview containers!");
	            return Unit.Instance; });
    }
    
    //Add a day
    public void CompleteDay()
    {
	time = time.AddDays(1);
    }
    
    void Awake()
    {
	//Set up reference to the game logic controller
	var controllerUIComponent = GetComponent<UIController>();
	if(controllerUIComponent == null)
	{
	    Debug.LogError("Failed to access UI controller component!");
	}
	else
	{
	    controllerUI = new Some<UIController>(controllerUIComponent);
	}

	//Set upreference to the container prefab
        var containerPrefabObject = Resources.Load<GameObject>("Prefabs/Activities/ActivityContainer");
        if(containerPrefabObject == null)
        {
            Debug.LogError("Failed to load container prefab!");
        }
        else
        {
            containerPrefab = new Some<GameObject>(containerPrefabObject);
        }

	//Set up reference to the studio button
        var buttonStudioObject = transform.Find("CanvasSchedule/ButtonStudio");
        if(buttonStudioObject == null)
        {
            Debug.LogError("Failed to access studio button object!");
        }
        else
        {
	    var buttonStudioComponent = buttonStudioObject.GetComponent<Button>();
	    if(buttonStudioComponent == null)
	    {
		Debug.LogError("Failed to access studio button component!");
	    }
	    else
	    {
		buttonStudio = new Some<Button>(buttonStudioComponent);
	    }
        }
	
	//Set up references to activity sprites
	if(activitySprites == null)
	{
	    Debug.LogError("Failed to access activity sprites dictionary!");
	}
	else
	{
	    activitySprites.Add(ActivityType.Human,     LoadActivitySprite("human"));
	    activitySprites.Add(ActivityType.Social,    LoadActivitySprite("social"));
	    activitySprites.Add(ActivityType.Financial, LoadActivitySprite("financial"));
	    activitySprites.Add(ActivityType.Job,       LoadActivitySprite("job"));
	}

	//Set up schedule preview
	var previewObject = GameObject.FindWithTag("SchedulePreview");
	if(previewObject == null)
	{
	    Debug.LogError("Failed to access schedule preview object!");
	}
	else
	{
	    try
	    {
		var containers = new List<UIActivityContainer>();
		foreach(Transform container in previewObject.transform)
		{
		    if(!container.IsChildOf(previewObject.transform) || container.name == "Frame")
		    {
			continue;
		    }
		    else
		    {
			var containerComponent = container.GetComponent<UIActivityContainer>();
			if(containerComponent == null)
			{
			    Debug.LogError("Failed to access UI activity container component!");
			}
			else
			{
			    containers.Add(containerComponent);
			}
		    }
		}

		previewContainers = new Some<List<UIActivityContainer>>(containers);
	    }
	    catch(OutOfMemoryException _e)
	    {
		Debug.LogError(_e.Message);
	    }
	}

	//Set up continuous activities
	var continuousObject = GameObject.FindWithTag("ContinuousActivitiesContainer");
	if(continuousObject == null)
	{
	    Debug.LogError("Failed to access continuous activities container!");
	}
	else
	{
	    try
	    {
		var containers = new List<UIActivityContainer>();
		foreach(Transform entry in continuousObject.transform)
		{
		    var containerComponent = entry.GetComponent<UIActivityContainer>();
		    if(containerComponent == null)
		    {
			Debug.LogError("Failed to access activity container component!");
		    }
		    else
		    {
			containers.Add(containerComponent);
		    }
		}
	    
		continuousContainers = new Some<List<UIActivityContainer>>(containers);
	    }
	    catch(OutOfMemoryException _e)
	    {
		Debug.LogError(_e.Message);
	    }
	}

	//Populate continuous containers with appropriate activity IDs and sprites
	continuousContainers.Visit<Unit>(
	    x  => controllerUI.Visit<Unit>(
	    y  => { if(activitySprites == null)
		    {
			Debug.LogError("Failed to access activity sprites!");
		    }
		    else
		    {
			if(x.Count >= 3)
			{
			    foreach(var entry in x)
			    {
				entry.SetID(y.EmergeContinuousActivity(entry.kind));
				if(activitySprites.ContainsKey(entry.kind))
				{
				    entry.SetSprite(activitySprites[entry.kind]);
				}
			    }
			}
		    }
		    
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access UI controller!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access continuous activity containers!");
		    return Unit.Instance; });

	//Set up schedule and associated preview
        PopulateSchedule();
	UpdateActivities(new None<IDictionary<int, float>>());
    }
    
    //Populate the schedule with activity containers
    void PopulateSchedule()
    {
	containerPrefab.Visit<Unit>(
            x =>  { var containerComponents = new ContainerDict();
		    
		    var scheduleObject = GameObject.FindWithTag("Schedule");
		    if(scheduleObject == null)
		    {
			Debug.LogError("Failed to access scheduler contents object!");
		    }
		    
		    var time = new DateTime(2016, 1, 1);
		    while(time.Year == 2016)
		    {
			var containerObject = GameObject.Instantiate(x, Vector3.zero, Quaternion.identity) as GameObject;
			if(containerObject == null)
			{
			    Debug.LogError("Failed to instantiate container object!");
			    return Unit.Instance;
			}
			else
			{
			    if(scheduleObject != null)
			    {
				containerObject.transform.SetParent(scheduleObject.transform);
			    }
			    
			    var containerComponent = containerObject.GetComponent<UIActivityContainer>();
			    if(containerComponent == null)
			    {
				Debug.LogError("Failed to access activity container component!");
				return Unit.Instance;
			    }
			    else
			    {
				containerComponent.SetTime(time);
				containerComponents.Add(time, containerComponent);
			    }
			}
			
			time = time.AddDays(1);
		    }
		    
		    scheduleContainers = new Some<ContainerDict>(containerComponents);
                    return Unit.Instance; },
            () => { Debug.LogError("Failed to access container prefab!");
                    return Unit.Instance; });
    }
    
    //Attempt to schedule an activity
    void AttemptSchedule(ActivityType _kind, int _days, DateTime _from)
    {
	scheduleContainers.Visit<Unit>(
	    x  => controllerUI.Visit<Unit>(
	    y  => { if(_from <= time)
		    {
			//Can't schedule in the past
			return Unit.Instance;
		    }

		    foreach(DateTime day in EachDay(_from, _from.AddDays(_days)))
		    {
			if(x.ContainsKey(day))
			{
			    if(x[day].Occupied())
			    {
				return Unit.Instance;
			    }
			}
			else
			{
			    Debug.LogError("Failed to find key "
					   + day.ToShortDateString()
					   + " in the container dictionary!");
			    return Unit.Instance;
			}
		    }
		    
		    //If no days in the range were occupied
		    Option<int> id = y.EmergeScheduledActivity(_kind, _from, _from.AddDays(_days));
		    id.Visit<Unit>(
			z  => { foreach(DateTime day in EachDay(_from, _from.AddDays(_days)))
		                {
				    if(activitySprites.ContainsKey(_kind))
				    {
					activitySprites[_kind].Visit<Unit>(
					    w  => { x[day].Occupy(z, w);
						    return Unit.Instance; },
					    () => { Debug.LogError("Failed to access activity sprite!");
						    return Unit.Instance; });
				    }
				    else
				    {
					Debug.LogError("Failed to access activity sprite!");
				    }
				}
				
				return Unit.Instance; },
			() => { Debug.LogError("Failed to access activity ID!");
				return Unit.Instance; });
		    
		    //Scheduling was succesful
		    activityToSchedule = new None<ActivityType>();
		    daysToSchedule     = new None<int>();
		    UpdateActivities(new None<IDictionary<int, float>>());
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access UI controller!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access container dictionary!");
	            return Unit.Instance; });
    }
}
