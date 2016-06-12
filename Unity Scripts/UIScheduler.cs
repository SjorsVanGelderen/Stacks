/*
    Controller for the scheduler
    Handles basic scheduling activities in the UI
*/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using ContainerDict = System.Collections.Generic.Dictionary<System.DateTime, UIActivityContainer>;

public class UIScheduler : MonoBehaviour
{
    private Option<GameObject>    containerPrefab   = new None<GameObject>();
    private Option<ContainerDict> containers        = new None<ContainerDict>();
    
    void GenerateBarPopulation(GameObject _prefab)
    {
        var containerComponents = new ContainerDict();
        if(containerComponents == null)
        {
            Debug.LogError("Failed to instantiate container components list!");
            return;
        }
        
        var time = new DateTime(2016, 1, 1);
        while(time.Year == 2016)
        {
            var containerObject = GameObject.Instantiate(_prefab, Vector3.zero, Quaternion.identity) as GameObject;
            if(containerObject == null)
            {
                Debug.LogError("Failed to instantiate container object!");
                return;
            }
            
            var containerComponent = containerObject.GetComponent<UIActivityContainer>();
            if(containerComponent == null)
            {
                Debug.LogError("Failed to access activity container component!");
                return;
            }
            
            containerComponent.SetMonth(time);
            containerComponents.Add(time, containerComponent);
            time = time.AddDays(1);
        }
        
        containers = new Some<ContainerDict>(containerComponents);
        return;
    }
    
    void Start()
    {
        var prefab = Resources.Load<GameObject>("Prefabs/Activities/ActivityContainer");
        if(prefab == null)
        {
            Debug.LogError("Failed to load container prefab!");
        }
        else
        {
            containerPrefab = new Some<GameObject>(prefab);
        }
        
        containerPrefab.Visit<Unit>(
            x => { GenerateBarPopulation(x);
                   return Unit.Instance; },
            () => { Debug.LogError("Failed to access container prefab!");
                    return Unit.Instance; });
    }
    
    void Update()
    {
        
    }
    
    public void ScheduleActivity()
    {
        
    }
}