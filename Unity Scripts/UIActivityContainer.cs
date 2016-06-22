/*
    Controller for containers of scheduled UI activities
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIActivityContainer : MonoBehaviour
{
    private Option<DateTime>    time      = new None<DateTime>();
    private Option<int>         id        = new None<int>();
    private Option<UIScheduler> scheduler = new None<UIScheduler>();
    private Option<Button>      button    = new None<Button>();
    private Option<Image>       image     = new None<Image>();
    private Option<Text>        text      = new None<Text>();

    //Various colors for distinguishing between months
    static Color[] monthColors = 
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

    /*
    public void SetMode(bool _continuous)
    {
    
    }
    */
    
    //Assign a date to this container
    public void SetTime(DateTime _time)
    {
        time = new Some<DateTime>(_time);
	name = _time.ToShortDateString();

	text.Visit<Unit>(
	    x  => { x.text = _time.ToShortDateString();
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access text component!");
		    return Unit.Instance; });

	image.Visit<Unit>(
	    x  => { x.color = monthColors[_time.Month - 1];
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access image component!");
		    return Unit.Instance; });
    }
    
    //Activity container clicked; trigger a scheduler action
    public void PokeScheduler()
    {
        scheduler.Visit<Unit>(
            x  => { time.Visit<Unit>(
			y  => { x.ContainerInteraction(y);
				return Unit.Instance; },
			() => { Debug.LogError("Failed to access time!");
				return Unit.Instance; });
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access scheduler!");
                    return Unit.Instance; });
    }

    //Check whether this container is already occupied by an activity
    public bool Occupied()
    {
	return id.Visit<bool>(
	    x  => true,
	    () => false);
    }
    
    //Schedule an activity inside this container
    public void Occupy(int _id, Sprite _sprite)
    {
	id = new Some<int>(_id);
	
	text.Visit<Unit>(
	    x  => { x.enabled = false;
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access text component!");
		    return Unit.Instance; });
	
	image.Visit<Unit>(
	    x  => { x.sprite = _sprite;
		    return Unit.Instance; },
	    () => { Debug.LogError("Failed to access image component!");
		    return Unit.Instance; });
    }

    //Free this container for a new activity
    public void Free()
    {
	id = new None<int>();

	text.Visit<Unit>(
	    x  => time.Visit<Unit>(
	        y  => { x.text = y.ToShortDateString();
		        return Unit.Instance; },
		() => { Debug.LogError("Failed to access time!");
		        return Unit.Instance; }),
	    () => { Debug.LogError("Failed to access text component!");
		    return Unit.Instance; });
    }

    public Option<Sprite> GetSprite()
    {
	return image.Visit<Option<Sprite>>(
	    x  => { if(x.sprite == null)
		    {
			Debug.LogError("Failed to access sprite!");
			return new None<Sprite>();
		    }
		    
		    return new Some<Sprite>(x.sprite); },
	    () => { Debug.LogError("Failed to access image component!");
		    return new None<Sprite>(); });
    }
    
    void Awake()
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
	
        var schedulerObject = GameObject.FindWithTag("UI");
        if(schedulerObject == null)
        {
            Debug.LogError("Failed to access scheduler object!");
        }
        else
        {
            var schedulerComponent = schedulerObject.GetComponent<UIScheduler>();
            if(schedulerComponent == null)
            {
                Debug.LogError("Failed to access scheduler component!");
            }
            else
            {
                scheduler = new Some<UIScheduler>(schedulerComponent);
            }
        }
        
        var buttonComponent = GetComponent<Button>();
        if(buttonComponent == null)
        {
            Debug.LogError("Failed to access button component!");
        }
        else
        {
            button = new Some<Button>(buttonComponent);
        }

	var textComponent = GetComponentInChildren<Text>();
        if(textComponent == null)
        {
            Debug.LogError("Failed to access text component!");
        }
        else
        {
            text = new Some<Text>(textComponent);
        }

	var imageTransform = transform.Find("Image");
	if(imageTransform == null)
	{
	    Debug.LogError("Failed to access image transform!");
	}
	else
	{
	    var imageComponent = imageTransform.GetComponent<Image>();
	    if(imageComponent == null)
	    {
		Debug.LogError("Failed to access image component!");
	    }
	    else
	    {
		image = new Some<Image>(imageComponent);
	    }
	}
    }
}
