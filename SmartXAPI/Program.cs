using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using SmartX.Shared;
using System.Collections.Generic;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapPost("/api/telemetry/environmental", (TelemetryPacket<float> packet) =>
{
    float[][] rawSensorBatches = new float[3][];
    rawSensorBatches[0] = new float[] { packet.SensorValue, packet.SensorValue + 0.5f };
    rawSensorBatches[1] = new float[] { packet.SensorValue - 0.2f };
    rawSensorBatches[2] = new float[] { packet.SensorValue + 0.1f, packet.SensorValue, packet.SensorValue - 0.1f };

    List<float> optimizedCollection = BatchProcessor.ProcessIncomingBatch(rawSensorBatches);

    return Results.Ok(new
    {
        Message = "Packet received and batched successfully",
        OptimizedCount = optimizedCollection.Count,
        MacAddress = packet.DeviceMacAddress
    });
});

app.Run();

public static class BatchProcessor
{
    public static List<float> ProcessIncomingBatch(float[][] jaggedBatches)
    {
        List<float> finalOptimizedList = new List<float>();

        for (int i = 0; i < jaggedBatches.Length; i++)
        {
            for (int j = 0; j < jaggedBatches[i].Length; j++)
            {
                finalOptimizedList.Add(jaggedBatches[i][j]);
            }
        }

        return finalOptimizedList;
    }
}