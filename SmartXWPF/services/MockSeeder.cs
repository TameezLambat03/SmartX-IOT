using SmartX.Shared.Models;
using System;
using System.Collections.Generic;

namespace SmartXWPF.Services;

public class MockTelemetryService
{
    private readonly Random _random = new();

    public IEnumerable<TelemetryPacket<float>> GenerateEnvironmental(
        string mac,
        string deployment,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            float value =
                40.0f +
                (float)(_random.NextDouble() * 30.0);

            yield return new TelemetryPacket<float>(
                mac,
                deployment,
                value);
        }
    }

    public IEnumerable<TelemetryPacket<int>> GeneratePower(
        string mac,
        string deployment,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            int value = _random.Next(200, 1001);

            yield return new TelemetryPacket<int>(
                mac,
                deployment,
                value);
        }
    }

    public IEnumerable<TelemetryPacket<bool>> GenerateActuator(
        string mac,
        string deployment,
        int count)
    {
        for (int i = 0; i < count; i++)
        {
            bool value = _random.Next(0, 2) == 1;

            yield return new TelemetryPacket<bool>(
                mac,
                deployment,
                value);
        }
    }
}