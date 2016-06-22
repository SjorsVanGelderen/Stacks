/*
    Controller for managing the UI at large
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Monaco;

public class UIController : MonoBehaviour
{    
    private Option<MonacoLogic> controllerGame  = new None<MonacoLogic>();
    private Option<UIScheduler> scheduler       = new None<UIScheduler>();
    private Option<Slider>      sliderDay       = new None<Slider>();
    
    void Awake()
    {
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

	var schedulerComponent = GetComponent<UIScheduler>();
	if(schedulerComponent == null)
	{
            Debug.LogError("Failed to access scheduler component!");
        }
        else
        {
            scheduler = new Some<UIScheduler>(schedulerComponent);
        }

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
	
        /*
        //Arguably the activities list does not need to be an Option
        var activityDictionary = new Dictionary<int, UIActivity>();
        if(activityDictionary == null)
        {
            Debug.LogError("Failed to instantiate activity dictionary!");
        }
        else
        {
            activities = new Some<ActivityDict>(activityDictionary);
        }
        
        //AddActivity("human", true);
        //AddActivity("social", true);
        //AddActivity("financial", false);
        */
    }
    
    //Update the representation of the daytime slider
    void UpdateSliderDay()
    {
        controllerGame.Visit<Unit>(
            x  => { sliderDay.Visit<Unit>(
                        y  => { y.value = x.QueryDayTime() / 24.0f;
                                return Unit.Instance; },
                        () => { Debug.LogError("Failed to access day slider!");
                                return Unit.Instance; }); 
                    return Unit.Instance;},
            () => { Debug.LogError ("Failed to access game controller!");
                    return Unit.Instance; });
    }
    
    //Update all activity UI objects
    void UpdateActivities()
    {
	
    }
}


    /*
    //Add a new activity object to the UI
    public void AddActivity(int _id, ActivityType _kind, DateTime _startDate, DateTime _endDate, bool _continuous)
    {
        activityPrefabs.Visit<Unit>(
            x => { if(x.ContainsKey(_kind))
                   {
                       var uiObject = GameObject.Instantiate(x[_kind], Vector3.zero, Quaternion.identity) as GameObject;
                       if(uiObject == null)
                       {
                           Debug.LogError("Failed to instantiate activity UI object!");
                           return Unit.Instance;
                       }
                       
                       var uiComponent = uiObject.GetComponent<UIActivity>();
                       if(uiComponent == null)
                       {
                           Debug.LogError("Failed to add UIActivity component!");
                           return Unit.Instance;
                       }
                       
                       uiComponent.SetMode(_continuous);
                       
                       activities.Visit<Unit>(
                           y => { y.Add(_id, uiComponent);
                                  return Unit.Instance; },
                           () => { Debug.LogError("Failed to access activities list!");
                                   return Unit.Instance; });
                       
                       return Unit.Instance;
                   }
                   
                   Debug.LogError("Failed to find activity prefab in dictionary!");
                   return Unit.Instance;
                 },
            () => { Debug.LogError("Failed to access activity prefabs!");
                    return Unit.Instance; });
    }
    */
    
    /*
    //Query an activity's start date
    public Option<DateTime> QueryStartDate(int _id)
    {
        return controllerGame.Visit<Option<DateTime>>(
            x  => { DateTime startDate = x.QueryStartDate(_id);
                    if(startDate.Year == 0)
                    {
                        Debug.LogError("Failed to query start date; invalid year!");
                        return new None<DateTime>();
                    }
                    return new Some<DateTime>(startDate); },
            () => { Debug.LogError("Failed to access game controller!");
                    return new None<DateTime>(); });
    }
    
    

    /*
        controllerGame.Visit<Unit>(
            x => { var data = new Dictionary<int, float>(x.QueryActivityProgress());
                   
                   activities.Visit<Unit>(
                       y => { foreach(var entry in data)
                              {
                                  if(y.ContainsKey(entry.Key))
                                  {
                                      y[entry.Key].SetProgress(entry.Value);
                                  }
                                  else
                                  {
                                      Debug.LogWarning("Failed to find activity to update!");
                                  }
                              }
                            
                              return Unit.Instance; },
                       () => { Debug.LogError("Failed to access activities list!");
                               return Unit.Instance; });
                   
                   return Unit.Instance; },
            () => { Debug.LogError("Failed to access game controller!");
                    return Unit.Instance; });
                    */
