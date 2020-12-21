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

type IStorage<'T> =
    abstract member Get : unit-> 'T list
    abstract member Add : 'T -> 'T

let getHeroes (storage : IStorage<Hero>) =
    storage.Get

let createHero (storage : IStorage<Hero>) hero =
    { Id = Guid.NewGuid()
      Name = hero.Name
      Species = hero.Species
      Abilities = hero.Abilities }
    |> storage.Add