
using Microsoft.AspNetCore.Http.Json;
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using SmartXAPI.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

builder.Services.AddSingleton<TelemetryIngestionService>();
builder.Services.AddHostedService<TelemetryQueueProcessor>();
builder.Services.AddSingleton<CriticalAlertService>();
builder.Services.AddSingleton<DeploymentValidationService>();
builder.Services.AddSingleton<SensorService>();
builder.Services.AddSingleton<CommandService>();
builder.Services.AddSingleton<AttachmentEncryptionService>();
builder.Services.AddSingleton<StartupSeeder>();
builder.Services.AddSingleton<PredictiveRecommendationService>();
builder.Services.AddSingleton<SearchHistoryService>();
var app = builder.Build();

using (IServiceScope scope = app.Services.CreateScope())
{
    StartupSeeder seeder =
        scope.ServiceProvider
            .GetRequiredService<StartupSeeder>();

    seeder.Seed();
}


// ENVIRONMENTAL TELEMETRY

app.MapPost(
    "/api/telemetry/environmental",
    (
        TelemetryPacket<float> packet,
        TelemetryIngestionService ingestionService,
        DeploymentValidationService validationService
    ) =>
    {
        if (packet == null)
        {
            return Results.BadRequest(new
            {
                Message = "Telemetry packet is required."
            });
        }

        if (string.IsNullOrWhiteSpace(packet.DeviceMacAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        if (string.IsNullOrWhiteSpace(packet.DeploymentLocation))
        {
            return Results.BadRequest(new
            {
                Message = "Deployment location is required."
            });
        }

        if (float.IsNaN(packet.SensorValue) ||
            float.IsInfinity(packet.SensorValue))
        {
            return Results.BadRequest(new
            {
                Message = "Environmental sensor value must be a valid number."
            });
        }

        if (packet.SensorValue < -100 ||
            packet.SensorValue > 200)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Environmental sensor value is outside the allowed range."
            });
        }

        DeploymentNode deploymentTree =
            validationService.BuildDeploymentHierarchy(
                packet.DeploymentLocation.Trim());

        bool isValid =
            validationService.ValidateDeploymentRecursively(
                deploymentTree);

        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Message = "Invalid deployment hierarchy."
            });
        }

        packet.DeviceMacAddress =
            packet.DeviceMacAddress.Trim();

        packet.DeploymentLocation =
            packet.DeploymentLocation.Trim();

        ingestionService.IngestPacket(packet);

        return Results.Ok(new
        {
            Message =
                "Packet successfully ingested into processing pipeline.",

            OptimizedCount =
                ingestionService.GetTotalOptimizedEnvironmentalRecords(),

            MacAddress =
                packet.DeviceMacAddress
        });
    });

app.MapPost("/api/search-history",
    (SearchHistoryEntry request,
     SearchHistoryService searchHistoryService) =>
    {
        if (string.IsNullOrWhiteSpace(request.SearchArea) ||
            string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return Results.BadRequest(new
            {
                message = "Search area and search term are required."
            });
        }

        if (request.SearchArea.Length > 50 ||
            request.SearchTerm.Length > 100)
        {
            return Results.BadRequest(new
            {
                message = "Search area or search term is too long."
            });
        }

        string area = request.SearchArea.Trim();

        if (area != "Devices" && area != "Command History")
        {
            return Results.BadRequest(new
            {
                message = "Unsupported search area."
            });
        }

        searchHistoryService.RecordSearch(area, request.SearchTerm);

        return Results.Ok(new { message = "Search recorded." });
    });

app.MapGet("/api/search-history",
    (SearchHistoryService searchHistoryService) =>
    {
        return Results.Ok(searchHistoryService.GetHistory());
    });


// POWER TELEMETRY

app.MapPost(
    "/api/telemetry/power",
    (
        TelemetryPacket<int> packet,
        TelemetryIngestionService ingestionService,
        DeploymentValidationService validationService
    ) =>
    {
        if (packet == null)
        {
            return Results.BadRequest(new
            {
                Message = "Telemetry packet is required."
            });
        }

        if (string.IsNullOrWhiteSpace(packet.DeviceMacAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        if (string.IsNullOrWhiteSpace(packet.DeploymentLocation))
        {
            return Results.BadRequest(new
            {
                Message = "Deployment location is required."
            });
        }

        if (packet.SensorValue < 0 ||
            packet.SensorValue > 100000)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Power consumption value is outside the allowed range."
            });
        }

        DeploymentNode deploymentTree =
            validationService.BuildDeploymentHierarchy(
                packet.DeploymentLocation.Trim());

        bool isValid =
            validationService.ValidateDeploymentRecursively(
                deploymentTree);

        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Message = "Invalid deployment hierarchy."
            });
        }

        packet.DeviceMacAddress =
            packet.DeviceMacAddress.Trim();

        packet.DeploymentLocation =
            packet.DeploymentLocation.Trim();

        ingestionService.IngestPacket(packet);

        return Results.Ok(new
        {
            Message =
                "Power load packet successfully ingested.",

            OptimizedCount =
                ingestionService.GetTotalOptimizedPowerRecords(),

            MacAddress =
                packet.DeviceMacAddress
        });
    });


