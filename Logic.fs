(*
   Core game logic
*)

namespace Monaco

open System
open System.Globalization
open UnityEngine
open Coroutine
open Helpers
open Lens
open Communications
open Model

//Alias for dictionaries matching activity IDs to progress measurements
type ActivityProgressDict = System.Collections.Generic.IDictionary<int, float32>

//Core game logic component
type MonacoLogic () =
    inherit MonoBehaviour ()
    
    let mutable requestsUI : Mail List = [] //UI mails to be captured by the state
    let mutable state : State = State.Zero  //The base game state

    member this.Start () =
        do state <- state |> State.Initialize

    member this.Update () =
        //Capture UI requests in the state before updating
        do state <- { state with Mailbox = Mailbox.SendUnique state.Mailbox 0 requestsUI }
        requestsUI <- [] //Clear the processed UI requests
        
        do state <- State.Update Time.deltaTime state
        
        if state.ExitFlag then
            state <- State.Terminate state
            Application.Quit ()

    member this.UIRequest (request : string) =
        //Translate string request to mail
        match request with
        | "pause"  -> requestsUI <- GlobalPause  :: requestsUI
        | "resume" -> requestsUI <- GlobalResume :: requestsUI
        | _        -> Debug.LogError("Failed to process UI request!")

    member this.Quit () =
        do state <- { state with ExitFlag = true }

    //When the UI requests a scheduled activity's emergence
    member this.EmergeScheduledActivity kind from until : int =
        let id = randomHash ()
        requestsUI <- AddActivity (id, kind, Scheduled (from, until)) :: requestsUI
        id

    //When the UI requests a continuous activity's emergence
    member this.EmergeContinuousActivity kind : int =
        let id = randomHash ()
        requestsUI <- AddActivity (id, kind, Continuous) :: requestsUI
        id

    //Get a list of activity ID's and associated progress information
    member this.QueryActivityProgress () : ActivityProgressDict =
        List.fold (fun acc elem ->
            match elem.Fields.Mode with
            | Scheduled (from, until) ->
                let progress = float32 <| (elem.Fields.Progress * Math.Floor((until - from).TotalDays)) % 1.0
                (elem.Fields.ID, progress) :: acc
            | Continuous ->
                (elem.Fields.ID, float32 <| elem.Fields.Progress) :: acc)
            [] state.Activities
            |> Map.ofList |> Map.toSeq |> dict
    
    //Get the game time
    member this.QueryDateTime () =
        state.Timer.Fields.DateTime

    //Check if an event should happen
    member this.QueryEvent () =
        let dayComplete =
            List.exists (fun elem ->
                match elem with
                | CompleteDay -> true
                | _           -> false) <| Mailbox.Access state.Mailbox 0

        if dayComplete then
            let dayOccupied =
                let time = state.Timer.Fields.DateTime
                List.exists (fun (elem : Activity) ->
                    match elem.Fields.Mode with
                    | Scheduled (from, until) when time >= from && time < until -> true
                    | _ -> false) state.Activities

            if not dayOccupied then
                //Possibly trigger an event
                state <- { state with Paused = true }
                true
            else
                //Don't trigger an event
                false
        else
            //Day isn't over, don't trigger an event
            false

