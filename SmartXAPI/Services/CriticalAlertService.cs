
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class CriticalAlertService
{
    private readonly PriorityQueue<CriticalAlert, int> _alertQueue = new();

    // Tracks devices with active environmental alerts.//hashtrings store unique errors 
    private readonly HashSet<string> _activeErrors =
        new(StringComparer.OrdinalIgnoreCase);

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
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return;
        }

        string normalizedMacAddress = macAddress.Trim();

        string? severity = null;
        string? message = null;
        int priority = 0;

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

        lock (_lock)
        {
            if (severity == null)
            {
                // A safe reading clears the device's active error.
                _activeErrors.Remove(normalizedMacAddress);
                return;
            }

            // HashSet prevents duplicate active-error entries.
            _activeErrors.Add(normalizedMacAddress);

            CriticalAlert alert = new()
            {
                DeviceMacAddress = normalizedMacAddress,
                DeploymentLocation = location,
                Timestamp = timestamp,
                SensorValue = sensorValue,
                Severity = severity,
                Message = message!
            };

            // Preserve every alert in the existing alert history.
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

    // Return a snapshot of unique devices with active errors.
    public List<string> GetActiveErrors()
    {
        lock (_lock)
        {
            return _activeErrors.ToList();
        }
    }

    public bool HasActiveError(string macAddress)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
        {
            return false;
        }

        lock (_lock)
        {
            return _activeErrors.Contains(macAddress.Trim());
        }
    }

    public int GetActiveErrorCount()
    {
        lock (_lock)
        {
            return _activeErrors.Count;
        }
    }
}
