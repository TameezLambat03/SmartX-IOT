namespace SmartXWPF.Services;

public class AnomalyResult
{
    public bool IsAnomalous { get; }
    public string Status { get; }
    public string Message { get; }

    public AnomalyResult(
        bool isAnomalous,
        string status,
        string message)
    {
        IsAnomalous = isAnomalous;
        Status = status;
        Message = message;
    }
}

public class AnomalyDetector
{
    public AnomalyResult EvaluateEnvironmental(float value)
    {
        if (value > 85.0f || value < 10.0f)
        {
            return new AnomalyResult(
                true,
                "STATUS: ANOMALOUS SPIKE DETECTED",
                $"Threshold exceeded: {value} units!");
        }

        return new AnomalyResult(
            false,
            "STATUS: NORMAL / OPTIMAL",
            "Telemetry within expected baseline");
    }

    public AnomalyResult EvaluatePower(int value)
    {
        if (value > 8000)
        {
            return new AnomalyResult(
                true,
                "STATUS: HIGH POWER CONSUMPTION",
                $"Power consumption above threshold: {value} units");
        }

        return new AnomalyResult(
            false,
            "STATUS: NORMAL / OPTIMAL",
            "Power consumption within expected baseline");
    }
}