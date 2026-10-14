
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class PredictiveRecommendationService
{
    private readonly List<PredictiveRecommendation> _recommendations = new();
    private readonly object _lock = new();

    public void AnalyseEnvironmentalReading(
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
        else if (sensorValue < 20.0f)
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

        PredictiveRecommendation item = new()
        {
            DeviceMacAddress = macAddress,
            DeploymentLocation = location,
            Timestamp = timestamp,
            SensorValue = sensorValue,
            Severity = severity,
            Recommendation = recommendation,
            RecommendedAction = action
        };

        lock (_lock)
        {
            _recommendations.Add(item);
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
}
