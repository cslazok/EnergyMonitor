namespace EnergyMonitor

open System
open System.Text
open System.Text.Json
open System.Threading
open System.Threading.Tasks
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging
open Microsoft.Extensions.Configuration
open MQTTnet
open MQTTnet.Client

type MqttPublisher(logger: ILogger<MqttPublisher>, config: IConfiguration) =
    let factory    = MqttFactory()
    let client     = factory.CreateMqttClient()
    let brokerIp   = config.["Mqtt:BrokerIp"]
    let brokerPort = int config.["Mqtt:BrokerPort"]

    let buildOptions () =
        MqttClientOptionsBuilder()
            .WithTcpServer(brokerIp, brokerPort)
            .Build()

    member _.Publish (topic: string) (payload: string) =
        task {
            try
                if not client.IsConnected then
                    let! _ = client.ConnectAsync(buildOptions(), CancellationToken.None)
                    ()
                let msg =
                    MqttApplicationMessageBuilder()
                        .WithTopic(topic)
                        .WithPayload(Encoding.UTF8.GetBytes(payload))
                        .WithRetainFlag(true)
                        .Build()
                let! _ = client.PublishAsync(msg, CancellationToken.None)
                ()
            with ex ->
                logger.LogWarning("MQTT publish failed: {0}", ex.Message)
        }

    interface IDisposable with
        member _.Dispose() = client.Dispose()

type ShellyMqttService(logger: ILogger<ShellyMqttService>, config: IConfiguration) =
    inherit BackgroundService()

    let brokerIp    = config.["Mqtt:BrokerIp"]
    let brokerPort  = int config.["Mqtt:BrokerPort"]
    let emTopic     = config.["Mqtt:ShellyEmTopic"]
    let emdataTopic = config.["Mqtt:ShellyEmdataTopic"]

    let tryFloat (el: JsonElement) (key: string) =
        match el.TryGetProperty(key) with
        | true, v when v.ValueKind = JsonValueKind.Number -> Some (v.GetDouble())
        | _ -> None

    let handleEm (payload: string) =
        task {
            use doc = JsonDocument.Parse(payload)
            let r  = doc.RootElement
            let tf = tryFloat r
            let totalPower = tf "total_act_power" |> Option.defaultValue 0.0
            let row : Db.Tables.shelly_3em_live = {
                id = 0L; ts = DateTime.UtcNow; device_id = 1
                a_voltage = tf "a_voltage"; a_current = tf "a_current"
                a_act_power = tf "a_act_power"; a_aprt_power = tf "a_aprt_power"
                a_pf = tf "a_pf"; a_freq = tf "a_freq"
                b_voltage = tf "b_voltage"; b_current = tf "b_current"
                b_act_power = tf "b_act_power"; b_aprt_power = tf "b_aprt_power"
                b_pf = tf "b_pf"; b_freq = tf "b_freq"
                c_voltage = tf "c_voltage"; c_current = tf "c_current"
                c_act_power = tf "c_act_power"; c_aprt_power = tf "c_aprt_power"
                c_pf = tf "c_pf"; c_freq = tf "c_freq"
                n_current = tf "n_current"
                total_current = tf "total_current"
                total_act_power = tf "total_act_power"
                total_aprt_power = tf "total_aprt_power"
                import_power = Some (max 0.0 totalPower)
                export_power = Some (max 0.0 -totalPower)
            }
            do! Database.insertShellyLive row
        }

    let handleEmdata (payload: string) =
        task {
            use doc = JsonDocument.Parse(payload)
            let r  = doc.RootElement
            let tf = tryFloat r
            let totalAct    = tf "total_act"     |> Option.defaultValue 0.0
            let totalActRet = tf "total_act_ret" |> Option.defaultValue 0.0
            let row : Db.Tables.shelly_3em_energy = {
                id = 0L; ts = DateTime.UtcNow; device_id = Some 1
                total_act     = Some totalAct
                total_act_ret = Some totalActRet
                // Shelly Gen2 reports cumulative energy in Wh
                import_total_kwh = Some (totalAct    / 1000.0)
                export_total_kwh = Some (totalActRet / 1000.0)
                net_total_kwh    = Some ((totalAct - totalActRet) / 1000.0)
            }
            do! Database.insertShellyEnergy row
        }

    override _.ExecuteAsync(stoppingToken: CancellationToken) =
        let tsk = task {
            let factory = MqttFactory()
            use client  = factory.CreateMqttClient()

            client.add_ApplicationMessageReceivedAsync(
                Func<MqttApplicationMessageReceivedEventArgs, Task>(fun args ->
                    task {
                        let topic   = args.ApplicationMessage.Topic
                        let payload = Encoding.UTF8.GetString(args.ApplicationMessage.PayloadSegment)
                        try
                            if topic = emTopic then
                                do! handleEm payload
                            elif topic = emdataTopic then
                                do! handleEmdata payload
                        with ex ->
                            logger.LogWarning("Shelly parse error [{0}]: {1}", topic, ex.Message)
                    } :> Task
                )
            )

            while not stoppingToken.IsCancellationRequested do
                try
                    if not client.IsConnected then
                        let opts =
                            MqttClientOptionsBuilder()
                                .WithTcpServer(brokerIp, brokerPort)
                                .Build()
                        let! _ = client.ConnectAsync(opts, stoppingToken)
                        logger.LogInformation("Shelly MQTT connected to {0}:{1}", brokerIp, brokerPort)
                        let subOpts =
                            MqttFactory().CreateSubscribeOptionsBuilder()
                                .WithTopicFilter(fun f -> f.WithTopic(emTopic)     |> ignore)
                                .WithTopicFilter(fun f -> f.WithTopic(emdataTopic) |> ignore)
                                .Build()
                        let! _ = client.SubscribeAsync(subOpts, stoppingToken)
                        logger.LogInformation("Subscribed: {0}, {1}", emTopic, emdataTopic)
                    do! Task.Delay(5000, stoppingToken)
                with
                | :? OperationCanceledException -> ()
                | ex ->
                    logger.LogError("Shelly MQTT error: {0}", ex.Message)
                    try let! _ = client.DisconnectAsync() in () with _ -> ()
                    do! Task.Delay(10000, stoppingToken)
        }
        tsk :> Task
