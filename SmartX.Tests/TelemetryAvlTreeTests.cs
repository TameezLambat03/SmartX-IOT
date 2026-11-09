
using SmartX.Shared.Collections;
using SmartX.Shared.Models;
using System.Reflection;
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

    // ---------------------------------------------------------
    // AVL STRUCTURAL VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public void Insert_MaintainsAvlBalanceWithAscendingTimestamps()
    {
        var tree = new TelemetryAvlTree<double>();
        DateTime start = DateTime.Parse("2026-01-01T00:00:00Z");

        for (int i = 0; i < 10000; i++)
        {
            tree.Insert(CreatePacket(
                start.AddSeconds(i),
                "SENSOR-01",
                i));
        }

        Assert.Equal(10000, tree.Count);

        var validation = ValidateTree(tree);

        Assert.Equal(10000, validation.PacketCount);
        Assert.True(validation.Height > 0);
    }

    [Fact]
    public void Insert_MaintainsAvlBalanceWithRandomTimestamps()
    {
        var tree = new TelemetryAvlTree<double>();
        DateTime start = DateTime.Parse("2026-01-01T00:00:00Z");

        const int packetCount = 100000;

        var timestamps = Enumerable.Range(0, packetCount).ToArray();

        Random random = new(42);

        for (int i = timestamps.Length - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (timestamps[i], timestamps[j]) =
                (timestamps[j], timestamps[i]);
        }

        foreach (int timestamp in timestamps)
        {
            tree.Insert(CreatePacket(
                start.AddSeconds(timestamp),
                "SENSOR-01",
                timestamp));
        }

        Assert.Equal(packetCount, tree.Count);

        var validation = ValidateTree(tree);

        Assert.Equal(packetCount, validation.PacketCount);
        Assert.True(validation.Height > 0);
    }

    [Fact]
    public void Insert_PreservesLargeNumberOfDuplicateTimestamps()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime timestamp =
            DateTime.Parse("2026-01-01T10:00:00Z");

        const int packetCount = 10000;

        for (int i = 0; i < packetCount; i++)
        {
            tree.Insert(CreatePacket(
                timestamp,
                $"SENSOR-{i % 10:D2}",
                i));
        }

        var results = tree.QueryRange(timestamp, timestamp);

        Assert.Equal(packetCount, tree.Count);
        Assert.Equal(packetCount, results.Count);

        var validation = ValidateTree(tree);

        Assert.Equal(packetCount, validation.PacketCount);
    }

    // ---------------------------------------------------------
    // LARGE-SCALE RANGE QUERY VALIDATION
    // ---------------------------------------------------------

    [Fact]
    public void QueryRange_MatchesLinearSearchForLargeTelemetryHistory()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime start =
            DateTime.Parse("2026-01-01T00:00:00Z");

        const int packetCount = 100000;

        var packets = new List<TelemetryPacket<double>>(
            packetCount);

        Random random = new(42);

        for (int i = 0; i < packetCount; i++)
        {
            var packet = CreatePacket(
                start.AddSeconds(random.Next(packetCount)),
                $"SENSOR-{i % 5:D2}",
                i);

            packets.Add(packet);
            tree.Insert(packet);
        }

        DateTime rangeStart = start.AddSeconds(25000);
        DateTime rangeEnd = start.AddSeconds(30000);

        var expected = packets
            .Where(packet =>
                packet.Timestamp >= rangeStart &&
                packet.Timestamp <= rangeEnd)
            .OrderBy(packet => packet.Timestamp)
            .ToList();

        var actual = tree.QueryRange(rangeStart, rangeEnd);

        Assert.Equal(expected.Count, actual.Count);

        Assert.Equal(
            expected.Select(packet => packet.Timestamp),
            actual.Select(packet => packet.Timestamp));

        Assert.Equal(
            expected.Select(packet => packet.SensorValue),
            actual.Select(packet => packet.SensorValue));
    }

    [Fact]
    public void QueryRange_NarrowRangeReturnsOnlyMatchingPackets()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime start =
            DateTime.Parse("2026-01-01T00:00:00Z");

        const int packetCount = 10000;

        for (int i = 0; i < packetCount; i++)
        {
            tree.Insert(CreatePacket(
                start.AddSeconds(i),
                "SENSOR-01",
                i));
        }

        DateTime rangeStart = start.AddSeconds(5000);
        DateTime rangeEnd = start.AddSeconds(5009);

        var results = tree.QueryRange(rangeStart, rangeEnd);

        Assert.Equal(10, results.Count);

        Assert.All(results, packet =>
        {
            Assert.InRange(
                packet.Timestamp,
                rangeStart,
                rangeEnd);
        });
    }

    [Fact]
    public void QueryRange_FiltersLargeHistoryByDevice()
    {
        var tree = new TelemetryAvlTree<double>();

        DateTime start =
            DateTime.Parse("2026-01-01T00:00:00Z");

        const int packetCount = 10000;

        for (int i = 0; i < packetCount; i++)
        {
            tree.Insert(CreatePacket(
                start.AddSeconds(i),
                $"SENSOR-{i % 5:D2}",
                i));
        }

        var results = tree.QueryRange(
            start,
            start.AddSeconds(packetCount - 1),
            "SENSOR-02");

        Assert.Equal(2000, results.Count);

        Assert.All(results, packet =>
            Assert.Equal(
                "SENSOR-02",
                packet.DeviceMacAddress));
    }

    [Fact]
    public void Insert_NullPacketThrowsArgumentNullException()
    {
        var tree = new TelemetryAvlTree<double>();

        Assert.Throws<ArgumentNullException>(() =>
            tree.Insert(null!));

        Assert.Equal(0, tree.Count);
    }

    // ---------------------------------------------------------
    // PRIVATE AVL TREE INSPECTION
    // ---------------------------------------------------------

    private static (int Height, int PacketCount) ValidateTree(
        TelemetryAvlTree<double> tree)
    {
        const BindingFlags flags =
            BindingFlags.Instance | BindingFlags.NonPublic;

        FieldInfo rootField = typeof(TelemetryAvlTree<double>)
            .GetField("_root", flags)!;

        Assert.NotNull(rootField);

        object? root = rootField.GetValue(tree);

        var result = ValidateNode(
            root,
            null,
            null);

        Assert.Equal(tree.Count, result.PacketCount);

        return result;
    }

    private static (int Height, int PacketCount) ValidateNode(
        object? node,
        DateTime? lowerBound,
        DateTime? upperBound)
    {
        if (node is null)
            return (0, 0);

        Type nodeType = node.GetType();

        DateTime timestamp = (DateTime)GetProperty(
            nodeType, "Timestamp").GetValue(node)!;

        int storedHeight = (int)GetProperty(
            nodeType, "Height").GetValue(node)!;

        object? left = GetProperty(
            nodeType, "Left").GetValue(node);

        object? right = GetProperty(
            nodeType, "Right").GetValue(node);

        var packets = (IEnumerable<TelemetryPacket<double>>)
            GetProperty(nodeType, "Packets").GetValue(node)!;

        if (lowerBound.HasValue)
            Assert.True(timestamp > lowerBound.Value);

        if (upperBound.HasValue)
            Assert.True(timestamp < upperBound.Value);

        var leftResult = ValidateNode(
            left,
            lowerBound,
            timestamp);

        var rightResult = ValidateNode(
            right,
            timestamp,
            upperBound);

        int actualHeight = 1 + Math.Max(
            leftResult.Height,
            rightResult.Height);

        int balanceFactor =
            leftResult.Height - rightResult.Height;

        Assert.Equal(actualHeight, storedHeight);
        Assert.InRange(balanceFactor, -1, 1);

        int localPacketCount = 0;

        foreach (var packet in packets)
        {
            Assert.Equal(timestamp, packet.Timestamp);
            localPacketCount++;
        }

        Assert.True(localPacketCount > 0);

        return (
            actualHeight,
            leftResult.PacketCount +
            rightResult.PacketCount +
            localPacketCount);
    }

    private static PropertyInfo GetProperty(
        Type nodeType,
        string propertyName)
    {
        return nodeType.GetProperty(
            propertyName,
            BindingFlags.Instance | BindingFlags.Public)!;
    }
}
