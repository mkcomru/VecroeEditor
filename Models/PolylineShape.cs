using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public class PolylineShape : Shape
    {
        public List<Point> Points { get; set; } = new List<Point>();

        public override void Draw(DrawingContext drawingContext)
        {
            if (Points.Count < 2) return;

            var geometry = new StreamGeometry();
            using (var context = geometry.Open())
            {
                context.BeginFigure(Points[0], false, false);
                context.PolyLineTo(Points.Skip(1).ToList(), true, false);
            }

            drawingContext.DrawGeometry(null, new Pen(Stroke, StrokeThickness), geometry);
            
            if (IsSelected)
            {
                foreach (var point in Points)
                {
                    var thumbRect = new Rect(point.X - 3, point.Y - 3, 6, 6);
                    drawingContext.DrawRectangle(Brushes.Blue, new Pen(Brushes.Blue, 1), thumbRect);
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