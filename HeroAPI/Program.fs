module HeroAPI.Program

open System
open Falco
open Falco.Routing
open Falco.HostBuilder
open HeroAPI.Domain
open HeroAPI.DataAccess
open HeroAPI.Mapper

let handleGenericBadRequest _ =
    Response.withStatusCode 400 >> Response.ofPlainText "Bad request"

let handleError =
    function
    | GenericError message -> Response.withStatusCode 400 >> Response.ofPlainText message
    | DbError (message, _) -> Response.withStatusCode 500 >> Response.ofPlainText message

let handleGetHeroes getHeroesUseCase : HttpHandler =
    Request.mapRoute
        (ignore)
        (fun _ -> 
            getHeroesUseCase ()
            |> function
                | Result.Ok heroes ->
                    heroes
                    |> List.map HeroMapper.output
                    |> Response.ofJson
                | Result.Error error -> handleError error)

let handleCreateHero createHeroUseCase : HttpHandler = 
    Request.bindJson
        (fun heroInput ->
            HeroMapper.input Guid.Empty heroInput
            |> createHeroUseCase
            |> function
                | Result.Ok hero     -> HeroMapper.output hero |> Response.ofJson
                | Result.Error error -> handleError error)
        handleGenericBadRequest

let handleUpdateHero updateHeroUseCase : HttpHandler =
    Request.bindRoute
        (fun routeCollection -> 
            let mutable heroId = Guid.Empty
            routeCollection.TryGetString "id"
            |> function 
                | Some id when Guid.TryParse(id, &heroId) -> Result.Ok heroId
                | _  -> Result.Error "No valid Hero Id provided")
        (fun heroId ->
            Request.bindJson
                (fun heroInput -> 
                      HeroMapper.input heroId heroInput
                      |> updateHeroUseCase
                      |> function
                          | Result.Ok hero     -> HeroMapper.output hero |> Response.ofJson
                          | Result.Error error -> handleError error)
                handleGenericBadRequest)
        handleGenericBadRequest

[<EntryPoint>]
let main args =
        
    let storage = new HeroSqliteStorage()
    
    let getHeroesWithStorage = getHeroes storage
    let createHeroWithStorage = createHero storage
    let updateHeroWithStorage = updateHero storage

    webHost args {
        endpoints [            
            get "/heroes" (handleGetHeroes getHeroesWithStorage)

            post "/heroes" (handleCreateHero createHeroWithStorage)

            put "/heroes/{id:guid}" (handleUpdateHero updateHeroWithStorage)

            get "/" (Response.ofPlainText "Hello, HeroAPI here!")
        ]
    }        
    0