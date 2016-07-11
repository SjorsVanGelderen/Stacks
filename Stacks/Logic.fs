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

    let mutable event      : Event Option = None       //Event to be triggered
    let mutable requestsUI : Mail List    = []         //UI mails to be captured by the state
    let mutable state      : State        = State.Zero //The base game state

    member this.Start () =
        do state <- state |> State.Initialize

    member this.Update () =
        //Capture UI requests in the state before updating
        do state <- { state with Mailbox = Mailbox.Send state.Mailbox 0 requestsUI }
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
        | _        -> Debug.LogError <| "Failed to process UI request!"

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

    //When the UI requests an activity's cleansing
    member this.CleanseActivity id =
        requestsUI <- (RemoveActivity id) :: requestsUI

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
    
    //Get the date from which an activity will start
    member this.QueryActivityStartDate id : DateTime =
        let dateOption =
            List.tryPick (fun (elem : Activity) ->
                if elem.Fields.ID = id then
                    match elem.Fields.Mode with
                    | Scheduled (from, _) -> Some from
                    | _                   -> None
                else
                    None) state.Activities
        
        match dateOption with
        | Some date -> date
        | None      -> Debug.LogError("Failed to access activity start date!"); new DateTime(2016, 1, 1);

    //Get the date until which an activity will last
    member this.QueryActivityEndDate id : DateTime =
        let dateOption =
            List.tryPick (fun (elem : Activity) ->
                if elem.Fields.ID = id then
                    match elem.Fields.Mode with
                    | Scheduled (_, until) -> Some until
                    | _                    -> None
                else
                    None) state.Activities
        
        match dateOption with
        | Some date -> date
        | None      -> Debug.LogError("Failed to access activity end date!"); new DateTime(2016, 1, 1);

    //Get the game time
    member this.QueryDateTime () =
        state.Timer.Fields.DateTime

    //Check if an event should happen
    member this.QueryEvent () =
        if state.Events.IsEmpty then
            Debug.LogError <| "Failed to roll for event! None have been defined!"
            false
        else
            let dayOccupied =
                let time = state.Timer.Fields.DateTime
                List.exists (fun (elem : Activity) ->
                    match elem.Fields.Mode with
                    | Scheduled (from, until) when time >= from && time < until -> true
                    | _ -> false) state.Activities

            if dayOccupied then
                false
            else
                if random.NextDouble () < 0.2 then
                    event <- Some state.Events.[ random.Next (state.Events.Length) ]
                    state <- { state with Paused = true }
                    true
                else
                    event <- None
                    false

    //Get description of planned event
    member this.QueryEventDescription () =
        match event with
        | Some e -> e.Description
        | None   -> Debug.LogError <| "Failed to access event description, no event specified!"; ""

    //Check whether planned event is mandatory
    member this.QueryEventMandatory () =
        match event with
        | Some e -> e.Mandatory
        | None   -> Debug.LogError <| "Failed to access event description, no event specified!"; false

    //Check whether planned event is open to inquiry
    member this.QueryEventInvestigable () =
        match event with
        | Some e -> e.Investigable
        | None   -> Debug.LogError <| "Failed to access event description, no event specified!"; false

    //Get associated activity of planned event
    member this.QueryEventActivity () : ActivityType =
        match event with
        | Some e ->
            match e.Activity with
            | Some activity -> activity
            | None          -> ActivityType.Default
        | None   -> Debug.LogError <| "Failed to access event activity type, no event specified!"; ActivityType.Default
        (*
        match event with
        | Some e -> match e.Activity with | Some activity -> activity | None -> ActivityType.Default
        | None   -> Debug.LogError <| "Failed to access event description, no event specified!"; None
        *)

    //Request the player state information
    member this.QueryPlayerStatus () =
        let status = state.Player.Fields.State
        let products = List.fold (fun acc (elem : Product) ->
            elem.Name + "\n" + acc) "" status.Products

        "Player state\n\n"
        + "Health:\t\t\t"    + status.Health.ToString    () + "\n"
        + "Energy:\t\t"      + status.Energy.ToString    () + "\n"
        + "Financial:\t\t"   + status.Financial.ToString () + "\n"
        + "Brand:\t\t\t"     + status.Brand.ToString     () + "\n\n"
        + "Products:\n"      + products                     + "\n\n"
        + "Expertise and skills not included yet"

    //Process the effects of the event and flush
    member this.ProcessEvent () =
        match event with
        | Some e ->
            requestsUI <- e.Effects @ requestsUI
            event      <- None //Flush the event after processing
        | None   ->
            Debug.LogError <| "Failed to process event; none set!"

    //Just flush the event without processing effects
    member this.FlushEvent () =
        match event with
        | Some e ->
            event <- None //Flush the event after processing
        | None   ->
            Debug.LogError <| "Failed to process event; none set!"
    
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
                    do! wait_ 0.001

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
                     | AffectHealth    change  -> ({ state with Health    = Math.Max(0, Math.Min(state.Health + change, 100)) }, acc)
                     | AffectEnergy    change  -> ({ state with Energy    = Math.Max(0, Math.Min(state.Energy + change, 100)) }, acc)
                     | AffectFinancial change  -> ({ state with Financial = Math.Max(0, state.Financial + change) }, acc)
                     | AffectBrand     change  -> ({ state with Brand     = Math.Max(0, state.Brand     + change) }, acc)
                     | AffectExpertise _       -> (state, acc)
                     | AffectSkill     change  -> ({ state with Skill     = Math.Max(0, state.Skill     + change) }, acc)
                     //| AffectSkills    _       -> (state, acc)
                     | AddProduct      product -> ({ state with Products = product :: state.Products }, acc)
                     | _                       -> (state, mail :: acc)) (fs.State, []) mails

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
            Effects = [ AffectEnergy -1
                        AffectSkill  5 ] }
    
    static member Practice =
        { ActivityFields.Zero with
            Kind    = ActivityType.Practice
            Effects = [ AffectEnergy -1
                        AffectSkill  20 ] }
    
    static member Lessons =
        { ActivityFields.Zero with
            Kind    = ActivityType.Lessons
            Effects = [ AffectEnergy    -3
                        AffectFinancial -100
                        AffectSkill     60] }
    
    static member Analysis =
        { ActivityFields.Zero with
            Kind    = ActivityType.Analysis
            Effects = [ AffectEnergy    -3
                        AffectSkill     30] }

    static member Social =
        { ActivityFields.Zero with
            Kind    = ActivityType.Social
            Effects = [ AffectEnergy -1
                        AffectBrand  5 ] }
    
    static member SocialMedia =
        { ActivityFields.Zero with
            Kind    = ActivityType.SocialMedia
            Effects = [ AffectEnergy    -2
                        AffectBrand     100 ] }
    
    static member NetworkLunch =
        { ActivityFields.Zero with
            Kind    = ActivityType.NetworkLunch
            Effects = [ AffectEnergy    -1
                        AffectBrand     200 ] }
    
    static member ColdCall =
        { ActivityFields.Zero with
            Kind    = ActivityType.ColdCall
            Effects = [ AffectEnergy    -2
                        AffectBrand     150 ] }
    
    static member Financial =
        { ActivityFields.Zero with
            Kind    = ActivityType.Financial
            Effects = [ AffectEnergy    -1
                        AffectFinancial 10 ] }
    
    static member Job =
        { ActivityFields.Zero with
            Kind    = ActivityType.Job
            Effects = [ AffectEnergy    -3
                        AffectFinancial 100 ] }
    
    static member Single =
        { ActivityFields.Zero with
            Kind    = ActivityType.Single
            Effects = [ AffectEnergy    -3
                        AffectBrand     15
                        AffectFinancial -50
                        AddProduct      Product.Single ] }
    
    static member Album =
        { ActivityFields.Zero with
            Kind    = ActivityType.Album
            Effects = [ AffectEnergy    -5
                        AffectBrand     50
                        AffectFinancial -120
                        AddProduct      Product.Album ] }
    
    static member Gig =
        { ActivityFields.Zero with
            Kind    = ActivityType.Gig
            Effects = [ AffectHealth    -2
                        AffectBrand     100
                        AffectFinancial 100 ] }
    
    static member Concert =
        { ActivityFields.Zero with
            Kind    = ActivityType.Concert
            Effects = [ AffectEnergy    -5
                        AffectHealth    -10
                        AffectBrand     120
                        AffectFinancial 200 ] }
    
    static member DayOff =
        { ActivityFields.Zero with
            Kind    = ActivityType.DayOff
            Effects = [ AffectHealth    30
                        AffectEnergy    40 ] }
    
    static member Vacation =
        { ActivityFields.Zero with
            Kind    = ActivityType.Vacation
            Effects = [ AffectHealth    100
                        AffectEnergy    100
                        AffectFinancial -200 ] }
    
    static member Rules =
        [ fun w fs dt -> fs ]

    static member Scripts =
        let progressRoutine =
            co { let! w       = getGlobalState_
                 let! mailbox = getOuterState_
                 let! fs      = getInnerState_
                 let  time    = w.Timer.Fields.DateTime

                 let progress, mailbox' =
                     match fs.Mode with
                     | Scheduled (from, until) ->
                        if time < from then
                                0.0, mailbox
                             else if time >= until then
                                1.0, Mailbox.Send mailbox 0 <| (RemoveActivity fs.ID) :: fs.Effects
                             else
                                normalize (time - from).TotalDays 0.0 (until - from).TotalDays, mailbox
                     | Continuous ->
                         let dayOccupied =
                             List.exists (fun (elem : Activity) ->
                                match elem.Fields.Mode with
                                | Scheduled (from, until) when time >= from && time < until -> true
                                | _ -> false) w.Activities
                         
                         if not dayOccupied then
                            let progress' = normalize (double time.Hour + double time.Minute / 60.0) 0.0 24.0
                            if progress' < fs.Progress then
                                0.0, Mailbox.Send mailbox 0 fs.Effects
                            else
                                progress', mailbox
                         else
                            fs.Progress, mailbox
                 
                 do! setOuterState_ mailbox'
                 do! setInnerState_ { fs with Progress = progress }

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

//Used to define the properties of events
and Event =
    { ID           : int
      Name         : string
      Description  : string
      Mandatory    : bool
      Investigable : bool
      Effects      : Mail List //Only necessary if there are more effects than belong to the activity
      Activity     : ActivityType Option } with

    static member Zero =
        { ID           = -1
          Name         = ""
          Description  = ""
          Mandatory    = false
          Investigable = false
          Effects      = List.empty
          Activity     = None }

    static member RecordDealGood =
        { Event.Zero with
              Name         = "Record deal"
              Description  = "ExcellentRep Inc. is offering you a record deal!"
              Investigable = true
              Effects      = [ AffectFinancial 1000 ]
              Activity     = None }

    static member RecordDealBad =
        { Event.Zero with
              Name         = "Record deal"
              Description  = "Surreptitious Inc. is offering you a record deal!"
              Investigable = true
              Effects      = [ AffectFinancial -300 ]
              Activity     = None }

    static member NegativeReview =
        { Event.Zero with
              Name         = "Negative review"
              Description  = "A famous blogger wrote a negative review of your latest song!"
              Investigable = true
              Effects      = [ AffectBrand -30 ]
              Activity     = None }

    static member Plagiarism =
        { Event.Zero with
              Name        = "Plagiarism"
              Description = "An artist is plagiarising your work!"
              Mandatory   = true
              Effects     = [ AffectFinancial -100 ]
              Activity    = None }

    static member PlagiarismClaim =
        { Event.Zero with
              Name        = "Plagiarism"
              Description = "An artist claims you are plagiarising their work!"
              Mandatory   = true
              Effects     = [ AffectFinancial -100
                              AffectBrand     -10 ]
              Activity    = None }

    static member Illness =
        { Event.Zero with
              Name        = "Illness"
              Description = "You have fallen ill!"
              Mandatory   = true
              Effects     = [ AffectHealth -10 ]
              Activity    = None }

    static member Gig =
        { Event.Zero with
              Name        = "Gig"
              Description = "The local pub asks you to play a gig for three days!"
              Mandatory   = true
              Effects     = List.empty
              Activity    = Some ActivityType.Gig }

    static member CommercialJingle =
        { Event.Zero with
              Name        = "Commercial jingle"
              Description = "A local company wants to use your music as a jingle for their commercial!"
              Mandatory   = true
              Effects     = [ AffectFinancial 500
                              AffectBrand     100 ]
              Activity    = None }

    static member ProductionAssistance =
        { Event.Zero with
              Name        = "Production assistance"
              Description = "You are asked to work on a production, but they are not willing to pay!"
              Mandatory   = true
              Effects     = [ AffectSkill 50
                              AffectBrand 100 ]
              Activity    = None }

    static member Rules =
        [ fun w fs dt -> fs ]

    static member Scripts =
        [ co { do! yield_ } |> repeat_ ]

    static member name =
        { Get = fun (x : Event) -> x.Name
          Set = fun v (x : Event) -> {x with Name = v} }

    static member description =
        { Get = fun (x : Event) -> x.Description
          Set = fun v (x : Event) -> {x with Description = v} }

    static member effects =
        { Get = fun (x : Event) -> x.Effects
          Set = fun v (x : Event) -> {x with Effects = v} }

    static member activity =
        { Get = fun (x : Event) -> x.Activity
          Set = fun v (x : Event) -> {x with Activity = v} }

and Timer    = Entity<State, TimerFields,    Mailbox>
and Player   = Entity<State, PlayerFields,   Mailbox>
and Activity = Entity<State, ActivityFields, Mailbox>

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
          Events     = [ Event.RecordDealGood
                         Event.RecordDealBad
                         Event.NegativeReview
                         Event.Plagiarism
                         Event.PlagiarismClaim
                         Event.Illness
                         Event.Gig
                         Event.CommercialJingle
                         Event.ProductionAssistance ]
          Paused     = true
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
                | GlobalPause  -> { s with Paused = true  }, mails
                | GlobalResume -> { s with Paused = false }, mails
                | _            -> state, mail :: mails) (s, []) mails

        let mailbox' = Mailbox.Replace state'.Mailbox 0 mails'
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
            | ActivityType.Default      -> Some ActivityFields.Zero
            | ActivityType.Human        -> Some ActivityFields.Human
            | ActivityType.Practice     -> Some ActivityFields.Practice
            | ActivityType.Lessons      -> Some ActivityFields.Lessons
            | ActivityType.Analysis     -> Some ActivityFields.Analysis
            | ActivityType.Social       -> Some ActivityFields.Social
            | ActivityType.SocialMedia  -> Some ActivityFields.SocialMedia
            | ActivityType.NetworkLunch -> Some ActivityFields.NetworkLunch
            | ActivityType.ColdCall     -> Some ActivityFields.ColdCall
            | ActivityType.Financial    -> Some ActivityFields.Financial
            | ActivityType.Job          -> Some ActivityFields.Job
            | ActivityType.Single       -> Some ActivityFields.Single
            | ActivityType.Album        -> Some ActivityFields.Album
            | ActivityType.Gig          -> Some ActivityFields.Gig
            | ActivityType.Concert      -> Some ActivityFields.Concert
            | ActivityType.DayOff       -> Some ActivityFields.DayOff
            | ActivityType.Vacation     -> Some ActivityFields.Vacation
            | _ -> Debug.LogError <| "Failed to match activity type: " + kind.ToString() + "!"; None
        
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

        { s with Mailbox    = Mailbox.Replace s.Mailbox 0 mails'
                 Activities = activities' }

    //Remove old entities
    static member Cleanse s =
        let mails = Mailbox.Access s.Mailbox 0

        let removals, mails' =
            List.fold (fun (ids, mails) mail ->
                match mail with
                | RemoveActivity id -> (id :: ids, mails)
                | _                 -> (ids, mail :: mails))
                ([], []) mails

        let activities' =
            List.filter (fun (activity : Activity) ->
                not <| List.exists (fun id -> id = activity.Fields.ID) removals) s.Activities

        { s with Activities = activities'
                 Mailbox    = Mailbox.Replace s.Mailbox 0 mails' }

    static member Update dt s =
        s |> State.ProcessRequests
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
