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

let webApp =
    choose [
        GET >=> choose [
            route "/"            >=> dashboardHandler
            route "/history"     >=> historyHandler
            route "/api/history" >=> apiHistoryHandler
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
    builder.Services.AddSingleton<InverterState>()             |> ignore
    builder.Services.AddHostedService<ModbusReaderService>()   |> ignore

    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    app.UseStaticFiles() |> ignore
    app.UseGiraffe webApp

    app.Run()
    0
