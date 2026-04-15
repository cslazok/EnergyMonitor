namespace ha_dashboard

type Sensor = {
    Name: string
    Room: string
    Value: float
    Unit: string
}

type DeviceState =
    | On
    | Off
    | Open
    | Closed
    | Partial of int

type Device = {
    Name: string
    Room: string
    State: DeviceState
}

type EnergyDay = {
    Day: string
    Consumption: float
    Production: float
}