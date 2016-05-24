(*
    Basic communications within and from outside of the state
*)

module Communications

open Lens
open Model

//Unique identifier
type ID = int

//The basic unit of communication
type Mail =
    | AddActivity     of string
    | RemoveActivity  of string
    | PauseActivity   of string
    | ResumeActivity  of string
    | AffectFinancial of int
    | AffectHealth    of int
    | AffectEnergy    of int
    | AffectBrand     of int
    | AffectExpertise of Audience * int
    | AffectSkills    of Skill * int

//A collection of communication units
type Mailbox =
    { Contents : Map<ID, Mail List> } with
    
    static member Send mailbox id mails =
        let contents' = 
            match mailbox.Contents.TryFind id with
            | Some existingMails ->  mailbox.Contents.Add (id, List.fold (fun acc mail -> mail :: acc) existingMails mails)
            | None -> mailbox.Contents.Add (id, mails)
        { mailbox with Contents = contents' }
        
    static member Zero =
        { Contents = Map.empty }
    
    static member contents =
        { Get = fun (x : Mailbox) -> x.Contents
          Set = fun v (x : Mailbox) -> {x with Contents = v} }