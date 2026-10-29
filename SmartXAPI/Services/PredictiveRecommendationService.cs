
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
        // Reject invalid input before it enters the prediction history.
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException(
                "A device MAC address is required.", nameof(macAddress));

        if (macAddress.Length > 64)
            throw new ArgumentException(
                "The device identifier is too long.", nameof(macAddress));

        if (string.IsNullOrWhiteSpace(location))
            throw new ArgumentException(
                "A deployment location is required.", nameof(location));

        if (location.Length > 200)
            throw new ArgumentException(
                "The deployment location is too long.", nameof(location));

        if (!float.IsFinite(sensorValue))
            throw new ArgumentOutOfRangeException(
                nameof(sensorValue),
                "The sensor reading must be a finite number.");

        if (timestamp == DateTime.MinValue)
            throw new ArgumentException(
                "A valid reading timestamp is required.", nameof(timestamp));

        macAddress = macAddress.Trim();
        location = location.Trim();

        lock (_lock)
        {
            if (!_readingHistory.TryGetValue(
                    macAddress, out List<ReadingRecord>? readings))
            {
                readings = new List<ReadingRecord>();
                _readingHistory[macAddress] = readings;
            }

            // Keep readings in chronological order.
            readings.Add(new ReadingRecord
            {
                Timestamp = timestamp,
                Value = sensorValue
            });

            readings.Sort((a, b) => a.Timestamp.CompareTo(b.Timestamp));

            // Retain only the most recent readings.
            if (readings.Count > MaxReadingsPerDevice)
            {
                readings.RemoveRange(
                    0, readings.Count - MaxReadingsPerDevice);
            }

            AddThresholdRecommendation(
                macAddress, location, timestamp, sensorValue);

            AddDecliningTrendRecommendation(
                macAddress, location, timestamp, sensorValue, readings);

            AddCommandPatternRecommendation(
                macAddress, location, timestamp, sensorValue);
        }
    }


    public void RecordManualCommand(
    string macAddress,
    string command,
    DateTime timestamp)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException(
                "A device MAC address is required.", nameof(macAddress));

        if (macAddress.Length > 64)
            throw new ArgumentException(
                "The device identifier is too long.", nameof(macAddress));

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException(
                "A command is required.", nameof(command));

        string normalisedCommand = command.Trim().ToUpperInvariant();

        if (normalisedCommand is not ("ON" or "OFF"))
            throw new ArgumentException(
                "Only ON and OFF commands can be recorded for pattern analysis.",
                nameof(command));

        if (timestamp == DateTime.MinValue)
            throw new ArgumentException(
                "A valid command timestamp is required.", nameof(timestamp));

        lock (_lock)
        {
            _commandHistory.Add(new CommandRecord
            {
                DeviceMacAddress = macAddress.Trim(),
                Command = normalisedCommand,
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

        // Reuse the recent threshold recommendation for this device
        // when it represents the same severity.
        PredictiveRecommendation? existing = _recommendations
            .Where(item =>
                item.DeviceMacAddress.Equals(
                    macAddress, StringComparison.OrdinalIgnoreCase) &&
                item.Severity == severity &&
                item.Recommendation.Contains(
                    "environmental reading",
                    StringComparison.OrdinalIgnoreCase) &&
                timestamp >= item.Timestamp &&
                (timestamp - item.Timestamp).TotalMinutes <= 5)
            .OrderByDescending(item => item.Timestamp)
            .FirstOrDefault();

        if (existing != null)
        {
            existing.Timestamp = timestamp;
            existing.SensorValue = sensorValue;
            existing.DeploymentLocation = location;
            existing.Recommendation = recommendation;
            existing.RecommendedAction = action;
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
            return;

        // Use the latest three readings in chronological order.
        List<ReadingRecord> recentReadings = readings
            .OrderBy(r => r.Timestamp)
            .TakeLast(3)
            .ToList();

        ReadingRecord first = recentReadings[0];
        ReadingRecord middle = recentReadings[1];
        ReadingRecord last = recentReadings[2];

        // Require distinct, increasing timestamps.
        if (first.Timestamp >= middle.Timestamp ||
            middle.Timestamp >= last.Timestamp)
            return;

        // Confirm that the readings show a declining pattern.
        if (first.Value <= middle.Value ||
            middle.Value <= last.Value)
            return;

        double elapsedMinutes =
            (last.Timestamp - first.Timestamp).TotalMinutes;

        if (elapsedMinutes <= 0)
            return;

        float totalDrop = first.Value - last.Value;

        // Ignore insignificant changes.
        if (totalDrop < 2.0f)
            return;

        double declinePerMinute = totalDrop / elapsedMinutes;

        // Estimate the time until the reading reaches the warning threshold.
        double? minutesUntilThreshold = null;

        if (declinePerMinute > 0 && last.Value > LowReadingThreshold)
        {
            minutesUntilThreshold =
                (last.Value - LowReadingThreshold) / declinePerMinute;
        }

        string trendDescription =
            $"Declining sensor trend: reading dropped from " +
            $"{first.Value:F1} to {last.Value:F1} over " +
            $"{elapsedMinutes:F1} minutes " +
            $"({declinePerMinute:F2} units per minute).";

        string action;

        if (declinePerMinute >= 1.0)
        {
            action =
                "Rapid decline detected. Inspect the sensor and connected " +
                "equipment promptly.";
        }
        else if (declinePerMinute >= 0.2)
        {
            action =
                "Monitor the sensor closely and inspect the equipment " +
                "if the decline continues.";
        }
        else
        {
            action =
                "Continue monitoring readings and investigate if the " +
                "declining trend persists.";
        }

        if (minutesUntilThreshold.HasValue)
        {
            trendDescription +=
                $" At the current rate, the reading could reach " +
                $"{LowReadingThreshold:F1} in approximately " +
                $"{minutesUntilThreshold.Value:F1} minutes.";

            action +=
                " The estimated time is a projection, not a guaranteed forecast.";
        }

        string severity = GetSeverity(sensorValue);

        // Update an existing recent trend recommendation instead of
        // adding another one for every incoming reading.
        PredictiveRecommendation? existing = _recommendations
            .Where(r =>
                r.DeviceMacAddress.Equals(
                    macAddress, StringComparison.OrdinalIgnoreCase) &&
                r.Recommendation.Contains(
                    "Declining sensor trend",
                    StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(r => r.Timestamp)
            .FirstOrDefault();

        if (existing != null &&
            timestamp >= existing.Timestamp &&
            (timestamp - existing.Timestamp).TotalMinutes <= 5)
        {
            existing.Timestamp = timestamp;
            existing.SensorValue = sensorValue;
            existing.DeploymentLocation = location;
            existing.Severity = severity;
            existing.Recommendation = trendDescription;
            existing.RecommendedAction = action;
            return;
        }

        AddRecommendation(new PredictiveRecommendation
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = severity,
            Recommendation = trendDescription,
            RecommendedAction = action
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
            item.DeviceMacAddress.Equals(
                macAddress,
                StringComparison.OrdinalIgnoreCase) &&
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
