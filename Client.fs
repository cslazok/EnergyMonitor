namespace EnergyMonitor

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.JavaScript

[<JavaScript>]
module Client =

    type Page = | Live | History
    let currentPage = Var.Create Live

    type LiveEnergyData = {
        inverterActivePower: float
        inverterDailyYield: float
        totalActivePower: float
        calculatedMeterWh: float
        lastUpdated: string
    }

    let liveData = Var.Create (None: LiveEnergyData option)

    let refreshLive () =
        async {
            while true do
                try
                    let! data = 
                        Fetch.Fetch("/api/data")
                        |> Async.AwaitTask
                    let! json = data.Json() |> Async.AwaitTask
                    let parsed = As<LiveEnergyData> json
                    liveData.Value <- Some parsed
                with _ -> ()
                do! Async.Sleep 1000
        } |> Async.Start

    let historyView =
        div [attr.``class`` "section"] [
            h2 [] [text "Múltbéli adatok (Utolsó 1 óra)"]
            p [] [text "A múltbéli adatok betöltése folyamatban..."]
        ]

    let liveView =
        div [attr.``class`` "row"] [
            liveData.View.DocLens (function
                | None -> div [attr.``class`` "col-12 text-center"] [ text "Várakozás adatokra..." ]
                | Some data ->
                    div [attr.``class`` "col-12"] [
                        div [attr.``class`` "row"] [
                            // Inverter Card
                            div [attr.``class`` "col-md-6 mb-3"] [
                                div [attr.``class`` "card bg-success text-white"] [
                                    div [attr.``class`` "card-body"] [
                                        h5 [attr.``class`` "card-title"] [text "Napelem Inverter"]
                                        h2 [] [text (sprintf "%.1f W" data.inverterActivePower)]
                                        p [] [text (sprintf "Ma: %.2f kWh" data.inverterDailyYield)]
                                    ]
                                ]
                            ]
                            // Shelly Card
                            div [attr.``class`` "col-md-6 mb-3"] [
                                div [attr.``class`` "card bg-primary text-white"] [
                                    div [attr.``class`` "card-body"] [
                                        h5 [attr.``class`` "card-title"] [text "Ház Fogyasztása"]
                                        h2 [] [text (sprintf "%.1f W" data.totalActivePower)]
                                        p [] [text (sprintf "Óraállás: %.2f kWh" (data.calculatedMeterWh / 1000.0))]
                                    ]
                                ]
                            ]
                        ]
                        p [attr.``class`` "text-muted small"] [text (sprintf "Utolsó frissítés: %s" data.lastUpdated)]
                    ]
            )
        ]

    let pageView =
        div [] [
            div [attr.``class`` "navbar bg-dark text-white p-3"] [
                h1 [] [text "Energy Monitor"]
                nav [] [
                    button [attr.``class`` "btn btn-outline-light mx-2"; on.click (fun _ _ -> currentPage.Value <- Live)] [text "Élő"]
                    button [attr.``class`` "btn btn-outline-light mx-2"; on.click (fun _ _ -> currentPage.Value <- History)] [text "Múlt (1h)"]
                ]
            ]
            div [attr.``class`` "container mt-4"] [
                currentPage.View.DocLens (function
                    | Live -> div [] [ h2 [attr.``class`` "mb-4"] [text "Pillanatnyi adatok"]; liveView ]
                    | History -> historyView
                )
            ]
        ]

    [<SPAEntryPoint>]
    let Main () =
        refreshLive()
        pageView |> Doc.RunById "main"