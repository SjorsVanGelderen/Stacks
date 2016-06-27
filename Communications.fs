(*
    Basic communications within and from outside of the state
    Note that some types are only declared here to prevent module ordering problems
*)

module Communications

open System
open Lens
open Model

//Unique identifier
type ID = int

//Identifiers for any activity, enumerated because it's used in a C# switch elsewhere
type ActivityType =
    | Default   = 0
    | Human     = 1
    | Social    = 2
    | Financial = 3
    | Job       = 4

//Activities can be scheduled or continuous
type ActivityMode =
    | Scheduled of DateTime * DateTime
    | Continuous

//The basic unit of communication
type Mail =
    | GlobalPause
    | GlobalResume
    | CompleteDay
    | ElapseTime       of double //In days
    | AddActivity      of ID * ActivityType * ActivityMode
    | RemoveActivity   of string
    | PauseActivity    of string
    | ResumeActivity   of string
    | CompleteActivity of string
    | AffectFinancial  of int
    | AffectHealth     of int
    | AffectEnergy     of int
    | AffectBrand      of int
    | AffectExpertise  of Audience * int
    | AffectSkills     of Skill * int

//A collection of communication units
type Mailbox =
    { Contents : Map<ID, Mail List> } with

    static member Zero =
        { Contents = Map.empty }

    static member Send mailbox id mailQueue =
        let mails = Mailbox.Access mailbox id
        { mailbox with Contents = mailbox.Contents.Add (id, mailQueue @ mails) }

    static member SendUnique mailbox id mailQueue =
        let mails = Mailbox.Access mailbox id
        let filteredQueue = List.filter (fun mail ->
            not <| List.exists (fun otherMail -> mail = otherMail) mails) mailQueue

        { mailbox with Contents = mailbox.Contents.Add (id, filteredQueue @ mails) }

    static member Filter mailbox id predicate =
        let mails = Mailbox.Access mailbox id
        { mailbox with Contents = Map.add id <| List.filter predicate mails <| mailbox.Contents }

    static member Replace mailbox id mails =
        Map.add id mails mailbox.Contents

    static member Access mailbox id =
        match Map.tryFind id mailbox.Contents with
        | Some mails -> mails
        | None       -> []

    static member contents =
        { Get = fun (x : Mailbox) -> x.Contents
          Set = fun v (x : Mailbox) -> {x with Contents = v} }
