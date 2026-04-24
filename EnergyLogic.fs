namespace EnergyMonitor

open EnergyMonitor.Db


module EnergyLogic =

    /// Általános szűrő, ami visszaadja az utolsó 'hours' óra adatait
    let getLiveEntriesByTime (hours: float) =
        task {
            let! allData = Database.getShellyDataLastHour()
            let threshold = System.DateTime.Now.AddHours(-hours)
            
            return allData 
                   |> Seq.filter (fun x -> x.ts >= threshold)
                   |> Seq.sortByDescending (fun x -> x.ts)
                   |> Seq.toList
        }

    /// Kiszámolja az átlagos feszültséget a kapott listából
    let calculateAverageVoltage (data: Db.Tables.shelly_3em_live list) =
        if data.IsEmpty then 
            0.0
        else 
            data |> List.averageBy (fun x -> x.a_voltage |> Option.defaultValue 0.0)