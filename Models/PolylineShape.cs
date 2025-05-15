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
        
        // Точки ломаной до применения поворота
        private List<Point> originalPoints = new List<Point>();
        
        // Выбранная точка для редактирования
        private int selectedPointIndex = -1;
        
        public int SelectedPointIndex => selectedPointIndex;

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
                // Рисуем маркеры в каждой точке ломаной
                for (int i = 0; i < Points.Count; i++)
                {
                    var point = Points[i];
                    Color handleColor = (i == selectedPointIndex) ? Colors.Red : Colors.Blue;
                    
                    GraphicsAlgorithms.DrawRectangle(bitmap,
                        (int)(point.X - 3), (int)(point.Y - 3),
                        6, 6,
                        handleColor, true);
                    
                    GraphicsAlgorithms.DrawRectangle(bitmap,
                        (int)(point.X - 3), (int)(point.Y - 3),
                        6, 6,
                        Colors.Black, false);
                }
                
                // Рисуем маркер вращения, если есть хотя бы 2 точки
                if (Points.Count >= 2)
                {
                    Point center = GetCenter();
                    Point rotationHandlePos = GetRotationHandlePosition();
                    
                    // Линия от центра к маркеру вращения
                    GraphicsAlgorithms.DrawLine(bitmap,
                        (int)center.X, (int)center.Y,
                        (int)rotationHandlePos.X, (int)rotationHandlePos.Y,
                        Colors.Green);
                    
                    // Маркер вращения (круг)
                    GraphicsAlgorithms.DrawCircle(bitmap,
                        (int)rotationHandlePos.X, (int)rotationHandlePos.Y,
                        5, Colors.Green, true);
                    GraphicsAlgorithms.DrawCircle(bitmap,
                        (int)rotationHandlePos.X, (int)rotationHandlePos.Y,
                        5, Colors.Black, false);
                }
            }
        }

        public override bool Contains(Point point)
        {
            if (Points.Count < 2) return false;
            
            // Если выбрана, проверяем маркер вращения
            if (IsSelected && IsRotationHandleHit(point))
            {
                return true;
            }
            
            // Проверяем, не нажат ли один из маркеров точек
            if (IsSelected)
            {
                for (int i = 0; i < Points.Count; i++)
                {
                    if (CalculateDistance(point, Points[i]) <= 10)
                    {
                        selectedPointIndex = i;
                        return true;
                    }
                }
                
                // Если не нажат ни один маркер, сбрасываем выбор точки
                selectedPointIndex = -1;
            }

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
            // Если выбрана точка, перемещаем только её
            if (selectedPointIndex >= 0 && selectedPointIndex < Points.Count)
            {
                Points[selectedPointIndex] = new Point(
                    Points[selectedPointIndex].X + delta.X,
                    Points[selectedPointIndex].Y + delta.Y);
                
                // Обновляем позицию всей фигуры, если это первая точка
                if (selectedPointIndex == 0)
                {
                    Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
                }
                
                return;
            }
            
            // Перемещаем все точки
            Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
            
            for (int i = 0; i < Points.Count; i++)
            {
                Points[i] = new Point(Points[i].X + delta.X, Points[i].Y + delta.Y);
            }
        }
        
        // Получение центра ломаной
        public Point GetCenter()
        {
            if (Points.Count == 0)
                return Position;
                
            double sumX = 0;
            double sumY = 0;
            
            foreach (var point in Points)
            {
                sumX += point.X;
                sumY += point.Y;
            }
            
            return new Point(sumX / Points.Count, sumY / Points.Count);
        }
        
        // Получение позиции маркера вращения
        private Point GetRotationHandlePosition()
        {
            Point center = GetCenter();
            
            // Находим самую верхнюю точку
            Point topPoint = Points.OrderBy(p => p.Y).First();
            
            // Создаем маркер вращения над самой верхней точкой
            Point rotationHandlePos = new Point(center.X, topPoint.Y - 20);
            
            // Если есть поворот, применяем его к маркеру
            if (RotationAngle != 0)
            {
                rotationHandlePos = RotatePoint(rotationHandlePos, center, RotationAngle);
            }
            
            return rotationHandlePos;
        }
        
        // Проверка, находится ли точка в области маркера вращения
        public bool IsRotationHandleHit(Point point)
        {
            if (!IsSelected || Points.Count < 2) return false;
            
            Point rotationHandlePos = GetRotationHandlePosition();
            return CalculateDistance(point, rotationHandlePos) <= 8;
        }
        
        // Вычисление расстояния между двумя точками
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        
        // Переопределяем метод вращения для поворота всех точек
        public override void Rotate(double angleDelta)
        {
            base.Rotate(angleDelta);
            
            // Сохраняем оригинальные точки при первом повороте
            if (originalPoints.Count == 0 && Points.Count > 0)
            {
                originalPoints = Points.Select(p => new Point(p.X, p.Y)).ToList();
            }
            
            // Поворачиваем все точки вокруг центра
            Point center = GetCenter();
            
            for (int i = 0; i < Points.Count; i++)
            {
                // Если есть оригинальные точки, используем их как основу для поворота
                Point basePoint = (originalPoints.Count > i) ? originalPoints[i] : Points[i];
                Points[i] = RotatePoint(basePoint, center, RotationAngle);
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
                IsClosed = this.IsClosed,
                RotationAngle = this.RotationAngle
            };
        }
    }
} 