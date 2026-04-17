namespace HaDashboard

open Npgsql
open SqlHydra.Query
open HaDashboard.DbLive
open HaDashboard.DbEnergy

module Database =

    let private connString =
        "Host=192.168.0.99;Port=5432;Database=energydb;Username=energyuser;Password=ujjelszo123"

    let getLatestShellyData () =
        task {
            use conn = new NpgsqlConnection(connString)
            do! conn.OpenAsync()

            let! rows =
                selectTask conn {
                    for x in Shelly3EmData do
                    take 1
                }

            return rows
        }

    let getLatestPowerData () =
        task {
            use conn = new NpgsqlConnection(connString)
            do! conn.OpenAsync()

            let! rows =
                selectTask conn {
                    for x in PowerData do
                    take 1
                }

            return rows
        }
