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

type ActivityProgressDict = System.Collections.Generic.IDictionary<int, float32>

//Core game logic component
type MonacoLogic () =
    inherit MonoBehaviour ()
    
    [<SerializeField>]
    let mutable uiController : MonacoUIController = Unchecked.defaultof<MonacoUIController> //Base UI controller
    
    let mutable requestsUI : Mail List = [] //UI mails to be captured by the state
    let mutable state : State = State.Zero  //The base game state

    member this.Start () =
        do state <- state |> State.Initialize

        //Process any startup entities here

    member this.Update () =
        //Capture UI requests in the state before updating
        do state <- { state with Mailbox = Mailbox.SendUnique state.Mailbox 0 requestsUI }
        requestsUI <- [] //Clear the processed UI requests

        do state <- State.Update Time.deltaTime state
        //this.UpdateUI ()

        if state.ExitFlag then
            state <- State.Terminate state
            Application.Quit ()

    member this.UpdateUI () =
        match uiController with
        | null -> Debug.LogError ("Failed to access UI controller!")
        | _    ->
            uiController.UpdateDaySlider state.Timer.Fields.DateTime.Hour

            let getProgress (startDate, endDate) =
                let currentDate : DateTime = state.Timer.Fields.DateTime
                if currentDate > startDate && currentDate < endDate then
                    double currentDate.Hour / 24.0
                else
                    0.0
            
            let activityProgressMap =
                List.fold (fun (map : Map<string, double>) (activity : Activity) ->
                    let progress = getProgress activity.Fields.Schedule
                    match activity.Fields.Kind with
                    | ActivityType.Human     -> map.Add ("human",     progress)
                    | ActivityType.Social    -> map.Add ("social",    progress)
                    | ActivityType.Financial -> map.Add ("financial", progress)
                    | ActivityType.Job       -> map.Add ("job",       progress)
                    | _                      -> map) Map.empty state.Activities
            
            uiController.UpdateActivities activityProgressMap

    member this.UIRequest (request : string) =
        //Translate string request to mail
        match request with
        | "pause"  -> requestsUI <- GlobalPause  :: requestsUI
        | "resume" -> requestsUI <- GlobalResume :: requestsUI
        | _        -> Debug.LogError("Failed to process UI request!")

    member this.Quit () =
        do state <- { state with ExitFlag = true }

    member this.QueryActivityProgress () : ActivityProgressDict =
        List.fold (fun acc elem -> (elem.Fields.ID, float32 <| random.NextDouble() ) :: acc) [] state.Activities
            |> Map.ofList |> Map.toSeq |> dict

    member this.QueryDayTime () =
        state.Timer.Fields.DateTime.Hour

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
    { ID         : int
      Kind       : ActivityType
      Effects    : Mail List
      Schedule   : DateTime * DateTime
      Continuous : bool
      Paused     : bool } with
    
    static member Zero =
        { ID         = -1
          Kind       = ActivityType.Default
          Effects    = List.empty
          Schedule   = new DateTime(2016, 1, 1), new DateTime(2016, 1, 2)
          Continuous = false
          Paused     = false }
    
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
            co { let! fs = getInnerState_
                 let! mailbox = getOuterState_
                 let mails = Mailbox.Access mailbox 0

                 (*
                 let fs', mails' =
                     List.fold (fun (fields, mails) mail ->
                         match mail with
                         | ElapseTime amount -> (fields, mail :: mails)
                         | CompleteDay       -> (fields, mail :: mails)
                         | _                 -> (fields, mail :: mails) ) (fs, []) mails
                 
                 do! setOuterState_ { mailbox with Contents = Mailbox.Replace mailbox 0 mails' }*)
                 do! yield_ } |> repeat_

        [ progressRoutine ]

        (*
        let getMailList id mailbox =
            co { match Map.tryFind id mailbox.Contents with
                 | Some mailList -> return mailList
                 | None -> return [] }

        let getPauseMail mailList activityName =
            List.exists (fun mail -> match mail with | PauseActivity name -> name = activityName | _ -> false) mailList

        let getResumeMail mailList activityName =
            List.exists (fun mail -> match mail with | ResumeActivity name -> name = activityName | _ -> false) mailList
        
        let getProgress () =
            List.fold (fun count mail ->
                match mail with
                | TimeElapsed amount -> count + amount
                | _ -> count) 0.0

        let filterMail activityName =
            function
            | AddActivity    name
            | RemoveActivity name
            | PauseActivity  name -> name <> activityName
            | _ -> true

        let mailRoutine =
            co { let! mailbox = getOuterState_
                 let! mailList = getMailList 0 mailbox
                 let! fs = getInnerState_

                 //See if this activity has been paused
                 let fs'  = if getPauseMail  mailList fs.Name then { fs  with Paused = true }  else fs
                 let fs'' = if getResumeMail mailList fs.Name then { fs' with Paused = false } else fs'
                 do! setInnerState_ fs''

                 //Remove any mails relating to this activity
                 let mailbox' = Mailbox.Filter mailbox 0 <| filterMail fs.Name
                 do! setOuterState_ mailbox'
                 do! yield_ } |> repeat_

        let progressRoutine =
            co { let! (w : State) = getGlobalState_
                 let! mailbox = getOuterState_
                 let! mailList = getMailList 0 mailbox
                 let! fs = getInnerState_

                 if fs.Progress < fs.Duration then
                     let progress = getProgress () mailList
                     let fs' = { fs with Progress = fs.Progress + progress }
                     do! setInnerState_ fs'

                     if fs'.Progress >= fs.Duration then
                        let mailbox'  = Mailbox.Send mailbox 0 fs.Effects
                        do! setOuterState_ mailbox'

                        if fs.Continuous then
                            do! setInnerState_ { fs' with Progress = 0.0 }
                        else
                            let mailbox'' = Mailbox.SendUnique mailbox' 0 [ RemoveActivity fs.Name ]
                            do! setOuterState_ mailbox''
                 do! yield_ } |> repeat_

        [ mailRoutine
          progressRoutine ]
        *)

    static member kind =
        { Get = fun (x : ActivityFields) -> x.Kind
          Set = fun v (x : ActivityFields) -> {x with Kind = v} }

    static member effects =
        { Get = fun (x : ActivityFields) -> x.Effects
          Set = fun v (x : ActivityFields) -> {x with Effects = v} }

    static member schedule =
        { Get = fun (x : ActivityFields) -> x.Schedule
          Set = fun v (x : ActivityFields) -> {x with Schedule = v} }

    static member continuous =
        { Get = fun (x : ActivityFields) -> x.Continuous
          Set = fun v (x : ActivityFields) -> {x with Continuous = v} }

    static member paused =
        { Get = fun (x : ActivityFields) -> x.Paused
          Set = fun v (x : ActivityFields) -> {x with Paused = v} }

//Used to track the progress of events
(*
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
*)

and Timer    = Entity<State, TimerFields,    Mailbox>
and Player   = Entity<State, PlayerFields,   Mailbox>
and Activity = Entity<State, ActivityFields, Mailbox>
//and Event    = Entity<State, EventFields,    Mailbox>

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
        { Timer      = Timer.Create (TimerFields.Zero, TimerFields.Rules, TimerFields.Scripts)
          Player     = Player.Create (PlayerFields.Zero, PlayerFields.Rules, PlayerFields.Scripts)
          Activities = [ Activity.Create ({ ActivityFields.Human with ID = randomHash () },
                                            ActivityFields.Rules,
                                            ActivityFields.Scripts) ]
          Events     = []
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
        s

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