// ACTUATOR TELEMETRY

app.MapPost(
    "/api/telemetry/actuator",
    (
        TelemetryPacket<bool> packet,
        TelemetryIngestionService ingestionService,
        DeploymentValidationService validationService
    ) =>
    {
        if (packet == null)
        {
            return Results.BadRequest(new
            {
                Message = "Telemetry packet is required."
            });
        }

        if (string.IsNullOrWhiteSpace(packet.DeviceMacAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        if (string.IsNullOrWhiteSpace(packet.DeploymentLocation))
        {
            return Results.BadRequest(new
            {
                Message = "Deployment location is required."
            });
        }

        DeploymentNode deploymentTree =
            validationService.BuildDeploymentHierarchy(
                packet.DeploymentLocation.Trim());

        bool isValid =
            validationService.ValidateDeploymentRecursively(
                deploymentTree);

        if (!isValid)
        {
            return Results.BadRequest(new
            {
                Message = "Invalid deployment hierarchy."
            });
        }

        packet.DeviceMacAddress =
            packet.DeviceMacAddress.Trim();

        packet.DeploymentLocation =
            packet.DeploymentLocation.Trim();

        ingestionService.IngestPacket(packet);

        return Results.Ok(new
        {
            Message =
                "Actuator state packet successfully ingested.",

            OptimizedCount =
                ingestionService.GetTotalOptimizedActuatorRecords(),

            MacAddress =
                packet.DeviceMacAddress
        });
    });


// TELEMETRY RETRIEVAL

app.MapGet(
    "/api/telemetry/environmental",
    (TelemetryIngestionService ingestionService) =>
    {
        return Results.Ok(
            ingestionService.GetEnvironmentalData());
    });

app.MapGet(
    "/api/telemetry/power",
    (TelemetryIngestionService ingestionService) =>
    {
        return Results.Ok(
            ingestionService.GetPowerData());
    });

app.MapGet(
    "/api/telemetry/actuator",
    (TelemetryIngestionService ingestionService) =>
    {
        return Results.Ok(
            ingestionService.GetActuatorData());
    });


// TIMESTAMPED TELEMETRY HISTORY

app.MapGet(
    "/api/telemetry/environmental/history",
    (TelemetryIngestionService ingestionService) =>
    {
        return Results.Ok(
            ingestionService.GetEnvironmentalTimestampedLogs());
    });

app.MapGet(
    "/api/telemetry/power/history",
    (TelemetryIngestionService ingestionService) =>
    {
        return Results.Ok(
            ingestionService.GetPowerTimestampedLogs());
    });

app.MapGet(
    "/api/telemetry/actuator/history",
    (TelemetryIngestionService ingestionService) =>
    {
        return Results.Ok(
            ingestionService.GetActuatorTimestampedLogs());
    });


// ALERTS

app.MapGet(
    "/api/alerts",
    (CriticalAlertService alertService) =>
    {
        return Results.Ok(alertService.GetAlerts());
    });

app.MapGet(
    "/api/alerts/active",
    (CriticalAlertService alertService) =>
    {
        return Results.Ok(alertService.GetActiveErrors());
    });

app.MapGet(
    "/api/alerts/active/count",
    (CriticalAlertService alertService) =>
    {
        return Results.Ok(new
        {
            ActiveErrorCount = alertService.GetActiveErrorCount()
        });
    });


// SENSOR REGISTRATION

app.MapPost(
    "/api/sensors",
    (SensorProfile sensor, SensorService sensorService) =>
    {
        if (sensor == null)
        {
            return Results.BadRequest(new
            {
                Message = "Sensor data is required."
            });
        }

        if (string.IsNullOrWhiteSpace(sensor.DeviceMacAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        string macAddress =
            sensor.DeviceMacAddress.Trim();

        if (macAddress.Length > 17)
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is invalid."
            });
        }

        if (string.IsNullOrWhiteSpace(sensor.DeploymentLocation))
        {
            return Results.BadRequest(new
            {
                Message = "Deployment location is required."
            });
        }

        string deploymentLocation =
            sensor.DeploymentLocation.Trim();

        if (deploymentLocation.Length > 200)
        {
            return Results.BadRequest(new
            {
                Message = "Deployment location is too long."
            });
        }

        if (!Enum.IsDefined(sensor.Category))
        {
            return Results.BadRequest(new
            {
                Message = "A valid sensor category is required."
            });
        }

        SensorProfile? existingSensor =
            sensorService.GetSensorByMacAddress(macAddress);

        if (existingSensor != null)
        {
            return Results.Conflict(new
            {
                Message =
                    "A sensor with this MAC address is already registered."
            });
        }

        sensor.DeviceMacAddress = macAddress;
        sensor.DeploymentLocation = deploymentLocation;

        SensorProfile registeredSensor =
            sensorService.RegisterSensor(sensor);

        return Results.Ok(new
        {
            Message = "Sensor successfully registered.",
            Sensor = registeredSensor
        });
    });


// SENSOR RETRIEVAL

app.MapGet(
    "/api/sensors",
    (SensorService sensorService) =>
    {
        return Results.Ok(sensorService.GetAllSensors());
    });


// COMMAND EXECUTION

app.MapPost(
    "/api/commands",
    (
        CommandRequest request,
        SensorService sensorService,
        CommandService commandService,
        PredictiveRecommendationService recommendationService
    ) =>
    {
        if (request == null)
        {
            return Results.BadRequest(new
            {
                Message = "Command request is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.DeviceMacAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        if (string.IsNullOrWhiteSpace(request.Command))
        {
            return Results.BadRequest(new
            {
                Message = "Command must be ON or OFF."
            });
        }

        string macAddress = request.DeviceMacAddress.Trim();
        string command = request.Command.Trim().ToUpperInvariant();

        if (command != "ON" && command != "OFF")
        {
            return Results.BadRequest(new
            {
                Message = "Only ON and OFF commands are supported."
            });
        }

        SensorProfile? sensor =
            sensorService.GetSensorByMacAddress(macAddress);

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                Message = "The specified device is not registered."
            });
        }

        if (sensor.Category != SensorCategory.Actuator)
        {
            return Results.BadRequest(new
            {
                Message =
                    "Commands can only be sent to registered actuator devices."
            });
        }

        CommandHistoryEntry entry =
            commandService.ExecuteCommand(
                sensor.DeviceMacAddress,
                command == "ON");

        // Record the actual manual command for predictive analysis.
        recommendationService.RecordManualCommand(
            sensor.DeviceMacAddress,
            command,
            entry.Timestamp);

        return Results.Ok(new
        {
            Message =
                $"Simulated {command} command recorded successfully.",

            Command = entry
        });
    });


// RECOMMENDATIONS

app.MapGet(
    "/api/recommendations",
    (PredictiveRecommendationService recommendationService) =>
    {
        return Results.Ok(
            recommendationService.GetRecommendations());
    });


// COMMAND HISTORY

app.MapGet(
    "/api/commands",
    (CommandService commandService) =>
    {
        return Results.Ok(commandService.GetCommandHistory());
    });


// SIMULATED DEVICE STATE

app.MapGet(
    "/api/commands/{macAddress}/state",
    (
        string macAddress,
        SensorService sensorService,
        CommandService commandService
    ) =>
    {
        SensorProfile? sensor =
            sensorService.GetSensorByMacAddress(macAddress);

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                Message = "The specified device is not registered."
            });
        }

        if (sensor.Category != SensorCategory.Actuator)
        {
            return Results.BadRequest(new
            {
                Message = "The specified device is not an actuator."
            });
        }

        bool? state =
            commandService.GetDeviceState(macAddress);

        return Results.Ok(new
        {
            DeviceMacAddress = macAddress,
            IsOn = state,
            State = state.HasValue
                ? (state.Value ? "ON" : "OFF")
                : "Unknown - no command issued yet"
        });
    });


