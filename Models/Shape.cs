using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace VectorEditor.Models
{
    public abstract class Shape
    {
        public Point Position { get; set; }
        public Brush Fill { get; set; } = Brushes.White;
        public Brush Stroke { get; set; } = Brushes.Black;
        public double StrokeThickness { get; set; } = 1.0;
        public bool IsSelected { get; set; }

        // Угол поворота в градусах
        public double RotationAngle { get; set; } = 0.0;

        /// <summary>
        /// Отрисовывает фигуру на WriteableBitmap
        /// </summary>
        public abstract void Draw(WriteableBitmap bitmap);
        
        /// <summary>
        /// Проверяет, содержит ли фигура точку
        /// </summary>
        public abstract bool Contains(Point point);
        
        /// <summary>
        /// Перемещает фигуру
        /// </summary>
        public abstract void Move(Vector delta);
        
        /// <summary>
        /// Создает копию фигуры
        /// </summary>
        public abstract Shape Clone();
        
        /// <summary>
        /// Вращает фигуру на заданный угол в градусах
        /// </summary>
        public virtual void Rotate(double angleDelta)
        {
            RotationAngle = (RotationAngle + angleDelta) % 360;
            if (RotationAngle < 0)
                RotationAngle += 360;
        }
        
        /// <summary>
        /// Поворачивает точку вокруг центра на заданный угол
        /// </summary>
        protected Point RotatePoint(Point point, Point center, double angleDegrees)
        {
            if (angleDegrees == 0)
                return point;
                
            // Преобразуем угол в радианы
            double angleRadians = angleDegrees * Math.PI / 180.0;
            
            // Смещаем точку относительно центра вращения
            double x = point.X - center.X;
            double y = point.Y - center.Y;
            
            // Поворачиваем точку
            double newX = x * Math.Cos(angleRadians) - y * Math.Sin(angleRadians);
            double newY = x * Math.Sin(angleRadians) + y * Math.Cos(angleRadians);
            
            // Возвращаем точку в исходную систему координат
            return new Point(newX + center.X, newY + center.Y);
        }
        
        /// <summary>
        /// Поворачивает массив точек вокруг центра на заданный угол
        /// </summary>
        protected Point[] RotatePoints(Point[] points, Point center, double angleDegrees)
        {
            if (angleDegrees == 0)
                return points;
                
            Point[] rotatedPoints = new Point[points.Length];
            for (int i = 0; i < points.Length; i++)
            {
                rotatedPoints[i] = RotatePoint(points[i], center, angleDegrees);
            }
            
            return rotatedPoints;
        }
        
        /// <summary>
        /// Преобразует Brush в Color для использования в алгоритмах отрисовки
        /// </summary>
        protected Color GetColorFromBrush(Brush brush)
        {
            if (brush is SolidColorBrush solidColorBrush)
            {
                return solidColorBrush.Color;
            }
            
            // По умолчанию возвращаем черный цвет
            return Colors.Black;
        }
        
        /// <summary>
        /// Сериализует базовые свойства фигуры
        /// </summary>
        public virtual ShapeData Serialize()
        {
            return new ShapeData
            {
                Type = this.GetType().Name,
                X = Position.X,
                Y = Position.Y,
                FillColor = GetColorFromBrush(Fill).ToString(),
                StrokeColor = GetColorFromBrush(Stroke).ToString(),
                StrokeThickness = StrokeThickness,
                RotationAngle = RotationAngle
            };
        }
        
        /// <summary>
        /// Десериализует базовые свойства фигуры
        /// </summary>
        public virtual void Deserialize(ShapeData data)
        {
            Position = new Point(data.X, data.Y);
            Fill = new SolidColorBrush((Color)ColorConverter.ConvertFromString(data.FillColor));
            Stroke = new SolidColorBrush((Color)ColorConverter.ConvertFromString(data.StrokeColor));
            StrokeThickness = data.StrokeThickness;
            RotationAngle = data.RotationAngle;
        }
    }
    
    /// <summary>
    /// Класс для хранения сериализованных данных фигуры
    /// </summary>
    public class ShapeData
    {
        public string Type { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public string FillColor { get; set; }
        public string StrokeColor { get; set; }
        public double StrokeThickness { get; set; }
        public double RotationAngle { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
    }
} 