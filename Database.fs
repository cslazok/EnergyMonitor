namespace EnergyMonitor
open System
open Npgsql
open EnergyMonitor.Db.Tables
open DotNetEnv

module Database =
    let getConnString () = 
        Env.Load() |> ignore
        Env.GetString("DB_CONNECTION")

    let getShellyDataLastHour () =
        task {
            let connStr = getConnString()
            use conn = new NpgsqlConnection(connStr)
            do! conn.OpenAsync()
            let sql = """
                SELECT id, ts, device_id, a_voltage, total_act_power, total_current
                FROM shelly_3em_live
                WHERE ts >= NOW() - INTERVAL '1 hour'
                ORDER BY ts DESC
            """
            use cmd = new NpgsqlCommand(sql, conn)
            use! reader = cmd.ExecuteReaderAsync()
            let results = System.Collections.Generic.List<shelly_3em_live>()
            while reader.Read() do
                results.Add({
                    id = reader.GetInt64(0)
                    ts = reader.GetDateTime(1)
                    device_id = reader.GetInt32(2)
                    a_current = None
                    a_voltage = if reader.IsDBNull(3) then None else Some(reader.GetDouble(3))
                    a_act_power = None; a_aprt_power = None; a_pf = None; a_freq = None
                    b_current = None; b_voltage = None; b_act_power = None; b_aprt_power = None; b_pf = None; b_freq = None
                    c_current = None; c_voltage = None; c_act_power = None; c_aprt_power = None; c_pf = None; c_freq = None
                    n_current = None
                    total_current = if reader.IsDBNull(5) then None else Some(reader.GetDouble(5))
                    total_act_power = if reader.IsDBNull(4) then None else Some(reader.GetDouble(4))
                    total_aprt_power = None
                    import_power = None; export_power = None
                })
            return results |> Seq.toList
        }

    let getEnergyDataLastHour () =
        task {
            let connStr = getConnString()
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
                    id = reader.GetInt64(0)
                    ts = reader.GetDateTime(1)
                    device_id = if reader.IsDBNull(2) then None else Some(reader.GetInt32(2))
                    total_act = if reader.IsDBNull(3) then None else Some(reader.GetDouble(3))
                    total_act_ret = if reader.IsDBNull(4) then None else Some(reader.GetDouble(4))
                    import_total_kwh = if reader.IsDBNull(5) then None else Some(reader.GetDouble(5))
                    export_total_kwh = if reader.IsDBNull(6) then None else Some(reader.GetDouble(6))
                    net_total_kwh = if reader.IsDBNull(7) then None else Some(reader.GetDouble(7))
                })
            return results |> Seq.toList
        }