
using SmartX.Shared.Enums;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;

namespace SmartXAPI.Services;

public class CommandService
{
    private readonly Dictionary<string, DeviceSimulationState> _deviceStates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<CommandHistoryEntry> _commandHistory = new();
    private readonly Stack<UndoEntry> _undoStack = new();
    private readonly object _lock = new();

    /// <summary>
    /// Defines which simulated commands are supported for each device category.
    /// </summary>
    public static bool SupportsCommand(SensorCategory category, string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return false;

        string normalizedCommand = command.Trim().ToUpperInvariant();

        return category switch
        {
            SensorCategory.Actuator =>
                normalizedCommand is "ON" or "OFF",

            SensorCategory.Environmental =>
                normalizedCommand is "READ" or "CALIBRATE" or "RESET",

            SensorCategory.PowerConsumption =>
                normalizedCommand is "READ"
                    or "RESET"
                    or "START_MONITORING"
                    or "STOP_MONITORING",

            _ => false
        };
    }

    /// <summary>
    /// Executes a category-specific simulated command.
    /// The optional status allows the API to include the result of a READ.
    /// </summary>
    public CommandHistoryEntry ExecuteCommand(
        string macAddress,
        SensorCategory category,
        string command,
        string? status = null)
    {
        if (string.IsNullOrWhiteSpace(macAddress))
            throw new ArgumentException(
                "A device MAC address is required.", nameof(macAddress));

        string normalizedMac = macAddress.Trim();

        if (string.IsNullOrWhiteSpace(command))
            throw new ArgumentException(
                "A command is required.", nameof(command));

        string normalizedCommand = command.Trim().ToUpperInvariant();

        if (!SupportsCommand(category, normalizedCommand))
            throw new ArgumentException(
                $"Command '{normalizedCommand}' is not supported for category '{category}'.",
                nameof(command));

        lock (_lock)
        {
            DeviceSimulationState currentState =
                GetOrCreateState(normalizedMac);

            // READ is an event, not a state-changing operation, so it
            // deliberately does not create an undo entry.
            if (normalizedCommand != "READ")
            {
                _undoStack.Push(new UndoEntry
                {
                    DeviceMacAddress = normalizedMac,
                    PreviousState = currentState.Clone(),
                    Category = category
                });
            }

            string defaultStatus;

            switch (normalizedCommand)
            {
                case "ON":
                    currentState.IsOn = true;
                    defaultStatus = "Simulated actuator switched ON.";
                    break;

                case "OFF":
                    currentState.IsOn = false;
                    defaultStatus = "Simulated actuator switched OFF.";
                    break;

                case "CALIBRATE":
                    currentState.IsCalibrated = true;
                    defaultStatus = "Simulated environmental calibration completed.";
                    break;

                case "RESET":
                    if (category == SensorCategory.Environmental)
                    {
                        currentState.IsCalibrated = false;
                        defaultStatus =
                            "Simulated environmental settings reset. Telemetry history preserved.";
                    }
                    else
                    {
                        // This is simulation state only. It does not delete
                        // or modify the recorded power telemetry.
                        currentState.MeterReset = true;
                        defaultStatus =
                            "Simulated power meter reset. Telemetry history preserved.";
                    }

                    break;

                case "START_MONITORING":
                    currentState.IsMonitoring = true;
                    defaultStatus = "Simulated power monitoring started.";
                    break;

                case "STOP_MONITORING":
                    currentState.IsMonitoring = false;
                    defaultStatus = "Simulated power monitoring stopped.";
                    break;

                case "READ":
                    defaultStatus =
                        "READ command recorded. No telemetry result was supplied.";
                    break;

                default:
                    // SupportsCommand should prevent reaching this branch.
                    throw new InvalidOperationException(
                        $"Unsupported command '{normalizedCommand}'.");
            }

            CommandHistoryEntry entry = new()
            {
                DeviceMacAddress = normalizedMac,
                Command = normalizedCommand,
                Timestamp = DateTime.UtcNow,
                Status = string.IsNullOrWhiteSpace(status)
                    ? defaultStatus
                    : status
            };

            _commandHistory.Add(entry);
            return entry;
        }
    }

