namespace EnergyMonitor

open Npgsql
open SqlHydra.Query
open EnergyMonitor.Db
open EnergyMonitor.Db.Tables

open DotNetEnv 

module Database =
  
   if System.IO.File.Exists(".env") then
    Env.Load() |> ignore
    
    let private connString = DotNetEnv.Env.GetString("DB_CONNECTION")
    let getLatestShellyData () =
        task {
            use conn = new NpgsqlConnection(connString)
            do! conn.OpenAsync()
            
            let compiler = SqlKata.Compilers.PostgresCompiler()
            let ctx = new QueryContext(conn, compiler)

            let! rows =
                selectTask ctx {
                    for x in shelly_3em_live do 
                    take 1
                    select x
                }

            return rows
        }

    let getLatestPowerData () =
        task {
            use conn = new NpgsqlConnection(connString)
            do! conn.OpenAsync()
            
            let compiler = SqlKata.Compilers.PostgresCompiler()
            let ctx = new QueryContext(conn, compiler)

            let! rows =
                selectTask ctx {
                    for x in shelly_3em_energy do 
                    take 1
                    select x
                }

            return rows
        }