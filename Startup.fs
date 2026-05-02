module EnergyMonitor.Startup

open System
open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Giraffe
open Giraffe.ViewEngine
open EnergyMonitor

// ---------------------------------
// Web API / Handlers
// ---------------------------------

let dashboardHandler : HttpHandler =
    fun (next : HttpFunc) (ctx : Microsoft.AspNetCore.Http.HttpContext) ->
        task {
            let! data = Database.getShellyDataLastHour()
            let! energyData = Database.getEnergyDataLastHour()
            let! inverterData = Inverter.getSnapshot()
            let latest = data |> List.tryHead |> Option.defaultValue (Database.generateDummyShellyData() |> List.head)
            let latestEnergy = energyData |> List.tryHead
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

// ---------------------------------
// Web Routes
// ---------------------------------

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

// ---------------------------------
// Main
// ---------------------------------

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    // Add Giraffe and other services
    builder.Services.AddGiraffe() |> ignore
    
    let app = builder.Build()

    if app.Environment.IsDevelopment() then
        app.UseDeveloperExceptionPage() |> ignore

    app.UseStaticFiles() |> ignore
    app.UseGiraffe webApp

    app.Run()
    0