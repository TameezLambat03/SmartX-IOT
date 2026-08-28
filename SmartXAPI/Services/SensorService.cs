using SmartX.Shared.Models;

namespace SmartXAPI.Services;

public class SensorService
{
    private readonly List<SensorProfile> _sensors = new();

    public SensorProfile RegisterSensor(SensorProfile sensor)
    {
        _sensors.Add(sensor);
        return sensor;
    }

    public List<SensorProfile> GetAllSensors()
    {
        return _sensors;
    }

    public SensorProfile? GetSensorByMacAddress(string macAddress)
    {
        return _sensors.FirstOrDefault(
            sensor => sensor.DeviceMacAddress.Equals(
                macAddress,
                StringComparison.OrdinalIgnoreCase));
    }
}