
using SmartXWPF.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartXWPF.Services;

public class NetworkTopologyRenderer
{
    public void Draw(
        Canvas canvas,
        List<MeshNetworkNode> nodes,
        List<MeshNetworkConnection> connections)
    {
        canvas.Children.Clear();

        if (nodes.Count == 0)
            return;

        double width = Math.Max(canvas.ActualWidth, 900);
        double height = Math.Max(canvas.ActualHeight, 520);

        MeshNetworkNode? gateway =
            nodes.FirstOrDefault(node => node.IsGateway);

        if (gateway == null)
            return;

        // Build adjacency using only the supplied connections.
        Dictionary<string, List<string>> adjacency =
            nodes.ToDictionary(
                node => node.Id,
                _ => new List<string>(),
                StringComparer.OrdinalIgnoreCase);

        foreach (MeshNetworkConnection connection in connections)
        {
            if (adjacency.ContainsKey(connection.DeviceA) &&
                adjacency.ContainsKey(connection.DeviceB))
            {
                adjacency[connection.DeviceA].Add(connection.DeviceB);
                adjacency[connection.DeviceB].Add(connection.DeviceA);
            }
        }

        // Determine hop levels from the gateway using BFS.
        Dictionary<string, int> levels =
            new(StringComparer.OrdinalIgnoreCase);

        Queue<string> queue = new();

        levels[gateway.Id] = 0;
        queue.Enqueue(gateway.Id);

        while (queue.Count > 0)
        {
            string current = queue.Dequeue();

            foreach (string neighbour in adjacency[current])
            {
                // Never traverse through an offline device.
                MeshNetworkNode? neighbourNode =
                    nodes.FirstOrDefault(node =>
                        node.Id.Equals(
                            neighbour,
                            StringComparison.OrdinalIgnoreCase));

                if (neighbourNode == null || !neighbourNode.IsOnline)
                    continue;

                if (levels.ContainsKey(neighbour))
                    continue;

                levels[neighbour] = levels[current] + 1;
                queue.Enqueue(neighbour);
            }
        }

        // Place connected devices in BFS levels.
        Dictionary<string, Point> positions = new();

        positions[gateway.Id] = new Point(width / 2, 55);

        var connectedLevels = nodes
            .Where(node =>
                !node.IsGateway &&
                levels.ContainsKey(node.Id))
            .GroupBy(node => levels[node.Id])
            .OrderBy(group => group.Key);

        foreach (var level in connectedLevels)
        {
            List<MeshNetworkNode> levelNodes = level.ToList();

            double spacing = width / (levelNodes.Count + 1);
            double y = 55 + level.Key * 105;

            for (int i = 0; i < levelNodes.Count; i++)
            {
                positions[levelNodes[i].Id] =
                    new Point(spacing * (i + 1), y);
            }
        }

        // Devices explicitly switched offline.
        List<MeshNetworkNode> offlineNodes = nodes
            .Where(node => !node.IsOnline)
            .ToList();

        // Online devices that have lost connectivity to the gateway.
        List<MeshNetworkNode> unreachableNodes = nodes
            .Where(node =>
                node.IsOnline &&
                !levels.ContainsKey(node.Id))
            .ToList();

        List<MeshNetworkNode> disconnectedNodes =
            offlineNodes.Concat(unreachableNodes).ToList();

        double offlineY = height - 90;
        double offlineSpacing = width / (disconnectedNodes.Count + 1);

        for (int i = 0; i < disconnectedNodes.Count; i++)
        {
            positions[disconnectedNodes[i].Id] =
                new Point(offlineSpacing * (i + 1), offlineY);
        }

        // Draw connections behind the nodes.
        HashSet<string> drawnEdges = new();

        foreach (MeshNetworkConnection connection in connections)
        {
            // 1. Ignore self-loops (e.g., B8 connected to B8)
            if (string.Equals(connection.DeviceA, connection.DeviceB, StringComparison.OrdinalIgnoreCase))
                continue;

            // 2. Ignore duplicate edges (A->B and B->A)
            string edgeKey = string.Compare(connection.DeviceA, connection.DeviceB, StringComparison.OrdinalIgnoreCase) < 0
                ? $"{connection.DeviceA}-{connection.DeviceB}"
                : $"{connection.DeviceB}-{connection.DeviceA}";

            if (!drawnEdges.Add(edgeKey))
                continue;

            if (!positions.TryGetValue(connection.DeviceA, out Point start) ||
                !positions.TryGetValue(connection.DeviceB, out Point end))
                continue;

            // Never draw an edge involving an unreachable node.
            if (!levels.ContainsKey(connection.DeviceA) ||
                !levels.ContainsKey(connection.DeviceB))
                continue;

            // Pass the positions of all nodes so the renderer knows where the overlap hazards are
            DrawConnection(canvas, start, end, connection.Weight, positions.Values.ToList());
        }

        // Draw connected nodes.
        foreach (MeshNetworkNode node in nodes)
        {
            if (levels.ContainsKey(node.Id) &&
                positions.TryGetValue(node.Id, out Point position))
            {
                DrawNode(canvas, node, position, false);
            }
        }

        // Draw offline and unreachable devices.
        if (disconnectedNodes.Count > 0)
        {
            TextBlock offlineHeading = new()
            {
                Text = "OFFLINE / UNREACHABLE",
                Foreground = Brushes.IndianRed,
                FontSize = 11,
                FontWeight = FontWeights.Bold
            };

            Canvas.SetLeft(offlineHeading, 12);
            Canvas.SetTop(offlineHeading, offlineY - 42);
            canvas.Children.Add(offlineHeading);

            foreach (MeshNetworkNode node in disconnectedNodes)
            {
                DrawNode(
                    canvas,
                    node,
                    positions[node.Id],
                    true);
            }
        }
    }


