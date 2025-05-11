using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public static class ExampleShapes
    {
        public static List<Shape> CreateExamples()
        {
            var examples = new List<Shape>();
            
            // Прямоугольник
            examples.Add(new RectangleShape
            {
                Position = new Point(50, 50),
                Width = 100,
                Height = 70,
                Fill = Brushes.LightBlue,
                Stroke = Brushes.Blue,
                StrokeThickness = 2
            });
            
            // Эллипс
            examples.Add(new EllipseShape
            {
                Position = new Point(250, 100),
                RadiusX = 80,
                RadiusY = 40,
                Fill = Brushes.LightGreen,
                Stroke = Brushes.Green,
                StrokeThickness = 2
            });
            
            // Линия
            examples.Add(new LineShape
            {
                Position = new Point(50, 200),
                EndPoint = new Point(200, 250),
                Stroke = Brushes.Red,
                StrokeThickness = 3
            });
            
            // Кривая Безье
            examples.Add(new BezierShape
            {
                Position = new Point(250, 180),
                ControlPoint1 = new Point(300, 100),
                ControlPoint2 = new Point(380, 250),
                EndPoint = new Point(450, 200),
                Stroke = Brushes.Purple,
                StrokeThickness = 3
            });
            
            // Ломаная
            var polyline = new PolylineShape
            {
                Position = new Point(100, 300),
                Stroke = Brushes.Orange,
                StrokeThickness = 2
            };
            polyline.Points.Add(new Point(100, 300));
            polyline.Points.Add(new Point(150, 350));
            polyline.Points.Add(new Point(200, 320));
            polyline.Points.Add(new Point(250, 370));
            polyline.Points.Add(new Point(300, 330));
            examples.Add(polyline);
            
            return examples;
        }
    }
} 