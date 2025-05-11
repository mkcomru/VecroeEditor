using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public class BezierShape : Shape
    {
        public Point ControlPoint1 { get; set; }
        public Point ControlPoint2 { get; set; }
        public Point EndPoint { get; set; }
        
        private int? selectedPointIndex = null;
        
        public void SelectPoint(Point point)
        {
            const double threshold = 10.0;
            var points = new[] { Position, ControlPoint1, ControlPoint2, EndPoint };
            
            for (int i = 0; i < points.Length; i++)
            {
                double distance = CalculateDistance(point, points[i]);
                if (distance <= threshold)
                {
                    selectedPointIndex = i;
                    return;
                }
            }
            
            selectedPointIndex = null;
        }
        
        public void MoveSelectedPoint(Point newPosition)
        {
            if (!selectedPointIndex.HasValue) return;
            
            switch (selectedPointIndex.Value)
            {
                case 0: Position = newPosition; break;
                case 1: ControlPoint1 = newPosition; break;
                case 2: ControlPoint2 = newPosition; break;
                case 3: EndPoint = newPosition; break;
            }
        }
        
        public bool HasSelectedPoint => selectedPointIndex.HasValue;
        
        public void ClearPointSelection()
        {
            selectedPointIndex = null;
        }
        
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }

        public override void Draw(DrawingContext drawingContext)
        {
            var geometry = new StreamGeometry();
            using (StreamGeometryContext context = geometry.Open())
            {
                context.BeginFigure(Position, false, false);
                context.BezierTo(ControlPoint1, ControlPoint2, EndPoint, true, false);
            }

            drawingContext.DrawGeometry(null, new Pen(Stroke, StrokeThickness), geometry);
            
            if (IsSelected)
            {
                // Нарисуем управляющие точки и линии к ним
                drawingContext.DrawLine(new Pen(Brushes.Gray, 1), Position, ControlPoint1);
                drawingContext.DrawLine(new Pen(Brushes.Gray, 1), EndPoint, ControlPoint2);
                
                var points = new[] { Position, ControlPoint1, ControlPoint2, EndPoint };
                for (int i = 0; i < points.Length; i++)
                {
                    var point = points[i];
                    bool isSelected = selectedPointIndex.HasValue && selectedPointIndex.Value == i;
                    
                    var thumbRect = new Rect(point.X - 3, point.Y - 3, 6, 6);
                    Brush fillBrush = isSelected ? Brushes.Red : 
                                     (i == 0 || i == 3) ? Brushes.Blue : Brushes.Green;
                    
                    drawingContext.DrawRectangle(
                        fillBrush, 
                        new Pen(Brushes.Black, 1), 
                        thumbRect);
                }
            }
        }

        public override bool Contains(Point point)
        {
            const double threshold = 5.0;
            const int steps = 30;
            
            // Проверка на совпадение с контрольными точками
            var controlPoints = new[] { Position, ControlPoint1, ControlPoint2, EndPoint };
            foreach (var cp in controlPoints)
            {
                if (Math.Sqrt(Math.Pow(cp.X - point.X, 2) + Math.Pow(cp.Y - point.Y, 2)) <= threshold)
                {
                    return true;
                }
            }
            
            // Аппроксимация кривой линейными сегментами
            for (int i = 0; i < steps; i++)
            {
                double t1 = (double)i / steps;
                double t2 = (double)(i + 1) / steps;
                
                Point p1 = CalculateBezierPoint(t1);
                Point p2 = CalculateBezierPoint(t2);
                
                double length = Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
                if (length == 0) continue;

                double distance = Math.Abs((p2.Y - p1.Y) * point.X - 
                    (p2.X - p1.X) * point.Y + 
                    p2.X * p1.Y - 
                    p2.Y * p1.X) / length;

                // Проверяем, находится ли точка между началом и концом сегмента
                double dotProduct = ((point.X - p1.X) * (p2.X - p1.X) + 
                                    (point.Y - p1.Y) * (p2.Y - p1.Y)) / (length * length);

                if (distance <= threshold && dotProduct >= 0 && dotProduct <= 1)
                {
                    return true;
                }
            }
            
            return false;
        }

        private Point CalculateBezierPoint(double t)
        {
            double u = 1 - t;
            double tt = t * t;
            double uu = u * u;
            double uuu = uu * u;
            double ttt = tt * t;
            
            double x = uuu * Position.X + 
                      3 * uu * t * ControlPoint1.X + 
                      3 * u * tt * ControlPoint2.X + 
                      ttt * EndPoint.X;
                      
            double y = uuu * Position.Y + 
                      3 * uu * t * ControlPoint1.Y + 
                      3 * u * tt * ControlPoint2.Y + 
                      ttt * EndPoint.Y;
                      
            return new Point(x, y);
        }

        public override void Move(Vector delta)
        {
            if (selectedPointIndex.HasValue)
            {
                MoveSelectedPoint(new Point(
                    (selectedPointIndex.Value == 0 ? Position.X : 
                     selectedPointIndex.Value == 1 ? ControlPoint1.X : 
                     selectedPointIndex.Value == 2 ? ControlPoint2.X : EndPoint.X) + delta.X,
                    (selectedPointIndex.Value == 0 ? Position.Y : 
                     selectedPointIndex.Value == 1 ? ControlPoint1.Y : 
                     selectedPointIndex.Value == 2 ? ControlPoint2.Y : EndPoint.Y) + delta.Y
                ));
            }
            else
            {
                Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
                ControlPoint1 = new Point(ControlPoint1.X + delta.X, ControlPoint1.Y + delta.Y);
                ControlPoint2 = new Point(ControlPoint2.X + delta.X, ControlPoint2.Y + delta.Y);
                EndPoint = new Point(EndPoint.X + delta.X, EndPoint.Y + delta.Y);
            }
        }

        public override Shape Clone()
        {
            return new BezierShape
            {
                Position = this.Position,
                ControlPoint1 = this.ControlPoint1,
                ControlPoint2 = this.ControlPoint2,
                EndPoint = this.EndPoint,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness
            };
        }
    }
} 