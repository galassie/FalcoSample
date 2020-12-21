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

let handleError =
    function
    | GenericError message -> Response.withStatusCode 400 >> Response.ofPlainText message
    | DbError (message, _) -> Response.withStatusCode 500 >> Response.ofPlainText message

type HeroInput =
    { Name: string
      Species: string
      Abilities: string array }

let handleGetHeroes : HttpHandler =
    Request.mapRoute
        (ignore)
        (fun _ -> 
            getHeroesWithStorage ()
            |> function
                | Result.Ok heroes ->
                    heroes
                    |> List.map mapHero
                    |> Response.ofJson
                | Result.Error error -> handleError error)

let handleCreateHero : HttpHandler = 
    Request.bindJson
        (fun heroInput ->
            { Id = Guid.Empty
              Name = heroInput.Name
              Species = (heroInput.Species |> Species.parse)
              Abilities = (heroInput.Abilities |> Array.map (fun ab -> Ability ab)) }
            |> createHeroWithStorage
            |> function
                | Result.Ok hero ->
                    hero
                    |> mapHero
                    |> Response.ofJson
                | Result.Error error -> handleError error)
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