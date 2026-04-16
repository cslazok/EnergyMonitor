open Microsoft.AspNetCore.Builder
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.DependencyInjection
open WebSharper.AspNetCore

[<EntryPoint>]
let main args =
    let builder = WebApplication.CreateBuilder(args)

    builder.Services.AddWebSharper()
        .AddAuthentication("WebSharper")
        .AddCookie("WebSharper", fun _ -> ())
    |> ignore

    let app = builder.Build()

    if not (app.Environment.IsDevelopment()) then
        app.UseExceptionHandler("/Error").UseHsts() |> ignore

    app.UseHttpsRedirection()
#if DEBUG
        .UseWebSharperScriptRedirect(startVite = true)
#endif
        .UseDefaultFiles()
        .UseStaticFiles()
    |> ignore

    app.Run()
    0