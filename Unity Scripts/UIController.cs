/*
    Controller for managing the UI at large
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Monaco;

using ActivityType       = Communications.ActivityType;
using ActivityPrefabDict = System.Collections.Generic.Dictionary<ActivityType, UnityEngine.GameObject>;
using ActivityDict       = System.Collections.Generic.Dictionary<int, UIActivity>;

class UIController : MonoBehaviour
{
    public MonacoLogic controllerGameReference;
    public Canvas canvasMainReference;
    public Slider sliderDayReference;
    public List<ActivityType> activityTypes = new List<ActivityType>();
    
    private Option<MonacoLogic>        controllerGame  = new None<MonacoLogic>();
    private Option<Canvas>             canvasMain      = new None<Canvas>();
    private Option<Slider>             sliderDay       = new None<Slider>();
    private Option<ActivityPrefabDict> activityPrefabs = new None<ActivityPrefabDict>();
    private Option<ActivityDict>       activities      = new None<ActivityDict>();
    
    Option<ActivityPrefabDict> LoadActivityPrefabs()
    {
        var prefabs = new ActivityPrefabDict();
        if(prefabs == null)
        {
            Debug.LogError ("Failed to instantiate prefab list!");
            return new None<ActivityPrefabDict>();
        }
        
        foreach(ActivityType kind in activityTypes)
        {
            if(prefabs.ContainsKey(kind))
            {
                Debug.LogError("Failed to add entry; duplicate key!");
                return new None<ActivityPrefabDict>();
            }
            
            string name = "";
            switch(kind)
            {
            case ActivityType.Human:
                name = "human";
                break;
                
            case ActivityType.Social:
                name = "social";
                break;
                
            case ActivityType.Financial:
                name = "financial";
                break;
                
            default:
                Debug.LogError("Failed to translate activity type to filename!");
                break;
            }
            
            if(name == "")
            {
                return new None<ActivityPrefabDict>();
            }
            
            var prefab = Resources.Load<GameObject>("Prefabs/Activities/" + name);
            if(prefab == null)
            {
                Debug.LogError("Failed to load prefab for " + name + "!");
                return new None<ActivityPrefabDict>();
            }
            
            prefabs.Add(kind, prefab);
        }
        
        return new Some<ActivityPrefabDict>(prefabs);
    }
    
    void Start()
    {        
        activityPrefabs = LoadActivityPrefabs();
        
        if(controllerGameReference == null)
        {
            Debug.LogError ("Failed to access game controller!");
        }
        else
        {
            controllerGame = new Some<MonacoLogic>(controllerGameReference);
        }
        
        if(canvasMainReference == null)
        {
            Debug.LogError ("Failed to access main canvas!");
        }
        else
        {
            canvasMain = new Some<Canvas>(canvasMainReference);
        }
        
        if(sliderDayReference == null)
        {
            Debug.LogError ("Failed to access day slider!");
        }
        else
        {
            sliderDay = new Some<Slider>(sliderDayReference);
        }
        
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
    }
    
    void Update()
    {
        UpdateSliderDay();
        UpdateActivities();
    }
    
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
    
    //Update the representation of the daytime slider
    void UpdateSliderDay()
    {
        controllerGame.Visit<Unit>(
            x => { sliderDay.Visit<Unit>(
                       y => { y.value = x.QueryDayTime() / 24.0f;
                              return Unit.Instance; },
                       () => { Debug.LogError("Failed to access day slider!");
                               return Unit.Instance; });
                               
                   return Unit.Instance; },
            () => { Debug.LogError ("Failed to access game controller!");
                    return Unit.Instance; });
    }
    
    //Update all activity UI objects
    void UpdateActivities()
    {
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
    }
}