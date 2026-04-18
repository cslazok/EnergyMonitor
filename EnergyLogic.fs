namespace EnergyMonitor

open WebSharper
open EnergyMonitor.Db

module EnergyLogic =

    /// Általános szűrő, ami visszaadja az utolsó 'hours' óra adatait
    let getLiveEntriesByTime (hours: float) =
        task {
            let! allData = Database.getLatestShellyData() // Feltételezve, hogy ez listát ad vissza
            let threshold = System.DateTime.Now.AddHours(-hours)
            
            return allData 
                   |> Seq.filter (fun x -> x.created_at >= threshold)
                   |> Seq.sortByDescending (fun x -> x.created_at)
                   |> Seq.toList
        }

    /// Kiszámolja az átlagos feszültséget a kapott listából
    let calculateAverageVoltage (data: Tables.shelly_3em_live list) =
        if data.IsEmpty then 0.0
        else data |> List.averageBy (fun x -> x.a_voltage |> Option.defaultValue 0.0)