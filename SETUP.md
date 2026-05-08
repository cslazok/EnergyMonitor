# Setup Guide

Step-by-step instructions for deploying EnergyMonitor from scratch.

## Prerequisites

- .NET 10 SDK
- PostgreSQL server
- MQTT broker (e.g. Mosquitto)
- Shelly Pro 3EM smart meter on the local network
- Huawei SUN2000 inverter with Modbus TCP enabled (optional)

---

## 1. PostgreSQL — Create database and user

```sql
CREATE USER solar WITH PASSWORD 'your_password';
CREATE DATABASE solar_data OWNER solar;
```

---

## 2. PostgreSQL — Create tables

Connect to the database (`\c solar_data`) and run:

```sql
-- Live 3-phase readings from Shelly 3EM
CREATE TABLE shelly_3em_live (
    id               BIGSERIAL PRIMARY KEY,
    ts               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    device_id        INTEGER     NOT NULL DEFAULT 1,
    a_voltage        DOUBLE PRECISION,
    a_current        DOUBLE PRECISION,
    a_act_power      DOUBLE PRECISION,
    a_aprt_power     DOUBLE PRECISION,
    a_pf             DOUBLE PRECISION,
    a_freq           DOUBLE PRECISION,
    b_voltage        DOUBLE PRECISION,
    b_current        DOUBLE PRECISION,
    b_act_power      DOUBLE PRECISION,
    b_aprt_power     DOUBLE PRECISION,
    b_pf             DOUBLE PRECISION,
    b_freq           DOUBLE PRECISION,
    c_voltage        DOUBLE PRECISION,
    c_current        DOUBLE PRECISION,
    c_act_power      DOUBLE PRECISION,
    c_aprt_power     DOUBLE PRECISION,
    c_pf             DOUBLE PRECISION,
    c_freq           DOUBLE PRECISION,
    n_current        DOUBLE PRECISION,
    total_current    DOUBLE PRECISION,
    total_act_power  DOUBLE PRECISION,
    total_aprt_power DOUBLE PRECISION,
    import_power     DOUBLE PRECISION,
    export_power     DOUBLE PRECISION
);
CREATE INDEX shelly_3em_live_ts_idx ON shelly_3em_live (ts DESC);

-- Cumulative energy counters from Shelly 3EM
CREATE TABLE shelly_3em_energy (
    id               BIGSERIAL PRIMARY KEY,
    ts               TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    device_id        INTEGER,
    total_act        DOUBLE PRECISION,
    total_act_ret    DOUBLE PRECISION,
    import_total_kwh DOUBLE PRECISION,
    export_total_kwh DOUBLE PRECISION,
    net_total_kwh    DOUBLE PRECISION
);
CREATE INDEX shelly_3em_energy_ts_idx ON shelly_3em_energy (ts DESC);

-- Inverter data polled over Modbus TCP (every 10 seconds)
CREATE TABLE inverter_live (
    id                   BIGSERIAL PRIMARY KEY,
    ts                   TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    connected            BOOLEAN     NOT NULL,
    active_power         DOUBLE PRECISION,
    pv_total_power       DOUBLE PRECISION,
    pv1_voltage          DOUBLE PRECISION,
    pv1_current          DOUBLE PRECISION,
    pv2_voltage          DOUBLE PRECISION,
    pv2_current          DOUBLE PRECISION,
    daily_yield          DOUBLE PRECISION,
    total_yield          DOUBLE PRECISION,
    battery_soc          DOUBLE PRECISION,
    battery_power        DOUBLE PRECISION,
    temperature          DOUBLE PRECISION,
    grid_frequency       DOUBLE PRECISION,
    power_factor         DOUBLE PRECISION,
    status               DOUBLE PRECISION,
    l1_voltage           DOUBLE PRECISION,
    l1_current           DOUBLE PRECISION,
    l2_voltage           DOUBLE PRECISION,
    l2_current           DOUBLE PRECISION,
    l3_voltage           DOUBLE PRECISION,
    l3_current           DOUBLE PRECISION,
    inverter_consumption DOUBLE PRECISION,
    pv1_power            DOUBLE PRECISION,
    pv2_power            DOUBLE PRECISION
);
CREATE INDEX inverter_live_ts_idx ON inverter_live (ts DESC);

-- Meter calibration: offset between Shelly counters and physical meter display
CREATE TABLE meter_calibration (
    id               SERIAL PRIMARY KEY,
    set_at           TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    import_offset    DOUBLE PRECISION NOT NULL,
    export_offset    DOUBLE PRECISION NOT NULL,
    baseline_import  DOUBLE PRECISION,   -- settlement year starting import reading
    baseline_export  DOUBLE PRECISION    -- settlement year starting export reading
);

-- Electricity prices for cost and ROI calculations
CREATE TABLE electricity_prices (
    id               SERIAL PRIMARY KEY,
    valid_from       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    import_low_huf   DOUBLE PRECISION NOT NULL,  -- price below annual limit
    import_high_huf  DOUBLE PRECISION NOT NULL,  -- price above annual limit
    export_huf       DOUBLE PRECISION NOT NULL,  -- feed-in price
    annual_limit_kwh DOUBLE PRECISION NOT NULL   -- tier boundary (kWh/year)
);

-- ROI calculator settings
CREATE TABLE roi_settings (
    id             SERIAL PRIMARY KEY,
    set_at         TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    investment_huf DOUBLE PRECISION NOT NULL,
    szaldo_start   DATE   -- start date of the settlement year
);
```

