# EnergyMonitor

A household energy monitoring application written in **F#**, tracking 3-phase consumption from a Shelly 3EM meter and real-time solar production from a Huawei SUN2000 inverter. The goal is to analyze whether adding battery storage makes financial sense after the phase-out of net metering.

**[→ Live Demo](https://cslazok.github.io/EnergyMonitor/)** — simulated data, no database required

## Architecture

```
Shelly 3EM ──→ Node-RED ──→ PostgreSQL ──┐
                                          ├──→ F# Web App (Giraffe) → browser
Huawei SUN2000 ──→ Modbus TCP ───────────┘
```

| Component | Role |
|-----------|------|
| **Shelly 3EM** | 3-phase smart energy meter |
| **Node-RED** | Forwards MQTT data to the database |
| **PostgreSQL** | Stores live and cumulative energy readings |
| **Huawei SUN2000** | Solar inverter with PV strings and battery |
| **Modbus TCP** | Direct inverter communication via FluentModbus |
| **F# + Giraffe** | Web server and data visualization |

## Features

- **Live 3-phase readings** — voltage, current, power, power factor per phase (Shelly 3EM)
- **Inverter dashboard** — PV output, daily and total yield, battery state of charge and power, temperature
- **Cumulative energy** — Import / Export / Net kWh
- **Demo mode** — runs with simulated data when no database is configured

## How it got here

**Starting point:** The app displayed Shelly 3EM consumption data through a Node-RED → PostgreSQL → Giraffe pipeline. No inverter data was available.

**Inverter integration:** Initially built as a separate `InverterReader` service that polled the Huawei SUN2000 over Modbus TCP (FluentModbus) and exposed the data through a local HTTP API on port 5051. EnergyMonitor fetched from it on each request.

**Merged into one process:** `InverterReader` was folded into EnergyMonitor. `ModbusReaderService` now runs as a hosted background service inside the same ASP.NET Core process — no separate binary, no inter-process HTTP call. `InverterState` is a singleton accessed through DI.

**Register mapping:** The Huawei SUN2000 Modbus registers didn't match publicly available documentation and required manual scanning:
- `32106–32107` — total AC yield (kWh), matches the inverter display; `32214–32215` is a per-string sub-meter
- `32115` — daily yield (kWh); `32216–32217` is always 0 on this unit
- `32016–32019` — PV1 and PV2 voltage / current

## Setup

1. Create a `.env` file with your PostgreSQL connection string:
   ```
   DB_CONNECTION=Host=...;Port=5432;Database=...;Username=...;Password=...
   ```

2. Set the inverter address in `appsettings.json`:
   ```json
   "Inverter": {
     "Ip": "192.168.x.x",
     "Port": 502,
     "UnitId": 1,
     "PollIntervalSeconds": 10
   }
   ```

3. Run:
   ```bash
   dotnet run
   ```

> Without a database the app starts automatically in **demo mode** with simulated data.

## Pages

| URL | Description |
|-----|-------------|
| `/` | Live dashboard — inverter card, 3-phase cards, cumulative energy |
| `/history` | Last hour of readings in a table |
| `/api/history` | JSON — Shelly live data |
| `/api/energy` | JSON — cumulative energy data |

## Demo

A live demo with simulated data is available at **https://cslazok.github.io/EnergyMonitor/**

[![Demo](https://raw.githubusercontent.com/cslazok/EnergyMonitor/main/docs/screenshot.png)](https://cslazok.github.io/EnergyMonitor/)

## Dependencies

| Library | Purpose |
|---------|---------|
| [Giraffe](https://github.com/giraffe-fsharp/Giraffe) | F# web framework on top of ASP.NET Core |
| [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe) | Server-side HTML rendering in F# |
| [FluentModbus](https://github.com/Apollo3zehn/FluentModbus) | Modbus TCP client for inverter communication |
| [SqlHydra.Query](https://github.com/JordanMarr/SqlHydra) | Type-safe SQL query builder |
| [Npgsql](https://github.com/npgsql/npgsql) | .NET PostgreSQL driver |
| [DotNetEnv](https://github.com/tonerdo/dotnet-env) | `.env` file loading |

---
Built with **F#** on .NET 10.
