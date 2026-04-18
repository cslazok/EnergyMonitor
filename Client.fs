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

    type LiveEnergy = {
        total_act_power: float
        total_current: float
        total_aprt_power: float
        a_voltage: float
        a_act_power: float
    }

    let historyView =
        let historyData = Var.Create []
        async {
            let! res = Server.GetLastHourData() |> Async.AwaitTask
            historyData.Value <- res
        } |> Async.Start

        div [attr.``class`` "section"] [
            h2 [] [text "Múltbéli adatok (Utolsó 1 óra)"]
            table [attr.``class`` "table table-striped"] [
                thead [ tr [ th [text "Idő"]; th [text "Teljesítmény (W)"]; th [text "Feszültség (V)"] ] ]
                tbody [
                    historyData.View.DocSeqCached(fun (row: Db.Tables.shelly_3em_live) ->
                        tr [
                            td [text (row.created_at.ToString("HH:mm:ss"))]
                            td [text (sprintf "%.1f W" (row.total_act_power |> Option.defaultValue 0.0))]
                            td [text (sprintf "%.1f V" (row.a_voltage |> Option.defaultValue 0.0))]
                        ]
                    )
                ]
            ]
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
                    | Live -> div [] [ h2 [] [text "Pillanatnyi adatok"]; p [] [text "Itt látszódnának az élő kártyák..."] ]
                    | History -> historyView
                )
            ]
        ]

    [<SPAEntryPoint>]
    let Main () =
        pageView |> Doc.RunById "main"