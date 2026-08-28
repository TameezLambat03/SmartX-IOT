
using SmartX.Shared.Models;
using SmartXAPI.Services;

var builder = WebApplication.CreateBuilder(args);

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
                ingestionService.GetTotalOptimizedRecords(),
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

