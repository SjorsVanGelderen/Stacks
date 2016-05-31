(*
   Core game logic
*)

namespace Monaco

open System
open UnityEngine
open Coroutine
open Helpers
open Lens
open Communications
open Model

//Core game logic component
type MonacoLogic () =
    inherit MonoBehaviour ()
    
    [<SerializeField>]
    let mutable activitiesController =
        Unchecked.defaultof<MonacoUIActivitiesController>

    [<SerializeField>]
    let mutable playerController =
        Unchecked.defaultof<MonacoPlayerController>

    let mutable requestsUI : Mail List = [] //UI mails to be captured by the state
    let mutable state = State.Zero //The base game state
    
    //Loads prefabs from the resources folder into the state as a list of game objects
    member this.LoadPrefabs filenames =
        let attemptLoad =
            fun acc filename ->
                let gameObject = Resources.Load<GameObject> ("Prefabs/" + filename)
                match gameObject with
                | null -> Debug.LogError ("Failed to load prefab " + filename); acc
                | _ -> (filename, gameObject) :: acc

        List.fold attemptLoad List.empty filenames |> Map.ofList

    member this.Start () =
        try            
            do state <- State.Initialize state
        
            let prefabFilenames = []
            do state <- State.SetPrefabs state (this.LoadPrefabs prefabFilenames)
        with
            | ex -> Debug.LogError (ex.Message)

    member this.Update () =
        try
            //Add the activity request mails
            do state <- { state with Mailbox = Mailbox.SendUnique state.Mailbox 0 requestsUI }
            requestsUI <- [] //Clear the processed UI requests

            //Update the state as usual
            do state <- State.Update Time.deltaTime state

            //Update progress of the state in UI elements
            this.UpdateUI ()
            
            if state.ExitFlag then
                state <- State.Terminate state
                Application.Quit ()
        with
            | ex -> Debug.LogError (ex.Message)
    
    member this.Quit () =
        try
            do state <- { state with ExitFlag = true }
        with
            | ex -> Debug.LogError (ex.Message)
    
    member this.UpdateUI () =
        this.UpdateActivities ()
        this.RemoveActivities ()
        this.UpdatePlayer ()
    
    member this.ActivityRequest (request : string) =
        if request.Contains ("pause_") then
            requestsUI <- (PauseActivity (request.Substring(6, request.Length))) :: requestsUI
        else if request.Contains ("resume_") then
            requestsUI <- (ResumeActivity (request.Substring(7, request.Length))) :: requestsUI
        else
            if not <| List.exists (fun (e : Activity) ->
                e.Fields.Name = request) state.Activities then
                requestsUI <- (AddActivity request) :: requestsUI
                
                match activitiesController with 
                | null -> Debug.LogError ("Failed to find activities controller!")
                | _ -> activitiesController.AddActivity request
        
    member this.UpdateActivities () =
        match activitiesController with 
        | null -> Debug.LogError ("Failed to access activities controller!")
        | _ ->
            List.iter (fun (activity : Activity) ->
                activitiesController.UpdateActivity activity.Fields.Name
                    (normalize activity.Fields.ElapsedTime 0.0 activity.Fields.Duration)) state.Activities
        
    member this.RemoveActivities removals =
        let mails () = state.Mailbox.Contents.TryFind 0
        match mails () with 
        | Some mails ->
            let removals, mails' =
                List.fold (fun (removals, mails) mail ->
                    match mail with
                    | RemoveActivity name -> (name :: removals), mails
                    | _ -> (removals, mail :: mails)) ([], []) mails
            
            match activitiesController with 
            | null -> Debug.LogError ("Failed to access activities controller!")
            | _ -> List.iter (fun activity -> activitiesController.RemoveActivity activity) removals

            do state <- { state with Mailbox = { state.Mailbox with Contents = Map.add 0 mails' state.Mailbox.Contents } }
        | None -> ()

    member this.UpdatePlayer () =
        try
            if playerController = null then
                Debug.LogError ("Failed to access player controller!")
            else
                playerController.SetHealth (state.Player.Fields.State.Health)
                playerController.SetEnergy (state.Player.Fields.State.Energy)
                playerController.SetFinancial (state.Player.Fields.State.Financial)
                playerController.SetBrand (state.Player.Fields.State.Brand)
        with
            | ex -> Debug.LogError (ex.Message)

//Global game object container
and Entity<'w, 'fs, 'mailbox> =
    { Fields  : 'fs
      Rules   : List<'w -> 'fs -> float32 -> 'fs>
      Scripts : Coroutine<'w, 'mailbox, 'fs, unit> } with
      
    static member Create (fields, rules, scripts) =
        { Fields  = fields
          Rules   = rules
          Scripts = andPassMany_ scripts }

    member this.Update (world, mailbox, dt) =
        let fsRules = this.Rules |> List.fold (fun fs rule -> rule world fs dt) this.Fields
        let mailbox', fsScripts, scripts' = step_ (this.Scripts world mailbox fsRules)
        { this with
              Fields  = fsScripts
              Rules   = this.Rules
              Scripts = scripts' }, mailbox'

//Keeps track of player state
and PlayerFields =
    { ID    : int
      State : PlayerState } with
    
    static member Zero =
        { ID    = -1
          State = PlayerState.Zero }

    static member Rules =
        [ fun w fs dt -> fs ]

    static member Scripts =
        let GetMailList id mailbox =
            match Map.tryFind id mailbox.Contents with 
             | Some mailList -> mailList
             | None -> []

        let mailRoutine =
            co { do! yield_
                 let! (fs : PlayerFields) = getInnerState_
                 let! mailbox = getOuterState_
                 let mails = GetMailList 0 mailbox
                 let state', mails' =
                    List.fold (fun ((state : PlayerState), mails) mail ->
                        match mail with
                        | AffectHealth change    -> ({ state with Health    = System.Math.Max(0, state.Health    + change) }, mails)
                        | AffectEnergy change    -> ({ state with Energy    = System.Math.Max(0, state.Energy    + change) }, mails)
                        | AffectFinancial change -> ({ state with Financial = System.Math.Max(0, state.Financial + change) }, mails)
                        | AffectBrand change     -> ({ state with Brand     = System.Math.Max(0, state.Brand     + change) }, mails)
                        | AffectExpertise _      -> (state, mails)
                        | AffectSkills _         -> (state, mails)
                        | _ -> (state, mail :: mails)) (fs.State, []) mails
                 do! setInnerState_ { fs with State = state' }
                 let mailbox' = { mailbox with Contents = Map.add 0 mails' mailbox.Contents }
                 do! setOuterState_ mailbox' } |> repeat_

        [ mailRoutine ]

    static member state =
        { Get = fun (x : PlayerFields) -> x.State
          Set = fun v (x : PlayerFields) -> {x with State = v} }

//Used to track the progress of activities
and ActivityFields =
    { ID          : int
      Name        : string
      Effects     : Mail List
      ElapsedTime : double
      Duration    : double
      Paused      : bool } with 
      
    static member Zero =
        { ID          = -1
          Name        = ""
          Effects     = List.empty
          ElapsedTime = 0.0
          Duration    = 1.0
          Paused      = false }
          
    static member Music =
        { ID          = -1
          Name        = "music"
          Effects     = [ AffectHealth -1
                          AffectBrand 10
                          AffectFinancial 10 ]
          ElapsedTime = 0.0
          Duration    = 10.0
          Paused      = false }
          
    static member Game =
        { ID          = -1
          Name        = "game"
          Effects     = [ AffectHealth 10
                          AffectBrand -5
                          AffectFinancial -10 ]
          ElapsedTime = 0.0
          Duration    = 5.0
          Paused      = false }
          
    static member Paint =
        { ID          = -1
          Name        = "paint"
          Effects     = [ AffectHealth 5
                          AffectBrand 15
                          AffectFinancial 3 ]
          ElapsedTime = 0.0
          Duration    = 3.0
          Paused      = false; }
          
    static member Rules =
        [ fun w fs dt -> fs ]
        
    static member Scripts =
        let GetMailList id mailbox =
            co { match Map.tryFind id mailbox.Contents with 
                 | Some mailList -> return mailList
                 | None -> return [] }
        
        let getPauseMail mailList activityName =
            List.exists (fun mail -> match mail with | PauseActivity name -> name = activityName | _ -> false) mailList
        
        let getResumeMail mailList activityName =
            List.exists (fun mail -> match mail with | ResumeActivity name -> name = activityName | _ -> false) mailList
        
        let filterMail mailList activityName =
            List.filter (fun mail ->
                match mail with 
                | AddActivity    name
                | RemoveActivity name
                | PauseActivity  name
                | RemoveActivity name -> name <> activityName
                | _ -> true) mailList
        
        let mailRoutine =
            co { do! yield_
                 let! fs = getInnerState_
                 let! mailbox = getOuterState_
                 let! mailList = GetMailList 0 mailbox
                 
                 let fs' = if getPauseMail mailList fs.Name then { fs with Paused = true } else fs
                 let fs'' = if getResumeMail mailList fs.Name then { fs' with Paused = false } else fs'
                 do! setInnerState_ fs''
                 
                 let mailList' = filterMail mailList fs.Name
                 let mailbox' = { mailbox with Contents = mailbox.Contents.Add (0, mailList') }
                 do! setOuterState_ mailbox'
                 
                 do! yield_ } |> repeat_
            
        let progressRoutine =
            co { do! yield_
                 do! wait_ 0.1
                 let! fs = getInnerState_
                 let! mailbox = getOuterState_
                 let! mailList = GetMailList 0 mailbox
                 if fs.ElapsedTime < fs.Duration then
                     if not fs.Paused then
                        if fs.ElapsedTime + 0.1 >= fs.Duration then
                            Debug.Log (fs.Effects)
                            let mailbox'  = Mailbox.SendUnique mailbox 0 [ RemoveActivity fs.Name ]
                            let mailbox'' = Mailbox.Send mailbox' 0 fs.Effects
                            do! setOuterState_ mailbox''
                            do! setInnerState_ { fs with ElapsedTime = fs.Duration }
                        else
                            do! setInnerState_ { fs with ElapsedTime = fs.ElapsedTime + 0.1 } } |> repeat_
        [ mailRoutine
          progressRoutine ]
        
    static member name =
        { Get = fun (x : ActivityFields) -> x.Name
          Set = fun v (x : ActivityFields) -> {x with Name = v} }
          
    static member elapsedTime =
        { Get = fun (x : ActivityFields) -> x.ElapsedTime
          Set = fun v (x : ActivityFields) -> {x with ElapsedTime = v} }
          
    static member duration =
        { Get = fun (x : ActivityFields) -> x.Duration
          Set = fun v (x : ActivityFields) -> {x with Duration = v} }
          
    static member paused =
        { Get = fun (x : ActivityFields) -> x.Paused
          Set = fun v (x : ActivityFields) -> {x with Paused = v} }

//Used to track the progress of events
and EventFields =
    { ID          : int
      Name        : string
      Description : string
      Effects     : Mail List
      Activity    : Activity Option
      Duration    : double } with 
      
    static member Zero =
        { ID          = -1
          Name        = ""
          Description = ""
          Effects     = List.empty
          Activity    = None
          Duration    = 1.0 }
          
    static member RecordDeal =
        { ID          = -1
          Name        = ""
          Description = ""
          Effects     = List.empty
          Activity    = None
          Duration    = 1.0 }
    
    static member Rules =
        [ fun w fs dt -> fs ]
        
    static member Scripts =
        [ co { do! yield_ } |> repeat_ ]
        
    static member name =
        { Get = fun (x : EventFields) -> x.Name
          Set = fun v (x : EventFields) -> {x with Name = v} }
          
    static member description =
        { Get = fun (x : EventFields) -> x.Description
          Set = fun v (x : EventFields) -> {x with Description = v} }
          
    static member effects =
        { Get = fun (x : EventFields) -> x.Effects
          Set = fun v (x : EventFields) -> {x with Effects = v} }
          
    static member activity =
        { Get = fun (x : EventFields) -> x.Activity
          Set = fun v (x : EventFields) -> {x with Activity = v} }
          
    static member duration =
        { Get = fun (x : EventFields) -> x.Duration
          Set = fun v (x : EventFields) -> {x with Duration = v} }

and Player   = Entity<State, PlayerFields,   Mailbox>
and Activity = Entity<State, ActivityFields, Mailbox>
and Event    = Entity<State, EventFields,    Mailbox>
 
//The global game state
and State =
    { Player     : Player
      Activities : Activity List
      Events     : Event List
      Mailbox    : Mailbox
      ExitFlag   : bool
      Prefabs    : Map<string, GameObject> } with
      
    static member Zero =
        { Player     = Player.Create(PlayerFields.Zero, PlayerFields.Rules, PlayerFields.Scripts)
          Activities = List.empty
          Events     = List.empty
          Mailbox    = Mailbox.Zero
          ExitFlag   = false
          Prefabs    = Map.empty }
    
    static member Initialize s =
        s
    
    static member Terminate s =
        s
    
    static member SetPrefabs s prefabs =
        { s with Prefabs = prefabs }
    
    //Process update logic for all entities
    static member UpdateEntities dt s =
        let activities', mailbox' =
             List.fold (fun (activities, mailbox) (activity : Activity) ->
                let activity', mailbox' = activity.Update (s, mailbox, dt)
                (activity' :: activities, mailbox')) (List.empty, s.Mailbox) s.Activities
                
        let player', mailbox'' = s.Player.Update (s, mailbox', dt)
        
        { s with Player     = player'
                 Activities = activities'
                 Mailbox    = mailbox'' }
    
    static member EmptyMailbox s =
        { s with Mailbox = Mailbox.Zero }
    
    //Add new entities
    static member Emerge s =
        let getFields name =
            match name with
            | "music" -> Some ActivityFields.Music
            | "game"  -> Some ActivityFields.Game
            | "paint" -> Some ActivityFields.Paint
            | _ -> Debug.LogError ("Failed to find activity fields for " + name + "!"); None
        
        let getAdditions =
            List.fold (fun additions mail ->
                match mail with
                | AddActivity name ->
                    match (getFields name) with 
                    | Some fields -> fields :: additions
                    | None -> additions
                | _ -> additions) []
        
        let getActivities =
            List.fold (fun activities (fields : ActivityFields) ->
                let activity =
                    Activity.Create({ fields with ID = randomHash () },
                                    ActivityFields.Rules,
                                    ActivityFields.Scripts)
                (activity :: activities)) s.Activities
        
        let filterMail =
            List.filter (fun mail -> match mail with | AddActivity _ -> false | _ -> true)
        
        let contentsLens = State.mailbox >>| Mailbox.contents
        
        match Map.tryFind 0 s.Mailbox.Contents with 
        | Some mails -> s |> State.activities.Set (mails |> getAdditions |> getActivities)
                          |> contentsLens.Set (Map.add 0 (filterMail mails) s.Mailbox.Contents) 
        | None -> s
    
    //Remove old entities
    static member Cleanse s =
        let getRemovals =
            List.fold (fun removals mail ->
                match mail with
                | RemoveActivity name -> name :: removals
                | _ -> removals) []
        
        let filterActivities =
            fun removals -> List.filter (fun (a : Activity) ->
                not <| List.exists (fun e -> a.Fields.Name = e) removals) s.Activities
        
        let filterMail =
            List.filter (fun mail -> match mail with | RemoveActivity _ -> false | _ -> true)
        
        let contentsLens = State.mailbox >>| Mailbox.contents
        
        match Map.tryFind 0 s.Mailbox.Contents with
        | Some mails ->
            s |> State.activities.Set (mails |> getRemovals |> filterActivities)
              //|> contentsLens.Set (Map.add 0 (filterMail mails) s.Mailbox.Contents)
        | None -> s
    
    static member Update dt s =
        s |> State.UpdateEntities dt
          |> State.Emerge
          |> State.Cleanse
    
    //Return a list of activity fields
    static member GetActivityData (s : State) =
        List.fold (fun acc activity -> activity.Fields :: acc) [] s.Activities
    
    static member player =
        { Get = fun (x : State) -> x.Player
          Set = fun v (x : State) -> {x with Player = v} }
    
    static member activities =
        { Get = fun (x : State) -> x.Activities
          Set = fun v (x : State) -> {x with Activities = v} }
    
    static member mailbox =
        { Get = fun (x : State) -> x.Mailbox
          Set = fun v (x : State) -> {x with Mailbox = v} }
    
    static member prefabs =
        { Get = fun (x : State) -> x.Prefabs
          Set = fun v (x : State) -> {x with Prefabs = v} }
