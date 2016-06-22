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
using ActivityDict       = System.Collections.Generic.Dictionary<int, Communications.ActivityType>;

public class UIScheduler : MonoBehaviour
{
    private Option<GameObject>    containerPrefab    = new None<GameObject>();
    private Option<Button>        buttonStudio       = new None<Button>();
    private Option<ActivityDict>  activities         = new None<ActivityDict>();
    private Option<ContainerDict> scheduleContainers = new None<ContainerDict>();
    private Option<List<Image>>   previewContainers  = new None<List<Image>>();
    private Option<ActivityType>  activityToSchedule = new None<ActivityType>();
    private Option<int>           daysToSchedule     = new None<int>();
    private ActivitySpriteDict    activitySprites    = new ActivitySpriteDict();
    private DateTime              time               = new DateTime(2016, 1, 1);
    
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
    
    void Awake()
    {
        var containerPrefabObject = Resources.Load<GameObject>("Prefabs/Activities/ActivityContainer");
        if(containerPrefabObject == null)
        {
            Debug.LogError("Failed to load container prefab!");
        }
        else
        {
            containerPrefab = new Some<GameObject>(containerPrefabObject);
        }
        
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

	var previewObject = GameObject.Find("SchedulePreview");
	if(previewObject == null)
	{
	    Debug.LogError("Failed to access schedule preview object!");
	}
	else
	{
	    var containers = new List<Image>();
	    if(containers == null)
	    {
		Debug.Log("Failed to instantiate container list!");
	    }
	    else
	    {
		foreach(Transform container in previewObject.transform)
		{
		    if(!container.IsChildOf(previewObject.transform))
		    {
			continue;
		    }
		    else
		    {
			var imageComponent = container.GetComponent<Image>();
			if(imageComponent == null)
			{
			    Debug.Log("Failed to access image component!");
			}
			else
			{
			    containers.Add(imageComponent);
			}
		    }
		}

		previewContainers = new Some<List<Image>>(containers);
	    }
	}
	
        PopulateSchedule();
	UpdatePreview();
    }
    
    //Populate the schedule with activity containers
    void PopulateSchedule()
    {
	containerPrefab.Visit<Unit>(
            x =>  { var containerComponents = new ContainerDict();
		    if(containerComponents == null)
		    {
			Debug.LogError("Failed to instantiate container components list!");
			return Unit.Instance;
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
			
			var containerComponent = containerObject.GetComponent<UIActivityContainer>();
			if(containerComponent == null)
			{
			    Debug.LogError("Failed to access activity container component!");
			    return Unit.Instance;
			}
			
			containerComponent.SetTime(time);
			containerComponents.Add(time, containerComponent);
			time = time.AddDays(1);
		    }
		    
		    scheduleContainers = new Some<ContainerDict>(containerComponents);
                    return Unit.Instance; },
            () => { Debug.LogError("Failed to access container prefab!");
                    return Unit.Instance; });
    }

    //Update the preview bar to match the appropriate slice of the schedule
    void UpdatePreview()
    {
	previewContainers.Visit<Unit>(
	    x  => scheduleContainers.Visit<Unit>(
	    y  => containerPrefab.Visit<Unit>(
	    z  => { for(int i = 0; i < x.Count; i++)
		    {
			var keyTime = time.AddDays(i - x.Count / 2);
			if(y.ContainsKey(keyTime))
			{
			    var sprite = y[keyTime].GetSprite();
			    sprite.Visit<Unit>(
				w  => { x[i].sprite = w;
					return Unit.Instance; },
				() => { Debug.LogError("Failed to get sprite!");
					return Unit.Instance; });
			}
		    }
		    
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access container prefab!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access schedule containers!");
		    return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access preview containers!");
	            return Unit.Instance; });
    }

    //Attempt to schedule an activity directly
    void AttemptSchedule(ActivityType _kind, int _days, DateTime _from)
    {
	scheduleContainers.Visit<Unit>(
	    x  => { foreach(DateTime day in EachDay(_from, _from.AddDays(_days)))
		    {
			if(x.ContainsKey(day))
			{
			    if(x[day].Occupied())
			    {
				//Free all previously occupied containers for this attempt
				foreach(DateTime otherDay in EachDay(_from, day))
				{
				    x[otherDay].Free();
				}
			      
				return Unit.Instance;
			    }
			    else
			    {
				if(activitySprites.ContainsKey(_kind))
				{
				    activitySprites[_kind].Visit<Unit>(
					y  => { x[day].Occupy(-1, y); //Dummy ID for now
						return Unit.Instance; },
					() => { Debug.LogError("Failed to access activity sprite!");
						return Unit.Instance; });
				}
				else
				{
				    Debug.LogError("Failed to access activity sprite!");
				}
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

		    //If scheduling was succesful
		    activityToSchedule = new None<ActivityType>();
		    daysToSchedule     = new None<int>();
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access container dictionary!");
	            return Unit.Instance; });
    }
}
