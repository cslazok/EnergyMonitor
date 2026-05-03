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
            route "/api/energy"  >=> (fun next ctx -> task {
                let! data = Database.getEnergyDataLastHour()
                return! json data next ctx
            })
        ]
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
