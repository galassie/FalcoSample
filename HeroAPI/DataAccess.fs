module HeroAPI.DataAccess

open System
open System.IO
open Donald
open Microsoft.Data.Sqlite
open HeroAPI.Domain

type HeroSqliteStorage() =
    let connString = "Filename=" + Path.Combine(Directory.GetCurrentDirectory(), "hero.db")

    interface IStorage<Hero> with
        member this.Add(hero : Hero): Hero = 
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
                | Result.Ok _ -> hero
                | Result.Error _ -> hero
        member this.Get(): Hero list = 
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
                | Result.Ok heroes -> heroes
                | Result.Error _ -> []
