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
    // Draws the supplied network nodes and connections onto a WPF Canvas.
    public void Draw(
        Canvas canvas,
        List<MeshNetworkNode> nodes,
        List<MeshNetworkConnection> connections)
    {
        // Remove the previous drawing before rendering the new topology.
        canvas.Children.Clear();

        if (nodes.Count == 0)
            return;

        // Use the actual canvas dimensions, with sensible fallbacks.
        double width = canvas.ActualWidth > 0 ? canvas.ActualWidth : 700;
        double height = canvas.ActualHeight > 0 ? canvas.ActualHeight : 260;

        // Define the positions of all nodes.
        Dictionary<string, Point> positions =
            CalculateNodePositions(nodes, width, height);

        // Draw connections first so they appear behind the nodes.
        foreach (MeshNetworkConnection connection in connections)
        {
            if (!positions.TryGetValue(connection.DeviceA, out Point start) ||
                !positions.TryGetValue(connection.DeviceB, out Point end))
            {
                continue;
            }

            DrawConnection(canvas, start, end, connection.Weight);
        }

        // Draw the nodes over the connections.
        foreach (MeshNetworkNode node in nodes)
        {
            if (positions.TryGetValue(node.Id, out Point position))
            {
                DrawNode(canvas, node, position);
            }
        }
    }

    // Assigns the gateway to the top and distributes sensors underneath it.
    private Dictionary<string, Point> CalculateNodePositions(
        List<MeshNetworkNode> nodes,
        double width,
        double height)
    {
        Dictionary<string, Point> positions = new();

        MeshNetworkNode? gateway =
            nodes.FirstOrDefault(node => node.IsGateway);

        List<MeshNetworkNode> sensors =
            nodes.Where(node => !node.IsGateway).ToList();

        // Position the gateway centrally near the top.
        if (gateway != null)
        {
            positions[gateway.Id] = new Point(width / 2, 55);
        }

        // Spread sensors evenly across the lower part of the canvas.
        for (int i = 0; i < sensors.Count; i++)
        {
            double spacing = width / (sensors.Count + 1);
            double x = spacing * (i + 1);

            positions[sensors[i].Id] = new Point(
                x,
                Math.Min(height - 65, 190));
        }

        return positions;
    }

    // Draws a weighted connection between two nodes.
    private void DrawConnection(
        Canvas canvas,
        Point start,
        Point end,
        double weight)
    {
        Line line = new()
        {
            X1 = start.X,
            Y1 = start.Y,
            X2 = end.X,
            Y2 = end.Y,
            Stroke = new SolidColorBrush(Color.FromRgb(75, 105, 115)),
            StrokeThickness = 2
        };

        canvas.Children.Add(line);

        // Place the weight label at the midpoint of the connection.
        TextBlock weightLabel = new()
        {
            Text = weight.ToString("0.##"),
            Foreground = Brushes.White,
            Background = new SolidColorBrush(Color.FromRgb(32, 40, 45)),
            FontSize = 11,
            FontWeight = FontWeights.Bold,
            Padding = new Thickness(5, 2, 5, 2)
        };

        Canvas.SetLeft(weightLabel, (start.X + end.X) / 2 - 10);
        Canvas.SetTop(weightLabel, (start.Y + end.Y) / 2 - 12);

        canvas.Children.Add(weightLabel);
    }

    // Draws a circular node with its label and identifier.
    private void DrawNode(
        Canvas canvas,
        MeshNetworkNode node,
        Point position)
    {
        const double nodeSize = 48;

        Ellipse circle = new()
        {
            Width = nodeSize,
            Height = nodeSize,
            Fill = node.IsGateway
                ? new SolidColorBrush(Color.FromRgb(32, 201, 151))
                : new SolidColorBrush(Color.FromRgb(40, 65, 78)),
            Stroke = node.IsGateway
                ? Brushes.White
                : new SolidColorBrush(Color.FromRgb(32, 201, 151)),
            StrokeThickness = 2
        };

        Canvas.SetLeft(circle, position.X - nodeSize / 2);
        Canvas.SetTop(circle, position.Y - nodeSize / 2);

        canvas.Children.Add(circle);

        // Show a short identifier inside the node.
        TextBlock nodeIcon = new()
        {
            Text = node.IsGateway ? "GW" : "SN",
            Foreground = node.IsGateway
                ? new SolidColorBrush(Color.FromRgb(16, 20, 24))
                : Brushes.White,
            FontSize = 13,
            FontWeight = FontWeights.Bold,
            Width = nodeSize,
            TextAlignment = TextAlignment.Center
        };

        Canvas.SetLeft(nodeIcon, position.X - nodeSize / 2);
        Canvas.SetTop(nodeIcon, position.Y - 9);

        canvas.Children.Add(nodeIcon);

        // Show the friendly name beneath the node.
        TextBlock label = new()
        {
            Text = node.Label,
            Foreground = Brushes.White,
            FontSize = 10,
            Width = 130,
            TextAlignment = TextAlignment.Center,
            TextWrapping = TextWrapping.Wrap
        };

        Canvas.SetLeft(label, position.X - 65);
        Canvas.SetTop(label, position.Y + 29);

        canvas.Children.Add(label);
    }
}