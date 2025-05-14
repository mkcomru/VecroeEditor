using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Graphics;

namespace VectorEditor.Models
{
    public class PolylineShape : Shape
    {
        public List<Point> Points { get; set; } = new List<Point>();

        public override void Draw(WriteableBitmap bitmap)
        {
            if (Points.Count < 2) return;

            // Рисуем ломаную линию
            Color strokeColor = GetColorFromBrush(Stroke);
            GraphicsAlgorithms.DrawPolyline(bitmap, Points, strokeColor);
            
            if (IsSelected)
            {
                foreach (var point in Points)
                {
                    // Рисуем маркеры в каждой точке ломаной
                    GraphicsAlgorithms.DrawRectangle(bitmap,
                        (int)(point.X - 3), (int)(point.Y - 3),
                        6, 6,
                        Colors.Blue, true);
                    
                    GraphicsAlgorithms.DrawRectangle(bitmap,
                        (int)(point.X - 3), (int)(point.Y - 3),
                        6, 6,
                        Colors.Black, false);
                }
            }
        }

        public override bool Contains(Point point)
        {
            if (Points.Count < 2) return false;

            const double threshold = 5.0;
            
            for (int i = 0; i < Points.Count - 1; i++)
            {
                Point p1 = Points[i];
                Point p2 = Points[i + 1];

                double length = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (length == 0) continue;

                double distance = Math.Abs((p2.Y - p1.Y) * point.X - 
                    (p2.X - p1.X) * point.Y + 
                    p2.X * p1.Y - 
                    p2.Y * p1.X) / length;

                // Проверяем, находится ли точка между началом и концом отрезка
                double dotProduct = ((point.X - p1.X) * (p2.X - p1.X) + 
                                    (point.Y - p1.Y) * (p2.Y - p1.Y)) / (length * length);

                if (distance <= threshold && dotProduct >= 0 && dotProduct <= 1)
                {
                    return true;
                }
            }
            
            return false;
        }

        public override void Move(Vector delta)
        {
            Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
            
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new Point(Points[i].X + delta.X, Points[i].Y + delta.Y);
            }
        }

        public override Shape Clone()
        {
            return new PolylineShape
            {
                Position = this.Position,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness,
                Points = this.Points.Select(p => new Point(p.X, p.Y)).ToList()
            };
        }
    }
} 