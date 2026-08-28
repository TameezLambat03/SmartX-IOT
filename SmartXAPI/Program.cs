using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Shared.Models;
using SmartXAPI.Services;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<TelemetryIngestionService>();

var app = builder.Build();

app.MapPost("/api/telemetry/environmental", (TelemetryPacket<float> packet, TelemetryIngestionService ingestionService) =>
{
    ingestionService.IngestPacket(packet);

    return Results.Ok(new
    {
        Message = "Packet successfully ingested into processing pipeline.",
        OptimizedCount = ingestionService.GetTotalOptimizedRecords(),
        MacAddress = packet.DeviceMacAddress
    });
});


app.MapGet("/api/telemetry/environmental", (TelemetryIngestionService ingestionService) =>
{
    return Results.Ok(ingestionService.GetOptimizedData());
});
app.MapPost("/api/telemetry/power", (TelemetryPacket<int> packet) =>
{
    // Simulating integer processing
    return Results.Ok(new { Message = "Power load packet ingested.", MacAddress = packet.DeviceMacAddress });
});

app.MapPost("/api/telemetry/actuator", (TelemetryPacket<bool> packet) =>
{
    // Simulating boolean switch processing
    return Results.Ok(new { Message = "Actuator state packet ingested.", MacAddress = packet.DeviceMacAddress });
});
app.Run();

