
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;

namespace SmartXAPI.Services;

public class CommandService
{
    private readonly Dictionary<string, bool> _deviceStates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<CommandHistoryEntry> _commandHistory = new();

    private readonly object _lock = new();

    public CommandHistoryEntry ExecuteCommand(
        string macAddress,
        bool turnOn)
    {
        lock (_lock)
        {
            _deviceStates[macAddress] = turnOn;

            CommandHistoryEntry entry = new()
            {
                DeviceMacAddress = macAddress,
                Command = turnOn ? "ON" : "OFF",
                Timestamp = DateTime.UtcNow,
                Status = "Simulated - recorded by API"
            };

            _commandHistory.Add(entry);

            return entry;
        }
    }

    public List<CommandHistoryEntry> GetCommandHistory()
    {
        lock (_lock)
        {
            return new List<CommandHistoryEntry>(_commandHistory);
        }
    }

    public bool? GetDeviceState(string macAddress)
    {
        lock (_lock)
        {
            return _deviceStates.TryGetValue(
                macAddress,
                out bool state)
                    ? state
                    : null;
        }
    }
}
