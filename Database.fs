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

    let private dbObj (v: float option) : obj =
        match v with Some x -> x :> obj | None -> DBNull.Value :> obj

    let insertShellyLive (row: Db.Tables.shelly_3em_live) =
        task {
            match getConnString() with
            | None -> ()
            | Some connStr ->
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync()
                let sql = """
                    INSERT INTO shelly_3em_live
                    (ts, device_id,
                     a_voltage, a_current, a_act_power, a_aprt_power, a_pf, a_freq,
                     b_voltage, b_current, b_act_power, b_aprt_power, b_pf, b_freq,
                     c_voltage, c_current, c_act_power, c_aprt_power, c_pf, c_freq,
                     n_current, total_current, total_act_power, total_aprt_power,
                     import_power, export_power)
                    VALUES (@ts, @device_id,
                            @a_voltage, @a_current, @a_act_power, @a_aprt_power, @a_pf, @a_freq,
                            @b_voltage, @b_current, @b_act_power, @b_aprt_power, @b_pf, @b_freq,
                            @c_voltage, @c_current, @c_act_power, @c_aprt_power, @c_pf, @c_freq,
                            @n_current, @total_current, @total_act_power, @total_aprt_power,
                            @import_power, @export_power)
                """
                use cmd = new NpgsqlCommand(sql, conn)
                let p (n: string) (v: obj) = cmd.Parameters.AddWithValue(n, v) |> ignore
                p "@ts"               (row.ts :> obj)
                p "@device_id"        (row.device_id :> obj)
                p "@a_voltage"        (dbObj row.a_voltage)
                p "@a_current"        (dbObj row.a_current)
                p "@a_act_power"      (dbObj row.a_act_power)
                p "@a_aprt_power"     (dbObj row.a_aprt_power)
                p "@a_pf"             (dbObj row.a_pf)
                p "@a_freq"           (dbObj row.a_freq)
                p "@b_voltage"        (dbObj row.b_voltage)
                p "@b_current"        (dbObj row.b_current)
                p "@b_act_power"      (dbObj row.b_act_power)
                p "@b_aprt_power"     (dbObj row.b_aprt_power)
                p "@b_pf"             (dbObj row.b_pf)
                p "@b_freq"           (dbObj row.b_freq)
                p "@c_voltage"        (dbObj row.c_voltage)
                p "@c_current"        (dbObj row.c_current)
                p "@c_act_power"      (dbObj row.c_act_power)
                p "@c_aprt_power"     (dbObj row.c_aprt_power)
                p "@c_pf"             (dbObj row.c_pf)
                p "@c_freq"           (dbObj row.c_freq)
                p "@n_current"        (dbObj row.n_current)
                p "@total_current"    (dbObj row.total_current)
                p "@total_act_power"  (dbObj row.total_act_power)
                p "@total_aprt_power" (dbObj row.total_aprt_power)
                p "@import_power"     (dbObj row.import_power)
                p "@export_power"     (dbObj row.export_power)
                let! _ = cmd.ExecuteNonQueryAsync()
                ()
        }

    let insertShellyEnergy (row: Db.Tables.shelly_3em_energy) =
        task {
            match getConnString() with
            | None -> ()
            | Some connStr ->
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync()
                let sql = """
                    INSERT INTO shelly_3em_energy
                    (ts, device_id, total_act, total_act_ret, import_total_kwh, export_total_kwh, net_total_kwh)
                    VALUES (@ts, @device_id, @total_act, @total_act_ret, @import_total_kwh, @export_total_kwh, @net_total_kwh)
                """
                use cmd = new NpgsqlCommand(sql, conn)
                let p (n: string) (v: obj) = cmd.Parameters.AddWithValue(n, v) |> ignore
                let optF (v: float option) : obj = match v with Some x -> x :> obj | None -> DBNull.Value :> obj
                let optI (v: int option)   : obj = match v with Some x -> x :> obj | None -> DBNull.Value :> obj
                p "@ts"               (row.ts :> obj)
                p "@device_id"        (optI row.device_id)
                p "@total_act"        (optF row.total_act)
                p "@total_act_ret"    (optF row.total_act_ret)
                p "@import_total_kwh" (optF row.import_total_kwh)
                p "@export_total_kwh" (optF row.export_total_kwh)
                p "@net_total_kwh"    (optF row.net_total_kwh)
                let! _ = cmd.ExecuteNonQueryAsync()
                ()
        }

    let insertInverterLive (connected: bool) (data: System.Collections.Generic.IDictionary<string, obj>) (houseConsumption: float option) (houseA: float option) (houseB: float option) (houseC: float option) =
        task {
            match getConnString() with
            | None -> ()
            | Some connStr ->
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync()
                let sql = """
                    INSERT INTO inverter_live
                    (ts, connected, active_power, pv_total_power,
                     pv1_voltage, pv1_current, pv2_voltage, pv2_current,
                     daily_yield, total_yield, battery_soc, battery_power,
                     temperature, grid_frequency, power_factor, status,
                     l1_voltage, l1_current, l2_voltage, l2_current, l3_voltage, l3_current,
                     inverter_consumption, pv1_power, pv2_power, house_consumption_w,
                     house_consumption_a_w, house_consumption_b_w, house_consumption_c_w)
                    VALUES (@ts, @connected, @active_power, @pv_total_power,
                            @pv1_voltage, @pv1_current, @pv2_voltage, @pv2_current,
                            @daily_yield, @total_yield, @battery_soc, @battery_power,
                            @temperature, @grid_frequency, @power_factor, @status,
                            @l1_voltage, @l1_current, @l2_voltage, @l2_current, @l3_voltage, @l3_current,
                            @inverter_consumption, @pv1_power, @pv2_power, @house_consumption_w,
                            @house_consumption_a_w, @house_consumption_b_w, @house_consumption_c_w)
                """
                use cmd = new NpgsqlCommand(sql, conn)
                let p (n: string) (v: obj) = cmd.Parameters.AddWithValue(n, v) |> ignore
                let f key : obj =
                    match data.TryGetValue(key) with
                    | true, (:? float as v) -> v :> obj
                    | _ -> DBNull.Value :> obj
                p "@ts"             (DateTime.UtcNow :> obj)
                p "@connected"      (connected :> obj)
                p "@active_power"   (f "activePower")
                p "@pv_total_power" (f "pvTotalPower")
                p "@pv1_voltage"    (f "pv1Voltage")
                p "@pv1_current"    (f "pv1Current")
                p "@pv2_voltage"    (f "pv2Voltage")
                p "@pv2_current"    (f "pv2Current")
                p "@daily_yield"    (f "dailyYield")
                p "@total_yield"    (f "totalYield")
                p "@battery_soc"    (f "batterySOC")
                p "@battery_power"  (f "batteryPower")
                p "@temperature"    (f "temperature")
                p "@grid_frequency" (f "gridFrequency")
                p "@power_factor"   (f "powerFactor")
                p "@status"         (f "status")
                p "@l1_voltage"     (f "l1Voltage")
                p "@l1_current"     (f "l1Current")
                p "@l2_voltage"     (f "l2Voltage")
                p "@l2_current"     (f "l2Current")
                p "@l3_voltage"           (f "l3Voltage")
                p "@l3_current"           (f "l3Current")
                p "@inverter_consumption" (f "inverterConsumption")
                p "@pv1_power"            (f "pv1Power")
                p "@pv2_power"            (f "pv2Power")
                let optBox o = o |> Option.map box |> Option.defaultValue (box DBNull.Value)
                p "@house_consumption_w"   (optBox houseConsumption)
                p "@house_consumption_a_w" (optBox houseA)
                p "@house_consumption_b_w" (optBox houseB)
                p "@house_consumption_c_w" (optBox houseC)
                let! _ = cmd.ExecuteNonQueryAsync()
                ()
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

    type MeterCalibration = {
        ImportOffset:    float
        ExportOffset:    float
        BaselineImport:  float option
        BaselineExport:  float option
        SetAt:           DateTime option
    }

    let private emptyCalibration = { ImportOffset = 0.0; ExportOffset = 0.0; BaselineImport = None; BaselineExport = None; SetAt = None }

    let getMeterCalibration () =
        task {
            match getConnString() with
            | None -> return emptyCalibration
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    use cmd = new NpgsqlCommand("SELECT import_offset, export_offset, baseline_import, baseline_export, set_at FROM meter_calibration ORDER BY set_at DESC LIMIT 1", conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    if reader.Read() then
                        return {
                            ImportOffset   = reader.GetDouble(0)
                            ExportOffset   = reader.GetDouble(1)
                            BaselineImport = if reader.IsDBNull(2) then None else Some (reader.GetDouble(2))
                            BaselineExport = if reader.IsDBNull(3) then None else Some (reader.GetDouble(3))
                            SetAt          = Some (reader.GetDateTime(4))
                        }
                    else return emptyCalibration
                with _ -> return emptyCalibration
        }

    let setMeterCalibration (importOffset: float) (exportOffset: float) (baselineImport: float option) (baselineExport: float option) =
        task {
            match getConnString() with
            | None -> ()
            | Some connStr ->
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync()
                use cmd = new NpgsqlCommand("INSERT INTO meter_calibration (import_offset, export_offset, baseline_import, baseline_export) VALUES (@i, @e, @bi, @be)", conn)
                cmd.Parameters.AddWithValue("@i",  importOffset) |> ignore
                cmd.Parameters.AddWithValue("@e",  exportOffset) |> ignore
                cmd.Parameters.AddWithValue("@bi", baselineImport |> Option.map box |> Option.defaultValue (box DBNull.Value)) |> ignore
                cmd.Parameters.AddWithValue("@be", baselineExport |> Option.map box |> Option.defaultValue (box DBNull.Value)) |> ignore
                let! _ = cmd.ExecuteNonQueryAsync()
                ()
        }

    type ElectricityPrices = {
        ImportLowHuf:    float
        ImportHighHuf:   float
        ExportHuf:       float
        AnnualLimitKwh:  float
    }

    let private defaultPrices = { ImportLowHuf = 36.0; ImportHighHuf = 70.1; ExportHuf = 5.11; AnnualLimitKwh = 2523.0 }

    let getElectricityPrices () =
        task {
            match getConnString() with
            | None -> return defaultPrices
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    use cmd = new NpgsqlCommand("SELECT import_low_huf, import_high_huf, export_huf, annual_limit_kwh FROM electricity_prices ORDER BY valid_from DESC LIMIT 1", conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    if reader.Read() then
                        return { ImportLowHuf = reader.GetDouble(0); ImportHighHuf = reader.GetDouble(1); ExportHuf = reader.GetDouble(2); AnnualLimitKwh = reader.GetDouble(3) }
                    else return defaultPrices
                with _ -> return defaultPrices
        }

    let setElectricityPrices (p: ElectricityPrices) =
        task {
            match getConnString() with
            | None -> ()
            | Some connStr ->
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync()
                use cmd = new NpgsqlCommand("INSERT INTO electricity_prices (import_low_huf, import_high_huf, export_huf, annual_limit_kwh) VALUES (@a, @b, @c, @d)", conn)
                cmd.Parameters.AddWithValue("@a", p.ImportLowHuf)   |> ignore
                cmd.Parameters.AddWithValue("@b", p.ImportHighHuf)  |> ignore
                cmd.Parameters.AddWithValue("@c", p.ExportHuf)      |> ignore
                cmd.Parameters.AddWithValue("@d", p.AnnualLimitKwh) |> ignore
                let! _ = cmd.ExecuteNonQueryAsync()
                ()
        }

    type RoiSettings = { InvestmentHuf: float; SzaldoStart: DateTime option; SetAt: DateTime option }

    let private defaultRoi = { InvestmentHuf = 0.0; SzaldoStart = None; SetAt = None }

    type ShellyPowers = {
        ImportPower: float option
        ExportPower: float option
        AActPower:   float option
        BActPower:   float option
        CActPower:   float option
    }

    let getLatestShellyPower () =
        task {
            match getConnString() with
            | None -> return { ImportPower = None; ExportPower = None; AActPower = None; BActPower = None; CActPower = None }
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    use cmd = new NpgsqlCommand(
                        "SELECT import_power, export_power, a_act_power, b_act_power, c_act_power FROM shelly_3em_live ORDER BY ts DESC LIMIT 1", conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    if reader.Read() then
                        let g i = if reader.IsDBNull(i) then None else Some (reader.GetDouble(i))
                        return { ImportPower = g 0; ExportPower = g 1; AActPower = g 2; BActPower = g 3; CActPower = g 4 }
                    else return { ImportPower = None; ExportPower = None; AActPower = None; BActPower = None; CActPower = None }
                with _ -> return { ImportPower = None; ExportPower = None; AActPower = None; BActPower = None; CActPower = None }
        }

    let getRoiSettings () =
        task {
            match getConnString() with
            | None -> return defaultRoi
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    use cmd = new NpgsqlCommand("SELECT investment_huf, szaldo_start, set_at FROM roi_settings ORDER BY set_at DESC LIMIT 1", conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    if reader.Read() then
                        return {
                            InvestmentHuf = reader.GetDouble(0)
                            SzaldoStart   = if reader.IsDBNull(1) then None else Some (reader.GetDateTime(1))
                            SetAt         = Some (reader.GetDateTime(2))
                        }
                    else return defaultRoi
                with _ -> return defaultRoi
        }

    let setRoiSettings (investmentHuf: float) (szaldoStart: DateTime option) =
        task {
            match getConnString() with
            | None -> ()
            | Some connStr ->
                use conn = new NpgsqlConnection(connStr)
                do! conn.OpenAsync()
                use cmd = new NpgsqlCommand("INSERT INTO roi_settings (investment_huf, szaldo_start) VALUES (@i, @s)", conn)
                cmd.Parameters.AddWithValue("@i", investmentHuf) |> ignore
                cmd.Parameters.AddWithValue("@s", szaldoStart |> Option.map box |> Option.defaultValue (box DBNull.Value)) |> ignore
                let! _ = cmd.ExecuteNonQueryAsync()
                ()
        }

    let getInverterYieldNearDate (dt: DateTime) =
        task {
            match getConnString() with
            | None -> return None
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    use cmd = new NpgsqlCommand(
                        "SELECT total_yield FROM inverter_live WHERE ts >= @dt AND total_yield IS NOT NULL AND total_yield > 0 ORDER BY ts ASC LIMIT 1", conn)
                    cmd.Parameters.AddWithValue("@dt", dt) |> ignore
                    use! reader = cmd.ExecuteReaderAsync()
                    if reader.Read() && not (reader.IsDBNull(0)) then return Some (reader.GetDouble(0))
                    else return None
                with _ -> return None
        }

    let getCurrentInverterYield () =
        task {
            match getConnString() with
            | None -> return None
            | Some connStr ->
                try
                    use conn = new NpgsqlConnection(connStr)
                    do! conn.OpenAsync()
                    use cmd = new NpgsqlCommand(
                        "SELECT total_yield FROM inverter_live WHERE total_yield IS NOT NULL AND total_yield > 0 ORDER BY ts DESC LIMIT 1", conn)
                    use! reader = cmd.ExecuteReaderAsync()
                    if reader.Read() && not (reader.IsDBNull(0)) then return Some (reader.GetDouble(0))
                    else return None
                with _ -> return None
        }