---

## 3. Application — Configuration files

**`.env`** (not committed to git, place next to the binary or in the project root):
```
DB_CONNECTION=Host=192.168.x.x;Port=5432;Database=solar_data;Username=solar;Password=your_password
```

**`appsettings.json`**:
```json
{
  "Inverter": {
    "Ip": "192.168.x.x",
    "Port": 502,
    "UnitId": 1,
    "PollIntervalSeconds": 10
  },
  "Mqtt": {
    "Broker": "192.168.x.x",
    "Port": 1883,
    "InverterTopic": "energymonitor/inverter",
    "ShellyLiveTopic": "shellypro3em/status/em:0",
    "ShellyEnergyTopic": "shellypro3em/status/emdata:0"
  }
}
```

---

## 4. MQTT — Shelly topics

The Shelly Pro 3EM must have MQTT enabled in its settings. It publishes to:

| Topic | Content |
|-------|---------|
| `shellypro3em/status/em:0` | Live per-phase measurements → `shelly_3em_live` |
| `shellypro3em/status/emdata:0` | Cumulative energy counters → `shelly_3em_energy` |

> Energy values in the `emdata:0` payload arrive in **Wh** — the app divides by 1000 before storing.

---

## 5. Build and run

```bash
dotnet publish -c Release -o ./publish
./publish/EnergyMonitor
```

Or for development:
```bash
dotnet run
```

> Without a `.env` file or a reachable database the app starts in **demo mode** with simulated data.

---

## 6. Initial calibration (optional)

After the first run, open `/energy` in the browser:

1. Read the current import and export values from the physical electricity meter.
2. Enter them in the **"Villanyóra leolvasás"** form. The app calculates the offset between the Shelly counters and the actual meter display.
3. If you want to track a settlement year (e.g. from the date the solar panels were installed), enter the meter readings from that date as the baseline values too.

---

## 7. Electricity prices (optional)

Open `/energy` and fill in the **"Áramdíj beállítás"** form with your actual tariff:

| Field | Description |
|-------|-------------|
| Import low (HUF/kWh) | Price below the annual limit |
| Import high (HUF/kWh) | Price above the annual limit |
| Feed-in (HUF/kWh) | Price you receive for exported energy |
| Annual limit (kWh) | Tier boundary |

Defaults: 36 / 70.1 / 5.11 HUF/kWh, 2523 kWh/year limit.

---

## 8. Systemd service (Linux)

Example unit file `/etc/systemd/system/energymonitor.service`:

```ini
[Unit]
Description=EnergyMonitor
After=network.target

[Service]
WorkingDirectory=/opt/energymonitor/publish
ExecStart=/usr/local/dotnet/dotnet /opt/energymonitor/publish/EnergyMonitor.dll
Restart=always
RestartSec=10
Environment=ASPNETCORE_URLS=http://0.0.0.0:5000

[Install]
WantedBy=multi-user.target
```

```bash
systemctl daemon-reload
systemctl enable energymonitor
systemctl start energymonitor
```
