module EnergyMonitor.Startup

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    
    let app = builder.Build()
    
    app.UseDefaultFiles() |> ignore
    app.UseStaticFiles() |> ignore

    app.MapGet("/api/history", new Func<Task<IResult>>(fun () ->
        task {
            try
                let! data = Database.getShellyDataLastHour()
                return Results.Ok(data)
            with ex ->
                return Results.Problem(ex.Message)
        })) |> ignore

    app.MapGet("/api/energy", new Func<Task<IResult>>(fun () ->
        task {
            try
                let! data = Database.getEnergyDataLastHour()
                return Results.Ok(data)
            with ex ->
                return Results.Problem(ex.Message)
        })) |> ignore

    app.Run()
    0