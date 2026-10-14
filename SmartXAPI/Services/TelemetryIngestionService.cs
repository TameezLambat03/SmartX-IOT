
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
    private const int MaxTimestampedRecords = 500;

    private readonly object _environmentalLock = new();
    private readonly object _powerLock = new();
    private readonly object _actuatorLock = new();

    // FIFO telemetry queues.
    private readonly Queue<TelemetryPacket<float>> _environmentalQueue = new();
    private readonly Queue<TelemetryPacket<int>> _powerQueue = new();
    private readonly Queue<TelemetryPacket<bool>> _actuatorQueue = new();

    // Timestamp-ordered telemetry logs.
    // Lists allow multiple packets to share the same timestamp.
    private readonly SortedDictionary<DateTime, List<TelemetryPacket<float>>>
        _environmentalLogs = new();

    private readonly SortedDictionary<DateTime, List<TelemetryPacket<int>>>
        _powerLogs = new();

    private readonly SortedDictionary<DateTime, List<TelemetryPacket<bool>>>
        _actuatorLogs = new();

    private readonly CriticalAlertService _criticalAlertService;
    private readonly PredictiveRecommendationService _recommendationService;

    // Environmental batch storage.
    private readonly SmartCollection<float> _currentEnvironmentalBuffer = new();
    private readonly float[][] _historicalEnvironmentalBatches =
        new float[MaxHistoricalBatches][];
    private int _environmentalBatchIndex = 0;
    private readonly SmartCollection<float> _optimizedEnvironmentalData = new();

    // Power batch storage.
    private readonly SmartCollection<int> _currentPowerBuffer = new();
    private readonly int[][] _historicalPowerBatches =
        new int[MaxHistoricalBatches][];
    private int _powerBatchIndex = 0;
    private readonly SmartCollection<int> _optimizedPowerData = new();

    // Actuator batch storage.
    private readonly SmartCollection<bool> _currentActuatorBuffer = new();
    private readonly bool[][] _historicalActuatorBatches =
        new bool[MaxHistoricalBatches][];
    private int _actuatorBatchIndex = 0;
    private readonly SmartCollection<bool> _optimizedActuatorData = new();

    public TelemetryIngestionService(
        CriticalAlertService criticalAlertService,
        PredictiveRecommendationService recommendationService)
    {
        _criticalAlertService = criticalAlertService;
        _recommendationService = recommendationService;
    }

    // Queue incoming environmental telemetry.
    public void IngestPacket(TelemetryPacket<float> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_environmentalLock)
        {
            _environmentalQueue.Enqueue(packet);
        }
    }

    // Queue incoming power telemetry.
    public void IngestPacket(TelemetryPacket<int> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_powerLock)
        {
            _powerQueue.Enqueue(packet);
        }
    }

    // Queue incoming actuator telemetry.
    public void IngestPacket(TelemetryPacket<bool> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_actuatorLock)
        {
            _actuatorQueue.Enqueue(packet);
        }
    }

    public void ProcessQueuedPackets()
    {
        ProcessEnvironmentalQueue();
        ProcessPowerQueue();
        ProcessActuatorQueue();
    }

    private void ProcessEnvironmentalQueue()
    {
        lock (_environmentalLock)
        {
            while (_environmentalQueue.Count > 0)
            {
                TelemetryPacket<float> packet =
                    _environmentalQueue.Dequeue();

                AddTimestampedLog(_environmentalLogs, packet);

                _criticalAlertService.CheckEnvironmentalReading(
                    packet.DeviceMacAddress,
                    packet.DeploymentLocation,
                    packet.Timestamp,
                    packet.SensorValue);

                _recommendationService.AnalyseEnvironmentalReading(
                    packet.DeviceMacAddress,
                    packet.DeploymentLocation,
                    packet.Timestamp,
                    packet.SensorValue);

                _currentEnvironmentalBuffer.Add(packet.SensorValue);

                if (_currentEnvironmentalBuffer.Count >= BatchSize)
                {
                    ProcessEnvironmentalBuffer();
                }
            }
        }
    }

    private void ProcessPowerQueue()
    {
        lock (_powerLock)
        {
            while (_powerQueue.Count > 0)
            {
                TelemetryPacket<int> packet = _powerQueue.Dequeue();

                AddTimestampedLog(_powerLogs, packet);

                _currentPowerBuffer.Add(packet.SensorValue);

                if (_currentPowerBuffer.Count >= BatchSize)
                {
                    ProcessPowerBuffer();
                }
            }
        }
    }

    private void ProcessActuatorQueue()
    {
        lock (_actuatorLock)
        {
            while (_actuatorQueue.Count > 0)
            {
                TelemetryPacket<bool> packet = _actuatorQueue.Dequeue();

                AddTimestampedLog(_actuatorLogs, packet);

                _currentActuatorBuffer.Add(packet.SensorValue);

                if (_currentActuatorBuffer.Count >= BatchSize)
                {
                    ProcessActuatorBuffer();
                }
            }
        }
    }

    // Store completed environmental batches.
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

    // Store completed power batches.
    private void ProcessPowerBuffer()
    {
        int[] completedBatch =
            _currentPowerBuffer.GetAll().ToArray();

        _historicalPowerBatches[_powerBatchIndex] = completedBatch;

        _powerBatchIndex =
            (_powerBatchIndex + 1) %
            _historicalPowerBatches.Length;

        foreach (int value in completedBatch)
        {
            AddWithLimit(_optimizedPowerData, value);
        }

        _currentPowerBuffer.Clear();
    }

    // Store completed actuator batches.
    private void ProcessActuatorBuffer()
    {
        bool[] completedBatch =
            _currentActuatorBuffer.GetAll().ToArray();

        _historicalActuatorBatches[_actuatorBatchIndex] = completedBatch;

        _actuatorBatchIndex =
            (_actuatorBatchIndex + 1) %
            _historicalActuatorBatches.Length;

        foreach (bool value in completedBatch)
        {
            AddWithLimit(_optimizedActuatorData, value);
        }

        _currentActuatorBuffer.Clear();
    }

    // Add a packet under its timestamp and enforce the record limit.
    private static void AddTimestampedLog<T>(
        SortedDictionary<DateTime, List<TelemetryPacket<T>>> logs,
        TelemetryPacket<T> packet)
    {
        if (!logs.TryGetValue(packet.Timestamp, out List<TelemetryPacket<T>>? packets))
        {
            packets = new List<TelemetryPacket<T>>();
            logs.Add(packet.Timestamp, packets);
        }

        packets.Add(packet);

        while (logs.Sum(entry => entry.Value.Count) > MaxTimestampedRecords)
        {
            KeyValuePair<DateTime, List<TelemetryPacket<T>>> oldestEntry =
                logs.First();

            oldestEntry.Value.RemoveAt(0);

            if (oldestEntry.Value.Count == 0)
            {
                logs.Remove(oldestEntry.Key);
            }
        }
    }

    // Return environmental packets in timestamp order.
    public IReadOnlyList<TelemetryPacket<float>>
        GetEnvironmentalTimestampedLogs()
    {
        lock (_environmentalLock)
        {
            return _environmentalLogs.Values
                .SelectMany(packets => packets)
                .ToList();
        }
    }

    // Return power packets in timestamp order.
    public IReadOnlyList<TelemetryPacket<int>>
        GetPowerTimestampedLogs()
    {
        lock (_powerLock)
        {
            return _powerLogs.Values
                .SelectMany(packets => packets)
                .ToList();
        }
    }

    // Return actuator packets in timestamp order.
    public IReadOnlyList<TelemetryPacket<bool>>
        GetActuatorTimestampedLogs()
    {
        lock (_actuatorLock)
        {
            return _actuatorLogs.Values
                .SelectMany(packets => packets)
                .ToList();
        }
    }

    private static void AddWithLimit<T>(
        SmartCollection<T> collection,
        T item)
    {
        collection.Add(item);

        if (collection.Count > MaxOptimizedRecords)
        {
            collection.Remove(collection[0]);
        }
    }

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
