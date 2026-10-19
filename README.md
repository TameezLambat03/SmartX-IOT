# Smart-X — IoT Ecosystem Gateway

Smart-X is a .NET-based IoT gateway simulation designed to monitor and manage distributed smart environments such as **hydroponic farms, automated utility systems, and smart-grid installations**.

The system combines a WPF desktop application with an ASP.NET Core Web API to simulate device registration, multi-typed telemetry ingestion, background processing, command execution, alert prioritisation, and predictive recommendations.

## YouTube Demo

https://youtu.be/9u5Y_kR71FQ?si=IEofcTExXlIfL66h

---

# Features

## Sensor Registration and Telemetry

Smart-X supports three device categories:

- **Environmental:** floating-point readings, such as soil moisture.
- **Power Consumption:** integer readings, such as electrical wattage.
- **Actuator:** boolean readings, such as valve states.

Telemetry is represented using generic packets:

```csharp
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
```

The API validates incoming telemetry and processes each category through its corresponding pipeline.

## Background Telemetry Processing

The telemetry pipeline uses:

- Separate FIFO queues for environmental, power, and actuator telemetry.
- A background service that checks for queued telemetry every 100 milliseconds.
- Batch processing of five readings.
- Bounded historical storage using jagged arrays and collections.
- A `SortedDictionary` to maintain timestamp-ordered telemetry history.

These structures support the processing and organisation of simulated incoming device data.

## Real-Time Command Stream and History

The command dashboard allows operators to select registered devices and issue commands according to their category.

Supported commands include:

| Device category | Commands |
|---|---|
| Actuator | `ON`, `OFF` |
| Environmental | `READ`, `CALIBRATE`, `RESET` |
| Power Consumption | `READ`, `RESET`, `START_MONITORING`, `STOP_MONITORING` |

The command system includes simulated device states, command history, and undo functionality for reversible commands.

The dashboard also provides:

- Device search and category filtering.
- Filtering by devices with active alerts.
- Command history and command filtering.
- Environmental telemetry history.
- Active alert monitoring.
- Search history.
- Predictive recommendations.
- Manual refresh and command controls.

## Alert Prioritisation

Environmental readings are checked against configured thresholds:

- **Critical:** below 10.
- **High:** below 15.
- **Warning:** below 20.

The alert service uses a `PriorityQueue` to prioritise alerts by severity and a `HashSet` to track devices with active errors without duplicating their MAC addresses.

When a reading returns to a safe range, the corresponding active error is cleared.

## Predictive Recommendation Engine

The recommendation service analyses environmental readings and recent command activity to identify patterns that may require operator attention.

It supports:

- Detection of unsafe environmental readings.
- Detection of declining readings across consecutive samples.
- Detection of repeated actuator `ON` or `OFF` commands while environmental readings are low.
- Severity-based recommendations and suggested actions.
- Repeated-search detection within a 30-minute window.

Recommendations are displayed in the command dashboard alongside alerts and historical information.

## Device Search History

The search history service records device-related searches and checks for repeated searches within a 30-minute period.

When a search reaches the configured repetition threshold, the pattern can be passed to the recommendation engine.

Search history is bounded to limit memory usage.

## Device Artifacts and Encryption

Registered devices can have supporting artifacts attached to them.

Supported file types include:

```text
.json
.txt
.log
.png
.jpg
```

Attachments are associated with a registered device using its MAC address and encrypted using AES-GCM before storage.

The encryption implementation uses a 256-bit key, random nonces, authentication tags, and configuration-based key storage.

---

# Data Structures and Programming Techniques

Smart-X demonstrates the use of several data structures and programming concepts.

## Generic Collections

`SmartCollection<T>` provides reusable collection functionality for supported sensor and telemetry data.

## Queues

`Queue<T>` implements FIFO processing for environmental, power, and actuator telemetry.

## Priority Queues

`PriorityQueue<TElement, TPriority>` organises alerts by severity so that critical alerts can be prioritised over lower-severity alerts.

## Stacks

`Stack<T>` stores previous simulated device states, allowing the most recent reversible command to be undone.

## Dictionaries and Hash Sets

- `Dictionary<TKey, TValue>` supports efficient device lookup by MAC address and tracking of simulated device states.
- `SortedDictionary<TKey, TValue>` stores timestamped telemetry in sorted key order.
- `HashSet<T>` tracks active device errors without duplicate MAC addresses.

## Arrays and Bounded Storage

Jagged arrays store completed telemetry batches. Ring-style batch indexing and storage limits prevent historical buffers from growing indefinitely.

## Generics and Method Overloading

`TelemetryPacket<T>` supports different telemetry value types through a shared model. Overloaded `IngestPacket` methods handle the supported types through appropriately typed processing paths.

## Operator Overloading

The telemetry packet model provides an overloaded `+` operator for supported numerical telemetry aggregation.

## Recursion

Deployment hierarchies are represented as trees of `DeploymentNode` objects. Recursive validation checks the hierarchy and enforces a maximum depth.

---

# WPF Dashboard

The WPF application provides the operator interface for Smart-X.

The main menu provides access to:

1. **Sensor Data Ingestion and Telemetry** — device management and telemetry monitoring.
2. **Real-Time Command Stream and History** — device commands, command history, alerts, and recommendations.
3. **Network Topology and Mesh Routing** — displayed as coming soon.

The telemetry dashboard provides sensor registration, telemetry submission, mock data generation, telemetry history, graphing, anomaly feedback, aggregation testing, and device artifact management.

The command dashboard provides device controls, category-specific commands, filtering, history, alerts, search history, and predictive recommendations.

---

# Architecture

