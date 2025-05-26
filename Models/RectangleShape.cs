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
            // Если нет поворота, используем обычную отрисовку прямоугольника
            if (RotationAngle == 0)
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
            }
            else
            {
                // Для повернутого прямоугольника используем полигон
                Point[] vertices = GetRectangleVertices();
                Point center = GetCenter();
                Point[] rotatedVertices = RotatePoints(vertices, center, RotationAngle);
                
                // Рисуем заполненный многоугольник
                Color fillColor = GetColorFromBrush(Fill);
                GraphicsAlgorithms.DrawFilledPolygon(bitmap, rotatedVertices, fillColor);
                
                // Рисуем контур
                Color strokeColor = GetColorFromBrush(Stroke);
                for (int i = 0; i < rotatedVertices.Length; i++)
                {
                    int nextIndex = (i + 1) % rotatedVertices.Length;
                    GraphicsAlgorithms.DrawLine(bitmap,
                        (int)rotatedVertices[i].X, (int)rotatedVertices[i].Y,
                        (int)rotatedVertices[nextIndex].X, (int)rotatedVertices[nextIndex].Y,
                        strokeColor);
                }
            }

            if (IsSelected)
            {
                // Получаем вершины прямоугольника
                Point[] vertices = GetRectangleVertices();
                Point center = GetCenter();
                Point[] rotatedVertices = RotatePoints(vertices, center, RotationAngle);
                
                // Рисуем контур выделения
                for (int i = 0; i < rotatedVertices.Length; i++)
                {
                    int nextIndex = (i + 1) % rotatedVertices.Length;
                    GraphicsAlgorithms.DrawLine(bitmap,
                        (int)rotatedVertices[i].X, (int)rotatedVertices[i].Y,
                        (int)rotatedVertices[nextIndex].X, (int)rotatedVertices[nextIndex].Y,
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
                Point rotationHandle = RotatePoint(new Point(Position.X + Width / 2, Position.Y - 15), center, RotationAngle);
                
                // Линия от центра к маркеру вращения
                Point topCenter = RotatePoint(new Point(Position.X + Width / 2, Position.Y), center, RotationAngle);
                GraphicsAlgorithms.DrawLine(bitmap,
                    (int)topCenter.X, (int)topCenter.Y,
                    (int)rotationHandle.X, (int)rotationHandle.Y,
                    Colors.Green);
                
                // Маркер вращения (круг)
                GraphicsAlgorithms.DrawCircle(bitmap,
                    (int)rotationHandle.X, (int)rotationHandle.Y,
                    5, Colors.Green, true);
                GraphicsAlgorithms.DrawCircle(bitmap,
                    (int)rotationHandle.X, (int)rotationHandle.Y,
                    5, Colors.Black, false);
            }
        }

        public override bool Contains(Point point)
        {
            // Если нет поворота, используем обычную проверку
            if (RotationAngle == 0)
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
                    
                    // Проверяем маркер вращения
                    if (IsRotationHandleHit(point))
                    {
                        return true;
                    }
            }
            
            return rect.Contains(point);
            }
            else
            {
                // Для повернутого прямоугольника проверяем, находится ли точка внутри многоугольника
                Point[] vertices = GetRectangleVertices();
                Point center = GetCenter();
                Point[] rotatedVertices = RotatePoints(vertices, center, RotationAngle);
                
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
                
                // Проверяем, находится ли точка внутри повернутого прямоугольника
                return IsPointInPolygon(point, rotatedVertices);
            }
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
                StrokeThickness = this.StrokeThickness,
                RotationAngle = this.RotationAngle
            };
        }
        
        public override ShapeData Serialize()
        {
            var data = base.Serialize();
            data.Properties["Width"] = Width;
            data.Properties["Height"] = Height;
            return data;
        }
        
        public override void Deserialize(ShapeData data)
        {
            base.Deserialize(data);
            
            if (data.Properties.ContainsKey("Width") && data.Properties["Width"] is double width)
                Width = width;
                
            if (data.Properties.ContainsKey("Height") && data.Properties["Height"] is double height)
                Height = height;
        }

        // Получение вершин прямоугольника
        private Point[] GetRectangleVertices()
        {
            return new Point[]
            {
                Position, // Верхний левый
                new Point(Position.X + Width, Position.Y), // Верхний правый
                new Point(Position.X + Width, Position.Y + Height), // Нижний правый
                new Point(Position.X, Position.Y + Height) // Нижний левый
            };
        }
        
        // Получение центра прямоугольника
        public Point GetCenter()
        {
            return new Point(Position.X + Width / 2, Position.Y + Height / 2);
        }
        
        // Проверка, находится ли точка в области маркера вращения
        public bool IsRotationHandleHit(Point point)
        {
            if (!IsSelected) return false;
            
            Point center = GetCenter();
            Point rotationHandlePos = RotatePoint(new Point(Position.X + Width / 2, Position.Y - 15), center, RotationAngle);
            
            return CalculateDistance(point, rotationHandlePos) <= 8;
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
    }
} 