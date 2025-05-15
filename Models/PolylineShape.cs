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
        
        // Флаг, указывающий, является ли ломаная замкнутой фигурой
        public bool IsClosed { get; set; } = false;

        public override void Draw(WriteableBitmap bitmap)
        {
            if (Points.Count < 2) return;

            // Рисуем заливку, если фигура замкнута
            if (IsClosed && Points.Count >= 3)
            {
                Color fillColor = GetColorFromBrush(Fill);
                GraphicsAlgorithms.DrawFilledPolygon(bitmap, Points.ToArray(), fillColor);
            }

            // Рисуем ломаную линию
            Color strokeColor = GetColorFromBrush(Stroke);
            
            if (IsClosed && Points.Count >= 2)
            {
                // Рисуем замкнутый контур
                for (int i = 0; i < Points.Count; i++)
                {
                    int nextIndex = (i + 1) % Points.Count;
                    GraphicsAlgorithms.DrawLine(bitmap, 
                        (int)Points[i].X, (int)Points[i].Y, 
                        (int)Points[nextIndex].X, (int)Points[nextIndex].Y,
                        strokeColor);
                }
            }
            else
            {
                // Рисуем открытую ломаную
                GraphicsAlgorithms.DrawPolyline(bitmap, Points, strokeColor);
            }
            
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

            // Если фигура замкнута и имеет заливку, проверяем, находится ли точка внутри многоугольника
            if (IsClosed && Points.Count >= 3)
            {
                if (IsPointInPolygon(point, Points.ToArray()))
                {
                    return true;
                }
            }

            const double threshold = 5.0;
            
            // Проверяем близость к контуру
            int lastIndex = IsClosed ? Points.Count : Points.Count - 1;
            for (int i = 0; i < lastIndex; i++)
            {
                Point p1 = Points[i];
                Point p2 = Points[(i + 1) % Points.Count];

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
        
        // Метод для проверки, находится ли точка внутри многоугольника
        private bool IsPointInPolygon(Point point, Point[] polygon)
        {
            if (polygon.Length < 3) return false;
            
            int i, j;
            bool result = false;
            for (i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if ((polygon[i].Y > point.Y) != (polygon[j].Y > point.Y) &&
                    (point.X < (polygon[j].X - polygon[i].X) * (point.Y - polygon[i].Y) / 
                    (polygon[j].Y - polygon[i].Y) + polygon[i].X))
                {
                    result = !result;
                }
            }
            
            return result;
        }

        // Метод для замыкания ломаной линии
        public void Close()
        {
            if (Points.Count >= 3)
            {
                IsClosed = true;
            }
        }
        
        // Метод для размыкания ломаной линии
        public void Open()
        {
            IsClosed = false;
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
                Fill = this.Fill,
                StrokeThickness = this.StrokeThickness,
                Points = this.Points.Select(p => new Point(p.X, p.Y)).ToList(),
                IsClosed = this.IsClosed
            };
        }
    }
} 