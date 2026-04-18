open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.DependencyInjection
open EnergyMonitor

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)
    // builder.Services.AddControllers() |> ignore // Ez opcionális, ha csak MapGet van
    
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

    app.Run()
    0