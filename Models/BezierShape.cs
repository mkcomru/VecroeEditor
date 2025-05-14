using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Graphics;

namespace VectorEditor.Models
{
    public class BezierShape : Shape
    {
        public Point EndPoint { get; set; }
        public Point ControlPoint1 { get; set; }
        public Point ControlPoint2 { get; set; }
        
        private BezierPointType selectedPointType = BezierPointType.None;
        
        public enum BezierPointType
        {
            None,
            Start,
            End,
            Control1,
            Control2
        }
        
        public bool HasSelectedPoint => selectedPointType != BezierPointType.None;

        public override void Draw(WriteableBitmap bitmap)
        {
            Color strokeColor = GetColorFromBrush(Stroke);
            
            // Рисуем кривую Безье
            GraphicsAlgorithms.DrawBezier(bitmap, Position, ControlPoint1, ControlPoint2, EndPoint, strokeColor);
            
            if (IsSelected)
            {
                // Рисуем маркеры для каждой точки кривой
                DrawControlPoint(bitmap, Position, selectedPointType == BezierPointType.Start);
                DrawControlPoint(bitmap, EndPoint, selectedPointType == BezierPointType.End);
                
                // Рисуем контрольные точки и соединяющие линии
                GraphicsAlgorithms.DrawLine(bitmap, 
                    (int)Position.X, (int)Position.Y, 
                    (int)ControlPoint1.X, (int)ControlPoint1.Y, 
                    Colors.Gray);
                    
                GraphicsAlgorithms.DrawLine(bitmap, 
                    (int)EndPoint.X, (int)EndPoint.Y, 
                    (int)ControlPoint2.X, (int)ControlPoint2.Y, 
                    Colors.Gray);
                
                DrawControlPoint(bitmap, ControlPoint1, selectedPointType == BezierPointType.Control1, true);
                DrawControlPoint(bitmap, ControlPoint2, selectedPointType == BezierPointType.Control2, true);
            }
        }
        
        private void DrawControlPoint(WriteableBitmap bitmap, Point point, bool isSelected, bool isControlPoint = false)
        {
            Color fillColor = isControlPoint 
                ? (isSelected ? Colors.Yellow : Colors.LightYellow)
                : (isSelected ? Colors.Red : Colors.Blue);
                
            GraphicsAlgorithms.DrawRectangle(bitmap,
                (int)(point.X - 3), (int)(point.Y - 3),
                6, 6,
                fillColor, true);
                
            GraphicsAlgorithms.DrawRectangle(bitmap,
                (int)(point.X - 3), (int)(point.Y - 3),
                6, 6,
                Colors.Black, false);
        }

        public override bool Contains(Point point)
        {
            const double threshold = 5.0;
            const int segments = 50; // Количество сегментов для аппроксимации кривой
            
            Point prev = Position;
            
            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double u = 1 - t;
                double tt = t * t;
                double uu = u * u;
                double uuu = uu * u;
                double ttt = tt * t;
                
                double x = uuu * Position.X + 3 * uu * t * ControlPoint1.X + 3 * u * tt * ControlPoint2.X + ttt * EndPoint.X;
                double y = uuu * Position.Y + 3 * uu * t * ControlPoint1.Y + 3 * u * tt * ControlPoint2.Y + ttt * EndPoint.Y;
                
                Point current = new Point(x, y);
                
                // Проверяем расстояние до текущего сегмента
                double length = Math.Sqrt(Math.Pow(current.X - prev.X, 2) + Math.Pow(current.Y - prev.Y, 2));
                if (length == 0)
                {
                    prev = current;
                    continue;
                }

                double distance = Math.Abs((current.Y - prev.Y) * point.X - 
                    (current.X - prev.X) * point.Y + 
                    current.X * prev.Y - 
                    current.Y * prev.X) / length;

                // Проверяем, находится ли точка между началом и концом сегмента
                double dotProduct = ((point.X - prev.X) * (current.X - prev.X) + 
                                    (point.Y - prev.Y) * (current.Y - prev.Y)) / (length * length);

                if (distance <= threshold && dotProduct >= 0 && dotProduct <= 1)
                {
                    return true;
                }
                
                prev = current;
            }
            
            return false;
        }
        
        public bool SelectPoint(Point point)
        {
            const double threshold = 10.0;
            
            if (Distance(point, Position) <= threshold)
            {
                selectedPointType = BezierPointType.Start;
                return true;
            }
            if (Distance(point, EndPoint) <= threshold)
            {
                selectedPointType = BezierPointType.End;
                return true;
            }
            if (Distance(point, ControlPoint1) <= threshold)
            {
                selectedPointType = BezierPointType.Control1;
                return true;
            }
            if (Distance(point, ControlPoint2) <= threshold)
            {
                selectedPointType = BezierPointType.Control2;
                return true;
            }
            
            selectedPointType = BezierPointType.None;
            return false;
        }
        
        public void MoveSelectedPoint(Point newPosition)
        {
            switch (selectedPointType)
            {
                case BezierPointType.Start:
                    Position = newPosition;
                    break;
                case BezierPointType.End:
                    EndPoint = newPosition;
                    break;
                case BezierPointType.Control1:
                    ControlPoint1 = newPosition;
                    break;
                case BezierPointType.Control2:
                    ControlPoint2 = newPosition;
                    break;
            }
        }
        
        public void ClearPointSelection()
        {
            selectedPointType = BezierPointType.None;
        }
        
        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        public override void Move(Vector delta)
        {
            Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
            EndPoint = new Point(EndPoint.X + delta.X, EndPoint.Y + delta.Y);
            ControlPoint1 = new Point(ControlPoint1.X + delta.X, ControlPoint1.Y + delta.Y);
            ControlPoint2 = new Point(ControlPoint2.X + delta.X, ControlPoint2.Y + delta.Y);
        }

        public override Shape Clone()
        {
            return new BezierShape
            {
                Position = this.Position,
                EndPoint = this.EndPoint,
                ControlPoint1 = this.ControlPoint1,
                ControlPoint2 = this.ControlPoint2,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness
            };
        }
    }
} 