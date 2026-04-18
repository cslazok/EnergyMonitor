namespace EnergyMonitor.Db

open SqlHydra.Query

module Tables = 

    type shelly_3em_live =
        { device_id: string
          a_current: float option
          a_voltage: float option
          a_act_power: float option
          a_aprt_power: float option
          a_pf: float option
          a_freq: float option
          b_current: float option
          b_voltage: float option
          b_act_power: float option
          b_aprt_power: float option
          b_pf: float option
          b_freq: float option
          c_current: float option
          c_voltage: float option
          c_act_power: float option
          c_aprt_power: float option
          c_pf: float option
          c_freq: float option
          n_current: float option
          total_current: float option
          total_act_power: float option
          total_aprt_power: float option
          import_power: float option
          export_power: float option }

    let shelly_3em_live = table<shelly_3em_live>

    type shelly_3em_energy =
        { device_id: string
          total_act: float option
          total_act_ret: float option
          import_total_kwh: float option
          export_total_kwh: float option
          net_total_kwh: float option }

    let shelly_3em_energy = table<shelly_3em_energy>