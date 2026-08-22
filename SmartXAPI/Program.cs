using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using SmartX.Shared;
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

app.Run();