﻿module HeroAPI.DataAccess

open System
open System.IO
open Donald
open Microsoft.Data.Sqlite
open HeroAPI.Domain

type HeroSqliteStorage() =
    let connString = "Filename=" + Path.Combine(Directory.GetCurrentDirectory(), "heroes.db")

    interface IStorage<Hero> with
        member _.GetAll(): Result<Hero list, Error> = 
            use conn = new SqliteConnection(connString)
            dbCommand conn {
                cmdText "SELECT * FROM hero"
            }
            |> DbConn.query (fun rd -> 
                { Id = (rd.ReadString "Id" |> Guid.Parse)
                  Name = rd.ReadString "Name"
                  Species = (rd.ReadString "Species" |> Species.parse)
                  Abilities = (rd.ReadString "Abilities" |> (fun el -> el.Split [|','|]) |> Array.map (fun ab -> Ability ab))})
            |> function
                | Ok heroes  -> Ok heroes
                | Error err  -> Error (DbError (err.Statement, err.Error :> Exception))
                
        member _.Get(heroId : Guid): Result<Hero option, Error> = 
            use conn = new SqliteConnection(connString)
            dbCommand conn {
                cmdText "SELECT * FROM hero WHERE Id=@Id"
                cmdParam [
                    "@Id", SqlType.String (heroId.ToString())
                ]
            }
            |> DbConn.query (fun rd -> 
                { Id = (rd.ReadString "Id" |> Guid.Parse)
                  Name = rd.ReadString "Name"
                  Species = (rd.ReadString "Species" |> Species.parse)
                  Abilities = (rd.ReadString "Abilities" |> (fun el -> el.Split [|','|]) |> Array.map (fun ab -> Ability ab))})
            |> function
                | Ok heroes ->
                    match heroes with
                    | hero::_ -> Ok (Option.Some hero)
                    | []      -> Ok Option.None
                | Error err -> Error (DbError (err.Statement, err.Error :> Exception))

        member _.Add(hero : Hero) : Result<Hero, Error> = 
            use conn = new SqliteConnection(connString)
            dbCommand conn {
                cmdText "INSERT INTO hero VALUES (@Id,@Name,@Species,@Abilities)"
                cmdParam [
                    "@Id", SqlType.String (hero.Id.ToString())
                    "@Name", SqlType.String hero.Name
                    "@Species", SqlType.String (hero.Species |> Species.toString)
                    "@Abilities", SqlType.String (hero.Abilities |> Array.map (fun (Ability str) -> str) |> Array.reduce (fun acc elem -> acc + "," + elem))
                ]
            }
            |> DbConn.exec
            |> function
                | Ok _      -> Ok hero
                | Error err -> Error (DbError (err.Statement, err.Error :> Exception))

        member _.Update(hero : Hero) : Result<Hero, Error> = 
            use conn = new SqliteConnection(connString)
            dbCommand conn {
                cmdText "UPDATE hero SET Name=@Name,Species=@Species,Abilities=@Abilities WHERE Id=@Id"
                cmdParam [
                    "@Id", SqlType.String (hero.Id.ToString())
                    "@Name", SqlType.String hero.Name
                    "@Species", SqlType.String (hero.Species |> Species.toString)
                    "@Abilities", SqlType.String (hero.Abilities |> Array.map (fun (Ability str) -> str) |> Array.reduce (fun acc elem -> acc + "," + elem))
                ]
            }
            |> DbConn.exec
            |> function
                | Ok _      -> Ok hero
                | Error err -> Error (DbError (err.Statement, err.Error :> Exception))
                
        member _.Delete(heroId : Guid) : Result<unit, Error> = 
            use conn = new SqliteConnection(connString)
            dbCommand conn {
                cmdText "DELETE FROM hero WHERE Id=@Id"
                cmdParam [
                    "@Id", SqlType.String (heroId.ToString())
                ]
            }
            |> DbConn.exec
            |> function
                | Ok _      -> Ok ()
                | Error err -> Error (DbError (err.Statement, err.Error :> Exception))
