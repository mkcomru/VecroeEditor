using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Graphics;

namespace VectorEditor.Models
{
    public class EllipseShape : Shape
    {
        public double RadiusX { get; set; }
        public double RadiusY { get; set; }
        
        // Для изменения размера
        private ResizeHandle? selectedHandle;
        
        public ResizeHandle? SelectedResizeHandle => selectedHandle;
        
        public void SelectResizeHandle(Point point)
        {
            const double threshold = 10.0;
            selectedHandle = null;
            
            if (!IsSelected) return;
            
            // Проверяем, нажат ли маркер вращения
            if (IsRotationHandleHit(point))
            {
                return;
            }
            
            // Проверяем точки по периметру эллипса
            var handles = GetResizeHandles();
            foreach (var handle in handles)
            {
                if (CalculateDistance(point, handle.Position) <= threshold)
                {
                    selectedHandle = handle;
                    return;
                }
            }
        }
        
        public void ClearResizeHandleSelection()
        {
            selectedHandle = null;
        }
        
        public ResizeHandle[] GetResizeHandles()
        {
            Point center = Position;
            Point left = new Point(Position.X - RadiusX, Position.Y);
            Point right = new Point(Position.X + RadiusX, Position.Y);
            Point top = new Point(Position.X, Position.Y - RadiusY);
            Point bottom = new Point(Position.X, Position.Y + RadiusY);
            
            // Если эллипс повернут, поворачиваем и маркеры
            if (RotationAngle != 0)
            {
                left = RotatePoint(left, center, RotationAngle);
                right = RotatePoint(right, center, RotationAngle);
                top = RotatePoint(top, center, RotationAngle);
                bottom = RotatePoint(bottom, center, RotationAngle);
            }
            
            return new[]
            {
                new ResizeHandle { Position = left, Type = ResizeHandleType.Left },
                new ResizeHandle { Position = right, Type = ResizeHandleType.Right },
                new ResizeHandle { Position = top, Type = ResizeHandleType.Top },
                new ResizeHandle { Position = bottom, Type = ResizeHandleType.Bottom }
            };
        }
        
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        
        public void Resize(Point newPosition, bool maintainAspectRatio = false)
        {
            if (selectedHandle == null) return;
            
            // Если эллипс повернут, сначала поворачиваем точку newPosition в обратную сторону
            if (RotationAngle != 0)
            {
                newPosition = RotatePoint(newPosition, Position, -RotationAngle);
            }
            
            double aspectRatio = RadiusX / RadiusY;
            
            switch (selectedHandle.Type)
            {
                case ResizeHandleType.Left:
                    double newRadiusX = Position.X - newPosition.X;
                    if (maintainAspectRatio)
                    {
                        RadiusY = newRadiusX / aspectRatio;
                    }
                    RadiusX = newRadiusX;
                    break;
                    
                case ResizeHandleType.Right:
                    newRadiusX = newPosition.X - Position.X;
                    if (maintainAspectRatio)
                    {
                        RadiusY = newRadiusX / aspectRatio;
                    }
                    RadiusX = newRadiusX;
                    break;
                    
                case ResizeHandleType.Top:
                    double newRadiusY = Position.Y - newPosition.Y;
                    if (maintainAspectRatio)
                    {
                        RadiusX = newRadiusY * aspectRatio;
                    }
                    RadiusY = newRadiusY;
                    break;
                    
                case ResizeHandleType.Bottom:
                    newRadiusY = newPosition.Y - Position.Y;
                    if (maintainAspectRatio)
                    {
                        RadiusX = newRadiusY * aspectRatio;
                    }
                    RadiusY = newRadiusY;
                    break;
            }
            
            // Убеждаемся, что радиусы не отрицательные
            if (RadiusX < 1) RadiusX = 1;
            if (RadiusY < 1) RadiusY = 1;
        }

