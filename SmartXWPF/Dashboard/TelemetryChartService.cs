using SmartX.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace SmartXWPF.Dashboard;

public class TelemetryChartService
{
    private readonly Canvas _canvas;

    private readonly List<double> _environmentalHistory = new();
    private readonly List<double> _powerHistory = new();
    private readonly List<double> _actuatorHistory = new();

    private const int MaxChartReadings = 30;

    public TelemetryChartService(Canvas canvas)
    {
        _canvas = canvas;
    }

    public void AddPoint(
        double value,
        SensorCategory category)
    {
        List<double> history =
            GetHistory(category);

        history.Add(value);

        if (history.Count > MaxChartReadings)
        {
            history.RemoveAt(0);
        }

        Draw(history, category);
    }
    public void ShowHistory(
    SensorCategory category)
    {
        List<double> history =
            GetHistory(category);

        Draw(history, category);
    }

    private List<double> GetHistory(
        SensorCategory category)
    {
        return category switch
        {
            SensorCategory.Environmental =>
                _environmentalHistory,

            SensorCategory.PowerConsumption =>
                _powerHistory,

            SensorCategory.Actuator =>
                _actuatorHistory,

            _ => throw new ArgumentOutOfRangeException(
                nameof(category))
        };
    }

    private void Draw(
        List<double> history,
        SensorCategory category)
    {
        _canvas.Children.Clear();

        double width = _canvas.ActualWidth;
        double height = _canvas.ActualHeight;

        if (width <= 0 ||
            height <= 0 ||
            history.Count == 0)
        {
            return;
        }

        double minimum;
        double maximum;

        if (category == SensorCategory.Actuator)
        {
            minimum = 0;
            maximum = 1;
        }
        else
        {
            minimum = history.Min();
            maximum = history.Max();

            if (Math.Abs(maximum - minimum) < 0.01)
            {
                minimum -= 1;
                maximum += 1;
            }
        }

        double step =
            history.Count > 1
                ? width / (history.Count - 1)
                : width;

        for (int i = 0; i < history.Count; i++)
        {
            double value = history[i];

            double normalized =
                (value - minimum) /
                (maximum - minimum);

            normalized =
                Math.Max(0, Math.Min(1, normalized));

            double x = i * step;
            double y = height - normalized * height;

            if (i > 0)
            {
                double previousValue =
                    history[i - 1];

                double previousNormalized =
                    (previousValue - minimum) /
                    (maximum - minimum);

                previousNormalized =
                    Math.Max(
                        0,
                        Math.Min(
                            1,
                            previousNormalized));

                double previousX =
                    (i - 1) * step;

                double previousY =
                    height -
                    previousNormalized * height;

                Line line = new Line
                {
                    X1 = previousX,
                    Y1 = previousY,
                    X2 = x,
                    Y2 = y,
                    Stroke =
                        new SolidColorBrush(
                            Color.FromRgb(
                                32,
                                201,
                                151)),
                    StrokeThickness = 2
                };

                _canvas.Children.Add(line);
            }

            Ellipse point = new Ellipse
            {
                Width = 6,
                Height = 6,
                Fill =
                    new SolidColorBrush(
                        Color.FromRgb(
                            32,
                            201,
                            151))
            };

            Canvas.SetLeft(point, x - 3);
            Canvas.SetTop(point, y - 3);

            _canvas.Children.Add(point);
        }
    }
}