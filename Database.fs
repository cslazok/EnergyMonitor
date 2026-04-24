namespace EnergyMonitor
open System
open Npgsql
open EnergyMonitor.Db.Tables
open DotNetEnv

module Database =
    let rand = Random()

    let getConnString () = 
        try
            Env.Load() |> ignore
            let conn = Env.GetString("DB_CONNECTION")
            if String.IsNullOrWhiteSpace(conn) then None else Some conn
        with _ -> None

    let generateDummyEnergyData () =
        [ { Db.Tables.shelly_3em_energy.id = 1L
            ts = DateTime.Now
            device_id = Some 1
            total_act = Some 12.456
            total_act_ret = Some 0.123
            import_total_kwh = Some 8.312
            export_total_kwh = Some 24.891
            net_total_kwh = Some -16.579 } ]

    let generateDummyShellyData () =
        let now = DateTime.Now
        List.init 20 (fun i ->
            let baseVoltage = 230.0 + rand.NextDouble() * 4.0 - 2.0
            {
                id = int64 (i + 1)
                ts = now.AddMinutes(float -i * 3.0)
                device_id = 1
                a_voltage    = Some (baseVoltage + rand.NextDouble() * 2.0)
                a_current    = Some (rand.NextDouble() * 5.0)
                a_act_power  = Some (rand.NextDouble() * 800.0)
                a_aprt_power = Some (rand.NextDouble() * 850.0)
                a_pf         = Some (0.85 + rand.NextDouble() * 0.1)
                a_freq       = Some 50.0
                b_voltage    = Some (baseVoltage + rand.NextDouble() * 2.0)
                b_current    = Some (rand.NextDouble() * 5.0)
                b_act_power  = Some (rand.NextDouble() * 800.0)
                b_aprt_power = Some (rand.NextDouble() * 850.0)
                b_pf         = Some (0.85 + rand.NextDouble() * 0.1)
                b_freq       = Some 50.0
                c_voltage    = Some (baseVoltage + rand.NextDouble() * 2.0)
                c_current    = Some (rand.NextDouble() * 5.0)
                c_act_power  = Some (rand.NextDouble() * 800.0)
                c_aprt_power = Some (rand.NextDouble() * 850.0)
                c_pf         = Some (0.85 + rand.NextDouble() * 0.1)
                c_freq       = Some 50.0
                n_current    = Some (rand.NextDouble() * 0.5)
                total_current    = Some (rand.NextDouble() * 15.0)
                total_act_power  = Some (rand.NextDouble() * 2400.0)
                total_aprt_power = Some (rand.NextDouble() * 2500.0)
                import_power     = Some (rand.NextDouble() * 2400.0)
                export_power     = Some 0.0
            }
        )

    let readRow (reader: System.Data.Common.DbDataReader) =
        let getFloat (col: int) =
            if reader.IsDBNull(col) then None
            else Some (reader.GetDouble(col))
        {
            id           = reader.GetInt64(0)
            ts           = reader.GetDateTime(1)
            device_id    = reader.GetInt32(2)
            a_voltage    = getFloat 3
            a_current    = getFloat 4
            a_act_power  = getFloat 5
            a_aprt_power = getFloat 6
            a_pf         = getFloat 7
            a_freq       = getFloat 8
            b_voltage    = getFloat 9
            b_current    = getFloat 10
            b_act_power  = getFloat 11
            b_aprt_power = getFloat 12
            b_pf         = getFloat 13
            b_freq       = getFloat 14
            c_voltage    = getFloat 15
            c_current    = getFloat 16
            c_act_power  = getFloat 17
            c_aprt_power = getFloat 18
            c_pf         = getFloat 19
            c_freq       = getFloat 20
            n_current    = getFloat 21
            total_current    = getFloat 22
            total_act_power  = getFloat 23
            total_aprt_power = getFloat 24
            import_power     = getFloat 25
            export_power     = getFloat 26
        }

    let getShellyDataLastHour () =
        task {
            match getConnString() with
            | None ->
                printfn "[Demo mód] Nincs .env fájl – demo adatokat generálok."
                return generateDummyShellyData()
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    let sql = """
                        SELECT id, ts, device_id,
                               a_voltage, a_current, a_act_power, a_aprt_power, a_pf, a_freq,
                               b_voltage, b_current, b_act_power, b_aprt_power, b_pf, b_freq,
                               c_voltage, c_current, c_act_power, c_aprt_power, c_pf, c_freq,
                               n_current,
                               total_current, total_act_power, total_aprt_power,
                               import_power, export_power
                        FROM shelly_3em_live
                        WHERE ts >= NOW() - INTERVAL '1 hour'
                        ORDER BY ts DESC
                    """
                    use cmd = new NpgsqlCommand(sql, conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    let results = System.Collections.Generic.List<shelly_3em_live>()
                    while reader.Read() do
                        results.Add(readRow reader)
                    return results |> Seq.toList
                with ex ->
                    printfn "[Hiba] Adatbázis-kapcsolat sikertelen – demo adatokat generálok. Hiba: %s" ex.Message
                    return generateDummyShellyData()
        }

    let getEnergyDataLastHour () =
        task {
            match getConnString() with
            | None ->
                printfn "[Demo mód] Nincs .env fájl – demo energia adatokat generálok."
                return generateDummyEnergyData()
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    let sql = """
                        SELECT id, ts, device_id, total_act, total_act_ret, import_total_kwh, export_total_kwh, net_total_kwh
                        FROM shelly_3em_energy
                        WHERE ts >= NOW() - INTERVAL '1 hour'
                        ORDER BY ts DESC
                    """
                    use cmd = new NpgsqlCommand(sql, conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    let results = System.Collections.Generic.List<Db.Tables.shelly_3em_energy>()
                    while reader.Read() do
                        results.Add({
                            id             = reader.GetInt64(0)
                            ts             = reader.GetDateTime(1)
                            device_id      = if reader.IsDBNull(2) then None else Some(reader.GetInt32(2))
                            total_act      = if reader.IsDBNull(3) then None else Some(reader.GetDouble(3))
                            total_act_ret  = if reader.IsDBNull(4) then None else Some(reader.GetDouble(4))
                            import_total_kwh = if reader.IsDBNull(5) then None else Some(reader.GetDouble(5))
                            export_total_kwh = if reader.IsDBNull(6) then None else Some(reader.GetDouble(6))
                            net_total_kwh    = if reader.IsDBNull(7) then None else Some(reader.GetDouble(7))
                        })
                    return results |> Seq.toList
                with _ -> return []
        }