using SmartX.Shared.Collections;
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
namespace SmartXAPI.Services;


public class TelemetryIngestionService
{
    

    private const int BatchSize = 5;
    private const int MaxHistoricalBatches = 10;
    private const int MaxOptimizedRecords = 500;
    private readonly object _environmentalLock = new();
    private readonly object _powerLock = new();
    private readonly object _actuatorLock = new();
    private readonly SmartCollection<float> _currentEnvironmentalBuffer = new();
    //env array
    private readonly float[][] _historicalEnvironmentalBatches =
        new float[MaxHistoricalBatches][];
    private int _environmentalBatchIndex = 0;
    private readonly SmartCollection<float> _optimizedEnvironmentalData = new();

    private readonly SmartCollection<int> _currentPowerBuffer = new();
    //power array
    private readonly int[][] _historicalPowerBatches =
        new int[MaxHistoricalBatches][];
    private int _powerBatchIndex = 0;
    private readonly SmartCollection<int> _optimizedPowerData = new();

    private readonly SmartCollection<bool> _currentActuatorBuffer = new();
    //actuator array
    private readonly bool[][] _historicalActuatorBatches =
        new bool[MaxHistoricalBatches][];
    private int _actuatorBatchIndex = 0;
    private readonly SmartCollection<bool> _optimizedActuatorData = new();

    private void AddWithLimit<T>(
    SmartCollection<T> collection,
    T item)
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
            _currentEnvironmentalBuffer.Add(packet.SensorValue);

            if (_currentEnvironmentalBuffer.Count >= BatchSize)
            {
                ProcessEnvironmentalBuffer();
            }
        }
    }

    private void ProcessEnvironmentalBuffer()
    {
        float[] completedBatch =
            _currentEnvironmentalBuffer.GetAll().ToArray();

        // Store the completed batch in the historical jagged array.
        // The index wraps around when the array is full.
        _historicalEnvironmentalBatches[_environmentalBatchIndex] =
            completedBatch;

        _environmentalBatchIndex =
            (_environmentalBatchIndex + 1) %
            _historicalEnvironmentalBatches.Length;

        // Move the completed batch into the optimized collection.
        for (int i = 0; i < completedBatch.Length; i++)
        {
            AddWithLimit(
    _optimizedEnvironmentalData,
    completedBatch[i]);
        }

        // Clear the temporary buffer for the next batch.
        _currentEnvironmentalBuffer.Clear();
    }





    // POWER TELEMETRY


    public void IngestPacket(TelemetryPacket<int> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_powerLock)
        {
            _currentPowerBuffer.Add(packet.SensorValue);

            if (_currentPowerBuffer.Count >= BatchSize)
            {
                ProcessPowerBuffer();
            }
        }
    }

    private void ProcessPowerBuffer()
    {
        int[] completedBatch =
            _currentPowerBuffer.GetAll().ToArray();

        // Store the completed batch in the historical jagged array.
        // The index wraps around when the array is full.
        _historicalPowerBatches[_powerBatchIndex] =
            completedBatch;

        _powerBatchIndex =
            (_powerBatchIndex + 1) %
            _historicalPowerBatches.Length;

        // Move the completed batch into the optimized collection.
        for (int i = 0; i < completedBatch.Length; i++)
        {
            AddWithLimit(
    _optimizedPowerData,
    completedBatch[i]);
        }

        // Clear the temporary buffer for the next batch.
        _currentPowerBuffer.Clear();
    }





    // ACTUATOR TELEMETRY


    public void IngestPacket(TelemetryPacket<bool> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        lock (_actuatorLock)
        {
            _currentActuatorBuffer.Add(packet.SensorValue);

            if (_currentActuatorBuffer.Count >= BatchSize)
            {
                ProcessActuatorBuffer();
            }
        }
    }
    private void ProcessActuatorBuffer()
    {
        bool[] completedBatch =
            _currentActuatorBuffer.GetAll().ToArray();

        // Store the completed batch in the historical jagged array.
        // The index wraps around when the array is full.
        _historicalActuatorBatches[_actuatorBatchIndex] =
            completedBatch;

        _actuatorBatchIndex =
            (_actuatorBatchIndex + 1) %
            _historicalActuatorBatches.Length;

        // Move the completed batch into the optimized collection.
        for (int i = 0; i < completedBatch.Length; i++)
        {
            AddWithLimit(
     _optimizedActuatorData,
     completedBatch[i]);
        }

        // Clear the temporary buffer for the next batch.
        _currentActuatorBuffer.Clear();
    }
    

    


    // ============================
    // RETRIEVAL
    // ============================

    public int GetTotalOptimizedEnvironmentalRecords()
    {
        return _optimizedEnvironmentalData.Count;
    }

    public int GetTotalOptimizedPowerRecords()
    {
        return _optimizedPowerData.Count;
    }

    public int GetTotalOptimizedActuatorRecords()
    {
        return _optimizedActuatorData.Count;
    }


    public IReadOnlyList<float> GetEnvironmentalData()
    {
        return _optimizedEnvironmentalData.GetAll();
    }

    public IReadOnlyList<int> GetPowerData()
    {
        return _optimizedPowerData.GetAll();
    }

    public IReadOnlyList<bool> GetActuatorData()
    {
        return _optimizedActuatorData.GetAll();
    }
}