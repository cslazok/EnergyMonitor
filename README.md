# EnergyMonitor

EnergyMonitor is a household energy consumption tracking application built with **F#**. The goal of the project is to visualize 3-phase consumption data and calculate whether installing an energy storage system (battery) is worthwhile after the phase-out of net metering.

## Features
- **Real-time 3-phase data**: Live readings from a Shelly 3EM meter (Phase A, B, C).
- **Energy dashboard**: Instant power, voltage, current, and power factor per phase.
- **Cumulative energy tracking**: Import / Export / Net kWh values.
- **Demo mode**: Runs with simulated data if no database is configured.
- **Analysis**: Battery ROI calculation (under development).

## Architecture
```
Shelly 3EM → Node-RED → PostgreSQL → F# Web App (Giraffe)
```

| Component | Role |
|-----------|------|
| **Shelly 3EM** | 3-phase smart energy meter |
| **Node-RED** | Forwards MQTT data to the database |
| **PostgreSQL** | Stores live and cumulative energy readings |
| **F# + Giraffe** | Web server, data visualization |

## Usage

1. Copy `.env.example` to `.env` and fill in your PostgreSQL connection string:
   ```
   DB_CONNECTION=Host=...;Port=5432;Database=...;Username=...;Password=...
   ```
2. Start the application:
   ```bash
   dotnet run
   ```
3. Open [http://localhost:5000](http://localhost:5000) in your browser.

> **No database?** The app automatically starts in **Demo mode** with simulated data.

## Pages
| URL | Description |
|-----|-------------|
| `/` | Live dashboard with 3-phase cards and energy totals |
| `/history` | Last 1 hour of readings in a table |
| `/api/history` | JSON API for live data |
| `/api/energy` | JSON API for cumulative energy data |

## Future Plans
- Replacing Node-RED with direct F#-based MQTT/Modbus data collection.
- Historical charts and trend analysis.
- Detailed battery sizing advisor.

## Demo
[▶ Live Demo (GitHub Pages)](https://cslazok.github.io/EnergyMonitor/)

## Screenshot
![EnergyMonitor dashboard](https://raw.githubusercontent.com/cslazok/EnergyMonitor/main/docs/Screenshot.png)

---
Built with **F#** and [Giraffe](https://github.com/giraffe-fsharp/Giraffe).
