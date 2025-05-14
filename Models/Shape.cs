using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VectorEditor.Models
{
    public abstract class Shape
    {
        public Point Position { get; set; }
        public Brush Fill { get; set; } = Brushes.White;
        public Brush Stroke { get; set; } = Brushes.Black;
        public double StrokeThickness { get; set; } = 1.0;
        public bool IsSelected { get; set; }

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
    }
} 