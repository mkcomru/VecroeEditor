using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Graphics;

namespace VectorEditor.Models
{
    public class LineShape : Shape
    {
        public Point EndPoint { get; set; }
        
        // Выбранная точка для изменения размера/поворота
        public enum LineHandleType { Start, End, None }
        public LineHandleType SelectedHandle { get; private set; } = LineHandleType.None;
        
        // Сохраняем оригинальные точки для поворота
        private Point originalStartPoint;
        private Point originalEndPoint;
        private bool hasOriginalPoints = false;

        public override void Draw(WriteableBitmap bitmap)
        {
            // Рисуем линию с использованием алгоритма Брезенхема
            Color strokeColor = GetColorFromBrush(Stroke);
            
            // Используем алгоритм Брезенхема для отрисовки линии
            GraphicsAlgorithms.DrawLine(bitmap, 
                (int)Position.X, (int)Position.Y, 
                (int)EndPoint.X, (int)EndPoint.Y, 
                strokeColor);

            if (IsSelected)
            {
                // Рисуем более заметные маркеры на концах линии
                Color startColor = Colors.LightBlue;
                Color endColor = Colors.LightGreen;
                
                // Рисуем маркеры используя наш алгоритм DrawRectangle
                GraphicsAlgorithms.DrawRectangle(bitmap,
                    (int)(Position.X - 4), (int)(Position.Y - 4),
                    8, 8,
                    startColor, true);
                
                GraphicsAlgorithms.DrawRectangle(bitmap,
                    (int)(EndPoint.X - 4), (int)(EndPoint.Y - 4),
                    8, 8,
                    endColor, true);
                
                // Рисуем контур маркеров
                GraphicsAlgorithms.DrawRectangle(bitmap,
                    (int)(Position.X - 4), (int)(Position.Y - 4),
                    8, 8,
                    Colors.Blue, false);
                
                GraphicsAlgorithms.DrawRectangle(bitmap,
                    (int)(EndPoint.X - 4), (int)(EndPoint.Y - 4),
                    8, 8,
                    Colors.Green, false);
                
                // Рисуем маркер вращения (в центре линии)
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

        public override bool Contains(Point point)
        {
            // Проверяем, не нажат ли маркер вращения
            if (IsSelected && IsRotationHandleHit(point))
            {
                return true;
            }
            
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
                StrokeThickness = this.StrokeThickness,
                RotationAngle = this.RotationAngle
            };
        }
        
        // Проверяет, находится ли точка point рядом с одним из маркеров линии
        public bool SelectHandle(Point point)
        {
            const double threshold = 8.0; // Увеличиваем зону захвата для удобства
            
            // Проверка на расстояние до начальной точки
            double distanceToStart = Distance(point, Position);
            if (distanceToStart <= threshold)
            {
                SelectedHandle = LineHandleType.Start;
                return true;
            }
            
            // Проверка на расстояние до конечной точки
            double distanceToEnd = Distance(point, EndPoint);
            if (distanceToEnd <= threshold)
            {
                SelectedHandle = LineHandleType.End;
                return true;
            }
            
            SelectedHandle = LineHandleType.None;
            return false;
        }
        
        // Перемещает выбранную точку линии
        public void ResizeLine(Point newPosition, bool keepRatio)
        {
            if (SelectedHandle == LineHandleType.Start)
            {
                if (keepRatio)
                {
                    // При нажатой клавише Shift ограничиваем угол до 45 градусов
                    Position = ConstrainToAngle(newPosition, EndPoint);
                }
                else
                {
                    Position = newPosition;
                }
            }
            else if (SelectedHandle == LineHandleType.End)
            {
                if (keepRatio)
                {
                    // При нажатой клавише Shift ограничиваем угол до 45 градусов
                    EndPoint = ConstrainToAngle(newPosition, Position);
                }
                else
                {
                    EndPoint = newPosition;
                }
            }
        }
        
        // Сбрасывает выбранную точку
        public void ClearHandleSelection()
        {
            SelectedHandle = LineHandleType.None;
        }
        
        // Вычисляет расстояние между двумя точками
        private double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        
        // Ограничивает точку к 45-градусным углам относительно базовой точки
        private Point ConstrainToAngle(Point point, Point basePoint)
        {
            Vector delta = new Vector(point.X - basePoint.X, point.Y - basePoint.Y);
            
            // Рассчитываем угол в радианах
            double angle = Math.Atan2(delta.Y, delta.X);
            
            // Округляем угол до ближайших 45 градусов (π/4 радиан)
            double snappedAngle = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4);
            
            // Вычисляем длину исходного вектора
            double length = delta.Length;
            
            // Создаем новую точку с ограниченным углом
            return new Point(
                basePoint.X + length * Math.Cos(snappedAngle),
                basePoint.Y + length * Math.Sin(snappedAngle)
            );
        }
        
        // Получение центра линии
        public Point GetCenter()
        {
            return new Point(
                (Position.X + EndPoint.X) / 2,
                (Position.Y + EndPoint.Y) / 2
            );
        }
        
        // Получение позиции маркера вращения
        private Point GetRotationHandlePosition()
        {
            Point center = GetCenter();
            
            // Вычисляем вектор, перпендикулярный линии
            double dx = EndPoint.X - Position.X;
            double dy = EndPoint.Y - Position.Y;
            
            // Перпендикулярный вектор (-dy, dx)
            double perpDx = -dy;
            double perpDy = dx;
            
            // Нормализуем вектор и задаем длину
            double length = Math.Sqrt(perpDx * perpDx + perpDy * perpDy);
            double normalizedPerpDx = perpDx / length * 20; // 20 - расстояние до маркера
            double normalizedPerpDy = perpDy / length * 20;
            
            // Создаем точку маркера вращения
            Point rotationHandlePos = new Point(
                center.X + normalizedPerpDx,
                center.Y + normalizedPerpDy
            );
            
            return rotationHandlePos;
        }
        
        // Проверка, находится ли точка в области маркера вращения
        public bool IsRotationHandleHit(Point point)
        {
            if (!IsSelected) return false;
            
            Point rotationHandlePos = GetRotationHandlePosition();
            return Distance(point, rotationHandlePos) <= 8;
        }
        
        // Переопределяем метод вращения для поворота линии
        public override void Rotate(double angleDelta)
        {
            base.Rotate(angleDelta);
            
            // Сохраняем оригинальные точки при первом повороте
            if (!hasOriginalPoints)
            {
                originalStartPoint = Position;
                originalEndPoint = EndPoint;
                hasOriginalPoints = true;
            }
            
            // Поворачиваем обе точки вокруг центра
            Point center = GetCenter();
            
            // Поворачиваем точки относительно центра с учетом полного угла поворота
            Position = RotatePoint(originalStartPoint, center, RotationAngle);
            EndPoint = RotatePoint(originalEndPoint, center, RotationAngle);
        }
    }
} 