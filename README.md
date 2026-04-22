This project was started as a way to learn F#. Its main purpose is to monitor my household energy consumption. Since net metering will end soon, I would like to calculate whether investing in an energy storage system would be worthwhile.

The project is still in an early stage. At the moment, I have one power meter that sends data through Node-RED into a database. The F# application connects to this database, and the project will also be used as part of a university assignment.

In the future, I would like to replace Node-RED completely and build the whole system in F#. The long-term goal is for the F# application to handle data collection, serve the web interface, perform calculations, write data into the database, and read it back for visualization and analysis. I would also like to add MQTT and Modbus support later.

## Demo
https://cslazok.github.io/EnergyMonitor/energy.html

## Screenshots
![Live Data](https://raw.githubusercontent.com/cslazok/EnergyMonitor/main/docs/Screenshot.png)