        public override void Draw(WriteableBitmap bitmap)
        {
            if (RotationAngle == 0)
            {
                // Рисуем заполненный эллипс
                Color fillColor = GetColorFromBrush(Fill);
                GraphicsAlgorithms.DrawEllipse(bitmap,
                    (int)Position.X, (int)Position.Y,
                    (int)RadiusX, (int)RadiusY,
                    fillColor, true);
                
                // Рисуем контур
                Color strokeColor = GetColorFromBrush(Stroke);
                GraphicsAlgorithms.DrawEllipse(bitmap,
                    (int)Position.X, (int)Position.Y,
                    (int)RadiusX, (int)RadiusY,
                    strokeColor, false);
            }
            else
            {
                // Для повернутого эллипса рисуем многоугольник, аппроксимирующий эллипс
                int segments = 36; // Количество сегментов для аппроксимации
                Point[] points = new Point[segments];
                
                for (int i = 0; i < segments; i++)
                {
                    double angle = 2 * Math.PI * i / segments;
                    double x = Position.X + RadiusX * Math.Cos(angle);
                    double y = Position.Y + RadiusY * Math.Sin(angle);
                    
                    // Поворачиваем точку вокруг центра эллипса
                    points[i] = RotatePoint(new Point(x, y), Position, RotationAngle);
                }
                
                // Рисуем заполненный многоугольник
                Color fillColor = GetColorFromBrush(Fill);
                GraphicsAlgorithms.DrawFilledPolygon(bitmap, points, fillColor);
                
                // Рисуем контур
                Color strokeColor = GetColorFromBrush(Stroke);
                for (int i = 0; i < segments; i++)
                {
                    int nextIndex = (i + 1) % segments;
                    GraphicsAlgorithms.DrawLine(bitmap,
                        (int)points[i].X, (int)points[i].Y,
                        (int)points[nextIndex].X, (int)points[nextIndex].Y,
                        strokeColor);
                }
            }

            if (IsSelected)
            {
                // Рисуем прямоугольную область выделения с учетом поворота
                Point[] rectPoints = new Point[4];
                rectPoints[0] = new Point(Position.X - RadiusX, Position.Y - RadiusY); // Верхний левый
                rectPoints[1] = new Point(Position.X + RadiusX, Position.Y - RadiusY); // Верхний правый
                rectPoints[2] = new Point(Position.X + RadiusX, Position.Y + RadiusY); // Нижний правый
                rectPoints[3] = new Point(Position.X - RadiusX, Position.Y + RadiusY); // Нижний левый
                
                // Применяем поворот
                if (RotationAngle != 0)
                {
                    for (int i = 0; i < 4; i++)
                    {
                        rectPoints[i] = RotatePoint(rectPoints[i], Position, RotationAngle);
                    }
                }
                
                // Рисуем контур выделения
                for (int i = 0; i < 4; i++)
                {
                    int nextIndex = (i + 1) % 4;
                    GraphicsAlgorithms.DrawLine(bitmap,
                        (int)rectPoints[i].X, (int)rectPoints[i].Y,
                        (int)rectPoints[nextIndex].X, (int)rectPoints[nextIndex].Y,
                        Colors.Blue);
                }
                
                // Рисуем маркеры изменения размера
                foreach (var handle in GetResizeHandles())
                {
                    bool isSelected = selectedHandle != null && selectedHandle.Type == handle.Type;
                    
                    // Заливка маркера
                    Color handleColor = isSelected ? Colors.Red : Colors.Blue;
                    GraphicsAlgorithms.DrawRectangle(bitmap,
                        (int)(handle.Position.X - 3), (int)(handle.Position.Y - 3),
                        6, 6,
                        handleColor, true);
                    
                    // Контур маркера
                    GraphicsAlgorithms.DrawRectangle(bitmap,
                        (int)(handle.Position.X - 3), (int)(handle.Position.Y - 3),
                        6, 6,
                        Colors.Black, false);
                }
                
                // Рисуем маркер вращения (в центре верхней стороны)
                Point topCenter = new Point(Position.X, Position.Y - RadiusY - 15);
                if (RotationAngle != 0)
                {
                    topCenter = RotatePoint(topCenter, Position, RotationAngle);
                }
                
                // Линия от центра к маркеру вращения
                Point center = Position;
                Point topPoint = new Point(Position.X, Position.Y - RadiusY);
                if (RotationAngle != 0)
                {
                    topPoint = RotatePoint(topPoint, center, RotationAngle);
                }
                
                GraphicsAlgorithms.DrawLine(bitmap,
                    (int)topPoint.X, (int)topPoint.Y,
                    (int)topCenter.X, (int)topCenter.Y,
                    Colors.Green);
                
                // Маркер вращения (круг)
                GraphicsAlgorithms.DrawCircle(bitmap,
                    (int)topCenter.X, (int)topCenter.Y,
                    5, Colors.Green, true);
                GraphicsAlgorithms.DrawCircle(bitmap,
                    (int)topCenter.X, (int)topCenter.Y,
                    5, Colors.Black, false);
            }
        }

        public override bool Contains(Point point)
        {
            // Если выбран, проверяем также маркеры изменения размера
            if (IsSelected)
            {
                foreach (var handle in GetResizeHandles())
                {
                    if (CalculateDistance(point, handle.Position) <= 10)
                    {
                        return true;
                    }
                }
                
                // Проверяем маркер вращения
                if (IsRotationHandleHit(point))
                {
                    return true;
                }
            }
            
            // Для повернутого эллипса сначала поворачиваем точку в обратную сторону
            Point checkPoint = point;
            if (RotationAngle != 0)
            {
                checkPoint = RotatePoint(point, Position, -RotationAngle);
            }
        
            double normalizedX = (checkPoint.X - Position.X) / RadiusX;
            double normalizedY = (checkPoint.Y - Position.Y) / RadiusY;
            return (normalizedX * normalizedX + normalizedY * normalizedY) <= 1.0;
        }

        public override void Move(Vector delta)
        {
            if (selectedHandle != null)
            {
                // Если выбран маркер, то изменяем размер
                Point newPosition = new Point(
                    selectedHandle.Position.X + delta.X,
                    selectedHandle.Position.Y + delta.Y
                );
                Resize(newPosition);
                
                // Обновляем позицию маркера
                selectedHandle = new ResizeHandle
                {
                    Type = selectedHandle.Type,
                    Position = newPosition
                };
            }
            else
            {
                // Перемещаем весь эллипс
                Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
            }
        }
        
        // Проверка, находится ли точка в области маркера вращения
        public bool IsRotationHandleHit(Point point)
        {
            if (!IsSelected) return false;
            
            Point topCenter = new Point(Position.X, Position.Y - RadiusY - 15);
            if (RotationAngle != 0)
            {
                topCenter = RotatePoint(topCenter, Position, RotationAngle);
            }
            
            return CalculateDistance(point, topCenter) <= 8;
        }

        public override Shape Clone()
        {
            return new EllipseShape
            {
                Position = this.Position,
                Fill = this.Fill,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness,
                RadiusX = this.RadiusX,
                RadiusY = this.RadiusY,
                RotationAngle = this.RotationAngle
            };
        }
    }
} 