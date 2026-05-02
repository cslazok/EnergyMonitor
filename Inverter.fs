namespace EnergyMonitor

module Inverter =

    type Snapshot = {
        Connected:    bool
        StatusText:   string
        Pv1Voltage:   float option
        Pv1Current:   float option
        Pv2Voltage:   float option
        Pv2Current:   float option
        PvTotalPower: float option
        ActivePower:  float option
        DailyYield:   float option
        TotalYield:   float option
        BatterySOC:   float option
        BatteryPower: float option
        Temperature:  float option
    }

    let private tryFloat (data: System.Collections.Generic.IDictionary<string, obj>) (key: string) =
        match data.TryGetValue(key) with
        | true, (:? float as v) -> Some v
        | _ -> None

    let buildSnapshot (state: InverterState) : Snapshot option =
        if not (state.IsInitialized()) then None
        else
            let data = state.GetData()
            let tf   = tryFloat data
            let connected =
                match data.TryGetValue("connected") with
                | true, (:? bool as v) -> v
                | _ -> false
            let statusText =
                match data.TryGetValue("statusText") with
                | true, (:? string as v) -> v
                | _ -> "?"
            Some {
                Connected    = connected
                StatusText   = statusText
                Pv1Voltage   = tf "pv1Voltage"
                Pv1Current   = tf "pv1Current"
                Pv2Voltage   = tf "pv2Voltage"
                Pv2Current   = tf "pv2Current"
                PvTotalPower = tf "pvTotalPower"
                ActivePower  = tf "activePower"
                DailyYield   = tf "dailyYield"
                TotalYield   = tf "totalYield"
                BatterySOC   = tf "batterySOC"
                BatteryPower = tf "batteryPower"
                Temperature  = tf "temperature"
            }
