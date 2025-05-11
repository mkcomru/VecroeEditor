using System;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public class LineShape : Shape
    {
        public Point EndPoint { get; set; }
        
        // Выбранная точка для изменения размера/поворота
        public enum LineHandleType { Start, End, None }
        public LineHandleType SelectedHandle { get; private set; } = LineHandleType.None;

        public override void Draw(DrawingContext drawingContext)
        {
            drawingContext.DrawLine(new Pen(Stroke, StrokeThickness), Position, EndPoint);

            if (IsSelected)
            {
                // Рисуем более заметные маркеры на концах линии
                var startThumb = new Rect(Position.X - 4, Position.Y - 4, 8, 8);
                var endThumb = new Rect(EndPoint.X - 4, EndPoint.Y - 4, 8, 8);
                
                // Используем разные цвета для начальной и конечной точек
                drawingContext.DrawRectangle(Brushes.LightBlue, new Pen(Brushes.Blue, 1), startThumb);
                drawingContext.DrawRectangle(Brushes.LightGreen, new Pen(Brushes.Green, 1), endThumb);
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
    }
} 