
using SmartX.Shared.Models;
using SmartXAPI.Services;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Json;

var builder = WebApplication.CreateBuilder(args);
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter());
});

builder.Services.AddSingleton<TelemetryIngestionService>();
builder.Services.AddSingleton<DeploymentValidationService>();
builder.Services.AddSingleton<SensorService>();

var app = builder.Build();

app.MapPost(
    "/api/telemetry/environmental",
    (
        TelemetryPacket<float> packet,
        TelemetryIngestionService ingestionService,
        DeploymentValidationService validationService
    ) =>
    {
        DeploymentNode deploymentTree =
            validationService.BuildDeploymentHierarchy(
                packet.DeploymentLocation);

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

        ingestionService.IngestPacket(packet);

        return Results.Ok(new
        {
            Message = "Packet successfully ingested into processing pipeline.",
            OptimizedCount =
                ingestionService.GetTotalOptimizedEnvironmentalRecords(),
            MacAddress = packet.DeviceMacAddress
        });
    });


app.MapGet("/api/telemetry/environmental", (TelemetryIngestionService ingestionService) =>
{
    return Results.Ok(ingestionService.GetEnvironmentalData());
});
app.MapPost(
    "/api/telemetry/power",
    (
        TelemetryPacket<int> packet,
        TelemetryIngestionService ingestionService
    ) =>
    {
        ingestionService.IngestPacket(packet);

        return Results.Ok(new
        {
            Message = "Power load packet successfully ingested.",
            OptimizedCount =
                ingestionService.GetTotalOptimizedPowerRecords(),
            MacAddress = packet.DeviceMacAddress
        });
    });


app.MapPost(
    "/api/telemetry/actuator",
    (
        TelemetryPacket<bool> packet,
        TelemetryIngestionService ingestionService
    ) =>
    {
        ingestionService.IngestPacket(packet);

        return Results.Ok(new
        {
            Message = "Actuator state packet successfully ingested.",
            OptimizedCount =
                ingestionService.GetTotalOptimizedActuatorRecords(),
            MacAddress = packet.DeviceMacAddress
        });
    });

app.MapPost(
    "/api/sensors",
    (SensorProfile sensor, SensorService sensorService) =>
    {
        if (string.IsNullOrWhiteSpace(sensor.DeviceMacAddress))
        {
            return Results.BadRequest(new
            {
                Message = "Device MAC address is required."
            });
        }

        if (string.IsNullOrWhiteSpace(sensor.DeploymentLocation))
        {
            return Results.BadRequest(new
            {
                Message = "Deployment location is required."
            });
        }

        SensorProfile? existingSensor =
            sensorService.GetSensorByMacAddress(
                sensor.DeviceMacAddress);

        if (existingSensor != null)
        {
            return Results.Conflict(new
            {
                Message = "A sensor with this MAC address is already registered."
            });
        }

        SensorProfile registeredSensor =
            sensorService.RegisterSensor(sensor);

        return Results.Ok(new
        {
            Message = "Sensor successfully registered.",
            Sensor = registeredSensor
        });
    });
app.MapGet(
    "/api/sensors",
    (SensorService sensorService) =>
    {
        return Results.Ok(sensorService.GetAllSensors());
    });


app.Run();

