using SmartX.Shared.Collections;
using SmartX.Shared.Models;

namespace SmartXAPI.Services;

public class SensorService
{
    private readonly SmartCollection<SensorProfile> _sensors = new();

    public SensorProfile RegisterSensor(SensorProfile sensor)
    {
        _sensors.Add(sensor);
        return sensor;
    }

    public IReadOnlyList<SensorProfile> GetAllSensors()
    {
        return _sensors.GetAll();
    }

    public SensorProfile? GetSensorByMacAddress(string macAddress)
    {
        return _sensors.Find(
            sensor => sensor.DeviceMacAddress.Equals(
                macAddress,
                StringComparison.OrdinalIgnoreCase));
    }
}