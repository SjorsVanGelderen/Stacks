/*
    Controller for individual activity objects in the UI
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIActivity : MonoBehaviour
{
    private const sbyte SPRITE_COUNT = 36;
    
    private Option<Image> imagePie = new None<Image>();
    private Option<List<Sprite>> sprites = new None<List<Sprite>>();
    
    Option<Image> GetPieImage()
    {
        var pieChild = transform.Find("pie");
        if(pieChild == null)
        {
            Debug.LogError("Failed to access pie object!");
            return new None<Image>();
        }
        
        var component = pieChild.GetComponent<Image>();
        if(component == null)
        {
            return new None<Image>();
        }
        else
        {
            return new Some<Image>(component);
        }
    }
    
    Option<List<Sprite>> LoadSprites()
    {
        var sprites = new List<Sprite>();
        if(sprites == null)
        {
            return new None<List<Sprite>>();
        }
        
        for(int i = 0; i < SPRITE_COUNT; i++)
        {
            var sprite = Resources.Load<Sprite>("Sprites/Pie/pie_" + i.ToString());
            if(sprite == null)
            {
                Debug.LogError ("Failed to load pie sprite " + i.ToString() + "!");
                break;
            }
            
            sprites.Add(sprite);
        }
        
        return new Some<List<Sprite>>(sprites);
    }
    
    void Start()
    {        
        //Populate options
        imagePie = GetPieImage();
        sprites = LoadSprites();
    }
    
    void Update()
    {
        
    }
    
    public void SetMode(bool _continuous)
    {
        GameObject container = null;
        if(_continuous)
        {
            container = GameObject.FindWithTag("ContinuousActivitiesContainer");
        }
        else
        {
            container = GameObject.FindWithTag("ScheduledActivitiesContainer");
        }
        
        //Move the component to the appropriate location
        if(container == null)
        {
            Debug.LogError("Failed to access activities container!");
        }
        else
        {
            transform.SetParent(container.transform);
        }
    }
    
    //Update pie sprite progress
    public void SetProgress(float _progress)
    {
        int spriteNumber = (int)(SPRITE_COUNT * _progress);
        sprites.Visit<Unit>(
            x => { if (spriteNumber >= 0 && spriteNumber < SPRITE_COUNT)
                   {
                       imagePie.Visit<Unit>(
                           y => { y.sprite = x[spriteNumber];
                                  return Unit.Instance; },
                           () => { Debug.Log ("Failed to access image component!");
                                   return Unit.Instance; });
                       
                       return Unit.Instance;
                   }
                   
                   return Unit.Instance; },
            () => { Debug.LogError("Failed to access sprites!");
                    return Unit.Instance; });
    }
}