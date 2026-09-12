using SmartX.Shared.Enums;
using SmartX.Shared.Models;

namespace SmartXAPI.Services;

public class StartupSeeder
{
    private const int TelemetryPacketCount = 30;

    private readonly SensorService _sensorService;
    private readonly TelemetryIngestionService _telemetryService;

    public StartupSeeder(
        SensorService sensorService,
        TelemetryIngestionService telemetryService)
    {
        _sensorService = sensorService;
        _telemetryService = telemetryService;
    }

    public void Seed()
    {
        List<SensorProfile> demoSensors =
            CreateDemoSensors();

        foreach (SensorProfile sensor in demoSensors)
        {
            SensorProfile? existingSensor =
                _sensorService.GetSensorByMacAddress(
                    sensor.DeviceMacAddress);

            if (existingSensor != null)
            {
                continue;
            }

            _sensorService.RegisterSensor(sensor);

            SeedTelemetry(sensor);
        }
    }

    private List<SensorProfile> CreateDemoSensors()
    {
        return new List<SensorProfile>
        {
            new SensorProfile(
                "00:1B:44:11:3A:B7",
                "Facility A/Zone 1/Greenhouse A",
                SensorCategory.Environmental),

            new SensorProfile(
                "00:1B:44:11:3A:B8",
                "Facility A/Zone 1/Utility Room",
                SensorCategory.PowerConsumption),

            new SensorProfile(
                "00:1B:44:11:3A:B9",
                "Facility A/Zone 2/Valve Station",
                SensorCategory.Actuator)
        };
    }

    private void SeedTelemetry(
        SensorProfile sensor)
    {
        switch (sensor.Category)
        {
            case SensorCategory.Environmental:

                for (int i = 0;
                     i < TelemetryPacketCount;
                     i++)
                {
                    float value;

                    if (i == 9 || i == 19 || i == 29)
                    {
                        value = 90.0f;
                    }
                    else
                    {
                        value =
                            40.0f +
                            Random.Shared.NextSingle() * 30.0f;
                    }

                    TelemetryPacket<float> packet =
                        new TelemetryPacket<float>(
                            sensor.DeviceMacAddress,
                            sensor.DeploymentLocation,
                            value);

                    _telemetryService.IngestPacket(packet);
                }

                break;


            case SensorCategory.PowerConsumption:

                for (int i = 0;
                     i < TelemetryPacketCount;
                     i++)
                {
                    int value;

                    if (i == 9 || i == 19 || i == 29)
                    {
                        value = 8500;
                    }
                    else
                    {
                        value =
                            Random.Shared.Next(200, 1001);
                    }

                    TelemetryPacket<int> packet =
                        new TelemetryPacket<int>(
                            sensor.DeviceMacAddress,
                            sensor.DeploymentLocation,
                            value);

                    _telemetryService.IngestPacket(packet);
                }

                break;


            case SensorCategory.Actuator:

                for (int i = 0;
                     i < TelemetryPacketCount;
                     i++)
                {
                    bool value =
                        Random.Shared.Next(0, 2) == 1;

                    TelemetryPacket<bool> packet =
                        new TelemetryPacket<bool>(
                            sensor.DeviceMacAddress,
                            sensor.DeploymentLocation,
                            value);

                    _telemetryService.IngestPacket(packet);
                }

                break;
        }
    }
}