// UNDO LAST COMMAND

app.MapPost(
    "/api/commands/undo",
    (
        CommandService commandService,
        PredictiveRecommendationService recommendationService
    ) =>
    {
        CommandHistoryEntry? entry =
            commandService.UndoLastCommand();

        if (entry == null)
        {
            return Results.BadRequest(new
            {
                Message = "There are no commands available to undo."
            });
        }

        // Record the undo event as part of the operator's command history.
        recommendationService.RecordManualCommand(
            entry.DeviceMacAddress,
            entry.Command,
            entry.Timestamp);

        return Results.Ok(new
        {
            Message = "The last command was undone successfully.",
            Command = entry
        });
    });


// MEDIA / LOG FILE ATTACHMENT

app.MapPost(
    "/api/sensors/{macAddress}/attachment",
    async (
        string macAddress,
        IFormFile file,
        SensorService sensorService,
        AttachmentEncryptionService encryptionService) =>
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        if (file == null || file.Length == 0)
        {
            return Results.BadRequest(new
            {
                Message = "A valid attachment file is required."
            });
        }

        SensorProfile? sensor =
            sensorService.GetSensorByMacAddress(macAddress.Trim());

        if (sensor == null)
        {
            return Results.NotFound(new
            {
                Message =
                    "Sensor with the specified MAC address was not found."
            });
        }

        string attachmentsFolder =
            Path.Combine(
                AppContext.BaseDirectory,
                "Attachments");

        Directory.CreateDirectory(attachmentsFolder);

        string safeFileName =
            Path.GetFileName(file.FileName);

        string savedFilePath =
            Path.Combine(
                attachmentsFolder,
                $"{macAddress.Replace(":", "_")}_{safeFileName}");

        await using FileStream stream =
            new FileStream(
                savedFilePath,
                FileMode.Create);

        await encryptionService.EncryptFileAsync(
            file.OpenReadStream(),
            stream);

        sensor.AttachmentFileName = safeFileName;
        sensor.AttachmentFilePath = savedFilePath;

        return Results.Ok(new
        {
            Message = "Attachment successfully uploaded.",
            FileName = safeFileName,
            FilePath = savedFilePath,
            Sensor = sensor
        });
    })
    .DisableAntiforgery();

app.Run();
