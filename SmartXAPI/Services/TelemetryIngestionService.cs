using SmartX.Shared.Models;
using System;
using System.Collections.Generic;

namespace SmartXAPI.Services;

public class TelemetryIngestionService
{
    private readonly int _batchSize = 5;

    // Environmental telemetry
    private readonly List<float> _currentEnvironmentalBuffer = new();
    private readonly float[][] _historicalEnvironmentalBatches = new float[10][];
    private int _environmentalBatchIndex = 0;
    private readonly List<float> _optimizedEnvironmentalData = new();

    // Power telemetry
    private readonly List<int> _currentPowerBuffer = new();
    private readonly int[][] _historicalPowerBatches = new int[10][];
    private int _powerBatchIndex = 0;
    private readonly List<int> _optimizedPowerData = new();

    // Actuator telemetry
    private readonly List<bool> _currentActuatorBuffer = new();
    private readonly bool[][] _historicalActuatorBatches = new bool[10][];
    private int _actuatorBatchIndex = 0;
    private readonly List<bool> _optimizedActuatorData = new();


    // ============================
    // ENVIRONMENTAL TELEMETRY
    // ============================

    public void IngestPacket(TelemetryPacket<float> packet)
    {
        _currentEnvironmentalBuffer.Add(packet.SensorValue);

        if (_currentEnvironmentalBuffer.Count >= _batchSize)
        {
            ProcessEnvironmentalBuffer();
        }
    }

    private void ProcessEnvironmentalBuffer()
    {
        float[] completedBatch =
            _currentEnvironmentalBuffer.ToArray();

        // Store the completed batch in the historical jagged array
        if (_environmentalBatchIndex <
            _historicalEnvironmentalBatches.Length)
        {
            _historicalEnvironmentalBatches[_environmentalBatchIndex] =
                completedBatch;

            _environmentalBatchIndex++;
        }

        // Move the completed batch into the optimized collection
        for (int i = 0; i < completedBatch.Length; i++)
        {
            _optimizedEnvironmentalData.Add(completedBatch[i]);
        }

        // Clear the temporary buffer for the next batch
        _currentEnvironmentalBuffer.Clear();

        // If the historical array is full, reset its index
        if (_environmentalBatchIndex >=
            _historicalEnvironmentalBatches.Length)
        {
            _environmentalBatchIndex = 0;
        }
    }

    private void FlushEnvironmentalBatches()
    {
        for (int i = 0; i < _environmentalBatchIndex; i++)
        {
            for (int j = 0;
                 j < _historicalEnvironmentalBatches[i].Length;
                 j++)
            {
                _optimizedEnvironmentalData.Add(
                    _historicalEnvironmentalBatches[i][j]);
            }
        }

        _environmentalBatchIndex = 0;
    }


    // ============================
    // POWER TELEMETRY
    // ============================

    public void IngestPacket(TelemetryPacket<int> packet)
    {
        _currentPowerBuffer.Add(packet.SensorValue);

        if (_currentPowerBuffer.Count >= _batchSize)
        {
            ProcessPowerBuffer();
        }
    }

    private void ProcessPowerBuffer()
    {
        if (_powerBatchIndex >=
            _historicalPowerBatches.Length)
        {
            FlushPowerBatches();
        }

        _historicalPowerBatches[_powerBatchIndex] =
            _currentPowerBuffer.ToArray();

        _powerBatchIndex++;

        _currentPowerBuffer.Clear();
    }

    private void FlushPowerBatches()
    {
        for (int i = 0; i < _powerBatchIndex; i++)
        {
            for (int j = 0;
                 j < _historicalPowerBatches[i].Length;
                 j++)
            {
                _optimizedPowerData.Add(
                    _historicalPowerBatches[i][j]);
            }
        }

        _powerBatchIndex = 0;
    }


    // ============================
    // ACTUATOR TELEMETRY
    // ============================

    public void IngestPacket(TelemetryPacket<bool> packet)
    {
        _currentActuatorBuffer.Add(packet.SensorValue);

        if (_currentActuatorBuffer.Count >= _batchSize)
        {
            ProcessActuatorBuffer();
        }
    }

    private void ProcessActuatorBuffer()
    {
        if (_actuatorBatchIndex >=
            _historicalActuatorBatches.Length)
        {
            FlushActuatorBatches();
        }

        _historicalActuatorBatches[_actuatorBatchIndex] =
            _currentActuatorBuffer.ToArray();

        _actuatorBatchIndex++;

        _currentActuatorBuffer.Clear();
    }

    private void FlushActuatorBatches()
    {
        for (int i = 0; i < _actuatorBatchIndex; i++)
        {
            for (int j = 0;
                 j < _historicalActuatorBatches[i].Length;
                 j++)
            {
                _optimizedActuatorData.Add(
                    _historicalActuatorBatches[i][j]);
            }
        }

        _actuatorBatchIndex = 0;
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


    public List<float> GetEnvironmentalData()
    {
        return _optimizedEnvironmentalData;
    }

    public List<int> GetPowerData()
    {
        return _optimizedPowerData;
    }

    public List<bool> GetActuatorData()
    {
        return _optimizedActuatorData;
    }
}