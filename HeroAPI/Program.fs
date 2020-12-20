module HeroAPI.Program

open Falco
open Falco.Routing
open Falco.HostBuilder
open HeroAPI.Domain
open HeroAPI.DataAccess

let storage = new HeroSqliteStorage()

let getHeroesWithStorage = getHeroes storage
let createHeroWithStorage = createHero storage

let handleGetHeroes : HttpHandler =
    Request.mapRoute
        (ignore)
        (fun _ -> 
            getHeroesWithStorage ()
            |> Response.ofJson)

let handleCreateHero : HttpHandler = 
    Request.bindJson
        (fun heroDto ->
            createHero heroDto
            |> Response.ofJson)
        (fun error ->
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