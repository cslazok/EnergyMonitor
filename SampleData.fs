namespace ha_dashboard

open WebSharper

[<JavaScript>]
module SampleData =

    let sensors = [
        { Name = "Temperature"; Room = "Living room"; Value = 22.5; Unit = "°C" }
        { Name = "Humidity"; Room = "Living room"; Value = 48.0; Unit = "%" }
        { Name = "Temperature"; Room = "Bedroom"; Value = 21.0; Unit = "°C" }
        { Name = "Humidity"; Room = "Bedroom"; Value = 45.0; Unit = "%" }
        { Name = "Temperature"; Room = "Kitchen"; Value = 23.1; Unit = "°C" }
        { Name = "WiFi Signal"; Room = "Printer corner"; Value = -58.0; Unit = "dBm" }
    ]

    let devices = [
        { Name = "Living room lamp"; Room = "Living room"; State = On }
        { Name = "Bedroom lamp"; Room = "Bedroom"; State = Off }
        { Name = "Kitchen blinds"; Room = "Kitchen"; State = Partial 60 }
        { Name = "Terrace door blinds"; Room = "Living room"; State = Closed }
        { Name = "Ventilator"; Room = "Bedroom"; State = On }
    ]

    let energyDays = [
        { Day = "Monday"; Consumption = 12.4; Production = 8.1 }
        { Day = "Tuesday"; Consumption = 11.8; Production = 7.9 }
        { Day = "Wednesday"; Consumption = 13.2; Production = 9.4 }
        { Day = "Thursday"; Consumption = 10.7; Production = 6.8 }
        { Day = "Friday"; Consumption = 12.9; Production = 8.7 }
        { Day = "Saturday"; Consumption = 14.1; Production = 10.2 }
        { Day = "Sunday"; Consumption = 11.5; Production = 7.4 }
    ]