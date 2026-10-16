
using SmartX.Shared.Collections;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class SensorService
{
    // Keeps the existing collection functionality.
    private readonly SmartCollection<SensorProfile> _sensors = new();

    // Dictionary provides direct lookup by MAC address.
    private readonly Dictionary<string, SensorProfile> _sensorsByMacAddress =
        new(StringComparer.OrdinalIgnoreCase);

    public SensorProfile RegisterSensor(SensorProfile sensor)
    {
        _sensors.Add(sensor);
        _sensorsByMacAddress[sensor.DeviceMacAddress] = sensor;

        return sensor;
    }

    public IReadOnlyList<SensorProfile> GetAllSensors()
    {
        return _sensors.GetAll();
    }

    public SensorProfile? GetSensorByMacAddress(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return null;
        }

        return _sensorsByMacAddress.TryGetValue(
            macAddress.Trim(),
            out SensorProfile? sensor)
                ? sensor
                : null;
    }
}
