using System;

namespace SmartX.Shared.Models;

public class TelemetryPacket<T>
{
    public string DeviceMacAddress { get; set; }
    public string DeploymentLocation { get; set; }
    public DateTime Timestamp { get; set; }
    public T SensorValue { get; set; }

    public TelemetryPacket(string deviceMacAddress, string deploymentLocation, T sensorValue)
    {
        DeviceMacAddress = deviceMacAddress;
        DeploymentLocation = deploymentLocation;
        Timestamp = DateTime.UtcNow;
        SensorValue = sensorValue;
    }

    public static TelemetryPacket<T> operator +(TelemetryPacket<T> a, TelemetryPacket<T> b)
    {
        dynamic val1 = a.SensorValue;
        dynamic val2 = b.SensorValue;

        T aggregatedValue = val1 + val2;

        return new TelemetryPacket<T>(
            a.DeviceMacAddress + "_aggregated",
            a.DeploymentLocation,
            aggregatedValue
        );
    }
}
