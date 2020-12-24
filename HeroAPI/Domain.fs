module HeroAPI.Domain

open System

type Species =
    | Human
    | Extraterrestrial
    static member toString spec =
        match spec with
        | Human -> "Human"
        | Extraterrestrial -> "Extraterrestrial"
    static member parse spec =
        match spec with
        | "Human" -> Human
        | "Extraterrestrial" -> Extraterrestrial
        | _ -> failwith "Fail to parse Species"

type Ability = Ability of string

type Hero =
    { Id: Guid
      Name: string
      Species: Species 
      Abilities: Ability array }

type Error =
    | GenericError of string
    | NotFoundError of string
    | DbError of (string * Exception)

type IStorage<'T> =
    abstract member GetAll : unit -> Result<'T list, Error>
    abstract member Get    : Guid -> Result<'T option, Error>
    abstract member Add    : 'T   -> Result<'T, Error>
    abstract member Update : 'T   -> Result<'T, Error>
    abstract member Delete : Guid -> Result<unit, Error>

let getHeroes (storage : IStorage<Hero>) =
    storage.GetAll

let getHero (storage : IStorage<Hero>) heroId =
    storage.Get heroId
    |> Result.bind (fun heroOpt -> 
        match heroOpt with
        | Option.Some hero -> Result.Ok hero
        | Option.None      -> Result.Error (NotFoundError ("Hero Id not found: " + heroId.ToString())))

let createHero (storage : IStorage<Hero>) hero =
    { hero with Id = Guid.NewGuid() }
    |> storage.Add

let updateHero (storage : IStorage<Hero>) hero =
    storage.Get hero.Id
    |> Result.bind (fun heroOpt -> 
        match heroOpt with
        | Option.Some _ -> storage.Update hero
        | Option.None   -> Result.Error (NotFoundError ("Hero Id not found: " + hero.Id.ToString())))

let deleteHero (storage : IStorage<Hero>) heroId =
    storage.Get heroId
    |> Result.bind (fun heroOpt -> 
        match heroOpt with
        | Option.Some hero -> 
            storage.Delete heroId
            |> Result.map (fun _ -> hero)
        | Option.None -> Result.Error (NotFoundError ("Hero Id not found: " + heroId.ToString())))