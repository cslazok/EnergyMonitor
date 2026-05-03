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

    override _.ExecuteAsync(stoppingToken: CancellationToken) =
        let tsk = task {
            let ip   = config.["Inverter:Ip"]
            let port = int config.["Inverter:Port"]
            let unitId = int config.["Inverter:UnitId"]
            let pollMs = int config.["Inverter:PollIntervalSeconds"] * 1000
            let endpoint = sprintf "%s:%d" ip port
            logger.LogInformation("Inverter reader starting. IP: {0}", ip)

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

                    let pvTotalPower = pv1Voltage * pv1Current + pv2Voltage * pv2Current

                    state.UpdateData "pv1Voltage"    (pv1Voltage   :> obj)
                    state.UpdateData "pv1Current"    (pv1Current   :> obj)
                    state.UpdateData "pv2Voltage"    (pv2Voltage   :> obj)
                    state.UpdateData "pv2Current"    (pv2Current   :> obj)
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
                    state.UpdateData "batterySOC"    (batterySOC   :> obj)
                    state.UpdateData "batteryPower"  (batteryPower :> obj)

                    state.SetConnected true
                    do! Database.insertInverterLive true (state.GetData())
                    logger.LogInformation("Poll OK — Grid: {0}W  PV: {1}W  Daily: {2}kWh  SOC: {3}%%", activePower, pvTotalPower, dailyYield, batterySOC)
                    let topic = config.["Mqtt:InverterTopic"]
                    let payload = System.Text.Json.JsonSerializer.Serialize({|
                        connected    = true
                        activePower  = activePower
                        pvTotalPower = pvTotalPower
                        dailyYield   = dailyYield
                        totalYield   = totalYield
                        batterySOC   = batterySOC
                        batteryPower = batteryPower
                        temperature  = temperature |})
                    do! mqtt.Publish topic payload
                    do! Task.Delay(pollMs, stoppingToken)

                with ex ->
                    logger.LogError("Modbus error: {0}", ex.Message)
                    state.SetConnected false
                    try client.Disconnect() with _ -> ()
                    do! Task.Delay(30000, stoppingToken)
        }
        tsk :> Task
