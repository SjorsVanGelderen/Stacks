/*
    Controller for UI action buttons
*/

using System;
using UnityEngine;
using ActivityType = Communications.ActivityType;

public class UIAction : MonoBehaviour
{
    public ActivityType kind = ActivityType.Default;
    public int days = 1;
    
    private Option<UIScheduler> scheduler = new None<UIScheduler>();

    //Notify the scheduler that this action has been activated
    public void PokeScheduler()
    {
        scheduler.Visit<Unit>(
            x  => { x.StartScheduling(kind, days, false);
                    return Unit.Instance; },
            () => { Debug.LogError("Failed to access scheduler!");
                    return Unit.Instance; });
    }
    
    void Awake()
    {
	var objectUI = GameObject.FindWithTag("UI");
	if(objectUI == null)
	{
	    Debug.LogError("Failed to access UI object!");
	}
	else
	{
	    var schedulerComponent = objectUI.GetComponent<UIScheduler>();
	    if(schedulerComponent == null)
	    {
		Debug.LogError("Failed to access scheduler component!");
	    }
	    else
	    {
		scheduler = new Some<UIScheduler>(schedulerComponent);
	    }
	}
    }
}
