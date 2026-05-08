namespace EnergyMonitor

open System
open Giraffe.ViewEngine

module Views =

    let opt f = Option.map f >> Option.defaultValue "-"
    let optF fmt v = v |> Option.map (sprintf fmt) |> Option.defaultValue "-"

    let layout (titleStr: string) (content: XmlNode list) =
        html [ _lang "hu" ] [
            head [] [
                meta [ _charset "UTF-8" ]
                meta [ _name "viewport"; _content "width=device-width, initial-scale=1.0" ]
                title [] [ str titleStr ]
                link [ _rel "stylesheet"; _href "https://cdn.jsdelivr.net/npm/bootstrap@5.3.0/dist/css/bootstrap.min.css" ]
                link [ _rel "stylesheet"; _href "https://fonts.googleapis.com/css2?family=Inter:wght@300;400;600;700&display=swap" ]
                style [] [
                    str """
                    body { font-family: 'Inter', sans-serif; background: #f0f4f8; }
                    .navbar { background: linear-gradient(135deg, #1e3c72 0%, #2a5298 100%); }
                    .navbar-brand { font-weight: 700; font-size: 1.3rem; }
                    .stat-card { border: none; border-radius: 16px; box-shadow: 0 4px 20px rgba(0,0,0,0.08); transition: transform 0.2s; }
                    .stat-card:hover { transform: translateY(-4px); }
                    .stat-value { font-size: 1.8rem; font-weight: 700; }
                    .stat-label { font-size: 0.75rem; text-transform: uppercase; letter-spacing: 1px; color: #888; }
                    .phase-a { border-top: 4px solid #ef5350; }
                    .phase-b { border-top: 4px solid #42a5f5; }
                    .phase-c { border-top: 4px solid #66bb6a; }
                    .total-card { border-top: 4px solid #ab47bc; }
                    .energy-card { border-top: 4px solid #ff7043; background: linear-gradient(135deg, #fff3e0, #fff8f5); }
                    .inverter-card { border-top: 4px solid #f59e0b; background: linear-gradient(135deg, #fffbeb, #fefce8); }
                    .kwh-value { font-size: 1.5rem; font-weight: 700; }
                    .table-container { background: white; border-radius: 16px; padding: 24px; box-shadow: 0 4px 20px rgba(0,0,0,0.06); }
                    .phase-badge-a { background: #ffebee; color: #c62828; padding: 2px 8px; border-radius: 6px; font-weight: 600; }
                    .phase-badge-b { background: #e3f2fd; color: #1565c0; padding: 2px 8px; border-radius: 6px; font-weight: 600; }
                    .phase-badge-c { background: #e8f5e9; color: #2e7d32; padding: 2px 8px; border-radius: 6px; font-weight: 600; }
                    .nav-link { color: rgba(255,255,255,0.85) !important; font-weight: 500; }
                    .nav-link:hover, .nav-link.active { color: #fff !important; }
                    """
                ]
            ]
            body [] [
                nav [ _class "navbar navbar-expand-lg navbar-dark mb-4 shadow" ] [
                    div [ _class "container" ] [
                        a [ _class "navbar-brand"; _href "/" ] [
                            span [ _class "me-2" ] [ str "⚡" ]
                            str "Energy Monitor"
                        ]
                        div [ _class "navbar-nav ms-auto" ] [
                            a [ _class "nav-link"; _href "/" ] [ str "📊 Irányítópult" ]
                            a [ _class "nav-link"; _href "/history" ] [ str "🕒 Előzmények" ]
                            a [ _class "nav-link"; _href "/energy" ] [ str "⚡ Energia (kWh)" ]
                        ]
                    ]
                ]
                main [ _class "container pb-5" ] content
                footer [ _class "container text-center mt-4 mb-3 text-muted small" ] [
                    hr []
                    p [] [ str (sprintf "© %d EnergyMonitor · Shelly 3EM · F# + Giraffe" DateTime.Now.Year) ]
                ]
            ]
        ]

    let phaseCard (phase: string) (cssClass: string) (voltage: float option) (current: float option) (power: float option) (pf: float option) =
        div [ _class "col-md-4 col-sm-12" ] [
            div [ _class (sprintf "card stat-card %s p-4 h-100" cssClass) ] [
                h5 [ _class "fw-bold mb-3" ] [ str (sprintf "Fázis %s" phase) ]
                div [ _class "row g-2" ] [
                    div [ _class "col-6" ] [
                        div [ _class "stat-label" ] [ str "Feszültség" ]
                        div [ _class "stat-value" ] [ str (optF "%.1f V" voltage) ]
                    ]
                    div [ _class "col-6" ] [
                        div [ _class "stat-label" ] [ str "Áram" ]
                        div [ _class "stat-value" ] [ str (optF "%.2f A" current) ]
                    ]
                    div [ _class "col-6" ] [
                        div [ _class "stat-label" ] [ str "Teljesítmény" ]
                        div [ _class "stat-value" ] [ str (optF "%.1f W" power) ]
                    ]
                    div [ _class "col-6" ] [
                        div [ _class "stat-label" ] [ str "Cos φ" ]
                        div [ _class "stat-value" ] [ str (optF "%.2f" pf) ]
                    ]
                ]
            ]
        ]

    let private statCell label css value =
        div [ _class "col-md-3 col-6" ] [
            div [ _class "stat-label" ] [ str label ]
            div [ _class (sprintf "kwh-value %s" css) ] [ str value ]
        ]

    let inverterCard (inv: Inverter.Snapshot) =
        let statusBadge =
            if inv.Connected then span [ _class "badge bg-success ms-2" ] [ str "● Online" ]
            else span [ _class "badge bg-danger ms-2" ] [ str "● Offline" ]
        let batteryText =
            match inv.BatteryPower with
            | Some p when p < 0.0 -> sprintf "%.0f W (tölt)" p
            | Some p when p > 0.0 -> sprintf "%.0f W (merít)" p
            | Some _ -> "0 W"
            | None -> "-"
        let pvPower (v: float option) (i: float option) =
            match v, i with
            | Some v, Some i -> sprintf "%.0f W" (v * i)
            | _ -> "-"
        div [ _class "card stat-card inverter-card p-4 mb-4" ] [
            div [ _class "d-flex align-items-center mb-3" ] [
                h5 [ _class "fw-bold mb-0" ] [ str "☀️ Napelem Inverter" ]
                statusBadge
            ]
            div [ _class "row g-3 text-center" ] [
                statCell "PV összteljesítmény" "text-warning" (optF "%.0f W"   inv.PvTotalPower)
                statCell "Inverter kimenet"    "text-warning" (optF "%.0f W"   inv.ActivePower)
                statCell "Napi termelés"       "text-success" (optF "%.2f kWh" inv.DailyYield)
                statCell "Összes termelés"     ""             (optF "%.1f kWh" inv.TotalYield)
                statCell "Hőmérséklet"         ""             (optF "%.1f °C"  inv.Temperature)
            ]
            hr [ _class "my-3" ]
            div [ _class "row g-3 text-center" ] [
                statCell "PV1 feszültség"  "text-info"    (optF "%.1f V"  inv.Pv1Voltage)
                statCell "PV1 áram"        "text-info"    (optF "%.2f A"  inv.Pv1Current)
                statCell "PV1 teljesítmény" "text-warning" (pvPower inv.Pv1Voltage inv.Pv1Current)
                statCell "PV2 feszültség"  "text-info"    (optF "%.1f V"  inv.Pv2Voltage)
                statCell "PV2 áram"        "text-info"    (optF "%.2f A"  inv.Pv2Current)
                statCell "PV2 teljesítmény" "text-warning" (pvPower inv.Pv2Voltage inv.Pv2Current)
                statCell "Akksi töltöttség" "text-info"   (optF "%.1f %%" inv.BatterySOC)
                statCell "Akksi teljesítmény" ""          batteryText
            ]
        ]

    let liveDashboard (latest: Db.Tables.shelly_3em_live) (energy: Db.Tables.shelly_3em_energy option) (inverter: Inverter.Snapshot option) =
        [
            div [ _class "d-flex justify-content-between align-items-center mb-4" ] [
                h2 [ _class "fw-bold mb-0" ] [ str "Élő adatok" ]
                span [ _class "badge bg-success" ] [ str (sprintf "● Utolsó mérés: %s" (latest.ts.ToLocalTime().ToString("HH:mm:ss"))) ]
            ]
            match inverter with
            | Some inv -> inverterCard inv
            | None -> div [ _class "alert alert-secondary mb-4" ] [ str "☀️ Inverter nem elérhető." ]
            div [ _class "card stat-card total-card p-4 mb-4" ] [
                h5 [ _class "fw-bold mb-3" ] [ str "Összesített fogyasztás" ]
                div [ _class "row g-3" ] [
                    div [ _class "col-md-3 col-6" ] [
                        div [ _class "stat-label" ] [ str "Össz. Teljesítmény" ]
                        div [ _class "stat-value text-purple" ] [ str (optF "%.1f W" latest.total_act_power) ]
                    ]
                    div [ _class "col-md-3 col-6" ] [
                        div [ _class "stat-label" ] [ str "Össz. Áram" ]
                        div [ _class "stat-value" ] [ str (optF "%.2f A" latest.total_current) ]
                    ]
                    div [ _class "col-md-3 col-6" ] [
                        div [ _class "stat-label" ] [ str "Import" ]
                        div [ _class "stat-value text-danger" ] [ str (optF "%.1f W" latest.import_power) ]
                    ]
                    div [ _class "col-md-3 col-6" ] [
                        div [ _class "stat-label" ] [ str "Export" ]
                        div [ _class "stat-value text-success" ] [ str (optF "%.1f W" latest.export_power) ]
                    ]
                ]
            ]
            div [ _class "row g-4 mb-4" ] [
                phaseCard "A" "phase-a" latest.a_voltage latest.a_current latest.a_act_power latest.a_pf
                phaseCard "B" "phase-b" latest.b_voltage latest.b_current latest.b_act_power latest.b_pf
                phaseCard "C" "phase-c" latest.c_voltage latest.c_current latest.c_act_power latest.c_pf
            ]
            match energy with
            | Some e ->
                div [ _class "card stat-card energy-card p-4 mb-4" ] [
                    h5 [ _class "fw-bold mb-3" ] [ str "⚡ Göngyölt energia értékek" ]
                    div [ _class "row g-3 text-center" ] [
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "stat-label" ] [ str "Import összesen" ]
                            div [ _class "kwh-value text-danger" ] [ str (optF "%.3f kWh" e.import_total_kwh) ]
                        ]
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "stat-label" ] [ str "Export összesen" ]
                            div [ _class "kwh-value text-success" ] [ str (optF "%.3f kWh" e.export_total_kwh) ]
                        ]
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "stat-label" ] [ str "Nettó" ]
                            div [ _class "kwh-value text-primary" ] [ str (optF "%.3f kWh" e.net_total_kwh) ]
                        ]
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "stat-label" ] [ str "Össz. aktív" ]
                            div [ _class "kwh-value" ] [ str (optF "%.3f kWh" e.total_act) ]
                        ]
                    ]
                ]
            | None ->
                div [ _class "alert alert-warning" ] [ str "Nincs elérhető göngyölt energia adat." ]
        ] |> layout "Energy Monitor - Irányítópult"

    let private fmtHuf (v: float) =
        let abs = System.Math.Abs(v)
        let s = if abs >= 1000.0 then sprintf "%s %03.0f" (sprintf "%.0f" (System.Math.Floor(abs / 1000.0))) (abs % 1000.0) else sprintf "%.0f" abs
        if v < 0.0 then sprintf "-%s Ft" s else sprintf "+%s Ft" s

    let energyPage (data: Db.Tables.shelly_3em_energy list) (calibration: Database.MeterCalibration) (prices: Database.ElectricityPrices) =
        let latest = data |> List.tryHead
        let cal = calibration
        let calibrated (v: float option) (offset: float) = v |> Option.map (fun x -> x + offset)
        let importCost (kwh: float) =
            if kwh <= prices.AnnualLimitKwh then kwh * prices.ImportLowHuf
            else prices.AnnualLimitKwh * prices.ImportLowHuf + (kwh - prices.AnnualLimitKwh) * prices.ImportHighHuf
        let exportRevenue (kwh: float) = kwh * prices.ExportHuf
        [
            div [ _class "d-flex justify-content-between align-items-center mb-4" ] [
                h2 [ _class "fw-bold mb-0" ] [ str "⚡ Szaldó év" ]
                match latest with
                | Some e -> span [ _class "badge bg-success" ] [ str (sprintf "● Utolsó mérés: %s" (e.ts.ToLocalTime().ToString("HH:mm:ss"))) ]
                | None   -> span [ _class "badge bg-secondary" ] [ str "Nincs adat" ]
            ]
            match latest with
            | None -> div [ _class "alert alert-warning" ] [ str "Nincs elérhető energia adat." ]
            | Some e ->
                let dispImport = calibrated e.import_total_kwh cal.ImportOffset
                let dispExport = calibrated e.export_total_kwh cal.ExportOffset
                match cal.BaselineImport, cal.BaselineExport with
                | Some bi, Some be ->
                    let periodImport = dispImport |> Option.map (fun v -> v - bi)
                    let periodExport = dispExport |> Option.map (fun v -> v - be)
                    let periodNet    = Option.map2 (fun ex i -> ex - i) periodExport periodImport
                    let netHuf       = Option.map2 (fun imp exp -> exportRevenue exp - importCost imp) periodImport periodExport
                    // fő kártyák: szaldó év
                    div [ _class "row g-4 mb-4" ] [
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "card stat-card p-4 text-center"; _style "border-top: 4px solid #ef5350;" ] [
                                div [ _class "stat-label" ] [ str "Vásárolt (szaldó év)" ]
                                div [ _class "display-5 fw-bold text-danger" ] [ str (optF "%.1f kWh" periodImport) ]
                                small [ _class "text-muted" ] [ str (periodImport |> Option.map (fun v -> sprintf "%.0f Ft" (importCost v)) |> Option.defaultValue "-") ]
                            ]
                        ]
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "card stat-card p-4 text-center"; _style "border-top: 4px solid #66bb6a;" ] [
                                div [ _class "stat-label" ] [ str "Eladott (szaldó év)" ]
                                div [ _class "display-5 fw-bold text-success" ] [ str (optF "%.1f kWh" periodExport) ]
                                small [ _class "text-muted" ] [ str (periodExport |> Option.map (fun v -> sprintf "%.0f Ft" (exportRevenue v)) |> Option.defaultValue "-") ]
                            ]
                        ]
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "card stat-card p-4 text-center"; _style "border-top: 4px solid #42a5f5;" ] [
                                div [ _class "stat-label" ] [ str "Nettó kWh" ]
                                div [ _class (sprintf "display-5 fw-bold %s" (periodNet |> Option.map (fun n -> if n >= 0.0 then "text-success" else "text-danger") |> Option.defaultValue "")) ] [
                                    str (periodNet |> Option.map (fun n -> sprintf "%+.1f kWh" n) |> Option.defaultValue "-")
                                ]
                                small [ _class "text-muted" ] [ str "eladott − vásárolt" ]
                            ]
                        ]
                        div [ _class "col-md-3 col-6" ] [
                            div [ _class "card stat-card p-4 text-center"; _style "border-top: 4px solid #ab47bc;" ] [
                                div [ _class "stat-label" ] [ str "Nettó egyenleg" ]
                                div [ _class (sprintf "display-5 fw-bold %s" (netHuf |> Option.map (fun n -> if n >= 0.0 then "text-success" else "text-danger") |> Option.defaultValue "")) ] [
                                    str (netHuf |> Option.map fmtHuf |> Option.defaultValue "-")
                                ]
                                small [ _class "text-muted" ] [
                                    str (periodImport |> Option.map (fun v ->
                                        if v <= prices.AnnualLimitKwh then sprintf "%.0f Ft/kWh" prices.ImportLowHuf
                                        else sprintf "%.0f+%.0f Ft/kWh" prices.ImportLowHuf prices.ImportHighHuf
                                    ) |> Option.defaultValue "")
                                ]
                            ]
                        ]
                    ]
                    // referencia sor: villanyóra állás
                    div [ _class "card stat-card p-3 mb-4"; _style "border-top: 4px solid #dee2e6; background: #f8f9fa;" ] [
                        div [ _class "row g-2 text-center" ] [
                            div [ _class "col-4" ] [
                                div [ _class "stat-label" ] [ str "Villanyóra vétel" ]
                                div [ _class "fw-bold" ] [ str (optF "%.0f kWh" dispImport) ]
                            ]
                            div [ _class "col-4" ] [
                                div [ _class "stat-label" ] [ str "Villanyóra eladás" ]
                                div [ _class "fw-bold" ] [ str (optF "%.0f kWh" dispExport) ]
                            ]
                            div [ _class "col-4" ] [
                                div [ _class "stat-label" ] [ str "Szaldó alap (%.0f / %.0f)" ]
                                div [ _class "fw-bold text-muted" ] [ str (sprintf "%.0f / %.0f kWh" bi be) ]
                            ]
                        ]
                    ]
                | _ ->
                    div [ _class "alert alert-info" ] [ str "📋 Add meg a villanyóra állását és az időszak alapértékét (szaldó év kezdete) az alábbi formban." ]
            div [ _class "card stat-card p-4 mt-2"; _style "border-top: 4px solid #6c757d;" ] [
                div [ _class "d-flex justify-content-between align-items-center mb-3" ] [
                    h5 [ _class "fw-bold mb-0" ] [ str "🔧 Villanyóra leolvasás" ]
                    match cal.SetAt with
                    | Some t -> small [ _class "text-muted" ] [ str (sprintf "Utolsó korrekció: %s" (t.ToLocalTime().ToString("yyyy-MM-dd HH:mm"))) ]
                    | None   -> small [ _class "text-muted" ] [ str "Még nem volt leolvasás" ]
                ]
                p [ _class "text-muted small mb-3" ] [ str "Add meg az óra jelenlegi állását. Az időszak alapértéke opcionális — ha megadod, megmutatja az azóta eltelt deltát." ]
                form [ _action "/energy/calibrate"; _method "post"; _class "row g-3 align-items-end" ] [
                    div [ _class "col-md-3" ] [
                        label [ _class "form-label small fw-bold" ] [ str "Jelenlegi vétel (kWh)" ]
                        input [ _type "number"; _name "meter_import"; _class "form-control"; _step "0.001"; _placeholder "29248" ]
                    ]
                    div [ _class "col-md-3" ] [
                        label [ _class "form-label small fw-bold" ] [ str "Jelenlegi eladás (kWh)" ]
                        input [ _type "number"; _name "meter_export"; _class "form-control"; _step "0.001"; _placeholder "28499" ]
                    ]
                    div [ _class "col-md-2" ] [
                        label [ _class "form-label small fw-bold text-warning" ] [ str "Alap vétel (opcionális)" ]
                        input [ _type "number"; _name "baseline_import"; _class "form-control"; _step "0.001"; _placeholder "27682" ]
                    ]
                    div [ _class "col-md-2" ] [
                        label [ _class "form-label small fw-bold text-warning" ] [ str "Alap eladás (opcionális)" ]
                        input [ _type "number"; _name "baseline_export"; _class "form-control"; _step "0.001"; _placeholder "26339" ]
                    ]
                    div [ _class "col-md-2" ] [
                        button [ _type "submit"; _class "btn btn-secondary w-100 fw-bold" ] [ str "Mentés" ]
                    ]
                ]
            ]
            div [ _class "card stat-card p-4 mt-3"; _style "border-top: 4px solid #0d6efd;" ] [
                h5 [ _class "fw-bold mb-3" ] [ str "💰 Áramdíj beállítás" ]
                form [ _action "/energy/prices"; _method "post"; _class "row g-3 align-items-end" ] [
                    div [ _class "col-md-3" ] [
                        label [ _class "form-label small fw-bold" ] [ str (sprintf "Limit alatti ár (Ft/kWh) — jelenleg %.2f" prices.ImportLowHuf) ]
                        input [ _type "number"; _name "import_low"; _class "form-control"; _step "0.01"; _placeholder (sprintf "%.2f" prices.ImportLowHuf) ]
                    ]
                    div [ _class "col-md-3" ] [
                        label [ _class "form-label small fw-bold" ] [ str (sprintf "Limit feletti ár (Ft/kWh) — jelenleg %.2f" prices.ImportHighHuf) ]
                        input [ _type "number"; _name "import_high"; _class "form-control"; _step "0.01"; _placeholder (sprintf "%.2f" prices.ImportHighHuf) ]
                    ]
                    div [ _class "col-md-2" ] [
                        label [ _class "form-label small fw-bold" ] [ str (sprintf "Betáplálás (Ft/kWh) — %.2f" prices.ExportHuf) ]
                        input [ _type "number"; _name "export_huf"; _class "form-control"; _step "0.01"; _placeholder (sprintf "%.2f" prices.ExportHuf) ]
                    ]
                    div [ _class "col-md-2" ] [
                        label [ _class "form-label small fw-bold" ] [ str (sprintf "Éves limit (kWh) — %.0f" prices.AnnualLimitKwh) ]
                        input [ _type "number"; _name "annual_limit"; _class "form-control"; _step "1"; _placeholder (sprintf "%.0f" prices.AnnualLimitKwh) ]
                    ]
                    div [ _class "col-md-2" ] [
                        button [ _type "submit"; _class "btn btn-primary w-100 fw-bold" ] [ str "Mentés" ]
                    ]
                ]
            ]
        ] |> layout "Energy Monitor - Energia"

    let historyTable (data: Db.Tables.shelly_3em_live list) =
        [
            div [ _class "d-flex justify-content-between align-items-center mb-4" ] [
                h2 [ _class "fw-bold mb-0" ] [ str "Előzmények (Utolsó 1 óra)" ]
                span [ _class "badge bg-secondary fs-6" ] [ str (sprintf "%d bejegyzés" data.Length) ]
            ]
            div [ _class "table-container" ] [
                div [ _class "table-responsive" ] [
                    table [ _class "table table-hover align-middle" ] [
                        thead [ _class "table-dark" ] [
                            tr [] [
                                th [] [ str "Időpont" ]
                                th [] [ str "Össz. Teljesítmény" ]
                                th [ _class "text-center" ] [ span [ _class "phase-badge-a" ] [ str "Fázis A" ] ]
                                th [ _class "text-center" ] [ span [ _class "phase-badge-b" ] [ str "Fázis B" ] ]
                                th [ _class "text-center" ] [ span [ _class "phase-badge-c" ] [ str "Fázis C" ] ]
                                th [] [ str "Össz. Áram" ]
                            ]
                        ]
                        tbody [] [
                            for row in data ->
                                tr [] [
                                    td [ _class "fw-bold text-muted small" ] [ str (row.ts.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss")) ]
                                    td [ _class "fw-bold" ] [ str (optF "%.1f W" row.total_act_power) ]
                                    td [ _class "text-center" ] [
                                        div [ _class "small" ] [ str (optF "%.1f W" row.a_act_power) ]
                                        div [ _class "text-muted small" ] [ str (optF "%.1f V" row.a_voltage) ]
                                    ]
                                    td [ _class "text-center" ] [
                                        div [ _class "small" ] [ str (optF "%.1f W" row.b_act_power) ]
                                        div [ _class "text-muted small" ] [ str (optF "%.1f V" row.b_voltage) ]
                                    ]
                                    td [ _class "text-center" ] [
                                        div [ _class "small" ] [ str (optF "%.1f W" row.c_act_power) ]
                                        div [ _class "text-muted small" ] [ str (optF "%.1f V" row.c_voltage) ]
                                    ]
                                    td [] [ str (optF "%.2f A" row.total_current) ]
                                ]
                        ]
                    ]
                ]
            ]
        ] |> layout "Energy Monitor - Előzmények"