    /// <summary>
    /// Backward-compatible overload for the existing ON/OFF API endpoint.
    /// </summary>
    public CommandHistoryEntry ExecuteCommand(
        string macAddress,
        bool turnOn)
    {
        return ExecuteCommand(
            macAddress,
            SensorCategory.Actuator,
            turnOn ? "ON" : "OFF");
    }

    /// <summary>
    /// Undoes the most recent reversible command.
    /// READ commands are not undoable and do not enter the undo stack.
    /// </summary>
    public CommandHistoryEntry? UndoLastCommand()
    {
        lock (_lock)
        {
            if (_undoStack.Count == 0)
                return null;

            UndoEntry undoEntry = _undoStack.Pop();

            _deviceStates[undoEntry.DeviceMacAddress] =
                undoEntry.PreviousState.Clone();

            string restoredState = DescribeState(
                undoEntry.Category,
                undoEntry.PreviousState);

            CommandHistoryEntry entry = new()
            {
                DeviceMacAddress = undoEntry.DeviceMacAddress,
                Command = $"UNDO → {restoredState}",
                Timestamp = DateTime.UtcNow,
                Status = "Previous simulated device state restored."
            };

            _commandHistory.Add(entry);
            return entry;
        }
    }

    public bool CanUndo()
    {
        lock (_lock)
        {
            return _undoStack.Count > 0;
        }
    }

    public List<CommandHistoryEntry> GetCommandHistory()
    {
        lock (_lock)
        {
            return new List<CommandHistoryEntry>(_commandHistory);
        }
    }

    /// <summary>
    /// Preserved for the existing actuator state endpoint.
    /// Returns null when no ON/OFF state has been recorded.
    /// </summary>
    public bool? GetDeviceState(string macAddress)
    {
        lock (_lock)
        {
            if (!_deviceStates.TryGetValue(macAddress, out DeviceSimulationState? state))
                return null;

            return state.IsOn;
        }
    }

    /// <summary>
    /// Returns a readable snapshot of the simulated state for a device.
    /// </summary>
    public string GetDeviceStatus(
        string macAddress,
        SensorCategory category)
    {
        lock (_lock)
        {
            if (!_deviceStates.TryGetValue(macAddress, out DeviceSimulationState? state))
                return "No simulated commands issued yet.";

            return DescribeState(category, state);
        }
    }

    private DeviceSimulationState GetOrCreateState(string macAddress)
    {
        if (!_deviceStates.TryGetValue(macAddress, out DeviceSimulationState? state))
        {
            state = new DeviceSimulationState();
            _deviceStates[macAddress] = state;
        }

        return state;
    }

    private static string DescribeState(
        SensorCategory category,
        DeviceSimulationState state)
    {
        return category switch
        {
            SensorCategory.Actuator =>
                state.IsOn.HasValue
                    ? (state.IsOn.Value ? "ON" : "OFF")
                    : "UNKNOWN",

            SensorCategory.Environmental =>
                state.IsCalibrated ? "CALIBRATED" : "NOT CALIBRATED",

            SensorCategory.PowerConsumption =>
                $"Monitoring: {(state.IsMonitoring ? "ON" : "OFF")}; " +
                $"Meter reset: {(state.MeterReset ? "YES" : "NO")}",

            _ => "UNKNOWN"
        };
    }

    private sealed class DeviceSimulationState
    {
        public bool? IsOn { get; set; }
        public bool IsCalibrated { get; set; }
        public bool IsMonitoring { get; set; }
        public bool MeterReset { get; set; }

        public DeviceSimulationState Clone()
        {
            return new DeviceSimulationState
            {
                IsOn = IsOn,
                IsCalibrated = IsCalibrated,
                IsMonitoring = IsMonitoring,
                MeterReset = MeterReset
            };
        }
    }

    private sealed class UndoEntry
    {
        public string DeviceMacAddress { get; init; } = string.Empty;
        public DeviceSimulationState PreviousState { get; init; } = new();
        public SensorCategory Category { get; init; }
    }
}
