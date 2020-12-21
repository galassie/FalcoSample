module HeroAPI.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open HeroAPI.Domain
open HeroAPI.DataAccess
open System

let storage = new HeroSqliteStorage()

let getHeroesWithStorage = getHeroes storage
let createHeroWithStorage = createHero storage

let mapHero hero =
    {| Id = hero.Id.ToString()
       Name = hero.Name
       Species = hero.Species |> Species.toString 
       Abilities = hero.Abilities |> Array.map (fun (Ability ab) -> ab) |}

type HeroInput =
    { Name: string
      Species: string
      Abilities: string array }

let handleGetHeroes : HttpHandler =
    Request.mapRoute
        (ignore)
        (fun _ -> 
            getHeroesWithStorage ()
            |> List.map mapHero
            |> Response.ofJson)

let handleCreateHero : HttpHandler = 
    Request.bindJson
        (fun heroInput ->
            { Id = Guid.Empty
              Name = heroInput.Name
              Species = (heroInput.Species |> Species.parse)
              Abilities = (heroInput.Abilities |> Array.map (fun ab -> Ability ab)) }
            |> createHeroWithStorage
            |> mapHero
            |> Response.ofJson)
        (fun _ ->
            Response.withStatusCode 400 
            >> Response.ofPlainText "Bad request")

[<EntryPoint>]
let main args =      
    webHost args {
        endpoints [            
            get "/heroes" handleGetHeroes 

            post "/heroes" handleCreateHero

            get "/" (Response.ofPlainText "Hello, HeroAPI here!")
        ]
    }        
    0