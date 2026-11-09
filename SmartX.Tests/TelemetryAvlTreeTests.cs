
using SmartX.Shared.Collections;
using SmartX.Shared.Models;
using Xunit;

namespace SmartX.Shared.Tests;

public class TelemetryAvlTreeTests
{
    private static TelemetryPacket<double> CreatePacket(
        DateTime timestamp,
        string macAddress,
        double value)
    {
        var packet = new TelemetryPacket<double>(
            macAddress,
            "Test Facility/Zone 1",
            value);

        packet.Timestamp = timestamp;

        return packet;
    }

    [Fact]
    public void Insert_IncreasesCount()
    {
        var tree = new TelemetryAvlTree<double>();

        tree.Insert(CreatePacket(DateTime.Parse("2026-01-01T10:00:00Z"), "SENSOR-01", 10));
        tree.Insert(CreatePacket(DateTime.Parse("2026-01-01T10:01:00Z"), "SENSOR-02", 20));

        Assert.Equal(2, tree.Count);
    }

    [Fact]
    public void Insert_PreservesPacketsWithDuplicateTimestamps()
    {
        var tree = new TelemetryAvlTree<double>();
        DateTime timestamp = DateTime.Parse("2026-01-01T10:00:00Z");

        tree.Insert(CreatePacket(timestamp, "SENSOR-01", 10));
        tree.Insert(CreatePacket(timestamp, "SENSOR-02", 20));

        var results = tree.QueryRange(timestamp, timestamp);

        Assert.Equal(2, tree.Count);
        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void QueryRange_ReturnsPacketsInChronologicalOrder()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime first = DateTime.Parse("2026-01-01T10:00:00Z");
        DateTime second = DateTime.Parse("2026-01-01T10:01:00Z");
        DateTime third = DateTime.Parse("2026-01-01T10:02:00Z");

        tree.Insert(CreatePacket(third, "SENSOR-01", 30));
        tree.Insert(CreatePacket(first, "SENSOR-01", 10));
        tree.Insert(CreatePacket(second, "SENSOR-01", 20));

        var results = tree.QueryRange(first, third);

        Assert.Equal(3, results.Count);
        Assert.Equal(first, results[0].Timestamp);
        Assert.Equal(second, results[1].Timestamp);
        Assert.Equal(third, results[2].Timestamp);
    }

    [Fact]
    public void QueryRange_IncludesStartAndEndBoundaries()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime start = DateTime.Parse("2026-01-01T10:00:00Z");
        DateTime middle = DateTime.Parse("2026-01-01T10:01:00Z");
        DateTime end = DateTime.Parse("2026-01-01T10:02:00Z");

        tree.Insert(CreatePacket(start, "SENSOR-01", 10));
        tree.Insert(CreatePacket(middle, "SENSOR-01", 20));
        tree.Insert(CreatePacket(end, "SENSOR-01", 30));

        var results = tree.QueryRange(start, end);

        Assert.Equal(3, results.Count);
    }

    [Fact]
    public void QueryRange_FiltersByDeviceMacAddress()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime timestamp = DateTime.Parse("2026-01-01T10:00:00Z");

        tree.Insert(CreatePacket(timestamp, "SENSOR-01", 10));
        tree.Insert(CreatePacket(timestamp, "SENSOR-02", 20));
        tree.Insert(CreatePacket(timestamp, "SENSOR-03", 30));

        var results = tree.QueryRange(
            timestamp,
            timestamp,
            "SENSOR-02");

        Assert.Single(results);
        Assert.Equal("SENSOR-02", results[0].DeviceMacAddress);
    }

    [Fact]
    public void QueryRange_MacAddressFilterIsCaseInsensitive()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime timestamp = DateTime.Parse("2026-01-01T10:00:00Z");

        tree.Insert(CreatePacket(timestamp, "sensor-01", 10));

        var results = tree.QueryRange(
            timestamp,
            timestamp,
            "SENSOR-01");

        Assert.Single(results);
    }

    [Fact]
    public void QueryRange_WithoutMacAddressReturnsAllDevices()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime timestamp = DateTime.Parse("2026-01-01T10:00:00Z");

        tree.Insert(CreatePacket(timestamp, "SENSOR-01", 10));
        tree.Insert(CreatePacket(timestamp, "SENSOR-02", 20));

        var results = tree.QueryRange(timestamp, timestamp);

        Assert.Equal(2, results.Count);
    }

    [Fact]
    public void QueryRange_ReturnsEmptyForEmptyTree()
    {
        var tree = new TelemetryAvlTree<double>();

        var results = tree.QueryRange(
            DateTime.Parse("2026-01-01T10:00:00Z"),
            DateTime.Parse("2026-01-01T11:00:00Z"));

        Assert.Empty(results);
        Assert.Equal(0, tree.Count);
    }

    [Fact]
    public void QueryRange_ThrowsWhenStartIsAfterEnd()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime start = DateTime.Parse("2026-01-01T11:00:00Z");
        DateTime end = DateTime.Parse("2026-01-01T10:00:00Z");

        Assert.Throws<ArgumentException>(() =>
            tree.QueryRange(start, end));
    }

    [Fact]
    public void Insert_AscendingTimestampsRemainQueryable()
    {
        var tree = new TelemetryAvlTree<double>();
        DateTime start = DateTime.Parse("2026-01-01T10:00:00Z");

        for (int i = 0; i < 100; i++)
        {
            tree.Insert(CreatePacket(
                start.AddMinutes(i),
                "SENSOR-01",
                i));
        }

        var results = tree.QueryRange(
            start,
            start.AddMinutes(99));

        Assert.Equal(100, tree.Count);
        Assert.Equal(100, results.Count);
        Assert.Equal(start, results[0].Timestamp);
        Assert.Equal(start.AddMinutes(99), results[^1].Timestamp);
    }

    [Fact]
    public void Insert_DescendingTimestampsRemainQueryable()
    {
        var tree = new TelemetryAvlTree<double>();
        DateTime start = DateTime.Parse("2026-01-01T10:00:00Z");

        for (int i = 99; i >= 0; i--)
        {
            tree.Insert(CreatePacket(
                start.AddMinutes(i),
                "SENSOR-01",
                i));
        }

        var results = tree.QueryRange(
            start,
            start.AddMinutes(99));

        Assert.Equal(100, tree.Count);
        Assert.Equal(100, results.Count);
        Assert.Equal(start, results[0].Timestamp);
        Assert.Equal(start.AddMinutes(99), results[^1].Timestamp);
    }
}
