using System;
using System.Windows;
using System.Windows.Media;

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
            return new[]
            {
                new ResizeHandle { Position = new Point(Position.X - RadiusX, Position.Y), Type = ResizeHandleType.Left },
                new ResizeHandle { Position = new Point(Position.X + RadiusX, Position.Y), Type = ResizeHandleType.Right },
                new ResizeHandle { Position = new Point(Position.X, Position.Y - RadiusY), Type = ResizeHandleType.Top },
                new ResizeHandle { Position = new Point(Position.X, Position.Y + RadiusY), Type = ResizeHandleType.Bottom }
            };
        }
        
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        
        public void Resize(Point newPosition, bool maintainAspectRatio = false)
        {
            if (selectedHandle == null) return;
            
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

        public override void Draw(DrawingContext drawingContext)
        {
            drawingContext.DrawEllipse(Fill, new Pen(Stroke, StrokeThickness), Position, RadiusX, RadiusY);

            if (IsSelected)
            {
                var rect = new Rect(
                    Position.X - RadiusX, Position.Y - RadiusY,
                    RadiusX * 2, RadiusY * 2);
                drawingContext.DrawRectangle(null, new Pen(Brushes.Blue, 1), rect);
                
                // Рисуем маркеры изменения размера
                foreach (var handle in GetResizeHandles())
                {
                    bool isSelected = selectedHandle != null && selectedHandle.Type == handle.Type;
                    var handleRect = new Rect(handle.Position.X - 3, handle.Position.Y - 3, 6, 6);
                    drawingContext.DrawRectangle(
                        isSelected ? Brushes.Red : Brushes.Blue, 
                        new Pen(Brushes.Black, 1), 
                        handleRect);
                }
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
            }
        
            double normalizedX = (point.X - Position.X) / RadiusX;
            double normalizedY = (point.Y - Position.Y) / RadiusY;
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

        public override Shape Clone()
        {
            return new EllipseShape
            {
                Position = this.Position,
                Fill = this.Fill,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness,
                RadiusX = this.RadiusX,
                RadiusY = this.RadiusY
            };
        }
    }
} 