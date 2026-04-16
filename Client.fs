namespace ha_dashboard

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client
open WebSharper.JavaScript

[<JavaScript>]
module Client =

    type LiveEnergy = {
        a_current: float
        a_voltage: float
        a_act_power: float
        a_aprt_power: float
        a_pf: float
        a_freq: float
        b_current: float
        b_voltage: float
        b_act_power: float
        b_aprt_power: float
        b_pf: float
        b_freq: float
        c_current: float
        c_voltage: float
        c_act_power: float
        c_aprt_power: float
        c_pf: float
        c_freq: float
        total_current: float
        total_act_power: float
        total_aprt_power: float
    }

    let totalPower = Var.Create "loading..."
    let totalCurrent = Var.Create "-"
    let totalApparentPower = Var.Create "-"

    let phaseAPower = Var.Create "-"
    let phaseBPower = Var.Create "-"
    let phaseCPower = Var.Create "-"

    let phaseAVoltage = Var.Create "-"
    let phaseBVoltage = Var.Create "-"
    let phaseCVoltage = Var.Create "-"

    let phaseACurrent = Var.Create "-"
    let phaseBCurrent = Var.Create "-"
    let phaseCCurrent = Var.Create "-"

    let phaseAPf = Var.Create "-"
    let phaseBPf = Var.Create "-"
    let phaseCPf = Var.Create "-"

    let nodeRedUrl =
     if JS.Window.Location.Host.Contains("github.io") then
      "demo-energy.json"
     else
        "http://192.168.0.63:1880/energy/live"
    let getLiveEnergy () =
        promise {
            let! response = JS.Fetch nodeRedUrl
            let! json = response?text() : Promise<string>
            return JSON.Parse json |> As<LiveEnergy>
        }

    let loadEnergyData () =
        promise {
            try
                let! data = getLiveEnergy()

                totalPower.Value <- string data.total_act_power + " W"
                totalCurrent.Value <- string data.total_current + " A"
                totalApparentPower.Value <- string data.total_aprt_power + " VA"

                phaseAPower.Value <- string data.a_act_power + " W"
                phaseBPower.Value <- string data.b_act_power + " W"
                phaseCPower.Value <- string data.c_act_power + " W"

                phaseAVoltage.Value <- string data.a_voltage + " V"
                phaseBVoltage.Value <- string data.b_voltage + " V"
                phaseCVoltage.Value <- string data.c_voltage + " V"

                phaseACurrent.Value <- string data.a_current + " A"
                phaseBCurrent.Value <- string data.b_current + " A"
                phaseCCurrent.Value <- string data.c_current + " A"

                phaseAPf.Value <- string data.a_pf
                phaseBPf.Value <- string data.b_pf
                phaseCPf.Value <- string data.c_pf
            with _ ->
                totalPower.Value <- "Node-RED error"
                totalCurrent.Value <- "-"
                totalApparentPower.Value <- "-"

                phaseAPower.Value <- "-"
                phaseBPower.Value <- "-"
                phaseCPower.Value <- "-"

                phaseAVoltage.Value <- "-"
                phaseBVoltage.Value <- "-"
                phaseCVoltage.Value <- "-"

                phaseACurrent.Value <- "-"
                phaseBCurrent.Value <- "-"
                phaseCCurrent.Value <- "-"

                phaseAPf.Value <- "-"
                phaseBPf.Value <- "-"
                phaseCPf.Value <- "-"
        }
        |> ignore

    let overviewCard title valueDoc =
        div [attr.``class`` "card"] [
            h3 [] [text title]
            p [] [valueDoc]
        ]

    let summaryView =
        div [attr.``class`` "section"] [
            h2 [] [text "Energy Overview"]
            div [attr.``class`` "grid"] [
                overviewCard "Total active power" (Doc.TextView totalPower.View)
                overviewCard "Total current" (Doc.TextView totalCurrent.View)
                overviewCard "Total apparent power" (Doc.TextView totalApparentPower.View)
            ]
        ]

    let phasePowerView =
        div [attr.``class`` "section"] [
            h2 [] [text "Phase Power"]
            div [attr.``class`` "grid"] [
                overviewCard "L1 power" (Doc.TextView phaseAPower.View)
                overviewCard "L2 power" (Doc.TextView phaseBPower.View)
                overviewCard "L3 power" (Doc.TextView phaseCPower.View)
            ]
        ]

    let phaseVoltageView =
        div [attr.``class`` "section"] [
            h2 [] [text "Phase Voltage"]
            div [attr.``class`` "grid"] [
                overviewCard "L1 voltage" (Doc.TextView phaseAVoltage.View)
                overviewCard "L2 voltage" (Doc.TextView phaseBVoltage.View)
                overviewCard "L3 voltage" (Doc.TextView phaseCVoltage.View)
            ]
        ]

    let phaseCurrentView =
        div [attr.``class`` "section"] [
            h2 [] [text "Phase Current"]
            div [attr.``class`` "grid"] [
                overviewCard "L1 current" (Doc.TextView phaseACurrent.View)
                overviewCard "L2 current" (Doc.TextView phaseBCurrent.View)
                overviewCard "L3 current" (Doc.TextView phaseCCurrent.View)
            ]
        ]

    let phasePfView =
        div [attr.``class`` "section"] [
            h2 [] [text "Power Factor"]
            div [attr.``class`` "grid"] [
                overviewCard "L1 PF" (Doc.TextView phaseAPf.View)
                overviewCard "L2 PF" (Doc.TextView phaseBPf.View)
                overviewCard "L3 PF" (Doc.TextView phaseCPf.View)
            ]
        ]

    let pageView =
        div [] [
            div [attr.``class`` "topbar"] [
                h1 [] [text "Energy Dashboard"]
            ]
            div [attr.``class`` "page"] [
                p [] [text "Live 3-phase energy data from Node-RED."]
                summaryView
                phasePowerView
                phaseVoltageView
                phaseCurrentView
                phasePfView
            ]
        ]

    [<SPAEntryPoint>]
    let Main () =
        loadEnergyData()

        ignore (
            JS.Window.SetInterval(
                (fun () -> loadEnergyData() |> ignore),
                5000
            )
        )

        pageView
        |> Doc.RunById "main"