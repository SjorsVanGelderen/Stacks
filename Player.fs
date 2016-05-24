(*
    Component for the player game object in the scene
*)

namespace Monaco

open UnityEngine
open UnityEngine.UI
open System.Collections
open Helpers

//This controller manages the collection of activity UI elements
[<AllowNullLiteralAttribute>]
type MonacoPlayerController () =
    inherit MonoBehaviour ()
    
    [<SerializeField>]
    let mutable sprites : Generic.List<Sprite> = Generic.List<Sprite>()
    
    let mutable spriteRenderer = None
    
    let mutable activities : string List = []
    
    let mutable startTime : double = 0.0;
    let endTime = 2.0;
    
    member this.AddActivity name =
        if not <| List.exists (fun e -> e = name) activities then
            activities <- name :: activities
    
    member this.RemoveActivity name =
        Debug.Log ("Player controller RemoveActivity")
        activities <- List.filter (fun e -> e <> name) activities
        
    member this.Start () =
        try
            let renderer = this.GetComponent<SpriteRenderer>()
            match renderer with 
            | null -> Debug.LogError ("Failed to find sprite renderer!")
            | _ -> spriteRenderer <- Some renderer
        with 
            | ex -> Debug.LogError (ex.Message)
        
    member this.Update () =
        startTime <- startTime + double Time.deltaTime
        if startTime >= endTime then
            startTime <- 0.0
            match spriteRenderer with
            | Some renderer ->
                let sprite = 
                    if activities.Length = 0 then
                        sprites.Find(fun e -> e.name = "PlayerSprite_idle")
                    else
                        let activity = activities.Item (random.Next (activities.Length))
                        sprites.Find(fun e -> e.name = "PlayerSprite_" + activity)
                
                match sprite with
                | null -> Debug.LogError ("Failed to find activity sprite!")
                | _ -> renderer.sprite <- sprite
            | None -> Debug.LogError ("Failed to change sprite: sprite renderer not found!")