namespace ha_dashboard

open WebSharper
open WebSharper.UI
open WebSharper.UI.Html
open WebSharper.UI.Client

[<JavaScript>]
module Client =

    let formatDeviceState (state: DeviceState) =
        match state with
        | On -> "On"
        | Off -> "Off"
        | Open -> "Open"
        | Closed -> "Closed"
        | Partial value -> "Partial (" + string value + "%)"

    let overviewCard title value =
        div [attr.``class`` "card"] [
            h3 [] [text title]
            p [] [text value]
        ]

    let sensorCard (sensor: Sensor) =
        div [attr.``class`` "card"] [
            h3 [] [text sensor.Room]
            p [] [text (sensor.Name + ": " + string sensor.Value + " " + sensor.Unit)]
        ]

    let deviceCard (device: Device) =
        div [attr.``class`` "card"] [
            h3 [] [text device.Name]
            p [] [text ("Room: " + device.Room)]
            p [] [text ("State: " + formatDeviceState device.State)]
        ]

    let energyCard (energy: EnergyDay) =
        div [attr.``class`` "card"] [
            h3 [] [text energy.Day]
            p [] [text ("Consumption: " + string energy.Consumption + " kWh")]
            p [] [text ("Production: " + string energy.Production + " kWh")]
        ]

    let dashboardView =
        div [attr.``class`` "section"] [
            h2 [] [text "Quick Overview"]
            div [attr.``class`` "grid"] [
                overviewCard "Living room temperature" "22.5 °C"
                overviewCard "Humidity" "48 %"
                overviewCard "Power consumption" "1.2 kW"
                overviewCard "Blinds status" "Partially open"
            ]
        ]

    let sensorsCards =
        SampleData.sensors
        |> List.map sensorCard

    let devicesCards =
        SampleData.devices
        |> List.map deviceCard

    let energyCards =
        SampleData.energyDays
        |> List.map energyCard

    let pageView =
        div [] [
            div [attr.``class`` "topbar"] [
                h1 [] [text "Home Assistant Dashboard Demo"]
            ]
            div [attr.``class`` "page"] [
                p [] [text "This is a demo smart home dashboard built with F# and WebSharper."]
                dashboardView
                div [attr.``class`` "section"] [
                    h2 [] [text "Sensors"]
                    div [attr.``class`` "grid"] sensorsCards
                ]
                div [attr.``class`` "section"] [
                    h2 [] [text "Devices"]
                    div [attr.``class`` "grid"] devicesCards
                ]
                div [attr.``class`` "section"] [
                    h2 [] [text "Energy"]
                    div [attr.``class`` "grid"] energyCards
                ]
            ]
        ]

    [<SPAEntryPoint>]
    let Main () =
        pageView
        |> Doc.RunById "main"