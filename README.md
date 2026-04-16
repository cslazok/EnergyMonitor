# Live 3-Phase Energy Dashboard (F# WebSharper Project)

Course: Introduction to Functional Programming in F#  
University of Dunaújváros  
Instructor: Adam Granicz


## Project description

This project is a web-based live energy monitoring dashboard written in F# using WebSharper.

The application displays real-time electrical measurements from a three-phase energy meter. Data is provided by Node-RED through a JSON HTTP endpoint and visualized in a browser-based dashboard.

The goal of the project was to create a practical smart-home monitoring interface using functional programming techniques in F#.


## Motivation

The dashboard was created to monitor the real-time electrical consumption of my home.

Instead of using Home Assistant directly, the application reads processed measurement data from Node-RED and presents it in a simplified custom interface.

This solution is lightweight, flexible and easy to extend.


## Features

- Live total power consumption display
- Per-phase power monitoring (L1, L2, L3)
- Voltage monitoring
- Current monitoring
- Power factor monitoring
- Automatic refresh every 5 seconds
- Demo mode support for GitHub Pages deployment


## Technologies used

- F#
- WebSharper
- ASP.NET Core
- Node-RED
- JSON HTTP API
- Git / GitHub


## How to run the project locally

Clone repository:
