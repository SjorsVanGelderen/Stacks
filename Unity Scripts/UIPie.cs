/*
  Controller for pie sprites that are used to indicate progress
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Singleton for shared sprites; not sure if the resource manager already does something like this
public sealed class PieSpritesContainer
{
    private Option<List<Sprite>> sprites = new None<List<Sprite>>();
    private static readonly PieSpritesContainer instance = new PieSpritesContainer();
    
    private PieSpritesContainer()
    {
	try
	{
	    var spriteList = new List<Sprite>();
	    for(int i = 0; i < 36; i++)
	    {
		var sprite = Resources.Load<Sprite>("Sprites/Pie/pie_" + i.ToString());
		if(sprite == null)
		{
		    Debug.LogError("Failed to load sprite: pie_" + i.ToString() + "!");
		    return;
		}
		else
		{
		    spriteList.Add(sprite);
		}
	    }
	    
	    sprites = new Some<List<Sprite>>(spriteList);
	}
	catch(OutOfMemoryException _e)
	{
	    Debug.LogError(_e.Message);
	}
    }

    public static PieSpritesContainer Instance
    {
	get
	{
	    return instance;
	}
    }

    public Option<Sprite> GetSprite(float _progress)
    {
	return sprites.Visit<Option<Sprite>>(
	    x  => { int index = (int)(x.Count * _progress);
		    if(index >= 0)
		    {
			if(index >= x.Count)
			{
			    index = x.Count - 1;
			}
			return new Some<Sprite>(x[index]);
		    }
		    else
		    {
			Debug.LogError("Failed to get sprite for progress "
				       + _progress.ToString()
				       + " and index "
				       + index.ToString()
				       + " out of bounds!");
			return new None<Sprite>();
		    } },
	    () => { Debug.LogError("Failed to access sprites!");
		    return new None<Sprite>(); });
    }
}

public class UIPie : MonoBehaviour
{
    private Option<Image> image = new None<Image>();

    public void SetProgress(float _progress)
    {
	if(_progress >= 0 && _progress <= 1.0)
	{
	    var sprite = PieSpritesContainer.Instance.GetSprite(_progress);
	
	    image.Visit<Unit>(
		x  => sprite.Visit<Unit>(
		y  => { x.sprite = y;
			return Unit.Instance; },
		() => { Debug.LogError("Failed to access sprite!");
			return Unit.Instance; }),
		() => { Debug.LogError("Failed to access image component!");
			return Unit.Instance; });
	}
    }
    
    void Awake()
    {
	var imageComponent = GetComponent<Image>();
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
