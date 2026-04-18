Live 3-Phase Energy Dashboard (F# WebSharper Project)

Course: Introduction to Functional Programming in F#
University: University of Dunaújváros
Instructor: Adam Granicz

Project Description

This project is a high-performance real-time energy monitoring dashboard developed in F# using the WebSharper framework. The application visualizes electrical measurements from a remote data server and provides an interactive interface for monitoring three-phase power metrics.

The system operates in a distributed architecture:

Central Data Server
A dedicated physical server running PostgreSQL, responsible for collecting and storing high-frequency three-phase electrical measurements.
Development & Visualization Node
A Lenovo ThinkCentre M710q workstation running Linux, used for development and for hosting the WebSharper-based web interface.

This separation ensures responsive visualization while heavy data processing remains isolated on the backend server.

Motivation and Architecture

The goal of the project was to build a fully independent monitoring solution instead of relying on monolithic smart-home platforms such as Home Assistant.

This approach provides:

full control over data processing
flexible database integration
improved performance
customizable visualization logic

The architecture follows a decoupled design pattern, where:

the backend server handles storage and queries
the frontend application handles visualization and user interaction

This makes the system scalable and responsive even under continuous high-frequency measurements.

Features
Standalone Monitoring Solution

The application operates independently from existing smart-home ecosystems.

Decoupled Architecture

The visualization layer runs separately from the database server, ensuring stable performance.

3-Phase Power Monitoring

Supports real-time visualization of:

Active power
Apparent power
Reactive power
Voltage (L1, L2, L3)
Current (L1, L2, L3)
Power factor (L1, L2, L3)
Reactive User Interface

The dashboard automatically refreshes measurement data every 5 seconds using WebSharper reactive components.

Secure Configuration

Sensitive configuration values (database connection parameters) are stored in a local .env file and are not committed to version control.

Technologies Used

The project was implemented using:

F#
WebSharper
.NET 10
ASP.NET Core
PostgreSQL
Npgsql
Node-RED
JSON HTTP API
Git / GitHub
GitHub Pages (demo deployment)
Project Structure

Database.fs

Contains database access logic and maps PostgreSQL query results into functional F# record types.

Client.fs

Implements reactive frontend logic and periodic data polling.

Startup.fs

Configures the ASP.NET Core hosting environment.

.env

Stores local configuration values such as:

database host
username
password
database name
Live Demo

The project is available online via GitHub Pages:

https://cslazok.github.io/ha-dashboard/

Since GitHub Pages supports only static hosting, the demo version uses sample measurement data stored in a JSON file instead of a live database connection.

Future Improvements

Possible extensions of the project include:

historical energy charts
mobile-optimized layout
abnormal consumption alert system
extended long-term storage integration
optional Home Assistant interoperability
Author

Dániel Csaba Lázok
University of Dunaújváros
Course: Introduction to Functional Programming in F#