//Global entity structure
and Entity<'w, 'fs, 'mailbox> =
    { Fields  : 'fs
      Rules   : List<'w -> 'fs -> float32 -> 'fs>
      Scripts : Coroutine<'w, 'mailbox, 'fs, unit> } with

    static member Create (fields, rules, scripts) =
        { Fields  = fields
          Rules   = rules
          Scripts = andPassMany_ scripts }

    member this.Update (world, mailbox, dt) =
        let fsRules = List.fold (fun fs rule -> rule world fs dt) this.Fields this.Rules
        let mailbox', fsScripts, scripts' = step_ (this.Scripts world mailbox fsRules)
        { this with Fields  = fsScripts
                    Rules   = this.Rules
                    Scripts = scripts' }, mailbox'

//Used to keep track of game time data
and TimerFields =
    { ID       : int
      DateTime : DateTime } with

    static member Zero =
        { ID       = -1
          DateTime = new DateTime(2016, 1, 1) }

    static member Rules =
        [ fun w fs dt -> fs ]

    static member Scripts =
        let timeProgressionRoutine =
            co { let! (w : State) = getGlobalState_
                 let! mailbox = getOuterState_
                 let! fs = getInnerState_

                 //Remove already processed messages
                 let mailbox' = Mailbox.Filter mailbox 0 (function
                     | ElapseTime _  -> false
                     | CompleteDay   -> false
                     | _             -> true)
                 
                 do! setOuterState_ mailbox'
                 
                 if not w.Paused then
                    do! wait_ 0.1
                    
                    let fs' = { fs with DateTime = fs.DateTime.AddHours 0.24 }
                    do! setInnerState_ fs'
                    let mails = [] //[ ElapseTime 0.01 ]

                    let mails'' =
                       if fs.DateTime.Day < fs'.DateTime.Day then
                           CompleteDay :: mails
                       else
                           mails
                    
                    do! setOuterState_ <| Mailbox.Send mailbox' 0 mails''
                 
                 do! yield_ } |> repeat_
        
        [ timeProgressionRoutine ]

    static member dateTime =
        { Get = fun (x : TimerFields) -> x.DateTime
          Set = fun v (x : TimerFields) -> {x with DateTime = v} }

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
        let mailRoutine =
            co { let! (fs : PlayerFields) = getInnerState_
                 let! mailbox = getOuterState_
                 let mails = Mailbox.Access mailbox 0

                 let state', mails' = List.fold (fun ((state : PlayerState), acc) mail ->
                     match mail with
                     | AffectHealth    change -> ({ state with Health    = System.Math.Max(0, state.Health    + change) }, acc)
                     | AffectEnergy    change -> ({ state with Energy    = System.Math.Max(0, state.Energy    + change) }, acc)
                     | AffectFinancial change -> ({ state with Financial = System.Math.Max(0, state.Financial + change) }, acc)
                     | AffectBrand     change -> ({ state with Brand     = System.Math.Max(0, state.Brand     + change) }, acc)
                     | AffectExpertise _      -> (state, acc)
                     | AffectSkills    _      -> (state, acc)
                     | _                      -> (state, mail :: acc)) (fs.State, []) mails

                 do! setInnerState_ { fs with State = state' }
                 do! setOuterState_ { mailbox with Contents = Map.add 0 mails' mailbox.Contents }
                 do! yield_ } |> repeat_

        [ mailRoutine ]

    static member state =
        { Get = fun (x : PlayerFields) -> x.State
          Set = fun v (x : PlayerFields) -> {x with State = v} }

//Used to track the progress of activities
and ActivityFields =
    { ID       : int
      Kind     : ActivityType
      Effects  : Mail List
      Mode     : ActivityMode
      Progress : double
      Paused   : bool } with
    
    static member Zero =
        { ID       = -1
          Kind     = ActivityType.Default
          Effects  = List.empty
          Mode     = Scheduled (new DateTime(2016, 1, 1), new DateTime(2016, 1, 2))
          Progress = 0.0
          Paused   = false }
    
    static member Human =
        { ActivityFields.Zero with
            Kind    = ActivityType.Human
            Effects = [ AffectHealth -1
                        AffectBrand 10
                        AffectFinancial 10 ] }
    
    static member Social =
        { ActivityFields.Zero with
            Kind    = ActivityType.Social
            Effects = [ AffectHealth 10
                        AffectBrand -5
                        AffectFinancial -10 ] }
    
    static member Financial =
        { ActivityFields.Zero with
            Kind    = ActivityType.Financial
            Effects = [ AffectHealth 5
                        AffectBrand 15
                        AffectFinancial 3 ] }
    
    static member Job =
        { ActivityFields.Zero with
            Kind    = ActivityType.Job
            Effects = [ AffectHealth 5
                        AffectBrand 15
                        AffectFinancial 3 ] }
    
    static member Rules =
        [ fun w fs dt -> fs ]

    static member Scripts =
        let progressRoutine =
            co { let! w    = getGlobalState_
                 let! fs   = getInnerState_
                 let  time = w.Timer.Fields.DateTime

                 let progress =
                     match fs.Mode with
                     | Scheduled (from, until) ->
                         if time < from then
                            0.0
                         else if time >= until then
                            1.0
                         else
                            normalize (time - from).TotalDays 0.0 (until - from).TotalDays
                     | Continuous ->
                         let dayOccupied =
                             List.exists (fun (elem : Activity) ->
                                match elem.Fields.Mode with
                                | Scheduled (from, until) when time >= from && time < until -> true
                                | _ -> false) w.Activities
                         
                         if not dayOccupied then
                            normalize (double time.Hour) 0.0 24.0
                         else
                            fs.Progress
                 
                 let fs' = { fs with Progress = progress }
                 do! setInnerState_ fs'

                 do! yield_ } |> repeat_

        [ progressRoutine ]

    static member kind =
        { Get = fun (x : ActivityFields) -> x.Kind
          Set = fun v (x : ActivityFields) -> {x with Kind = v} }

    static member effects =
        { Get = fun (x : ActivityFields) -> x.Effects
          Set = fun v (x : ActivityFields) -> {x with Effects = v} }

    static member schedule =
        { Get = fun (x : ActivityFields) -> x.Mode
          Set = fun v (x : ActivityFields) -> {x with Mode = v} }

    static member paused =
        { Get = fun (x : ActivityFields) -> x.Paused
          Set = fun v (x : ActivityFields) -> {x with Paused = v} }

//Used to track the progress of events
and EventFields =
    { ID          : int
      Name        : string
      Description : string
      Effects     : Mail List
      Activity    : Activity Option } with

    static member Zero =
        { ID          = -1
          Name        = ""
          Description = ""
          Effects     = List.empty
          Activity    = None }

    static member RecordDeal =
        { EventFields.Zero with
              Name        = "Record deal"
              Description = "Surreptitious Inc. is offering you a record deal!"
              Effects     = List.empty
              Activity    = None }

    static member Plagiarism =
        { EventFields.Zero with
              Name        = "Plagiarism"
              Description = "Someone is plagiarising your work!"
              Effects     = List.empty
              Activity    = None }

    static member Illness =
        { EventFields.Zero with
              Name        = "Illness"
              Description = "You have fallen ill!"
              Effects     = List.empty
              Activity    = None }

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

and Timer    = Entity<State, TimerFields,    Mailbox>
and Player   = Entity<State, PlayerFields,   Mailbox>
and Activity = Entity<State, ActivityFields, Mailbox>
and Event    = Entity<State, EventFields,    Mailbox>

//The global game state
and State =
    { Timer      : Timer
      Player     : Player
      Activities : Activity List
      Events     : Event List
      Paused     : bool
      Mailbox    : Mailbox
      ExitFlag   : bool
      Prefabs    : Map<string, GameObject> } with

    static member Zero =
        { Timer      = Timer.Create  (TimerFields.Zero,  TimerFields.Rules,  TimerFields.Scripts)
          Player     = Player.Create (PlayerFields.Zero, PlayerFields.Rules, PlayerFields.Scripts)
          Activities = List.empty
          Events     = [ Event.Create (EventFields.RecordDeal, EventFields.Rules, EventFields.Scripts)
                         Event.Create (EventFields.Plagiarism, EventFields.Rules, EventFields.Scripts)
                         Event.Create (EventFields.Illness,    EventFields.Rules, EventFields.Scripts) ]
          Paused     = false
          Mailbox    = Mailbox.Zero
          ExitFlag   = false
          Prefabs    = Map.empty }

    static member Initialize s =
        s

    static member Terminate s =
        s

    static member SetPrefabs prefabs s =
        { s with Prefabs = prefabs }

    //Process incoming UI requests
    static member ProcessRequests s =
        let mails = Mailbox.Access s.Mailbox 0

        let state', mails' =
            List.fold (fun (state, mails) mail ->
                match mail with
                | GlobalPause  -> { s with Paused = true },  mails
                | GlobalResume -> { s with Paused = false }, mails
                | _ -> s, mail :: mails) (s, []) mails

        let mailbox' = { state'.Mailbox with Contents = Mailbox.Replace s.Mailbox 0 mails' }
        { state' with Mailbox = mailbox' }

    //Process update logic for all entities
    static member UpdateEntities dt s =
        let timer', mailbox' = s.Timer.Update (s, s.Mailbox, dt)

        let activities', mailbox'' =
             List.fold (fun (activities, mailbox) (activity : Activity) ->
                let activity', mailbox' = activity.Update (s, mailbox, dt)
                (activity' :: activities, mailbox')) ([], mailbox') s.Activities
        
        let player', mailbox''' = s.Player.Update (s, mailbox'', dt)

        { s with Timer      = timer'
                 Player     = player'
                 Activities = activities'
                 Mailbox    = mailbox''' }

    static member EmptyMailbox s =
        { s with Mailbox = Mailbox.Zero }

    //Add new entities
    static member Emerge s =
        let mails = Mailbox.Access s.Mailbox 0

        let getFields kind : ActivityFields Option =
            match kind with
            | ActivityType.Default   -> Some ActivityFields.Zero
            | ActivityType.Human     -> Some ActivityFields.Human
            | ActivityType.Social    -> Some ActivityFields.Social
            | ActivityType.Financial -> Some ActivityFields.Financial
            | ActivityType.Job       -> Some ActivityFields.Job
            | _                      -> Debug.LogError ("Failed to match activity type!"); None
        
        let getActivity id mode fields =
            match fields with
            | Some fields ->
                [ Activity.Create(
                    { fields with ID = id; Mode = mode },
                    ActivityFields.Rules,
                    ActivityFields.Scripts) ]
            | None -> []

        let activities', mails' =
            List.fold (fun (activities, mails) mail ->
                match mail with
                | AddActivity (id, kind, mode) -> (kind |> getFields |> getActivity id mode) @ activities, mails
                | _ -> activities, (mail :: mails)) (s.Activities, []) mails

        { s with Mailbox    = { s.Mailbox with Contents = Mailbox.Replace s.Mailbox 0 mails' }
                 Activities = activities' }

    //Remove old entities
    static member Cleanse s =
        s

    static member Update dt s =
        s //|> State.ProcessRequests
          |> State.Emerge
          |> State.UpdateEntities dt
          |> State.Cleanse

    //Return a list of activity fields
    static member GetActivityData (s : State) =
        List.fold (fun acc activity -> activity.Fields :: acc) [] s.Activities

    static member timer =
        { Get = fun (x : State) -> x.Timer
          Set = fun v (x : State) -> {x with Timer = v} }

    static member player =
        { Get = fun (x : State) -> x.Player
          Set = fun v (x : State) -> {x with Player = v} }

    static member activities =
        { Get = fun (x : State) -> x.Activities
          Set = fun v (x : State) -> {x with Activities = v} }

    static member events =
        { Get = fun (x : State) -> x.Events
          Set = fun v (x : State) -> {x with Events= v} }

    static member paused =
        { Get = fun (x : State) -> x.Paused
          Set = fun v (x : State) -> {x with Paused = v} }

    static member mailbox =
        { Get = fun (x : State) -> x.Mailbox
          Set = fun v (x : State) -> {x with Mailbox = v} }

    static member exitFlag =
        { Get = fun (x : State) -> x.ExitFlag
          Set = fun v (x : State) -> {x with ExitFlag = v} }

    static member prefabs =
        { Get = fun (x : State) -> x.Prefabs
          Set = fun v (x : State) -> {x with Prefabs = v} }
