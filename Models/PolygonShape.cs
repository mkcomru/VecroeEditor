using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Graphics;

namespace VectorEditor.Models
{
    public class PolygonShape : Shape
    {
        private int _sides = 5; // Количество сторон по умолчанию
        private double _radius = 50; // Радиус описанной окружности
        private List<Point> _vertices = new List<Point>(); // Вершины многоугольника
        
        // Для изменения размера
        private ResizeHandle? selectedHandle;
        
        public ResizeHandle? SelectedResizeHandle => selectedHandle;
        
        // Свойство для количества сторон (углов) с ограничением от 3 до 6
        public int Sides
        {
            get => _sides;
            set
            {
                if (value >= 3 && value <= 6)
                {
                    _sides = value;
                    CalculateVertices();
                }
            }
        }
        
        // Радиус описанной окружности
        public double Radius
        {
            get => _radius;
            set
            {
                if (value > 0)
                {
                    _radius = value;
                    CalculateVertices();
                }
            }
        }
        
        // Вершины многоугольника
        public List<Point> Vertices => _vertices;
        
        // Конструктор
        public PolygonShape()
        {
            CalculateVertices();
        }
        
        // Пересчитывает вершины многоугольника на основе положения, радиуса и количества сторон
        private void CalculateVertices()
        {
            _vertices.Clear();
            
            double angleStep = 2 * Math.PI / _sides;
            double startAngle = -Math.PI / 2; // Начинаем с верхней точки
            
            for (int i = 0; i < _sides; i++)
            {
                double angle = startAngle + i * angleStep;
                double x = Position.X + _radius * Math.Cos(angle);
                double y = Position.Y + _radius * Math.Sin(angle);
                _vertices.Add(new Point(x, y));
            }
            
            // Применяем поворот к вершинам, если угол не нулевой
            if (RotationAngle != 0)
            {
                Point center = GetCenter();
                for (int i = 0; i < _vertices.Count; i++)
                {
                    _vertices[i] = RotatePoint(_vertices[i], center, RotationAngle);
                }
            }
        }
        
        public void SelectResizeHandle(Point point)
        {
            const double threshold = 10.0;
            selectedHandle = null;
            
            if (!IsSelected) return;
            
            // Проверяем маркер вращения
            if (IsRotationHandleHit(point))
            {
                return;
            }
            
            // Проверяем маркеры изменения размера
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
            // Создаем маркеры для вершин многоугольника
            ResizeHandle[] handles = new ResizeHandle[_sides];
            
            for (int i = 0; i < _sides; i++)
            {
                handles[i] = new ResizeHandle 
                { 
                    Position = _vertices[i],
                    Type = (ResizeHandleType)i // Используем тип как индекс вершины
                };
            }
            
            return handles;
        }
        
        private double CalculateDistance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow(p2.X - p1.X, 2) + Math.Pow(p2.Y - p1.Y, 2));
        }
        
        public void Resize(Point newPosition, bool maintainAspectRatio = false)
        {
            if (selectedHandle == null) return;
            
            // Вычисляем новый радиус на основе позиции маркера
            double newRadius = CalculateDistance(Position, newPosition);
            
            // Обновляем радиус
            if (newRadius > 5) // Минимальный радиус
            {
                Radius = newRadius;
            }
        }
        
        public override void Draw(WriteableBitmap bitmap)
        {
            // Проверяем, что у нас есть хотя бы 3 вершины
            if (_vertices.Count < 3) return;
            
            // Рисуем заполненный многоугольник
            Color fillColor = GetColorFromBrush(Fill);
            
            // Создаем массив точек для отрисовки
            Point[] points = _vertices.ToArray();
            
            // Рисуем заполненный многоугольник
            GraphicsAlgorithms.DrawFilledPolygon(bitmap, points, fillColor);
            
            // Рисуем контур многоугольника
            Color strokeColor = GetColorFromBrush(Stroke);
            for (int i = 0; i < _sides; i++)
            {
                int nextIndex = (i + 1) % _sides;
                GraphicsAlgorithms.DrawLine(
                    bitmap,
                    (int)_vertices[i].X, (int)_vertices[i].Y,
                    (int)_vertices[nextIndex].X, (int)_vertices[nextIndex].Y,
                    strokeColor);
            }
            
            // Если фигура выбрана, рисуем маркеры
            if (IsSelected)
            {
                // Рисуем контур выделения
                for (int i = 0; i < _sides; i++)
                {
                    int nextIndex = (i + 1) % _sides;
                    GraphicsAlgorithms.DrawLine(
                        bitmap,
                        (int)_vertices[i].X, (int)_vertices[i].Y,
                        (int)_vertices[nextIndex].X, (int)_vertices[nextIndex].Y,
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
                Point center = GetCenter();
                Point topPoint = new Point(center.X, center.Y - _radius - 15);
                if (RotationAngle != 0)
                {
                    topPoint = RotatePoint(topPoint, center, RotationAngle);
                }
                
                // Линия от центра к маркеру вращения
                GraphicsAlgorithms.DrawLine(bitmap,
                    (int)center.X, (int)center.Y,
                    (int)topPoint.X, (int)topPoint.Y,
                    Colors.Green);
                
                // Маркер вращения (круг)
                GraphicsAlgorithms.DrawCircle(bitmap,
                    (int)topPoint.X, (int)topPoint.Y,
                    5, Colors.Green, true);
                GraphicsAlgorithms.DrawCircle(bitmap,
                    (int)topPoint.X, (int)topPoint.Y,
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
            
            // Проверяем, находится ли точка внутри многоугольника
            return IsPointInPolygon(point, _vertices.ToArray());
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
                int handleIndex = (int)selectedHandle.Type;
                if (handleIndex >= 0 && handleIndex < _vertices.Count)
                {
                    selectedHandle = new ResizeHandle
                    {
                        Type = selectedHandle.Type,
                        Position = _vertices[handleIndex]
                    };
                }
            }
            else
            {
                // Перемещаем весь многоугольник
                Position = new Point(Position.X + delta.X, Position.Y + delta.Y);
                
                // Обновляем все вершины
                for (int i = 0; i < _vertices.Count; i++)
                {
                    _vertices[i] = new Point(_vertices[i].X + delta.X, _vertices[i].Y + delta.Y);
                }
            }
        }
        
        // Получение центра многоугольника
        public Point GetCenter()
        {
            return Position;
        }
        
        // Проверка, находится ли точка в области маркера вращения
        public bool IsRotationHandleHit(Point point)
        {
            if (!IsSelected) return false;
            
            Point center = GetCenter();
            Point topPoint = new Point(center.X, center.Y - _radius - 15);
            if (RotationAngle != 0)
            {
                topPoint = RotatePoint(topPoint, center, RotationAngle);
            }
            
            return CalculateDistance(point, topPoint) <= 8;
        }
        
        // Переопределяем метод вращения для пересчета вершин
        public override void Rotate(double angleDelta)
        {
            base.Rotate(angleDelta);
            
            // Пересчитываем вершины с учетом нового угла поворота
            CalculateVertices();
        }
        
        public override Shape Clone()
        {
            return new PolygonShape
            {
                Position = this.Position,
                Radius = this._radius,
                Sides = this._sides,
                Fill = this.Fill,
                Stroke = this.Stroke,
                StrokeThickness = this.StrokeThickness,
                RotationAngle = this.RotationAngle
            };
        }
    }
} 