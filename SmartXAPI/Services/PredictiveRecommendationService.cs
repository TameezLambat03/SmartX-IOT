using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class PredictiveRecommendationService
{
    private const int MaxReadingsPerDevice = 20;
    private const int MaxRecommendations = 100;
    private const int MaxCommandEvents = 200;
    private const float LowReadingThreshold = 20.0f;

    private readonly List<PredictiveRecommendation> _recommendations = new();

    private readonly Dictionary<string, List<ReadingRecord>>
        _readingHistory =
            new(StringComparer.OrdinalIgnoreCase);

    private readonly List<CommandRecord> _commandHistory = new();

    private readonly object _lock = new();

    public void AnalyseEnvironmentalReading(
        string macAddress,
        string location,
        DateTime timestamp,
        float sensorValue)
    {
        lock (_lock)
        {
            if (!_readingHistory.TryGetValue(
                    macAddress,
                    out List<ReadingRecord>? readings))
            {
                readings = new List<ReadingRecord>();
                _readingHistory[macAddress] = readings;
            }

            readings.Add(new ReadingRecord
            {
                Timestamp = timestamp,
                Value = sensorValue
            });

            if (readings.Count > MaxReadingsPerDevice)
            {
                readings.RemoveAt(0);
            }

            // Retain the existing threshold-based recommendations.
            AddThresholdRecommendation(
                macAddress,
                location,
                timestamp,
                sensorValue);

            // Look for a recurring decline across recent readings.
            AddDecliningTrendRecommendation(
                macAddress,
                location,
                timestamp,
                sensorValue,
                readings);
        }
    }

    public void RecordManualCommand(
        string macAddress,
        string command,
        DateTime timestamp)
    {
        lock (_lock)
        {
            _commandHistory.Add(new CommandRecord
            {
                DeviceMacAddress = macAddress,
                Command = command,
                Timestamp = timestamp
            });

            if (_commandHistory.Count > MaxCommandEvents)
            {
                _commandHistory.RemoveAt(0);
            }
        }
    }

    private void AddThresholdRecommendation(
        string macAddress,
        string location,
        DateTime timestamp,
        float sensorValue)
    {
        string severity;
        string recommendation;
        string action;

        if (sensorValue < 10.0f)
        {
            severity = "Critical";
            recommendation =
                "The environmental reading is critically low. Immediate investigation is recommended.";
            action =
                "Inspect the sensor and check the area's environmental controls immediately.";
        }
        else if (sensorValue < 15.0f)
        {
            severity = "High";
            recommendation =
                "The environmental reading is very low. Conditions may be deteriorating.";
            action =
                "Check the affected area's conditions and verify the sensor reading.";
        }
        else if (sensorValue < LowReadingThreshold)
        {
            severity = "Warning";
            recommendation =
                "The environmental reading is below the configured safe threshold.";
            action =
                "Monitor subsequent readings and inspect the area if the value continues to fall.";
        }
        else
        {
            return;
        }

        AddRecommendation(new PredictiveRecommendation
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = severity,
            Recommendation = recommendation,
            RecommendedAction = action
        });
    }

    private void AddDecliningTrendRecommendation(
        string macAddress,
        string location,
        DateTime timestamp,
        float sensorValue,
        List<ReadingRecord> readings)
    {
        // Require at least three readings before analysing a trend.
        if (readings.Count < 3)
        {
            return;
        }

        List<ReadingRecord> recentReadings =
            readings.TakeLast(3).ToList();

        bool consistentlyDeclining =
            recentReadings[0].Value > recentReadings[1].Value &&
            recentReadings[1].Value > recentReadings[2].Value;

        if (!consistentlyDeclining)
        {
            return;
        }

        float totalDrop =
            recentReadings[0].Value - recentReadings[2].Value;

        // Ignore negligible changes that may be normal sensor variation.
        if (totalDrop < 2.0f)
        {
            return;
        }

        // Avoid repeatedly creating the same trend recommendation
        // for the same device until the previous one is no longer current.

        PredictiveRecommendation? existingTrendRecommendation =
            _recommendations
                .Where(item =>
                    item.DeviceMacAddress.Equals(
                        macAddress,
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Recommendation.Contains(
                        "Repeated decline",
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Timestamp)
                .FirstOrDefault();

        if(existingTrendRecommendation != null &&
    timestamp >= existingTrendRecommendation.Timestamp &&
    timestamp - existingTrendRecommendation.Timestamp
        <= TimeSpan.FromMinutes(5))
        {
            // Update the existing trend recommendation instead of
            // adding another one for the same device and recent period.
            existingTrendRecommendation.Timestamp = timestamp;
            existingTrendRecommendation.SensorValue = sensorValue;
            existingTrendRecommendation.DeploymentLocation = location;

            existingTrendRecommendation.Severity =
                sensorValue < 10.0f ? "Critical" :
                sensorValue < 15.0f ? "High" :
                "Warning";

            existingTrendRecommendation.Recommendation =
                $"Repeated decline detected: the last three readings fell by {totalDrop:F1} units.";

            existingTrendRecommendation.RecommendedAction =
                "Investigate the cause of the continuing decline and check the environmental controls before conditions worsen.";

            return;
        }


        string severity =
            sensorValue < 10.0f ? "Critical" :
            sensorValue < 15.0f ? "High" :
            "Warning";

        AddRecommendation(new PredictiveRecommendation
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = severity,
            Recommendation =
                $"Repeated decline detected: the last three readings fell by {totalDrop:F1} units.",
            RecommendedAction =
                "Investigate the cause of the continuing decline and check the environmental controls before conditions worsen."
        });
    }

    public List<PredictiveRecommendation> GetRecommendations()
    {
        lock (_lock)
        {
            return _recommendations
                .OrderByDescending(item => GetSeverityRank(item.Severity))
                .ThenByDescending(item => item.Timestamp)
                .ToList();
        }
    }

    private void AddRecommendation(
        PredictiveRecommendation recommendation)
    {
        _recommendations.Add(recommendation);

        if (_recommendations.Count > MaxRecommendations)
        {
            _recommendations.RemoveAt(0);
        }
    }

    private static int GetSeverityRank(string severity)
    {
        return severity switch
        {
            "Critical" => 3,
            "High" => 2,
            "Warning" => 1,
            _ => 0
        };
    }

    private sealed class ReadingRecord
    {
        public DateTime Timestamp { get; init; }
        public float Value { get; init; }
    }

    private sealed class CommandRecord
    {
        public string DeviceMacAddress { get; init; } = string.Empty;
        public string Command { get; init; } = string.Empty;
        public DateTime Timestamp { get; init; }
    }
}