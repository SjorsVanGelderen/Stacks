(*
    Data records for the economic model
*)

module Model

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

type PlayerState =
    { Health    : int
      Energy    : int
      Financial : int
      Brand     : int
      Expertise : Map<Audience, int>
      Skills    : Map<Skill, int> } with
      
  static member Zero =
      { Health    = 100
        Energy    = 100
        Financial = 10000
        Brand     = 0
        Expertise = Map.empty
        Skills    = Map.empty }