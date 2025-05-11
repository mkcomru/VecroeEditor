using System;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public abstract class Shape
    {
        public Point Position { get; set; }
        public Brush Fill { get; set; } = Brushes.White;
        public Brush Stroke { get; set; } = Brushes.Black;
        public double StrokeThickness { get; set; } = 1.0;
        public bool IsSelected { get; set; }

        public abstract void Draw(DrawingContext drawingContext);
        public abstract bool Contains(Point point);
        public abstract void Move(Vector delta);
        public abstract Shape Clone();
    }
} 