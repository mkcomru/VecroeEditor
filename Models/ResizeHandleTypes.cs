using System;
using System.Windows;

namespace VectorEditor.Models
{
    public enum ResizeHandleType
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight,
        Left,
        Right,
        Top,
        Bottom
    }
    
    public class ResizeHandle
    {
        public Point Position { get; set; }
        public ResizeHandleType Type { get; set; }
    }
} 