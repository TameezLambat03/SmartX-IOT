using System;

namespace SmartX.Shared.Models;
//The T is a generic type parameter. It allows the same TelemetryPacket class to work with different telemetry types.
public class TelemetryPacket<T>
{
    public string DeviceMacAddress { get; set; }
    public string DeploymentLocation { get; set; }
    public DateTime Timestamp { get; set; }
    public T SensorValue { get; set; }

    public TelemetryPacket(
        string deviceMacAddress,
        string deploymentLocation,
        T sensorValue)
    {
        DeviceMacAddress = deviceMacAddress;
        DeploymentLocation = deploymentLocation;
        Timestamp = DateTime.UtcNow;
        SensorValue = sensorValue;
    }

    public static TelemetryPacket<T> operator +(
        TelemetryPacket<T> a,
        TelemetryPacket<T> b)
    {
        if (a == null)
        {
            throw new ArgumentNullException(nameof(a));
        }

        if (b == null)
        {
            throw new ArgumentNullException(nameof(b));
        }

        T aggregatedValue;
        //overloading
        if (typeof(T) == typeof(float))
        {
            float result =
                Convert.ToSingle(a.SensorValue) +
                Convert.ToSingle(b.SensorValue);

            aggregatedValue = (T)(object)result;
        }
        else if (typeof(T) == typeof(int))
        {
            int result =
                Convert.ToInt32(a.SensorValue) +
                Convert.ToInt32(b.SensorValue);

            aggregatedValue = (T)(object)result;
        }
        else
        {
            throw new InvalidOperationException(
                $"Operator '+' is not supported for telemetry type {typeof(T).Name}.");
        }

        return new TelemetryPacket<T>(
            a.DeviceMacAddress + "_aggregated",
            a.DeploymentLocation,
            aggregatedValue);
    }
}