namespace EnergyMonitor

open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Concurrent
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open FluentModbus

type InverterState() =
    let data = ConcurrentDictionary<string, obj>()
    do data.["statusText"] <- "Initializing..." :> obj

    member _.UpdateData (key: string) (value: obj) = data.[key] <- value
    member _.SetConnected (connected: bool) =
        data.["connected"]    <- connected :> obj
        data.["lastUpdated"]  <- DateTime.UtcNow.ToString("O") :> obj
        data.["statusText"]   <- (if connected then "Online" else "Offline") :> obj
    member _.IsInitialized () = data.ContainsKey("connected")
    member _.GetData () =
        data |> Seq.map (fun kvp -> kvp.Key, kvp.Value) |> dict

module ModbusHelpers =
    let toInt32  (regs: uint16[]) i = (int32  regs.[i] <<< 16) ||| int32  regs.[i + 1]
    let toUInt32 (regs: uint16[]) i = (uint32 regs.[i] <<< 16) ||| uint32 regs.[i + 1]

type ModbusReaderService(logger: ILogger<ModbusReaderService>, config: IConfiguration, state: InverterState, mqtt: MqttPublisher) =
    inherit BackgroundService()

    let publishDiscovery (mqtt: MqttPublisher) (stateTopic: string) =
        let device = {| identifiers = [| "energymonitor_inverter" |]; name = "Inverter"; model = "Huawei SUN2000"; manufacturer = "Huawei" |}
        let sensor (uid: string) (name: string) (valueTemplate: string) (unit: string) (deviceClass: string) (stateClass: string) =
            let payload = System.Text.Json.JsonSerializer.Serialize({|
                name             = name
                unique_id        = uid
                state_topic      = stateTopic
                value_template   = valueTemplate
                unit_of_measurement = unit
                device_class     = (if deviceClass = "" then null else deviceClass)
                state_class      = (if stateClass   = "" then null else stateClass)
                device           = device |})
            mqtt.Publish (sprintf "homeassistant/sensor/%s/config" uid) payload
        task {
            do! sensor "inv_active_power"         "Inverter Active Power"         "{{ value_json.activePower }}"         "W"   "power"       "measurement"
            do! sensor "inv_pv_total"             "Inverter PV Power"             "{{ value_json.pvTotalPower }}"        "W"   "power"       "measurement"
            do! sensor "inv_pv1_voltage"          "Inverter PV1 Voltage"          "{{ value_json.pv1Voltage }}"          "V"   "voltage"     "measurement"
            do! sensor "inv_pv1_current"          "Inverter PV1 Current"          "{{ value_json.pv1Current }}"          "A"   "current"     "measurement"
            do! sensor "inv_pv1_power"            "Inverter PV1 Power"            "{{ value_json.pv1Power }}"            "W"   "power"       "measurement"
            do! sensor "inv_pv2_voltage"          "Inverter PV2 Voltage"          "{{ value_json.pv2Voltage }}"          "V"   "voltage"     "measurement"
            do! sensor "inv_pv2_current"          "Inverter PV2 Current"          "{{ value_json.pv2Current }}"          "A"   "current"     "measurement"
            do! sensor "inv_pv2_power"            "Inverter PV2 Power"            "{{ value_json.pv2Power }}"            "W"   "power"       "measurement"
            do! sensor "inv_l1_voltage"           "Inverter L1 Voltage"           "{{ value_json.l1Voltage }}"           "V"   "voltage"     "measurement"
            do! sensor "inv_l1_current"           "Inverter L1 Current"           "{{ value_json.l1Current }}"           "A"   "current"     "measurement"
            do! sensor "inv_l2_voltage"           "Inverter L2 Voltage"           "{{ value_json.l2Voltage }}"           "V"   "voltage"     "measurement"
            do! sensor "inv_l2_current"           "Inverter L2 Current"           "{{ value_json.l2Current }}"           "A"   "current"     "measurement"
            do! sensor "inv_l3_voltage"           "Inverter L3 Voltage"           "{{ value_json.l3Voltage }}"           "V"   "voltage"     "measurement"
            do! sensor "inv_l3_current"           "Inverter L3 Current"           "{{ value_json.l3Current }}"           "A"   "current"     "measurement"
            do! sensor "inv_daily_yield"          "Inverter Daily Yield"          "{{ value_json.dailyYield }}"          "kWh" "energy"      "total_increasing"
            do! sensor "inv_total_yield"          "Inverter Total Yield"          "{{ value_json.totalYield }}"          "kWh" "energy"      "total_increasing"
            do! sensor "inv_battery_soc"          "Inverter Battery SOC"          "{{ value_json.batterySOC }}"          "%"   "battery"     "measurement"
            do! sensor "inv_battery_power"        "Inverter Battery Power"        "{{ value_json.batteryPower }}"        "W"   "power"       "measurement"
            do! sensor "inv_temperature"          "Inverter Temperature"          "{{ value_json.temperature }}"         "°C"  "temperature" "measurement"
            do! sensor "inv_grid_frequency"       "Inverter Grid Frequency"       "{{ value_json.gridFrequency }}"       "Hz"  "frequency"   "measurement"
            do! sensor "inv_power_factor"         "Inverter Power Factor"         "{{ value_json.powerFactor }}"         ""    ""            "measurement"
            do! sensor "inv_consumption"          "Inverter Consumption"          "{{ value_json.inverterConsumption }}" "W"   "power"       "measurement"
            do! sensor "inv_house_consumption"    "House Consumption"             "{{ value_json.houseConsumption }}"    "W"   "power"       "measurement"
            do! sensor "inv_house_consumption_a" "House Consumption Phase A"     "{{ value_json.houseConsumptionA }}"   "W"   "power"       "measurement"
            do! sensor "inv_house_consumption_b" "House Consumption Phase B"     "{{ value_json.houseConsumptionB }}"   "W"   "power"       "measurement"
            do! sensor "inv_house_consumption_c"   "House Consumption Phase C"     "{{ value_json.houseConsumptionC }}"          "W"   "power"       "measurement"
            do! sensor "inv_daily_house_kwh"       "Daily House Consumption"        "{{ value_json.dailyHouseConsumptionKwh }}"   "kWh" "energy"      "total_increasing"
            logger.LogInformation("MQTT discovery published.")
        }

    override this.ExecuteAsync(stoppingToken: CancellationToken) =
        let tsk = task {
            let ip   = config.["Inverter:Ip"]
            let port = int config.["Inverter:Port"]
            let unitId = int config.["Inverter:UnitId"]
            let pollMs = int config.["Inverter:PollIntervalSeconds"] * 1000
            let endpoint = sprintf "%s:%d" ip port
            let stateTopic = config.["Mqtt:InverterTopic"]
            logger.LogInformation("Inverter reader starting. IP: {0}", ip)
            do! publishDiscovery mqtt stateTopic

            use client = new ModbusTcpClient()
            client.ReadTimeout <- 8000
            let delay () = Task.Delay(300, stoppingToken)

            while not stoppingToken.IsCancellationRequested do
                try
                    if not client.IsConnected then
                        logger.LogInformation("Connecting to {0}...", endpoint)
                        client.Connect(endpoint, ModbusEndianness.BigEndian)
                        logger.LogInformation("Connected.")
                        do! Task.Delay(500, stoppingToken)

                    let b1 = client.ReadHoldingRegisters<uint16>(unitId, 32016, 4).ToArray()
                    let pv1Voltage = float (int16 b1.[0]) * 0.1
                    let pv1Current = float (int16 b1.[1]) * 0.01
                    let pv2Voltage = float (int16 b1.[2]) * 0.1
                    let pv2Current = float (int16 b1.[3]) * 0.01
                    do! delay()

                    let b2a = client.ReadHoldingRegisters<uint16>(unitId, 32069, 9).ToArray()
                    let l1Voltage = float b2a.[0] * 0.1
                    let l2Voltage = float b2a.[1] * 0.1
                    let l3Voltage = float b2a.[2] * 0.1
                    let l1Current = float (ModbusHelpers.toInt32 b2a 3) * 0.001
                    let l2Current = float (ModbusHelpers.toInt32 b2a 5) * 0.001
                    let l3Current = float (ModbusHelpers.toInt32 b2a 7) * 0.001
                    do! delay()

                    let b2b = client.ReadHoldingRegisters<uint16>(unitId, 32080, 2).ToArray()
                    let activePower = float (ModbusHelpers.toInt32 b2b 0)
                    do! delay()

                    let b2c = client.ReadHoldingRegisters<uint16>(unitId, 32084, 6).ToArray()
                    let powerFactor = float (int16 b2c.[0]) * 0.001
                    let gridFreq    = float b2c.[1] * 0.01
                    let temperature = float (int16 b2c.[3]) * 0.1
                    let status      = float b2c.[5]
                    do! delay()

                    // 32106 = AC output total (matches inverter display); 32214 is per-string sub-meter only
                    let b4a = client.ReadHoldingRegisters<uint16>(unitId, 32106, 2).ToArray()
                    let totalYield = float (ModbusHelpers.toUInt32 b4a 0) * 0.01
                    do! delay()

                    // 32114-32115: index [1] = daily kWh; 32216-32217 is always 0 on this inverter
                    let b4b = client.ReadHoldingRegisters<uint16>(unitId, 32114, 2).ToArray()
                    let dailyYield = float b4b.[1] * 0.01
                    do! delay()

                    let b5 = client.ReadHoldingRegisters<uint16>(unitId, 37760, 7).ToArray()
                    let batterySOC   = float b5.[0] * 0.1
                    let batteryPower = float (ModbusHelpers.toInt32 b5 5)

                    let pv1Power            = pv1Voltage * pv1Current
                    let pv2Power            = pv2Voltage * pv2Current
                    let pvTotalPower        = pv1Power + pv2Power
                    let inverterConsumption = pvTotalPower - activePower - batteryPower
                    let! dailyDelta = Database.getDailyEnergyDelta()
                    let dailyHouseKwh =
                        match dailyDelta with
                        | Some (dailyImp, dailyExp) -> Some (dailyYield + dailyImp - dailyExp)
                        | None -> None
                    let! shelly = Database.getLatestShellyPower()
                    let houseConsumption =
                        match shelly.ImportPower, shelly.ExportPower with
                        | Some imp, Some exp -> Some (activePower + imp - exp)
                        | _ -> None
                    let phaseHouse (shellyPhase: float option) (invV: float) (invI: float) =
                        shellyPhase |> Option.map (fun p -> p + invV * invI)
                    let houseA = phaseHouse shelly.AActPower l1Voltage l1Current
                    let houseB = phaseHouse shelly.BActPower l2Voltage l2Current
                    let houseC = phaseHouse shelly.CActPower l3Voltage l3Current

                    state.UpdateData "pv1Voltage"    (pv1Voltage   :> obj)
                    state.UpdateData "pv1Current"    (pv1Current   :> obj)
                    state.UpdateData "pv1Power"      (pv1Power     :> obj)
                    state.UpdateData "pv2Voltage"    (pv2Voltage   :> obj)
                    state.UpdateData "pv2Current"    (pv2Current   :> obj)
                    state.UpdateData "pv2Power"      (pv2Power     :> obj)
                    state.UpdateData "pvTotalPower"  (pvTotalPower :> obj)
                    state.UpdateData "l1Voltage"     (l1Voltage    :> obj)
                    state.UpdateData "l2Voltage"     (l2Voltage    :> obj)
                    state.UpdateData "l3Voltage"     (l3Voltage    :> obj)
                    state.UpdateData "l1Current"     (l1Current    :> obj)
                    state.UpdateData "l2Current"     (l2Current    :> obj)
                    state.UpdateData "l3Current"     (l3Current    :> obj)
                    state.UpdateData "activePower"   (activePower  :> obj)
                    state.UpdateData "powerFactor"   (powerFactor  :> obj)
                    state.UpdateData "gridFrequency" (gridFreq     :> obj)
                    state.UpdateData "temperature"   (temperature  :> obj)
                    state.UpdateData "status"        (status       :> obj)
                    state.UpdateData "dailyYield"    (dailyYield   :> obj)
                    state.UpdateData "totalYield"    (totalYield   :> obj)
                    state.UpdateData "batterySOC"           (batterySOC          :> obj)
                    state.UpdateData "batteryPower"         (batteryPower        :> obj)
                    state.UpdateData "inverterConsumption"  (inverterConsumption :> obj)
                    houseConsumption |> Option.iter (fun v -> state.UpdateData "houseConsumption" (v :> obj))
                    houseA |> Option.iter (fun v -> state.UpdateData "houseConsumptionA" (v :> obj))
                    houseB |> Option.iter (fun v -> state.UpdateData "houseConsumptionB" (v :> obj))
                    houseC |> Option.iter (fun v -> state.UpdateData "houseConsumptionC" (v :> obj))
                    dailyHouseKwh |> Option.iter (fun v -> state.UpdateData "dailyHouseConsumption" (v :> obj))

                    state.SetConnected true
                    do! Database.insertInverterLive true (state.GetData()) houseConsumption houseA houseB houseC dailyHouseKwh
                    logger.LogInformation("Poll OK — Grid: {0}W  PV: {1}W  Daily: {2}kWh  SOC: {3}%%", activePower, pvTotalPower, dailyYield, batterySOC)
                    let topic = config.["Mqtt:InverterTopic"]
                    let payload = System.Text.Json.JsonSerializer.Serialize({|
                        connected           = true
                        activePower         = activePower
                        pvTotalPower        = pvTotalPower
                        pv1Voltage          = pv1Voltage
                        pv1Current          = pv1Current
                        pv1Power            = pv1Power
                        pv2Voltage          = pv2Voltage
                        pv2Current          = pv2Current
                        pv2Power            = pv2Power
                        l1Voltage           = l1Voltage
                        l1Current           = l1Current
                        l2Voltage           = l2Voltage
                        l2Current           = l2Current
                        l3Voltage           = l3Voltage
                        l3Current           = l3Current
                        dailyYield          = dailyYield
                        totalYield          = totalYield
                        batterySOC          = batterySOC
                        batteryPower        = batteryPower
                        temperature         = temperature
                        gridFrequency       = gridFreq
                        powerFactor         = powerFactor
                        inverterConsumption = inverterConsumption
                        houseConsumption    = houseConsumption |> Option.defaultValue 0.0
                        houseConsumptionA   = houseA |> Option.defaultValue 0.0
                        houseConsumptionB   = houseB |> Option.defaultValue 0.0
                        houseConsumptionC          = houseC |> Option.defaultValue 0.0
                        dailyHouseConsumptionKwh   = dailyHouseKwh |> Option.defaultValue 0.0 |})
                    do! mqtt.Publish topic payload
                    do! Task.Delay(pollMs, stoppingToken)

                with ex ->
                    logger.LogError("Modbus error: {0}", ex.Message)
                    state.SetConnected false
                    try client.Disconnect() with _ -> ()
                    do! Task.Delay(30000, stoppingToken)
        }
        tsk :> Task
