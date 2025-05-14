using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Graphics;

namespace VectorEditor.Models
{
    public class RectangleShape : Shape
    {
        public double Width { get; set; }
        public double Height { get; set; }
        
        // Для изменения размера
        private ResizeHandle? selectedHandle;
        
        public ResizeHandle? SelectedResizeHandle => selectedHandle;
        
        public void SelectResizeHandle(Point point)
        {
            const double threshold = 10.0;
            selectedHandle = null;
            
            if (!IsSelected) return;
            
            // Проверяем углы прямоугольника
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
                new ResizeHandle { Position = Position, Type = ResizeHandleType.TopLeft },
                new ResizeHandle { Position = new Point(Position.X + Width, Position.Y), Type = ResizeHandleType.TopRight },
                new ResizeHandle { Position = new Point(Position.X, Position.Y + Height), Type = ResizeHandleType.BottomLeft },
                new ResizeHandle { Position = new Point(Position.X + Width, Position.Y + Height), Type = ResizeHandleType.BottomRight }
            };
        }
        
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        
        public void Resize(Point newPosition, bool maintainAspectRatio = false)
        {
            if (selectedHandle == null) return;
            
            double aspectRatio = Width / Height;
            
            switch (selectedHandle.Type)
            {
                case ResizeHandleType.TopLeft:
                    double newWidth = Position.X + Width - newPosition.X;
                    double newHeight = Position.Y + Height - newPosition.Y;
                    
                    if (maintainAspectRatio)
                    {
                        if (newWidth / newHeight > aspectRatio)
                        {
                            newWidth = newHeight * aspectRatio;
                        }
                        else
                        {
                            newHeight = newWidth / aspectRatio;
                        }
                    }
                    
                    Position = new Point(Position.X + Width - newWidth, Position.Y + Height - newHeight);
                    Width = newWidth;
                    Height = newHeight;
                    break;
                    
                case ResizeHandleType.TopRight:
                    newWidth = newPosition.X - Position.X;
                    newHeight = Position.Y + Height - newPosition.Y;
                    
                    if (maintainAspectRatio)
                    {
                        if (newWidth / newHeight > aspectRatio)
                        {
                            newWidth = newHeight * aspectRatio;
                        }
                        else
                        {
                            newHeight = newWidth / aspectRatio;
                        }
                    }
                    
                    Position = new Point(Position.X, Position.Y + Height - newHeight);
                    Width = newWidth;
                    Height = newHeight;
                    break;
                    
                case ResizeHandleType.BottomLeft:
                    newWidth = Position.X + Width - newPosition.X;
                    newHeight = newPosition.Y - Position.Y;
                    
                    if (maintainAspectRatio)
                    {
                        if (newWidth / newHeight > aspectRatio)
                        {
                            newWidth = newHeight * aspectRatio;
                        }
                        else
                        {
                            newHeight = newWidth / aspectRatio;
                        }
                    }
                    
                    Position = new Point(Position.X + Width - newWidth, Position.Y);
                    Width = newWidth;
                    Height = newHeight;
                    break;
                    
                case ResizeHandleType.BottomRight:
                    newWidth = newPosition.X - Position.X;
                    newHeight = newPosition.Y - Position.Y;
                    
                    if (maintainAspectRatio)
                    {
                        if (newWidth / newHeight > aspectRatio)
                        {
                            newWidth = newHeight * aspectRatio;
                        }
                        else
                        {
                            newHeight = newWidth / aspectRatio;
                        }
                    }
                    
                    Width = newWidth;
                    Height = newHeight;
                    break;
            }
            
            // Убеждаемся, что размеры не отрицательные
            if (Width < 1) Width = 1;
            if (Height < 1) Height = 1;
        }

        public override void Draw(WriteableBitmap bitmap)
        {
            // Рисуем заполненный прямоугольник
            Color fillColor = GetColorFromBrush(Fill);
            GraphicsAlgorithms.DrawRectangle(bitmap,
                (int)Position.X, (int)Position.Y,
                (int)Width, (int)Height,
                fillColor, true);
            
            // Рисуем контур
            Color strokeColor = GetColorFromBrush(Stroke);
            GraphicsAlgorithms.DrawRectangle(bitmap,
                (int)Position.X, (int)Position.Y,
                (int)Width, (int)Height,
                strokeColor, false);

            if (IsSelected)
            {
                // Рисуем контур выделения
                GraphicsAlgorithms.DrawRectangle(bitmap,
                    (int)Position.X, (int)Position.Y,
                    (int)Width, (int)Height,
                    Colors.Blue, false);
                
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
            }
        }

        public override bool Contains(Point point)
        {
            var rect = new Rect(Position, new Size(Width, Height));
            
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
            
            return rect.Contains(point);
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
                // Перемещаем весь прямоугольник
                Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
            }
        }

        public override Shape Clone()
        {
            return new RectangleShape
            {
                Position = this.Position,
                Width = this.Width,
                Height = this.Height,
                Fill = this.Fill,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness
            };
        }
    }
} 