# Smart-X — IoT Ecosystem Gateway

Smart-X is a .NET-based IoT ecosystem designed to simulate the monitoring and management of distributed smart environments such as **hydroponic farms, automated real-estate utility systems, and smart-grid installations**.

The system models a central IoT gateway receiving continuous telemetry from distributed sensors and processing different types of sensor data efficiently. The application combines a **WPF desktop dashboard** with an **ASP.NET Core Web API**, providing an operator-focused interface for sensor registration, telemetry ingestion, monitoring, mock data generation, anomaly feedback, and device artifact attachment.

---

## Features

### Sensor Registration

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

### Multi-Typed Telemetry Ingestion

The gateway supports strongly typed telemetry using generic telemetry packets:

```text
TelemetryPacket<float>
TelemetryPacket<int>
TelemetryPacket<bool>
```

Each telemetry type is processed through its corresponding API endpoint.

Example:

```text
Environmental → float
Power         → int
Actuator      → bool
```

This allows the system to model the different data types commonly produced by IoT devices without converting every reading into a common representation.

---

### Telemetry Processing Pipeline

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

## Data Structures

Smart-X intentionally demonstrates several data structures and programming techniques.

### Generic Collections

A custom generic collection is used throughout the application:

```text
SmartCollection<T>
```

It is used for sensor storage, telemetry buffers, and optimized telemetry data.

### Arrays

Telemetry history uses jagged arrays:

```text
float[][]
int[][]
bool[][]
```

These arrays maintain a bounded history of completed telemetry batches.

### Recursion

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

The resulting hierarchy is validated recursively, including depth and child-node validation.

### Operator Overloading

`TelemetryPacket<T>` implements an overloaded `+` operator for supported numerical telemetry types.

For example:

```text
TelemetryPacket<float> + TelemetryPacket<float>
```

produces an aggregated telemetry packet.

The same mechanism supports integer telemetry while rejecting unsupported operations such as boolean aggregation.

---

## WPF Dashboard

The WPF application acts as the **Smart-X operator control centre**.

The interface provides:

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

The dashboard separates configuration and device management from live monitoring.

### Live Telemetry Graph

The dashboard includes a custom telemetry trend visualisation that displays incoming readings over time.

The chart maintains a bounded history of recent readings and supports the different telemetry categories, including actuator states represented numerically for visualisation.

---

## Mock IoT Telemetry

Because the Smart-X environment is simulated, the application includes mock telemetry generation to represent data being produced by distributed IoT devices.

The mock telemetry system supports:

* Environmental readings
* Power consumption readings
* Actuator states
* Multiple telemetry packets
* Repeated ingestion through the normal API pipeline

This allows the application to demonstrate how the gateway behaves under a continuous stream of simulated sensor data.

The WPF dashboard also provides a **Seed Mock Telemetry** action for manually generating additional telemetry during demonstrations and testing.

---

## Validation

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

## Device Artifacts

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

## Architecture

Smart-X uses a two-application architecture:

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
│  SmartCollection<T>           │
│  Jagged Arrays                │
│  Batched Telemetry            │
│  Sensor Profiles              │
└───────────────────────────────┘
```

### Project Structure

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

---

## Application Flow

### 1. Start Smart-X

The application starts from the Smart-X architecture screen.

The current implementation exposes the first development pillar:

**Sensor Data Ingestion and Telemetry**

Future architecture areas are presented as planned functionality:

* Real-Time Command Stream and History
* Network Topology and Mesh Routing

---

### 2. Register a Sensor

The operator provides:

```text
MAC Address
Deployment Location
Sensor Category
```

The WPF application sends the registration request to the API.

The API validates and registers the sensor.

---

### 3. Select a Sensor

The operator selects a registered sensor from the dashboard.

The selected device becomes the target for telemetry submission and device artifact attachment.

---

### 4. Submit Telemetry

The operator can simulate a reading from the selected device.

The appropriate typed telemetry packet is created based on the device category and submitted to the API.

---

### 5. Monitor the Gateway

Successful telemetry ingestion updates:

* Current reading
* Last update time
* Packet count
* Telemetry graph
* Anomaly feedback
* Telemetry activity feed

---

### 6. Seed Mock Data

The operator can generate additional telemetry to demonstrate the gateway's processing pipeline and data structures under load.

---

### 7. Attach Device Artifacts

A configuration file, log, or image can be attached to the currently selected sensor.

The API associates the artifact with that device's MAC address and encrypts the file before storage.

---

## Technology Stack

| Component       | Technology                                |
| --------------- | ----------------------------------------- |
| Frontend        | WPF                                       |
| Backend         | ASP.NET Core Web API                      |
| Language        | C#                                        |
| Runtime         | .NET                                      |
| Communication   | HTTP / JSON                               |
| Serialization   | `System.Text.Json`                        |
| Encryption      | AES-GCM                                   |
| Data Structures | Generic Collections, Lists, Jagged Arrays |
| Processing      | Batched in-memory telemetry pipeline      |

---

## Design Goals

Smart-X was designed around several core IoT gateway principles:

### Strongly Typed Telemetry

Different IoT devices produce different types of data. Smart-X preserves those types throughout the telemetry pipeline.

### Bounded Memory Usage

Telemetry storage is deliberately bounded to prevent the simulated continuous stream from growing indefinitely.

### Gateway Validation

The API validates telemetry and deployment information before processing it.

### Efficient Data Processing

Telemetry is buffered and processed in batches before being moved into optimized storage structures.

### Operator Visibility

The WPF dashboard provides immediate visual feedback about the state of the simulated IoT ecosystem.

### Secure Artifact Storage

Device artifacts are encrypted before being persisted to disk.

---

## Running the Project

### Requirements

* Windows
* Visual Studio
* .NET SDK compatible with the project
* WPF desktop support

### Startup

Start the **SmartXAPI** project first so that the gateway API is available.

Then run the **SmartXWPF** project.

The WPF application connects to the configured API base URL and provides the operator dashboard.

---

## Demonstration

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

## Project Status

### Currently Implemented

* Sensor registration
* Multi-typed telemetry ingestion
* Environmental telemetry
* Power telemetry
* Actuator telemetry
* API validation
* Deployment hierarchy validation
* Recursive validation
* Generic telemetry packets
* Operator overloading
* Custom generic collections
* Jagged-array telemetry history
* Batched telemetry processing
* Mock telemetry generation
* Live telemetry dashboard
* Telemetry graph
* Anomaly feedback
* Device artifact uploads
* AES-GCM attachment encryption

### Planned Architecture Areas

The application architecture also provides space for future functionality:

* Real-time command streaming
* Extended telemetry history
* Network topology management
* Mesh routing visualisation

---

## Smart-X Concept

Smart-X represents a simulated central gateway for a distributed IoT environment.

```text
             ┌──────────────┐
             │ Environmental│
             │    Sensors   │
             └──────┬───────┘
                    │
             ┌──────▼───────┐
             │              │
             │   Smart-X    │
             │   Gateway    │
             │              │
             └──────┬───────┘
                    │
       ┌────────────┼────────────┐
       ▼            ▼            ▼
   Hydroponic   Real Estate   Smart Grid
     Farms       Utilities    Installations
```

The system demonstrates how a central gateway can receive, validate, process, optimise, and visualise heterogeneous telemetry originating from distributed IoT devices.

---

## Academic Context

Smart-X was developed as an IoT-focused software engineering project to demonstrate practical application of:

* Object-oriented programming
* Generic programming
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

The project is intentionally designed as an **IoT gateway simulation rather than a generic CRUD application**, with the architecture and data structures centred around continuous, multi-typed telemetry processing.
