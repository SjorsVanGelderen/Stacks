/*
    Controller for the schedule bar
    Handles basic scheduling activities in the UI
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIScheduleBar : MonoBehaviour
{
    private Option<RectTransform>    containerTransform = new None<RectTransform>();
    private Option<List<GameObject>> containers         = new None<List<GameObject>>();
    
    void Start()
    {
        var container = transform.Find("Viewport/Content");
        if(container == null)
        {
            Debug.LogError("Failed to access container object!");
        }
        else
        {
            var containerTransform = container.GetComponent<RectTransform>();
            if(containerTransform == null)
            {
                Debug.LogError("Failed to access transform component!");
            }
            else
            {
                containerTransform = new Some<RectTransform>(transformComponent);
            }
        }
    }
    
    void Update()
    {
        
    }
    
    public void ScheduleActivity()
    {
        
    }
}