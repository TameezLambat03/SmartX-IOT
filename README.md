# Smart-X — IoT Ecosystem Gateway

Smart-X is a .NET-based IoT ecosystem designed to simulate the monitoring and management of distributed smart environments such as **hydroponic farms, automated real-estate utility systems, and smart-grid installations**.

The system models a central IoT gateway receiving continuous telemetry from distributed sensors and processing different types of sensor data efficiently.

The application combines a **WPF desktop dashboard** with an **ASP.NET Core Web API**, providing an operator-focused interface for:

* Sensor registration
* Multi-typed telemetry ingestion
* Telemetry validation
* Mock telemetry generation
* Live telemetry monitoring
* Anomaly feedback
* Telemetry history
* Device artifact attachment
* Encrypted artifact storage
* Telemetry aggregation

---

## YouTube Demo

[Smart-X Demonstration](https://youtu.be/2Be9O4EpeEo?si=9uScNolnDWZYXcHT)

---

# Features

## Sensor Registration

Smart-X allows operators to register simulated IoT devices using:

* Device MAC address
* Deployment location
* Sensor category

Supported sensor categories include:

* **Environmental** — floating-point telemetry such as temperature or soil moisture
* **Power Consumption** — integer telemetry such as electrical wattage
* **Actuator** — boolean telemetry such as valve or device states

Registered devices are maintained by the gateway and displayed through the WPF dashboard.

---

## Multi-Typed Telemetry Ingestion

The gateway supports strongly typed telemetry using generic telemetry packets:

```text
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
```

Each telemetry type is processed through its corresponding API endpoint.

| Sensor Category   | Telemetry Type | Example                    |
| ----------------- | -------------- | -------------------------- |
| Environmental     | `float`        | Soil moisture              |
| Power Consumption | `int`          | Power consumption in watts |
| Actuator          | `bool`         | Valve ON/OFF state         |

This allows the system to preserve the original data type produced by each simulated IoT device rather than converting every reading into a common representation.

---

## Telemetry Processing Pipeline

Incoming telemetry passes through a processing pipeline that:

1. Receives the telemetry packet.
2. Validates the packet.
3. Validates the deployment hierarchy recursively.
4. Adds the reading to the appropriate processing buffer.
5. Groups readings into batches.
6. Stores completed batches in historical arrays.
7. Moves processed values into bounded collections.
8. Maintains a fixed maximum number of optimized records.

The system uses separate processing paths for environmental, power, and actuator telemetry.

---

# Data Structures and Programming Techniques

Smart-X intentionally demonstrates several data structures and programming concepts.

## Generic Collections

A custom generic collection is used throughout the application:

```text
SmartCollection<T>
```

It is used for:

* Sensor storage
* Telemetry buffers
* Optimized telemetry data

The generic design allows the same collection implementation to work with different telemetry types without duplicating collection logic.

---

## Arrays

Telemetry history uses **jagged arrays**:

```text
float[][]
int[][]
bool[][]
```

These arrays maintain a bounded history of completed telemetry batches.

For example:

```csharp
private readonly float[][] _historicalEnvironmentalBatches =
    new float[MaxHistoricalBatches][];
```

The outer array represents available batch slots, while each inner array contains the telemetry readings belonging to one completed batch.

The batch index wraps around when the maximum number of historical batches is reached, preventing the historical storage from growing indefinitely.

---

## Recursion

Deployment locations are converted into hierarchical deployment trees.

For example:

```text
Facility A / Zone 1 / Sub-Zone B
```

becomes:

```text
Facility A
└── Zone 1
    └── Sub-Zone B
```

Each deployment location is represented using `DeploymentNode` objects containing child deployment nodes.

The hierarchy is validated recursively.

The validation method checks the current node and then calls itself for each child node:

```csharp
foreach (DeploymentNode child in node.Children)
{
    if (!ValidateDeploymentRecursively(
            child,
            depth + 1))
    {
        return false;
    }
}
```

A maximum depth is also enforced to prevent excessively deep or malformed deployment structures.

---

## Generics

The `TelemetryPacket<T>` class allows the same telemetry model to represent different sensor value types:

```csharp
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
```

The generic type parameter allows the telemetry value to retain its original type throughout the API and processing pipeline.

This is particularly appropriate for an IoT gateway because different sensor categories naturally produce different kinds of data.

---

## Method Overloading

The telemetry ingestion service uses **method overloading** for its `IngestPacket` methods.

There are separate overloads for each supported telemetry type:

```csharp
public void IngestPacket(TelemetryPacket<float> packet)
```

```csharp
public void IngestPacket(TelemetryPacket<int> packet)
```

```csharp
public void IngestPacket(TelemetryPacket<bool> packet)
```

Each method has the same method name but accepts a different parameter type.

This allows the API to use the same `IngestPacket(...)` operation while processing each telemetry type through its appropriate strongly typed path.

---

## Operator Overloading

`TelemetryPacket<T>` also implements an overloaded `+` operator for supported numerical telemetry types.

For example:

```text
TelemetryPacket<float> + TelemetryPacket<float>
```

produces an aggregated telemetry packet.

The same mechanism supports integer telemetry while rejecting unsupported operations such as boolean aggregation.

Operator overloading is used specifically for the telemetry aggregation/developer testing functionality.

---

# WPF Dashboard

The WPF application acts as the **Smart-X operator control centre**.

The dashboard provides:

* Gateway connection status
* Registered device count
* Telemetry packet count
* Sensor registration
* Registered sensor selection
* Telemetry submission
* Mock telemetry generation
* Device artifact attachment
* Live telemetry monitoring
* Anomaly feedback
* Telemetry history retrieval
* Telemetry aggregation testing
* Live telemetry graphing

The dashboard separates device configuration and management from live telemetry monitoring.

---

## Live Telemetry Graph

The dashboard includes a custom telemetry trend visualisation that displays incoming readings over time.

The chart maintains a bounded history of recent readings and supports the different telemetry categories.

Actuator states are represented numerically when displayed on the graph so that their state changes can be visualised over time.

---

# Mock IoT Telemetry

Because the Smart-X environment is simulated, the application includes mock telemetry generation to represent data being produced by distributed IoT devices.

The mock telemetry system supports:

* Environmental readings
* Power consumption readings
* Actuator states
* Multiple telemetry packets
* Repeated ingestion through the normal API pipeline

This allows the application to demonstrate how the gateway behaves under a continuous stream of simulated sensor data.

The WPF dashboard also provides a **Seed Mock Telemetry** action for manually generating additional telemetry during demonstrations and load testing.

---

# Validation

Smart-X performs validation at the API boundary before telemetry enters the processing pipeline.

Examples include:

* Missing device identifiers
* Missing deployment locations
* Invalid environmental values
* Environmental values outside the permitted range
* Power values outside the permitted range
* Invalid deployment hierarchies
* Invalid sensor categories
* Duplicate sensor MAC addresses

This ensures that invalid telemetry is rejected before entering the gateway's processing structures.

---

# Device Artifacts

Registered devices can have supporting artifacts attached through the dashboard.

Supported file types include:

```text
.json
.txt
.log
.png
.jpg
```

Attachments are associated with a specific registered device using its MAC address.

The upload flow is:

```text
Select registered sensor
        ↓
Choose artifact
        ↓
WPF uploads artifact
        ↓
API identifies sensor by MAC address
        ↓
Artifact is encrypted
        ↓
Encrypted file is stored
        ↓
Attachment metadata is associated with sensor
```

Attachments are encrypted using **AES-GCM** before being written to disk.

The encryption implementation uses:

* 256-bit encryption keys
* Random nonces
* Authentication tags
* Configuration-based key storage

---

# Architecture

Smart-X uses a two-application architecture consisting of a WPF operator client and an ASP.NET Core backend API.

```text
┌───────────────────────────────┐
│       WPF Operator UI         │
│                               │
│  Sensor Dashboard             │
│  Telemetry Monitoring         │
│  Device Management            │
└───────────────┬───────────────┘
                │
                │ HTTP / JSON
                ▼
┌───────────────────────────────┐
│       ASP.NET Core API        │
│                               │
│  Sensor Registration          │
│  Telemetry Validation         │
│  Telemetry Ingestion          │
│  Deployment Validation        │
│  Attachment Encryption        │
└───────────────┬───────────────┘
                │
                ▼
┌───────────────────────────────┐
│      In-Memory Data Layer     │
│                               │
│  SmartCollection<T>            │
│  Jagged Arrays                │
│  Batched Telemetry            │
│  Sensor Profiles              │
└───────────────────────────────┘
```

The WPF application communicates with the ASP.NET Core API using HTTP requests and JSON serialization.

---

# Project Structure

```text
SmartX
│
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
│   │   ├── DeploymentValidationService.cs
│   │   ├── SensorService.cs
│   │   └── TelemetryIngestionService.cs
│   ├── Program.cs
│   └── appsettings.json
│
└── SmartXWPF
    ├── Dashboard
    │   ├── DashboardController.cs
    │   ├── DashboardState.cs
    │   ├── DashboardView.cs
    │   └── TelemetryChartService.cs
    ├── Services
    │   ├── AnomalyDetector.cs
    │   ├── ApiService.cs
    │   ├── AttachmentService.cs
    │   ├── MockTelemetryService.cs
    │   ├── SensorApiService.cs
    │   └── TelemetryApiService.cs
    ├── MainWindow.xaml
    └── SensorDashboardWindow.xaml
```

> **Note:** Local configuration files containing encryption keys or other secrets should not be committed to the repository. The files shown above represent the application structure rather than files that necessarily contain deployment-specific secrets.

---

# Application Flow

## 1. Start Smart-X

The application starts from the Smart-X architecture screen.

The current implementation exposes the first development pillar:

**Sensor Data Ingestion and Telemetry**

Future architecture areas are presented as planned functionality:

* Real-Time Command Stream and History
* Network Topology and Mesh Routing

---

## 2. Register a Sensor

The operator provides:

```text
MAC Address
Deployment Location
Sensor Category
```

The WPF application sends the registration request to the API.

The API validates and registers the sensor.

---

## 3. Select a Sensor

The operator selects a registered sensor from the dashboard.

The selected device becomes the target for telemetry submission and device artifact attachment.

---

## 4. Submit Telemetry

The operator can simulate a reading from the selected device.

The appropriate strongly typed telemetry packet is created based on the device category and submitted to the API.

---

## 5. Monitor the Gateway

Successful telemetry ingestion updates:

* Current reading
* Last update time
* Packet count
* Telemetry graph
* Anomaly feedback
* Telemetry activity feed

---

## 6. Seed Mock Data

The operator can generate additional telemetry to demonstrate the gateway's processing pipeline and data structures under load.

---

## 7. Attach Device Artifacts

A configuration file, log, or image can be attached to the currently selected sensor.

The API associates the artifact with that device's MAC address and encrypts the file before storage.

---

# Setup and Installation

This section describes how to restore dependencies, compile the solution, configure attachment encryption, start the backend API, and launch the WPF client.

## Requirements

* Windows
* Visual Studio with WPF desktop development support
* .NET 10 SDK
* PowerShell
* Git

---

## 1. Clone and Open the Solution

Clone the repository and open the `SmartX.sln` solution in Visual Studio.

The solution contains:

```text
SmartX.Shared
```

Shared models, enums, and custom collections.

```text
SmartXAPI
```

ASP.NET Core backend gateway.

```text
SmartXWPF
```

WPF operator client.

---

## 2. Restore Dependencies

From the solution directory, run:

```powershell
dotnet restore
```

This restores the NuGet dependencies required by the solution.

---

## 3. Compile the Project

Build the complete solution with:

```powershell
dotnet build
```

The build should complete successfully before the API or WPF client is launched.

---

## 4. Configure the Attachment Encryption Key

Smart-X encrypts uploaded device artifacts using AES-GCM.

The API requires a **256-bit (32-byte) encryption key in Base64 format**.

Generate a secure key with PowerShell:

```powershell
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()

$bytes = New-Object byte[] 32

$rng.GetBytes($bytes)

$rng.Dispose()

[Convert]::ToBase64String($bytes)
```

Copy the resulting Base64 string into the API's local configuration using the `AttachmentEncryptionKey` setting.

For example:

```json
{
  "AttachmentEncryptionKey": "YOUR_GENERATED_BASE64_KEY"
}
```

### Security Note

**Do not publish or commit a real encryption key to a public repository.**

For local development, keep the real key in an appropriate local configuration or secret-management mechanism that is excluded from source control.

If the encryption key is missing, malformed, or is not exactly 32 bytes after Base64 decoding, the API will refuse to start because encrypted attachment storage cannot be initialized safely.

---

## 5. Start the Backend API

Start `SmartXAPI` before launching the WPF client.

From the solution directory:

```powershell
dotnet run --project SmartXAPI
```

Alternatively, set `SmartXAPI` as the startup project in Visual Studio and run it.

Keep the API running while using the WPF client.

---

## 6. Configure the WPF API Address

The WPF client reads its API base URL from application configuration.

Make sure `ApiBaseUrl` points to the address where `SmartXAPI` is running before launching the client.

The API and WPF client must be running against the same configured endpoint.

---

## 7. Run the WPF Client

With the backend API running, start `SmartXWPF` from Visual Studio.

The application opens on the Smart-X architecture screen.

Select:

```text
Sensor Data Ingestion and Telemetry
```

to enter the main operator dashboard.

---

## 8. Verify the Installation

A successful startup should allow you to:

* Open the Smart-X WPF interface.
* Confirm the gateway reports as connected.
* Register a sensor.
* Select the registered sensor.
* Submit telemetry.
* Observe the current reading.
* Observe the live graph update.
* Seed mock telemetry.
* Attach a supported artifact.
* Observe anomaly feedback.
* Observe telemetry activity.

---

# Troubleshooting

## The WPF client reports that the gateway is offline

Confirm that:

1. `SmartXAPI` is running.
2. `ApiBaseUrl` matches the API's actual listening address.
3. The API is reachable from the machine running the WPF application.

Restart the API and then restart the WPF client if necessary.

---

## The API fails to start because of encryption configuration

Confirm that:

* `AttachmentEncryptionKey` exists.
* The key is valid Base64.
* The decoded key is exactly 32 bytes.
* The local configuration being used by the API contains the key.

Generate a new key using the PowerShell command provided in the setup section if necessary.

---

## The project does not compile

Run:

```powershell
dotnet restore
dotnet clean
dotnet build
```

Then check the Visual Studio Error List for the first compilation error.

---

# Demonstration

A typical demonstration can follow this sequence:

```text
1. Start Smart-X API
2. Launch WPF application
3. Confirm gateway connection
4. Register a sensor
5. Select the sensor
6. Submit environmental telemetry
7. Submit power telemetry
8. Submit actuator telemetry
9. Observe the live telemetry graph
10. Trigger anomaly conditions
11. Seed mock telemetry
12. Retrieve telemetry history
13. Attach a device artifact
14. Demonstrate encrypted artifact storage
15. Demonstrate telemetry aggregation
```

---

# Project Status

## Currently Implemented

* Sensor registration
* Multi-typed telemetry ingestion
* Environmental telemetry
* Power telemetry
* Actuator telemetry
* API validation
* Deployment hierarchy validation
* Recursive validation
* Generic telemetry packets
* Generic collections
* Method overloading
* Operator overloading
* Jagged-array telemetry history
* Batched telemetry processing
* Mock telemetry generation
* Live telemetry dashboard
* Telemetry graph
* Anomaly feedback
* Device artifact uploads
* AES-GCM attachment encryption
* Startup data seeding
* Telemetry aggregation

---

## Startup Data Seeding

The Smart-X gateway supports startup seeding of simulated devices and telemetry so that the application can demonstrate an already-populated IoT environment when it boots.

The WPF **Seed Mock Telemetry** action remains available for manually generating additional telemetry during demonstrations and load testing.

---

## Planned Architecture Areas

The application architecture also provides space for future functionality:

* Real-time command streaming
* Extended telemetry history
* Network topology management
* Mesh routing visualisation

These areas are represented in the Smart-X startup architecture but are not part of the currently implemented telemetry pillar.

---

# Smart-X Concept

Smart-X represents a simulated central gateway for a distributed IoT environment.

```text
             ┌──────────────────┐
             │  Environmental   │
             │     Sensors      │
             └────────┬─────────┘
                      │
               ┌──────▼───────┐
               │              │
               │   Smart-X    │
               │   Gateway    │
               │              │
               └──────┬───────┘
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
      Hydroponic  Real Estate  Smart Grid
        Farms      Utilities   Installations
```

The system demonstrates how a central gateway can receive, validate, process, optimise, and visualise heterogeneous telemetry originating from distributed IoT devices.

---

# Technology Stack

| Component       | Technology                                |
| --------------- | ----------------------------------------- |
| Frontend        | WPF                                       |
| Backend         | ASP.NET Core Web API                      |
| Language        | C#                                        |
| Runtime         | .NET 10                                   |
| Communication   | HTTP / JSON                               |
| Serialization   | `System.Text.Json`                        |
| Encryption      | AES-GCM                                   |
| Data Structures | Generic Collections, Lists, Jagged Arrays |
| Processing      | Batched in-memory telemetry pipeline      |

---

# Design Goals

Smart-X was designed around several core IoT gateway principles.

## Strongly Typed Telemetry

Different IoT devices produce different types of data. Smart-X preserves those types throughout the telemetry pipeline.

## Bounded Memory Usage

Telemetry storage is deliberately bounded to prevent the simulated continuous stream from growing indefinitely.

## Gateway Validation

The API validates telemetry and deployment information before processing it.

## Efficient Data Processing

Telemetry is buffered and processed in batches before being moved into optimized storage structures.

## Operator Visibility

The WPF dashboard provides immediate visual feedback about the state of the simulated IoT ecosystem.

## Secure Artifact Storage

Device artifacts are encrypted before being persisted to disk.

---

# Academic Context

Smart-X was developed as an IoT-focused software engineering project to demonstrate practical application of:

* Object-oriented programming
* Generic programming
* Method overloading
* Operator overloading
* Recursive algorithms
* Arrays and collections
* API architecture
* Client-server communication
* Data validation
* Telemetry processing
* Mock IoT data generation
* Secure file handling
* WPF interface design

The project is intentionally designed as an **IoT gateway simulation rather than a generic CRUD application**, with its architecture and data structures centred around continuous, multi-typed telemetry processing.
