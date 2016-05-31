(*
    Logic for activities and the UI
*)

namespace Monaco

open UnityEngine
open UnityEngine.UI

//This controller manages the collection of activity UI elements
[<AllowNullLiteralAttribute>]
type MonacoUIActivitiesController () =
    inherit MonoBehaviour ()
     
    [<SerializeField>]
    let mutable playerController = Unchecked.defaultof<MonacoPlayerController>
    
    [<SerializeField>]
    let mutable prefabMusic      = Unchecked.defaultof<GameObject>

    [<SerializeField>]
    let mutable prefabGame       = Unchecked.defaultof<GameObject>

    [<SerializeField>]
    let mutable prefabPaint      = Unchecked.defaultof<GameObject>
    
    let mutable pieSprites = Unchecked.defaultof<Sprite List>
    
    member this.AddActivity name =
        match playerController with 
        | null -> Debug.LogError ("Failed to find player controller!")
        | _ -> playerController.AddActivity name
        
        let prefab =
            match name with 
            | "music" -> if prefabMusic = null then None else Some prefabMusic
            | "game"  -> if prefabGame  = null then None else Some prefabGame
            | "paint" -> if prefabPaint = null then None else Some prefabPaint
            | _ -> Debug.LogError ("Failed to add unknown activity " + name + "!"); None
        
        match prefab with
        | Some prefab ->
            try
                let containerTransform = this.transform.Find("Container")
                if containerTransform = null then
                    Debug.LogError ("Failed to find container transform!")
                else
                    let go = GameObject.Instantiate(prefab, Vector3.zero, Quaternion.identity) :?> GameObject
                    if go = null then
                        Debug.LogError ("Failed to instantiate the activity object!")
                    else
                        go.transform.SetParent(containerTransform, false)
                        go.name <- name
            with 
                | ex -> Debug.LogError (ex.Message)
        | None -> Debug.LogError ("Failed to load prefab for " + name + "!")
        ()
        
    member this.UpdateActivity name progress =
        try
            let activityObject = this.transform.Find("Container/" + name)
            if activityObject = null then
                Debug.LogError ("Failed to find activity object!")
            else
                let activityComponent = activityObject.GetComponent<MonacoUIActivityController>()
                if activityComponent = null then
                    Debug.LogError ("Failed to get activity component!")
                else
                    activityComponent.SetProgress (progress)
        with
            | ex -> Debug.LogError (ex.Message)
                    
    
    member this.RemoveActivity name =
        match playerController with 
        | null -> Debug.LogError ("Failed to find player controller!")
        | _ -> playerController.RemoveActivity name
        
        let activityObject = this.transform.Find ("Container/" + name)
        if activityObject = null then
            Debug.LogError ("Failed to find activity object to remove!")
        else
            GameObject.Destroy(activityObject.gameObject)

//This controller manages a single activity UI element
and [<AllowNullLiteralAttribute>]
    MonacoUIActivityController () =
    inherit MonoBehaviour ()
    
    [<SerializeField>]
    let mutable pieSprites : Sprite List = List.empty
    let mutable paused = false
    
    member this.Click (request : string) = ()
        (*
        try
            match logicController with 
            | null -> Debug.LogError ("Failed to find logic controller!")
            | _ ->
                if paused then
                    do logicController.ActivityRequest <| "resume_" + this.name
                    paused <- not paused
                else 
                    do logicController.ActivityRequest <| "pause_" + this.name
                    paused <- not paused
        with 
            ex -> Debug.LogError (ex.Message)*)
        
    member this.SetProgress progress =
        try
            let pieObject = this.transform.Find("Pie")
            if pieObject = null then
                Debug.LogError ("Failed to find pie object!")
            else
                let pieImage = pieObject.GetComponent<Image>()
                if pieImage = null then
                    Debug.LogError ("Failed to get pie image component!")
                else
                    let index = int (progress * double (pieSprites.Length - 1))
                    if index < pieSprites.Length then
                        do pieImage.sprite <- pieSprites.Item index
        with 
            | ex -> Debug.LogError (ex.Message)
    
    member this.Start () =
        try
            do pieSprites <-
                [
                    for i = 0 to 35 do
                        let path = "Sprites/Pie/pie_" + i.ToString()
                        let sprite = Resources.Load<Sprite>(path)
                        if sprite = null then
                            Debug.LogWarning("Failed to load sprite " + path + "!")
                        yield sprite
                ]
        with 
            | ex -> Debug.LogError (ex.Message)