module EnergyMonitor.Startup

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open EnergyMonitor

let dashboardHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
        task {
            let t1 = Database.getShellyDataLastHour()
            let t2 = Database.getEnergyDataLastHour()
            let! data        = t1
            let! energyData  = t2
            let latest = data |> List.tryHead |> Option.defaultValue (Database.generateDummyShellyData() |> List.head)
            let latestEnergy = energyData |> List.tryHead
            let inverterState = ctx.RequestServices.GetRequiredService<InverterState>()
            let inverterData  = Inverter.buildSnapshot inverterState
            return! htmlView (Views.liveDashboard latest latestEnergy inverterData) next ctx
        }

let historyHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
        task {
            let! data = Database.getShellyDataLastHour()
            return! htmlView (Views.historyTable data) next ctx
        }

let apiHistoryHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
        task {
            let! data = Database.getShellyDataLastHour()
            return! json data next ctx
        }

let private startTime = DateTime.UtcNow

let statusHandler : HttpHandler =
    fun next ctx ->
        task {
            let inverterState = ctx.RequestServices.GetRequiredService<InverterState>()
            let inv  = Inverter.buildSnapshot inverterState
            let! shellyData = Database.getShellyDataLastHour()
            let uptime = DateTime.UtcNow - startTime
            let uptimeStr =
                if uptime.TotalHours >= 1.0 then sprintf "%dh %dm" (int uptime.TotalHours) uptime.Minutes
                else sprintf "%dm" uptime.Minutes
            let invData = inverterState.GetData()
            let lastPoll =
                match invData.TryGetValue("lastUpdated") with
                | true, (:? string as s) ->
                    match DateTime.TryParse(s) with
                    | true, dt -> dt.ToLocalTime().ToString("HH:mm:ss")
                    | _ -> "-"
                | _ -> "-"
            let status = {|
                inverter = {|
                    connected    = inv |> Option.map    (fun s -> s.Connected)    |> Option.defaultValue false
                    lastPoll     = lastPoll
                    activePower  = inv |> Option.bind   (fun s -> s.ActivePower)  |> Option.defaultValue 0.0
                    pvTotalPower = inv |> Option.bind   (fun s -> s.PvTotalPower) |> Option.defaultValue 0.0
                    dailyYield   = inv |> Option.bind   (fun s -> s.DailyYield)   |> Option.defaultValue 0.0
                    batterySOC   = inv |> Option.bind   (fun s -> s.BatterySOC)   |> Option.defaultValue 0.0
                |}
                database = {|
                    ok           = shellyData.Length > 0
                    lastRead     = shellyData |> List.tryHead |> Option.map (fun r -> r.ts.ToLocalTime().ToString("HH:mm:ss")) |> Option.defaultValue "-"
                    rowsLastHour = shellyData.Length
                |}
                uptime = uptimeStr
            |}
            return! json status next ctx
        }

let webApp =
    choose [
        GET >=> choose [
            route "/"            >=> dashboardHandler
            route "/history"     >=> historyHandler
            route "/api/history" >=> apiHistoryHandler
            route "/api/status"  >=> statusHandler
            route "/energy"      >=> (fun next ctx -> task {
                let! data         = Database.getEnergyDataLastHour()
                let! calibration  = Database.getMeterCalibration()
                let! prices       = Database.getElectricityPrices()
                let! roi          = Database.getRoiSettings()
                let! currentYield = Database.getCurrentInverterYield()
                return! htmlView (Views.energyPage data calibration prices roi None currentYield) next ctx
            })
            route "/api/energy"  >=> (fun next ctx -> task {
                let! data = Database.getEnergyDataLastHour()
                return! json data next ctx
            })
        ]
        POST >=> route "/energy/calibrate" >=> (fun next ctx -> task {
            let! form = ctx.Request.ReadFormAsync()
            let! energyData = Database.getEnergyDataLastHour()
            let latestShelly = energyData |> List.tryHead
            match latestShelly with
            | None -> return! text "Nincs Shelly adat" next ctx
            | Some shelly ->
                let parseFloat (key: string) =
                    let raw = form.[key].ToString().Trim()
                    let mutable v = 0.0
                    if System.String.IsNullOrWhiteSpace(raw) then None
                    elif System.Double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, &v) then Some v
                    elif System.Double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, &v) then Some v
                    else None
                match parseFloat "meter_import", parseFloat "meter_export" with
                | None, _ | _, None -> return! text "Hibás vagy hiányzó érték" next ctx
                | Some meterImport, Some meterExport ->
                    let shellyImport = shelly.import_total_kwh |> Option.defaultValue 0.0
                    let shellyExport = shelly.export_total_kwh |> Option.defaultValue 0.0
                    let importOffset = meterImport - shellyImport
                    let exportOffset = meterExport - shellyExport
                    let baselineImport = parseFloat "baseline_import"
                    let baselineExport = parseFloat "baseline_export"
                    do! Database.setMeterCalibration importOffset exportOffset baselineImport baselineExport
                    return! redirectTo false "/energy" next ctx
        })
        POST >=> route "/energy/prices" >=> (fun next ctx -> task {
            let! form = ctx.Request.ReadFormAsync()
            let parseFloat (key: string) =
                let raw = form.[key].ToString().Trim()
                let mutable v = 0.0
                if System.Double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, &v) then Some v
                elif System.Double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, &v) then Some v
                else None
            match parseFloat "import_low", parseFloat "import_high", parseFloat "export_huf", parseFloat "annual_limit" with
            | Some a, Some b, Some c, Some d ->
                do! Database.setElectricityPrices { ImportLowHuf = a; ImportHighHuf = b; ExportHuf = c; AnnualLimitKwh = d }
                return! redirectTo false "/energy" next ctx
            | _ -> return! text "Hibás érték" next ctx
        })
        POST >=> route "/energy/roi" >=> (fun next ctx -> task {
            let! form = ctx.Request.ReadFormAsync()
            let parseFloat (key: string) =
                let raw = form.[key].ToString().Trim()
                let mutable v = 0.0
                if System.Double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, &v) then Some v
                elif System.Double.TryParse(raw, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.CurrentCulture, &v) then Some v
                else None
            let parseDate (key: string) =
                let raw = form.[key].ToString().Trim()
                let mutable dt = System.DateTime.MinValue
                if System.DateTime.TryParse(raw, &dt) then Some dt else None
            match parseFloat "investment_huf" with
            | Some inv ->
                let szaldoStart = parseDate "szaldo_start"
                do! Database.setRoiSettings inv szaldoStart
                return! redirectTo false "/energy" next ctx
            | None -> return! text "Hibás érték" next ctx
        })
        setStatusCode 404 >=> text "Not Found"
    ]

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddGiraffe()                              |> ignore
    builder.Services.AddSingleton<MqttPublisher>()             |> ignore
    builder.Services.AddSingleton<InverterState>()             |> ignore
    builder.Services.AddHostedService<ModbusReaderService>()   |> ignore
    builder.Services.AddHostedService<ShellyMqttService>()     |> ignore

    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    app.UseStaticFiles() |> ignore
    app.UseGiraffe webApp

    app.Run()
    0
