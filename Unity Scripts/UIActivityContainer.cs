/*
    Controller for containers of scheduled UI activities
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIActivityContainer : MonoBehaviour
{
    void Start()
    {
        var scheduleObject = GameObject.FindWithTag("Schedule");
        if(scheduleObject == null)
        {
            Debug.LogError("Failed to access scheduler contents object!");
        }
        else
        {
            transform.SetParent(scheduleObject.transform);
        }
    }

    void Update()
    {

    }
    
    public void AttemptSchedule()
    {
        Debug.Log("Attempting to schedule an activity!");
    }
    
    public void SetMonth(DateTime _time)
    {
        var textComponent = GetComponentInChildren<Text>();
        if(textComponent == null)
        {
            Debug.LogError("Failed to access text component!");
        }
        else
        {
            textComponent.text = _time.ToString("d MMM");
        }
        
        var imageComponent = GetComponent<Image>();
        if(imageComponent == null)
        {
            Debug.LogError("Failed to access image component!");
        }
        else
        {
            int month = _time.Month;
            
            Color[] monthColors = new Color[12]
                { new Color(1.0f,  0.8f,  0.8f,  1.0f),
                  new Color(1.0f,  0.87f, 0.8f,  1.0f),
                  new Color(1.0f,  0.91f, 0.8f,  1.0f),
                  new Color(1.0f,  0.95f, 0.8f,  1.0f),
                  new Color(1.0f,  0.99f, 0.8f,  1.0f),
                  new Color(0.93f, 0.98f, 0.78f, 1.0f),
                  new Color(0.78f, 0.96f, 0.76f, 1.0f),
                  new Color(0.76f, 0.94f, 0.95f, 1.0f),
                  new Color(0.76f, 0.85f, 0.95f, 1.0f),
                  new Color(0.78f, 0.76f, 0.95f, 1.0f),
                  new Color(0.88f, 0.76f, 0.95f, 1.0f),
                  new Color(0.96f, 0.77f, 0.9f,  1.0f) };
            
            if(month > 0 && month <= 12)
            {
                imageComponent.color = monthColors[month - 1];
            }
        }
    }
}