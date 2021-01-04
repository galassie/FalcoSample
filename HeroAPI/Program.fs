module HeroAPI.Program

open System
open Falco
open Falco.Routing
open Falco.HostBuilder
open HeroAPI.Domain
open HeroAPI.DataAccess
open HeroAPI.Mapper

let getHeroIdFromRoute (routeCollection : RouteCollectionReader) =
    routeCollection.TryGetGuid "id"
    |> function 
        | Some heroId -> Result.Ok heroId
        | _           -> Result.Error "No valid Hero Id provided"

let handleGenericBadRequest _ =
    Response.withStatusCode 400 >> Response.ofPlainText "Bad request"

let handleError =
    function
    | GenericError message  -> Response.withStatusCode 400 >> Response.ofPlainText message
    | NotFoundError message -> Response.withStatusCode 404 >> Response.ofPlainText message
    | DbError (message, _)  -> Response.withStatusCode 500 >> Response.ofPlainText message

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

let handleGetHero getHeroUseCase : HttpHandler =
    Request.bindRoute
        getHeroIdFromRoute
        (fun heroId ->
            getHeroUseCase heroId
            |> function
                | Result.Ok hero ->
                    hero
                    |> HeroMapper.output
                    |> Response.ofJson
                | Result.Error error -> handleError error)
        handleGenericBadRequest

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
        getHeroIdFromRoute
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

let handleDeleteHero deleteHeroUseCase : HttpHandler =
    Request.bindRoute
        getHeroIdFromRoute
        (fun heroId ->
            deleteHeroUseCase heroId
            |> function
                | Result.Ok hero     -> HeroMapper.output hero |> Response.ofJson
                | Result.Error error -> handleError error)
        handleGenericBadRequest

[<EntryPoint>]
let main args =
        
    let storage = new HeroSqliteStorage()
    
    let getHeroesWithStorage = getHeroes storage
    let getHeroWithStorage = getHero storage
    let createHeroWithStorage = createHero storage
    let updateHeroWithStorage = updateHero storage
    let deleteHeroWithStorage = deleteHero storage

    webHost args {
        endpoints [
            get "/heroes" (handleGetHeroes getHeroesWithStorage)

            get "/heroes/{id:guid}" (handleGetHero getHeroWithStorage)

            post "/heroes" (handleCreateHero createHeroWithStorage)

            put "/heroes/{id:guid}" (handleUpdateHero updateHeroWithStorage)

            delete "/heroes/{id:guid}" (handleDeleteHero deleteHeroWithStorage)

            get "/" (Response.ofPlainText "Hello, HeroAPI here!")
        ]
    }        
    0