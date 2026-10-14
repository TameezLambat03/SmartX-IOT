
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;

namespace SmartXAPI.Services;

public class CommandService
{
    private readonly Dictionary<string, bool> _deviceStates =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<CommandHistoryEntry> _commandHistory = new();

    // Stores the previous state for each manual command.
    private readonly Stack<UndoEntry> _undoStack = new();

    private readonly object _lock = new();

    public CommandHistoryEntry ExecuteCommand(
        string macAddress,
        bool turnOn)
    {
        lock (_lock)
        {
            bool? previousState =
                _deviceStates.TryGetValue(macAddress, out bool state)
                    ? state
                    : null;

            _undoStack.Push(new UndoEntry
            {
                DeviceMacAddress = macAddress,
                PreviousState = previousState
            });

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




    public CommandHistoryEntry? UndoLastCommand()
    {
        lock (_lock)
        {
            if (_undoStack.Count == 0)
            {
                return null;
            }

            UndoEntry undoEntry = _undoStack.Pop();

            string undoCommand;

            if (undoEntry.PreviousState.HasValue)
            {
                bool previousState = undoEntry.PreviousState.Value;

                _deviceStates[undoEntry.DeviceMacAddress] =
                    previousState;

                undoCommand = previousState ? "UNDO → ON" : "UNDO → OFF";
            }
            else
            {
                _deviceStates.Remove(undoEntry.DeviceMacAddress);
                undoCommand = "UNDO → UNKNOWN";
            }

            CommandHistoryEntry entry = new()
            {
                DeviceMacAddress = undoEntry.DeviceMacAddress,
                Command = undoCommand,
                Timestamp = DateTime.UtcNow,
                Status = "Undo applied successfully."
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

    private sealed class UndoEntry
    {
        public string DeviceMacAddress { get; init; } = string.Empty;
        public bool? PreviousState { get; init; }
    }

}
