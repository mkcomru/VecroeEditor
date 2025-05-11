using System;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public class LineShape : Shape
    {
        public Point EndPoint { get; set; }

        public override void Draw(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Stroke, StrokeThickness), Position, EndPoint);

            if (IsSelected)
            {
                var startThumb = new Rect(Position.X - 3, Position.Y - 3, 6, 6);
                var endThumb = new Rect(EndPoint.X - 3, EndPoint.Y - 3, 6, 6);
                drawingContext.DrawRectangle(Brushes.Blue, new Pen(Brushes.Blue, 1), startThumb);
                drawingContext.DrawRectangle(Brushes.Blue, new Pen(Brushes.Blue, 1), endThumb);
            }
        }

        public override bool Contains(Point point)
        {
            const double threshold = 5.0;
            
            double length = Math.Sqrt(Math.Pow(EndPoint.X - Position.X, 2) + Math.Pow(EndPoint.Y - Position.Y, 2));
            if (length == 0) return false;

            double distance = Math.Abs((EndPoint.Y - Position.Y) * point.X - 
                (EndPoint.X - Position.X) * point.Y + 
                EndPoint.X * Position.Y - 
                EndPoint.Y * Position.X) / length;

            // Проверяем, находится ли точка между началом и концом линии
            double dotProduct = ((point.X - Position.X) * (EndPoint.X - Position.X) + 
                                (point.Y - Position.Y) * (EndPoint.Y - Position.Y)) / (length * length);

            return distance <= threshold && dotProduct >= 0 && dotProduct <= 1;
        }

        public override void Move(Vector delta)
        {
            Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
            EndPoint = new Point(EndPoint.X + delta.X, EndPoint.Y + delta.Y);
        }

        public override Shape Clone()
        {
            return new LineShape
            {
                Position = this.Position,
                EndPoint = this.EndPoint,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness
            };
        }
    }
} 