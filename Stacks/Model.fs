(*
    Data records for the economic model
*)

module Model

open Lens

type Audience =
    | Rock
    | Classical
    | Techno

type Skill =
    | Drums
    | Guitar

type Product =
    { Name        : string
      Audience    : Audience List
      Quality     : int
      BaseIPValue : int } with
      
    static member Zero =
        { Name        = "Unnamed product"
          Audience    = List.empty
          Quality     = 0
          BaseIPValue = 0 }
      
    static member Single =
        { Product.Zero with
            Name        = "Single"
            Quality     = 10
            BaseIPValue = 10 }
      
    static member Album =
        { Product.Zero with
            Name        = "Album"
            Quality     = 20
            BaseIPValue = 100 }

    static member name =
        { Get = fun (x : Product) -> x.Name
          Set = fun v (x : Product) -> {x with Name = v} }

    static member audience =
        { Get = fun (x : Product) -> x.Audience
          Set = fun v (x : Product) -> {x with Audience = v} }

    static member quality =
        { Get = fun (x : Product) -> x.Quality
          Set = fun v (x : Product) -> {x with Quality = v} }

    static member baseIPValue =
        { Get = fun (x : Product) -> x.BaseIPValue
          Set = fun v (x : Product) -> {x with BaseIPValue = v} }

type PlayerState =
    { Health    : int
      Energy    : int
      Financial : int
      Brand     : int
      Expertise : Map<Audience, int>
      //Skills    : Map<Skill, int>
      Skill     : int
      Products  : Product List } with
      
    static member Zero =
      { Health    = 100
        Energy    = 100
        Financial = 1000
        Brand     = 0
        Expertise = Map.empty
        //Skills    = Map.empty
        Skill     = 0
        Products  = List.empty }

    static member health =
        { Get = fun (x : PlayerState) -> x.Health
          Set = fun v (x : PlayerState) -> {x with Health = v} }

    static member energy =
        { Get = fun (x : PlayerState) -> x.Energy
          Set = fun v (x : PlayerState) -> {x with Energy = v} }

    static member financial =
        { Get = fun (x : PlayerState) -> x.Financial
          Set = fun v (x : PlayerState) -> {x with Financial = v} }

    static member brand =
        { Get = fun (x : PlayerState) -> x.Brand
          Set = fun v (x : PlayerState) -> {x with Brand = v} }

    static member expertise =
        { Get = fun (x : PlayerState) -> x.Expertise
          Set = fun v (x : PlayerState) -> {x with Expertise = v} }

    static member skill =
        { Get = fun (x : PlayerState) -> x.Skill
          Set = fun v (x : PlayerState) -> {x with Skill = v} }

    (*static member skills =
        { Get = fun (x : PlayerState) -> x.Skills
          Set = fun v (x : PlayerState) -> {x with Skills = v} }*)