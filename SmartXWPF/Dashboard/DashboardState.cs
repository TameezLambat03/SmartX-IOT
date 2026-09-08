using SmartX.Shared.Models;

namespace SmartXWPF.Dashboard;

public class DashboardState
{
    public SensorProfile? SelectedSensor { get; set; }

    public int PacketCount { get; private set; }

    public void IncrementPacketCount()
    {
        PacketCount++;
    }
}