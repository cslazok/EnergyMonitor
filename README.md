# Live 3-Phase Energy Dashboard (F# WebSharper Project)

Course: Introduction to Functional Programming in F#
University of Dunaújváros
Instructor: Adam Granicz


## Project description

This project is a web-based live energy monitoring dashboard written in F# using WebSharper.

The application displays real-time electrical measurements from a three-phase energy meter. Data is provided by Node-RED through a JSON HTTP endpoint and visualized in a browser-based dashboard interface.

The goal of the project was to create a practical smart-home monitoring interface using functional programming techniques in F#.


## Motivation

I created this dashboard to monitor the real-time electrical energy consumption of my home.

Although Home Assistant already provides visualization tools, I wanted to build a lightweight custom monitoring interface using functional programming in F#. The application reads processed energy data from Node-RED and displays it in a simplified dashboard optimized for fast overview and clarity.

This solution demonstrates how F# and WebSharper can be used to build real-world smart-home monitoring applications.


## Features

The dashboard provides the following functionality:

- Live total active power measurement
- Total current measurement
- Total apparent power measurement
- Per-phase power monitoring (L1, L2, L3)
- Per-phase voltage monitoring
- Per-phase current monitoring
- Per-phase power factor monitoring
- Automatic refresh every 5 seconds
- Demo mode support for GitHub Pages deployment


## Technologies used

The project was implemented using the following technologies:

- F#
- WebSharper
- ASP.NET Core
- Node-RED
- JSON HTTP API
- Git
- GitHub
- GitHub Pages


## How to run the project locally

Clone the repository:

git clone https://github.com/cslazok/ha-dashboard.git

Navigate into the project folder:

cd ha-dashboard

Build the application:

dotnet build

Run the application:

dotnet run

Open the application in your browser:

http://localhost:5000

Make sure the Node-RED endpoint is available:

http://192.168.0.63:1880/energy/live

If necessary, the endpoint URL can be modified inside:

Client.fs


## Live demo link

The project is available online via GitHub Pages:

https://cslazok.github.io/ha-dashboard/

Since GitHub Pages supports only static websites, the live demo version runs in demo mode using sample measurement data stored in a JSON file.


## Screenshot

![Dashboard screenshot](docs/screenshot.png)


## Project structure overview

Main important files:

Client.fs

Contains dashboard UI logic and live data refresh.

Startup.fs

Configures ASP.NET Core application and API endpoint routing.

wwwroot/demo-energy.json

Provides demo data for GitHub Pages deployment.


## Future improvements

Possible future extensions of the project include:

- historical energy charts
- mobile layout optimization
- alert system for abnormal consumption
- database integration for long-term storage
- Home Assistant integration support


## Author

Dániel Csaba Lázok
University of Dunaújváros
Course: Introduction to Functional Programming in F#
