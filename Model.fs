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

(*
type Activity =
    { Name             : string
      TimeInvestment   : int
      EnergyInvestment : int
      HealthInvestment : int
      Product          : Product Option
      Continuous       : bool } with
      
    static member Zero =
        { Name             = "Unnamed activity"
          TimeInvestment   = 0
          EnergyInvestment = 0
          HealthInvestment = 0
          Product          = None
          Continuous       = false }

    static member name =
        { Get = fun (x : Activity) -> x.Name
          Set = fun v (x : Activity) -> {x with Name = v} }

    static member timeInvestment =
        { Get = fun (x : Activity) -> x.TimeInvestment
          Set = fun v (x : Activity) -> {x with TimeInvestment = v} }

    static member energyInvestment =
        { Get = fun (x : Activity) -> x.EnergyInvestment
          Set = fun v (x : Activity) -> {x with EnergyInvestment = v} }

    static member healthInvestment =
        { Get = fun (x : Activity) -> x.HealthInvestment
          Set = fun v (x : Activity) -> {x with HealthInvestment = v} }

    static member product =
        { Get = fun (x : Activity) -> x.Product
          Set = fun v (x : Activity) -> {x with Product = v} }

    static member continuous =
        { Get = fun (x : Activity) -> x.Continuous
          Set = fun v (x : Activity) -> {x with Continuous = v} }
*)

(*
type Event =
    { Name        : string
      Description : string
      Activity    : Activity
      BrandValue  : int } with
      
    static member Zero =
        { Name        = "Unnamed event"
          Description = "No description"
          Activity    = Activity.Zero
          BrandValue  = 0 }

    static member name =
        { Get = fun (x : Event) -> x.Name
          Set = fun v (x : Event) -> {x with Name = v} }

    static member description =
        { Get = fun (x : Event) -> x.Description
          Set = fun v (x : Event) -> {x with Description = v} }

    static member activity =
        { Get = fun (x : Event) -> x.Activity
          Set = fun v (x : Event) -> {x with Activity = v} }

    static member brandValue =
        { Get = fun (x : Event) -> x.BrandValue
          Set = fun v (x : Event) -> {x with BrandValue = v} }
*)

type PlayerState =
    { Health    : int
      Energy    : int
      Financial : int
      Brand     : int
      Expertise : Map<Audience, int>
      Skills    : Map<Skill, int>
      Products  : Product List } with
      
    static member Zero =
      { Health    = 100
        Energy    = 100
        Financial = 1000
        Brand     = 0
        Expertise = Map.empty
        Skills    = Map.empty
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

    static member skills =
        { Get = fun (x : PlayerState) -> x.Skills
          Set = fun v (x : PlayerState) -> {x with Skills = v} }