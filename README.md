# Real-Time Sensor Analytics Dashboard

This repository contains a proof-of-concept implementation of a real-time sensor analytics platform consisting of:

- **Backend**: .NET 8 Web API that simulates 1,000 sensor readings per second, pushes real-time updates through SignalR, maintains a 24-hour retention policy, and exposes aggregated statistics.
- **Frontend**: Angular 17 dashboard that visualizes streaming data, displays aggregated metrics, and highlights anomaly alerts.

## Project Structure

```
/README.md
/SensorAnalytics.sln
/src/SensorAnalytics.Api/        # ASP.NET Core API project
/sensor-dashboard/               # Angular SPA dashboard
```

## Backend Overview (`SensorAnalytics.Api`)

- `SensorSimulationService` generates synthetic sensor data, detects anomalies, and broadcasts batches to connected clients through SignalR.
- `SensorDataStore` maintains an in-memory buffer capable of handling 100,000 data points while enforcing a 24-hour retention window and tracking aggregate statistics.
- `SensorHub` (SignalR) and `ReadingsController` expose real-time streams and REST endpoints for statistics, recent readings, and alerts.

### Running the API

```bash
cd src/SensorAnalytics.Api
dotnet restore
dotnet run
```

The API listens on `http://localhost:5000` by default (adjust via `ASPNETCORE_URLS`).

## Frontend Overview (`sensor-dashboard`)

- Connects to the SignalR hub for live batches and keeps a rolling chart of average sensor values per second.
- Displays aggregated statistics, a live alert feed, and a table of recent sensor readings.

### Running the Dashboard

```bash
cd sensor-dashboard
npm install
npm start
```

By default the Angular dev server runs on `http://localhost:4200` and proxies data directly from the API.

## Notes

- Ensure the API is running before starting the dashboard to allow the SignalR connection to succeed.
- The system is tuned for proof-of-concept demonstrations and can be extended with persistent storage, authentication, and production-ready deployment tooling.
