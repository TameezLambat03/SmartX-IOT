
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class CriticalAlertService
{
    private readonly PriorityQueue<CriticalAlert, int> _alertQueue = new();
    private readonly object _lock = new();

    // Lower numbers mean higher urgency.
    private const int CriticalPriority = 1;
    private const int HighPriority = 2;
    private const int WarningPriority = 3;

    public void CheckEnvironmentalReading(
        string macAddress,
        string location,
        DateTime timestamp,
        float sensorValue)
    {
        string severity;
        string message;
        int priority;

        if (sensorValue < 10.0f)
        {
            severity = "Critical";
            message = $"Environmental reading is critically low: {sensorValue}";
            priority = CriticalPriority;
        }
        else if (sensorValue < 15.0f)
        {
            severity = "High";
            message = $"Environmental reading is very low: {sensorValue}";
            priority = HighPriority;
        }
        else if (sensorValue < 20.0f)
        {
            severity = "Warning";
            message = $"Environmental reading is below the safe threshold: {sensorValue}";
            priority = WarningPriority;
        }
        else
        {
            return;
        }

        CriticalAlert alert = new()
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = severity,
            Message = message
        };

        lock (_lock)
        {
            _alertQueue.Enqueue(alert, priority);
        }
    }

    public List<CriticalAlert> GetAlerts()
    {
        lock (_lock)
        {
            return _alertQueue.UnorderedItems
                .OrderBy(item => item.Priority)
                .ThenBy(item => item.Element.Timestamp)
                .Select(item => item.Element)
                .ToList();
        }
    }

    public int GetAlertCount()
    {
        lock (_lock)
        {
            return _alertQueue.Count;
        }
    }
}
