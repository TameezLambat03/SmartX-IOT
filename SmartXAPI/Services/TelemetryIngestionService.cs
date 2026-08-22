using SmartX.Shared;
using System;
using System.Collections.Generic;

namespace SmartXAPI.Services;

public class TelemetryIngestionService
{
    private readonly int _batchSize = 5;
    private List<float> _currentBuffer = new List<float>();

    private float[][] _historicalBatches = new float[10][];
    private int _batchIndex = 0;

    private List<float> _optimizedDataStore = new List<float>();

    public void IngestPacket(TelemetryPacket<float> packet)
    {
        _currentBuffer.Add(packet.SensorValue);

        if (_currentBuffer.Count >= _batchSize)
        {
            ProcessBufferIntoBatch();
        }
    }

    private void ProcessBufferIntoBatch()
    {
        if (_batchIndex >= _historicalBatches.Length)
        {
            FlushBatchesToOptimizedStore();
        }

        _historicalBatches[_batchIndex] = _currentBuffer.ToArray();
        _batchIndex++;

        _currentBuffer.Clear();
    }

    private void FlushBatchesToOptimizedStore()
    {
        for (int i = 0; i < _batchIndex; i++)
        {
            for (int j = 0; j < _historicalBatches[i].Length; j++)
            {
                _optimizedDataStore.Add(_historicalBatches[i][j]);
            }
        }

        _batchIndex = 0;
    }

    public int GetTotalOptimizedRecords()
    {
        return _optimizedDataStore.Count;
    }

    public List<float> GetOptimizedData()
    {
        return _optimizedDataStore;
    }
}