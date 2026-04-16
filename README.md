!# Live 3-Phase Energy Dashboard (F# WebSharper Project)

Course: Introduction to Functional Programming in F#  
University of Dunaújváros  
Instructor: Adam Granicz


## Project description

This project is a web-based live energy monitoring dashboard written in F# using WebSharper.

The application displays real-time electrical measurements from a three-phase energy meter. Data is provided by Node-RED through a JSON HTTP endpoint and visualized in a browser-based dashboard.

The goal of the project was to create a practical smart-home monitoring interface using functional programming techniques in F#.

## Motivation

I built this dashboard to monitor the real-time energy consumption of my home electrical system.

Instead of using Home Assistant directly, the application reads processed data from Node-RED and presents it in a simplified custom interface.

This makes the solution lightweight, flexible and easy to extend.

## Features

- Live total power consumption display
- Per-phase power monitoring (L1, L2, L3)
- Voltage monitoring
- Current monitoring
- Power factor monitoring
- Automatic refresh every 5 seconds

## Technologies used

- F#
- WebSharper
- ASP.NET Core
- Node-RED
- JSON HTTP API
- Git / GitHub

## How to run the project

Clone repository:

```
git clone https://github.com/cslazok/due-fsharp-project.git
```

Navigate to folder:

```
cd due-fsharp-project
```

Build project:

```
dotnet build
```

Run application:

```
dotnet run
```

Open browser:

```
http://localhost:5000
```

Make sure Node-RED endpoint is available:

```
http://192.168.0.63:1880/energy/live
```

(or change URL inside Client.fs if needed)

## Screenshot

![Dashboard screenshot](screenshot.png)

## Live demo link
...
https://cslazok.github.io/ha-dashboard/
...
Local demo example:

```
http://localhost:5000
```

## Future improvements

Planned extensions:

- historical energy charts
- mobile layout optimization
- alert system for high consumption
- export measurements to database

## Author

Dániel Csaba Lázok  
University of Dunaújváros  
Introduction to Functional Programming in F#[Dashboard screenshot](https://raw.githubusercontent.com/cslazok/ha-dashboard/main/docs/screenshot.png)
