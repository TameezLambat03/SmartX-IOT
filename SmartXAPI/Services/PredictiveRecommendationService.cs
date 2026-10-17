
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
    private const int RequiredRepeatedCommands = 3;

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

            AddThresholdRecommendation(
                macAddress,
                location,
                timestamp,
                sensorValue);

            AddDecliningTrendRecommendation(
                macAddress,
                location,
                timestamp,
                sensorValue,
                readings);

            AddCommandPatternRecommendation(
                macAddress,
                location,
                timestamp,
                sensorValue);
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
                DeviceMacAddress = macAddress.Trim(),
                Command = command.Trim().ToUpperInvariant(),
                Timestamp = timestamp,
                RecordedAtUtc = DateTime.UtcNow
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

        if (totalDrop < 2.0f)
        {
            return;
        }

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

        if (existingTrendRecommendation != null &&
            timestamp >= existingTrendRecommendation.Timestamp &&
            timestamp - existingTrendRecommendation.Timestamp
                <= TimeSpan.FromMinutes(5))
        {
            existingTrendRecommendation.Timestamp = timestamp;
            existingTrendRecommendation.SensorValue = sensorValue;
            existingTrendRecommendation.DeploymentLocation = location;
            existingTrendRecommendation.Severity =
                GetSeverity(sensorValue);

            existingTrendRecommendation.Recommendation =
                $"Repeated decline detected: the last three readings fell by {totalDrop:F1} units.";

            existingTrendRecommendation.RecommendedAction =
                "Investigate the cause of the continuing decline and check the environmental controls before conditions worsen.";

            return;
        }

        AddRecommendation(new PredictiveRecommendation
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = GetSeverity(sensorValue),
            Recommendation =
                $"Repeated decline detected: the last three readings fell by {totalDrop:F1} units.",
            RecommendedAction =
                "Investigate the cause of the continuing decline and check the environmental controls before conditions worsen."
        });
    }

    private void AddCommandPatternRecommendation(
        string macAddress,
        string location,
        DateTime timestamp,
        float sensorValue)
    {
        if (sensorValue >= LowReadingThreshold)
        {
            return;
        }

        // Use server-side recording times rather than telemetry timestamps.
        // This prevents an older packet timestamp from excluding commands
        // that were actually recorded before the reading was processed.
        DateTime now = DateTime.UtcNow;

        List<CommandRecord> recentCommands =
            _commandHistory
                .Where(item =>
                    (item.Command == "ON" || item.Command == "OFF") &&
                    item.RecordedAtUtc <= now &&
                    now - item.RecordedAtUtc <= TimeSpan.FromMinutes(30))
                .ToList();

        // Evaluate each actuator separately so commands from different
        // actuators are never combined to satisfy the repetition threshold.
        var matchingPattern = recentCommands
            .GroupBy(
                item => item.DeviceMacAddress,
                StringComparer.OrdinalIgnoreCase)
            .Select(group =>
            {
                List<CommandRecord> commands =
                    group.OrderByDescending(
                        item => item.RecordedAtUtc).ToList();

                string latestCommand = commands[0].Command;

                int matchingCount = commands.Count(
                    item => item.Command == latestCommand);

                return new
                {
                    DeviceMacAddress = group.Key,
                    Command = latestCommand,
                    Count = matchingCount,
                    LatestTime = commands[0].RecordedAtUtc
                };
            })
            .Where(item => item.Count >= RequiredRepeatedCommands)
            .OrderByDescending(item => item.Count)
            .ThenByDescending(item => item.LatestTime)
            .FirstOrDefault();

        if (matchingPattern == null)
        {
            return;
        }

        string recommendationText =
            $"Repeated actuator commands detected: {matchingPattern.Count} recent {matchingPattern.Command} commands were recorded for actuator {matchingPattern.DeviceMacAddress} while monitoring a low environmental reading.";

        string actionText =
            "Review the actuator command history and environmental readings to determine whether the current operating procedure is addressing the low reading.";

        PredictiveRecommendation? existingRecommendation =
            _recommendations
                .Where(item =>
                    item.DeviceMacAddress.Equals(
                        macAddress,
                        StringComparison.OrdinalIgnoreCase) &&
                    item.Recommendation.Contains(
                        "Repeated actuator commands",
                        StringComparison.OrdinalIgnoreCase))
                .OrderByDescending(item => item.Timestamp)
                .FirstOrDefault();

        if (existingRecommendation != null &&
            timestamp >= existingRecommendation.Timestamp &&
            timestamp - existingRecommendation.Timestamp
                <= TimeSpan.FromMinutes(5))
        {
            existingRecommendation.Timestamp = timestamp;
            existingRecommendation.SensorValue = sensorValue;
            existingRecommendation.DeploymentLocation = location;
            existingRecommendation.Severity = GetSeverity(sensorValue);
            existingRecommendation.Recommendation = recommendationText;
            existingRecommendation.RecommendedAction = actionText;
            return;
        }

        AddRecommendation(new PredictiveRecommendation
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = GetSeverity(sensorValue),
            Recommendation = recommendationText,
            RecommendedAction = actionText
        });
    }

    public void AnalyseRepeatedSearch(
        string searchArea,
        string searchTerm,
        int repeatedCount,
        DateTime timestamp)
    {
        if (repeatedCount != 3)
        {
            return;
        }

        lock (_lock)
        {
            AddRecommendation(new PredictiveRecommendation
            {
                DeviceMacAddress = "N/A",
                DeploymentLocation = searchArea,
                Timestamp = timestamp,
                SensorValue = 0,
                Severity = "Warning",
                Recommendation =
                    $"The search '{searchTerm}' has been repeated " +
                    $"3 times in {searchArea} within 30 minutes.",
                RecommendedAction =
                    "Review this repeated search to determine whether " +
                    "the operator needs a quicker way to access this information."
            });
        }
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

    private static string GetSeverity(float sensorValue)
    {
        return sensorValue < 10.0f ? "Critical" :
               sensorValue < 15.0f ? "High" :
               "Warning";
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
        public DateTime RecordedAtUtc { get; init; }
    }
}
