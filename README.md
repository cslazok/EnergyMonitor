# EnergyMonitor

A household energy monitoring web application written in **F#**. Displays real-time data from a Shelly 3EM three-phase smart meter and a Huawei SUN2000 solar inverter system. The goal is to determine how long it takes for a battery storage expansion to pay for itself under the current net-metering tariff structure.

**[→ Live Demo](https://cslazok.github.io/EnergyMonitor/)** — runs with simulated data, no database required

## Motivation

From 2026, traditional net metering is being phased out in Hungary. During the transition period the feed-in price (5.11 HUF/kWh) is a fraction of the purchase price (36–70 HUF/kWh), making self-consumption maximization and battery ROI calculation a real financial decision.

This application supports that decision: it derives annual savings and estimated payback period from live inverter and consumption data, calibrated meter readings, and user-defined electricity prices.

## Architecture

```
Shelly 3EM ──→ MQTT broker ──→ ShellyMqttService ──→ PostgreSQL ──┐
                                                                    ├──→ F# Web App (Giraffe) → browser
Huawei SUN2000 ──→ Modbus TCP ──→ ModbusReaderService ─────────────┘
                                        │
                                        └──→ MQTT broker (Home Assistant auto-discovery)
```

| Component | Role |
|-----------|------|
| **Shelly 3EM** | Three-phase smart energy meter |
| **MQTT broker** | Relays Shelly data and publishes inverter state |
| **PostgreSQL** | Stores live readings and cumulative energy values |
| **Huawei SUN2000** | Solar inverter with PV strings and LUNA2000 battery |
| **Modbus TCP** | Direct inverter communication via FluentModbus |
| **F# + Giraffe** | Web server, business logic, server-side HTML |

## Features

- **Live dashboard** — per-phase voltage, current, power, power factor (Shelly 3EM)
- **Inverter card** — PV1/PV2 power, daily and total yield, battery SOC and power, temperature
- **Settlement year view** — calibrated meter readings, purchased/exported kWh for the period, net balance in HUF (correct net-metering logic)
- **ROI calculator** — self-consumption value + settlement balance → annual savings → investment ÷ annual savings = payback years
- **Home Assistant integration** — inverter data over MQTT with automatic sensor discovery (20 sensors, no YAML required)
- **Demo mode** — starts without a database using simulated data

## How it got here

**Starting point:** Shelly 3EM readings displayed through a Node-RED → PostgreSQL → Giraffe pipeline. No inverter data.

**Inverter integration:** Initially a separate `InverterReader` service that polled the Huawei SUN2000 over Modbus TCP and exposed the data via a local HTTP API on port 5051. EnergyMonitor fetched from it on each request.

**Merged into one process:** `InverterReader` was folded into EnergyMonitor. `ModbusReaderService` now runs as a hosted background service inside the same ASP.NET Core process — no separate binary, no inter-process HTTP call. `InverterState` is a singleton accessed through DI.

**MQTT takeover:** Node-RED was replaced. Shelly data is received directly by `ShellyMqttService` (MQTTnet) and inverter data is published by `ModbusReaderService` after every poll, including Home Assistant auto-discovery messages at startup.

**Register mapping:** The SUN2000 Modbus registers did not match the publicly available documentation and required manual scanning:
- `32106–32107` — total AC yield (kWh), matches the inverter display; `32214–32215` is a per-string sub-meter only
- `32115` — daily yield (kWh); `32216–32217` is always 0 on this unit
- `32016–32019` — PV1 and PV2 voltage / current

## Setup

1. Create a `.env` file with your PostgreSQL connection string:
   ```
   DB_CONNECTION=Host=...;Port=5432;Database=...;Username=...;Password=...
   ```

2. Configure inverter and MQTT settings in `appsettings.json`:
   ```json
   "Inverter": {
     "Ip": "192.168.x.x",
     "Port": 502,
     "UnitId": 1,
     "PollIntervalSeconds": 10
   },
   "Mqtt": {
     "Broker": "192.168.x.x",
     "Port": 1883,
     "InverterTopic": "energymonitor/inverter"
   }
   ```

3. Run:
   ```bash
   dotnet run
   ```

> Without a database the app starts in **demo mode** with simulated data.

## Pages and API

| URL | Description |
|-----|-------------|
| `/` | Live dashboard — inverter card, 3-phase cards, cumulative energy |
| `/history` | Last hour of readings in a table |
| `/energy` | Settlement year view + ROI calculator |
| `/api/history` | JSON — Shelly live data |
| `/api/energy` | JSON — cumulative energy data |
| `/api/status` | JSON — health check (inverter + DB + uptime) |

## Demo

**https://cslazok.github.io/EnergyMonitor/**

[![Demo](https://raw.githubusercontent.com/cslazok/EnergyMonitor/main/docs/screenshot.png)](https://cslazok.github.io/EnergyMonitor/)

## Dependencies

| Library | Purpose |
|---------|---------|
| [Giraffe](https://github.com/giraffe-fsharp/Giraffe) | F# web framework on top of ASP.NET Core |
| [Giraffe.ViewEngine](https://github.com/giraffe-fsharp/Giraffe) | Server-side HTML rendering in F# |
| [FluentModbus](https://github.com/Apollo3zehn/FluentModbus) | Modbus TCP client for inverter communication |
| [MQTTnet](https://github.com/dotnet/MQTTnet) | MQTT client (Shelly subscriber + inverter publisher) |
| [Npgsql](https://github.com/npgsql/npgsql) | .NET PostgreSQL driver |
| [DotNetEnv](https://github.com/tonerdo/dotnet-env) | `.env` file loading |

---
Built with **F#** on .NET 10.