Smart-X uses a WPF client connected to an ASP.NET Core Web API over HTTP and JSON.

```text
┌──────────────────────────────────┐
│            WPF Client            │
│                                  │
│  Main Menu                       │
│  Sensor and Telemetry Dashboard  │
│  Command Stream and History      │
└────────────────┬─────────────────┘
                 │
              HTTP / JSON
                 │
┌────────────────▼─────────────────┐
│         ASP.NET Core API          │
│                                  │
│  Sensor Registration             │
│  Telemetry Ingestion             │
│  Background Queue Processing     │
│  Alert Management                │
│  Command Execution and Undo      │
│  Search History                  │
│  Predictive Recommendations      │
│  Attachment Encryption           │
└────────────────┬─────────────────┘
                 │
┌────────────────▼─────────────────┐
│        In-Memory Data Layer       │
│                                  │
│  Dictionaries and Hash Sets      │
│  FIFO Queues and Priority Queue  │
│  Command Undo Stack              │
│  Sorted Telemetry History        │
│  Bounded Collections and Arrays  │
└──────────────────────────────────┘
```

Device telemetry and command execution are simulated. Network topology and mesh routing are not yet implemented.

---

# Project Structure

```text
SmartX
├── SmartX.Shared
│   ├── Collections
│   │   └── SmartCollection.cs
│   ├── Enums
│   │   └── SensorCategory.cs
│   └── Models
│       ├── DeploymentNode.cs
│       ├── SensorProfile.cs
│       └── TelemetryPacket.cs
│
├── SmartXAPI
│   ├── Services
│   │   ├── AttachmentEncryptionService.cs
│   │   ├── CommandService.cs
│   │   ├── CriticalAlertService.cs
│   │   ├── DeploymentValidationService.cs
│   │   ├── PredictiveRecommendationService.cs
│   │   ├── SearchHistoryService.cs
│   │   ├── SensorService.cs
│   │   ├── TelemetryIngestionService.cs
│   │   └── TelemetryQueueProcessor.cs
│   └── Program.cs
│
└── SmartXWPF
    ├── MainWindow.xaml
    ├── MainWindow.xaml.cs
    ├── SensorDashboardWindow.xaml
    ├── CommandDashboardWindow.xaml
    ├── CommandDashboardWindow.xaml.cs
    └── Services
        └── API and dashboard services
```

---

# Setup and Installation

## Requirements

- Windows
- .NET 10 SDK
- Visual Studio with WPF development support
- Git

## 1. Clone the Repository

Clone the repository and open the solution in Visual Studio.

## 2. Restore Dependencies

From the solution directory:

```powershell
dotnet restore
```

## 3. Build the Solution

```powershell
dotnet build
```

## 4. Configure Attachment Encryption

Create `SmartXAPI/appsettings.json` locally if it is required by the application configuration.

```json
{
  "AttachmentEncryptionKey": "YOUR_GENERATED_BASE64_KEY"
}
```

Generate a 32-byte encryption key using PowerShell:

```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
$bytes = New-Object byte[] 32
$rng.GetBytes($bytes)
$rng.Dispose()
[Convert]::ToBase64String($bytes)
```

Replace the placeholder with the generated Base64 value. The decoded key must be exactly 32 bytes.

Do not commit real encryption keys or other secrets to source control.

## 5. Start the API

```powershell
dotnet run --project SmartXAPI
```

Keep the API running while using the desktop application.

## 6. Configure the WPF Client

Ensure the configured `ApiBaseUrl` matches the address at which the API is listening.

## 7. Run the WPF Client

Start `SmartXWPF` from Visual Studio and select the desired dashboard from the main menu.

---

# Demonstration Workflow

A typical demonstration can follow this sequence:

1. Launch the API and WPF client.
2. Open the telemetry dashboard.
3. Register or select a simulated device.
4. Submit environmental, power, and actuator telemetry.
5. Generate mock telemetry and review the telemetry history.
6. Open the command dashboard.
7. Issue category-specific device commands.
8. Demonstrate undo functionality.
9. Review command history and active alerts.
10. Trigger environmental thresholds and review recommendations.
11. Demonstrate device filtering and search history.
12. Upload a device artifact and demonstrate encrypted storage.

---

# Project Status

## Implemented

- Sensor registration and category-specific telemetry.
- Telemetry validation and deployment hierarchy validation.
- Background queue processing and batched telemetry storage.
- Generic collections, telemetry packets, and numerical aggregation.
- Mock telemetry generation and telemetry history.
- WPF telemetry dashboard and live graphing.
- Command execution and simulated device states.
- Command history and undo functionality.
- Priority-based alerts and active-error tracking.
- Device filtering and search history.
- Predictive recommendations.
- Device artifact uploads and AES-GCM encryption.

## Planned

- Network topology visualisation.
- Mesh routing management.

---

# Technology Stack

| Component | Technology |
|---|---|
| Language | C# |
| Runtime | .NET 10 |
| Frontend | WPF |
| Backend | ASP.NET Core Web API |
| Communication | HTTP / JSON |
| Serialization | `System.Text.Json` |
| Encryption | AES-GCM |
| Processing | Background telemetry queues and batching |
| Data structures | Queues, priority queues, stacks, dictionaries, hash sets, sorted dictionaries, arrays, and generic collections |

---

# Academic Context

Smart-X demonstrates practical applications of object-oriented programming, generic programming, data structures, recursive algorithms, client-server architecture, API validation, background processing, telemetry analysis, predictive recommendations, and secure file handling.

The project is designed as an **IoT gateway simulation**, with an emphasis on processing heterogeneous telemetry, managing simulated devices, and providing operators with actionable monitoring and command-management tools.