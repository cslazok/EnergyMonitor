namespace EnergyMonitor

open System
open System.Net.Http
open System.Text.Json

module Inverter =

    type Snapshot = {
        Connected: bool
        StatusText: string
        InputPower:   float option
        ActivePower:  float option
        DailyYield:   float option
        TotalYield:   float option
        BatterySOC:   float option
        BatteryPower: float option
        Temperature:  float option
        Efficiency:   float option
    }

    let private http = new HttpClient(Timeout = TimeSpan.FromSeconds(3.0))

    let private tryFloat (root: JsonElement) (key: string) =
        match root.TryGetProperty(key) with
        | true, el when el.ValueKind = JsonValueKind.Number -> Some (el.GetDouble())
        | _ -> None

    let getSnapshot () =
        task {
            try
                let! json = http.GetStringAsync("http://localhost:5051/api/inverter")
                use doc = JsonDocument.Parse(json)
                let r = doc.RootElement
                let connected =
                    match r.TryGetProperty("connected") with
                    | true, el -> el.GetBoolean()
                    | _ -> false
                let statusText =
                    match r.TryGetProperty("statusText") with
                    | true, el -> el.GetString() |> Option.ofObj |> Option.defaultValue "?"
                    | _ -> "?"
                return Some {
                    Connected   = connected
                    StatusText  = statusText
                    InputPower  = tryFloat r "inputPower"
                    ActivePower = tryFloat r "activePower"
                    DailyYield  = tryFloat r "dailyYield"
                    TotalYield  = tryFloat r "totalYield"
                    BatterySOC  = tryFloat r "batterySOC"
                    BatteryPower = tryFloat r "batteryPower"
                    Temperature = tryFloat r "temperature"
                    Efficiency  = tryFloat r "efficiency"
                }
            with _ ->
                return None
        }