    private void DrawConnection(Canvas canvas, Point start, Point end, double weight, List<Point> allNodes)
    {
        if (start == end) return;

        Line line = new() { X1 = start.X, Y1 = start.Y, X2 = end.X, Y2 = end.Y, Stroke = new SolidColorBrush(Color.FromRgb(75, 105, 115)), StrokeThickness = 2 };
        Panel.SetZIndex(line, 0);
        canvas.Children.Add(line);

        TextBlock weightLabel = new() { Text = weight.ToString("0.##"), Foreground = Brushes.White, Background = new SolidColorBrush(Color.FromRgb(32, 40, 45)), FontSize = 11, FontWeight = FontWeights.Bold, Padding = new Thickness(5, 2, 5, 2) };
        weightLabel.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        Size size = weightLabel.DesiredSize;

        double midX = (start.X + end.X) / 2;
        double midY = (start.Y + end.Y) / 2;
        double left = midX - (size.Width / 2) - 2;
        double top = midY - (size.Height / 2) - 8;

        // Check if this label overlaps ANY node
        bool overlapsNode = allNodes.Any(pos =>
            Math.Abs(pos.X - (left + size.Width / 2)) < 35 &&
            Math.Abs(pos.Y - (top + size.Height / 2)) < 35);

        // If it overlaps, slide it 60 pixels along the line away from the midpoint
        if (overlapsNode)
        {
            double dx = end.X - start.X;
            double dy = end.Y - start.Y;
            double len = Math.Sqrt(dx * dx + dy * dy);
            if (len > 0)
            {
                left += (dx / len) * 77;
                top += (dy / len) * 77;
            }
        }

        Canvas.SetLeft(weightLabel, left);
        Canvas.SetTop(weightLabel, top);
        Panel.SetZIndex(weightLabel, 999);
        canvas.Children.Add(weightLabel);
    }

    private void DrawNode(
        Canvas canvas,
        MeshNetworkNode node,
        Point position,
        bool disconnected)
    {
        const double nodeSize = 48;

        bool isRelay = string.Equals(
            node.Category,
            "Relay",
            StringComparison.OrdinalIgnoreCase);

        Brush fill;
        Brush stroke;
        string icon;

        if (node.IsGateway)
        {
            fill = new SolidColorBrush(Color.FromRgb(32, 201, 151));
            stroke = Brushes.White;
            icon = "GW";
        }
        else if (!node.IsOnline)
        {
            fill = new SolidColorBrush(Color.FromRgb(90, 45, 45));
            stroke = Brushes.IndianRed;
            icon = "OFF";
        }
        else if (disconnected)
        {
            fill = new SolidColorBrush(Color.FromRgb(100, 75, 30));
            stroke = Brushes.Orange;
            icon = "LOST";
        }
        else if (isRelay)
        {
            fill = new SolidColorBrush(Color.FromRgb(80, 90, 130));
            stroke = new SolidColorBrush(Color.FromRgb(145, 155, 220));
            icon = "R";
        }
        else
        {
            fill = new SolidColorBrush(Color.FromRgb(40, 65, 78));
            stroke = new SolidColorBrush(Color.FromRgb(32, 201, 151));
            icon = "SN";
        }

        Ellipse circle = new()
        {
            Width = nodeSize,
            Height = nodeSize,
            Fill = fill,
            Stroke = stroke,
            StrokeThickness = 2
        };

        Canvas.SetLeft(circle, position.X - nodeSize / 2);
        Canvas.SetTop(circle, position.Y - nodeSize / 2);
        canvas.Children.Add(circle);

        TextBlock nodeIcon = new()
        {
            Text = icon,
            Foreground = Brushes.White,
            FontSize = 12,
            FontWeight = FontWeights.Bold,
            Width = nodeSize,
            TextAlignment = TextAlignment.Center
        };

        Canvas.SetLeft(nodeIcon, position.X - nodeSize / 2);
        Canvas.SetTop(nodeIcon, position.Y - 9);
        canvas.Children.Add(nodeIcon);

        TextBlock label = new()
        {
            Text = node.Id,
            Foreground = !node.IsOnline
    ? Brushes.IndianRed
    : disconnected
        ? Brushes.Orange
        : Brushes.White,
            FontSize = 10,
            Width = 130,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        Canvas.SetLeft(label, position.X - 65);
        Canvas.SetTop(label, position.Y + 23);
        canvas.Children.Add(label);
    }
}
