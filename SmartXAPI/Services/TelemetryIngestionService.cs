
using SmartX.Shared.Collections;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartXAPI.Services;

public class TelemetryIngestionService
{
    private const int BatchSize = 5;
    private const int MaxHistoricalBatches = 10;
    private const int MaxOptimizedRecords = 500;

    private readonly object _environmentalLock = new();
    private readonly object _powerLock = new();
    private readonly object _actuatorLock = new();

    // FIFO queues for incoming telemetry packets.
    private readonly Queue<TelemetryPacket<float>> _environmentalQueue = new();
    private readonly Queue<TelemetryPacket<int>> _powerQueue = new();
    private readonly Queue<TelemetryPacket<bool>> _actuatorQueue = new();

    // Existing environmental storage.
    private readonly SmartCollection<float> _currentEnvironmentalBuffer = new();
    private readonly float[][] _historicalEnvironmentalBatches =
        new float[MaxHistoricalBatches][];
    private int _environmentalBatchIndex;
    private readonly SmartCollection<float> _optimizedEnvironmentalData = new();

    // Existing power storage.
    private readonly SmartCollection<int> _currentPowerBuffer = new();
    private readonly int[][] _historicalPowerBatches =
        new int[MaxHistoricalBatches][];
    private int _powerBatchIndex;
    private readonly SmartCollection<int> _optimizedPowerData = new();

    // Existing actuator storage.
    private readonly SmartCollection<bool> _currentActuatorBuffer = new();
    private readonly bool[][] _historicalActuatorBatches =
        new bool[MaxHistoricalBatches][];
    private int _actuatorBatchIndex;
    private readonly SmartCollection<bool> _optimizedActuatorData = new();

    private void AddWithLimit<T>(SmartCollection<T> collection, T item)
    {
        collection.Add(item);

        if (collection.Count > MaxOptimizedRecords)
        {
            collection.Remove(collection[0]);
        }
    }

    // ENVIRONMENTAL TELEMETRY

    public void IngestPacket(TelemetryPacket<float> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_environmentalLock)
        {
            // Add the incoming packet to the FIFO queue.
            _environmentalQueue.Enqueue(packet);

            // Process queued packets in arrival order.
            while (_environmentalQueue.Count > 0)
            {
                TelemetryPacket<float> nextPacket =
                    _environmentalQueue.Dequeue();

                _currentEnvironmentalBuffer.Add(nextPacket.SensorValue);

                if (_currentEnvironmentalBuffer.Count >= BatchSize)
                {
                    ProcessEnvironmentalBuffer();
                }
            }
        }
    }

    private void ProcessEnvironmentalBuffer()
    {
        float[] completedBatch =
            _currentEnvironmentalBuffer.GetAll().ToArray();

        _historicalEnvironmentalBatches[_environmentalBatchIndex] =
            completedBatch;

        _environmentalBatchIndex =
            (_environmentalBatchIndex + 1) %
            _historicalEnvironmentalBatches.Length;

        foreach (float value in completedBatch)
        {
            AddWithLimit(_optimizedEnvironmentalData, value);
        }

        _currentEnvironmentalBuffer.Clear();
    }

    // POWER TELEMETRY

    public void IngestPacket(TelemetryPacket<int> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_powerLock)
        {
            _powerQueue.Enqueue(packet);

            while (_powerQueue.Count > 0)
            {
                TelemetryPacket<int> nextPacket =
                    _powerQueue.Dequeue();

                _currentPowerBuffer.Add(nextPacket.SensorValue);

                if (_currentPowerBuffer.Count >= BatchSize)
                {
                    ProcessPowerBuffer();
                }
            }
        }
    }

    private void ProcessPowerBuffer()
    {
        int[] completedBatch =
            _currentPowerBuffer.GetAll().ToArray();

        _historicalPowerBatches[_powerBatchIndex] =
            completedBatch;

        _powerBatchIndex =
            (_powerBatchIndex + 1) %
            _historicalPowerBatches.Length;

        foreach (int value in completedBatch)
        {
            AddWithLimit(_optimizedPowerData, value);
        }

        _currentPowerBuffer.Clear();
    }

    // ACTUATOR TELEMETRY

    public void IngestPacket(TelemetryPacket<bool> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_actuatorLock)
        {
            _actuatorQueue.Enqueue(packet);

            while (_actuatorQueue.Count > 0)
            {
                TelemetryPacket<bool> nextPacket =
                    _actuatorQueue.Dequeue();

                _currentActuatorBuffer.Add(nextPacket.SensorValue);

                if (_currentActuatorBuffer.Count >= BatchSize)
                {
                    ProcessActuatorBuffer();
                }
            }
        }
    }

    private void ProcessActuatorBuffer()
    {
        bool[] completedBatch =
            _currentActuatorBuffer.GetAll().ToArray();

        _historicalActuatorBatches[_actuatorBatchIndex] =
            completedBatch;

        _actuatorBatchIndex =
            (_actuatorBatchIndex + 1) %
            _historicalActuatorBatches.Length;

        foreach (bool value in completedBatch)
        {
            AddWithLimit(_optimizedActuatorData, value);
        }

        _currentActuatorBuffer.Clear();
    }

    // RETRIEVAL

    public int GetTotalOptimizedEnvironmentalRecords()
    {
        lock (_environmentalLock)
        {
            return _optimizedEnvironmentalData.Count;
        }
    }

    public int GetTotalOptimizedPowerRecords()
    {
        lock (_powerLock)
        {
            return _optimizedPowerData.Count;
        }
    }

    public int GetTotalOptimizedActuatorRecords()
    {
        lock (_actuatorLock)
        {
            return _optimizedActuatorData.Count;
        }
    }

    public IReadOnlyList<float> GetEnvironmentalData()
    {
        lock (_environmentalLock)
        {
            return _optimizedEnvironmentalData.GetAll().ToList();
        }
    }

    public IReadOnlyList<int> GetPowerData()
    {
        lock (_powerLock)
        {
            return _optimizedPowerData.GetAll().ToList();
        }
    }

    public IReadOnlyList<bool> GetActuatorData()
    {
        lock (_actuatorLock)
        {
            return _optimizedActuatorData.GetAll().ToList();
        }
    }
}
