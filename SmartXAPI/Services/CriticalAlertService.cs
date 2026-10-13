using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class CriticalAlertService
{
    private readonly PriorityQueue<CriticalAlert, int> _alertQueue = new();

    private readonly object _lock = new();

    // Lower numbers represent higher-priority alerts.
    private const int CriticalPriority = 1;
    private const float CriticalEnvironmentalThreshold = 20.0f;

    public void CheckEnvironmentalReading(
        string macAddress,
        string location,
        DateTime timestamp,
        float sensorValue)
    {
        // Generate an alert when an environmental reading falls below 20.
        if (sensorValue >= CriticalEnvironmentalThreshold)
        {
            return;
        }

        CriticalAlert alert = new()
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = "Critical",
            Message = $"Environmental reading is critically low: {sensorValue}"
        };

        lock (_lock)
        {
            _alertQueue.Enqueue(alert, CriticalPriority);
        }
    }

    public List<CriticalAlert> GetAlerts()
    {
        lock (_lock)
        {
            // Return alerts in priority order without removing them
            // from the actual queue.
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