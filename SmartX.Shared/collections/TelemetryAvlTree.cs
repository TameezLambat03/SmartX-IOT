
using SmartX.Shared.Models;
using System;
using System.Collections.Generic;

namespace SmartX.Shared.Collections;

/// <summary>
/// Stores telemetry packets in a self-balancing AVL tree,
/// ordered by timestamp. Multiple packets may share a timestamp.
/// </summary>
public class TelemetryAvlTree<T>
{
    private sealed class Node
    {
        public DateTime Timestamp { get; }
        public List<TelemetryPacket<T>> Packets { get; } = new();
        public Node? Left { get; set; }
        public Node? Right { get; set; }
        public int Height { get; set; } = 1;

        public Node(TelemetryPacket<T> packet)
        {
            Timestamp = packet.Timestamp;
            Packets.Add(packet);
        }
    }

    private Node? _root;
    public int Count { get; private set; }

    //adds a new reading, and balances the tree
    public void Insert(TelemetryPacket<T> packet)
    {
        ArgumentNullException.ThrowIfNull(packet);

        _root = InsertNode(_root, packet);
        Count++;
    }

    private static Node InsertNode(
        Node? node,
        TelemetryPacket<T> packet)
    {
        if (node is null)
            return new Node(packet);

        if (packet.Timestamp < node.Timestamp)
        {
            node.Left = InsertNode(node.Left, packet);
        }
        else if (packet.Timestamp > node.Timestamp)
        {
            node.Right = InsertNode(node.Right, packet);
        }
        else
        {
            // Preserve packets that share the same timestamp.
            node.Packets.Add(packet);
            return node;
        }

        UpdateHeight(node);

        int balance = GetBalance(node);

        // Left-left case
        if (balance > 1 &&
            packet.Timestamp < node.Left!.Timestamp)
        {
            return RotateRight(node);
        }

        // Right-right case
        if (balance < -1 &&
            packet.Timestamp > node.Right!.Timestamp)
        {
            return RotateLeft(node);
        }

        // Left-right case
        if (balance > 1 &&
            packet.Timestamp > node.Left!.Timestamp)
        {
            node.Left = RotateLeft(node.Left!);
            return RotateRight(node);
        }

        // Right-left case
        if (balance < -1 &&
            packet.Timestamp < node.Right!.Timestamp)
        {
            node.Right = RotateRight(node.Right!);
            return RotateLeft(node);
        }

        return node;
    }

    public IReadOnlyList<TelemetryPacket<T>> QueryRange(//selects readings betwen two timestamps
        DateTime start,
        DateTime end,
        string? deviceMacAddress = null)
    {
        if (start > end)
        {
            throw new ArgumentException(
                "The start timestamp must not be after the end timestamp.");
        }

        var results = new List<TelemetryPacket<T>>();
        
        QueryRangeNode(
            _root,
            start,
            end,
            deviceMacAddress,
            results);

        return results;
    }

    private static void QueryRangeNode(
        Node? node,
        DateTime start,
        DateTime end,
        string? deviceMacAddress,
        List<TelemetryPacket<T>> results)
    {
        if (node is null)
            return;

        // Only inspect the left subtree when it could
        // contain timestamps at or after the start.
        if (node.Timestamp >= start)
        {
            QueryRangeNode(
                node.Left,
                start,
                end,
                deviceMacAddress,
                results);
        }

        if (node.Timestamp >= start &&
            node.Timestamp <= end)
        {
            foreach (TelemetryPacket<T> packet in node.Packets)
            {
                if (string.IsNullOrWhiteSpace(deviceMacAddress) ||
                    string.Equals(
                        packet.DeviceMacAddress,
                        deviceMacAddress,
                        StringComparison.OrdinalIgnoreCase))
                {
                    results.Add(packet);
                }
            }
        }

        // Only inspect the right subtree when it could
        // contain timestamps at or before the end.
        if (node.Timestamp <= end)
        {
            QueryRangeNode(
                node.Right,
                start,
                end,
                deviceMacAddress,
                results);
        }
    }

    private static int GetHeight(Node? node) =>
        node?.Height ?? 0;

    private static int GetBalance(Node? node) =>
        node is null
            ? 0
            : GetHeight(node.Left) - GetHeight(node.Right);

    private static void UpdateHeight(Node node)
    {
        node.Height = 1 + Math.Max(
            GetHeight(node.Left),
            GetHeight(node.Right));
    }
    //keeps tree balanced to prevent it from being inefficent as entires are added
    private static Node RotateRight(Node pivot)
    {
        Node newRoot = pivot.Left!;
        Node? transferredSubtree = newRoot.Right;

        newRoot.Right = pivot;
        pivot.Left = transferredSubtree;

        UpdateHeight(pivot);
        UpdateHeight(newRoot);

        return newRoot;
    }

    private static Node RotateLeft(Node pivot)
    {
        Node newRoot = pivot.Right!;
        Node? transferredSubtree = newRoot.Left;

        newRoot.Left = pivot;
        pivot.Right = transferredSubtree;

        UpdateHeight(pivot);
        UpdateHeight(newRoot);

        return newRoot;
    }
}
