using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace VectorEditor.Graphics
{
    /// <summary>
    /// Класс, содержащий низкоуровневые алгоритмы для отрисовки примитивов
    /// </summary>
    public static class GraphicsAlgorithms
    {
        /// <summary>
        /// Устанавливает пиксель на WriteableBitmap с заданным цветом
        /// </summary>
        public static void SetPixel(WriteableBitmap bitmap, int x, int y, Color color)
        {
            try
            {
                // Проверяем, находится ли пиксель в пределах изображения
                if (x < 0 || x >= bitmap.PixelWidth || y < 0 || y >= bitmap.PixelHeight)
                    return;

                // Получаем доступ к пикселям
                bitmap.Lock();

                unsafe
                {
                    // Получаем указатель на начало буфера пикселей
                    IntPtr pBackBuffer = bitmap.BackBuffer;
                    
                    // Находим позицию пикселя по координатам
                    int stride = bitmap.BackBufferStride;
                    int pixelOffset = y * stride + x * 4; // 4 байта на пиксель (BGRA)
                    
                    // Получаем указатель на конкретный пиксель
                    byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                    
                    // Записываем компоненты цвета в формате BGRA
                    pPixel[0] = color.B;
                    pPixel[1] = color.G;
                    pPixel[2] = color.R;
                    pPixel[3] = color.A;
                }

                // Определяем область для обновления - только один пиксель
                bitmap.AddDirtyRect(new Int32Rect(x, y, 1, 1));
            }
            finally
            {
                // Снимаем блокировку
                bitmap.Unlock();
            }
        }

        /// <summary>
        /// Алгоритм Брезенхема для отрисовки отрезка прямой линии
        /// </summary>
        public static void DrawLine(WriteableBitmap bitmap, int x1, int y1, int x2, int y2, Color color)
        {
            int dx = Math.Abs(x2 - x1);
            int dy = Math.Abs(y2 - y1);
            int sx = (x1 < x2) ? 1 : -1;
            int sy = (y1 < y2) ? 1 : -1;
            int err = dx - dy;
            
            bitmap.Lock();
            
            try
            {
                while (true)
                {
                    // Устанавливаем пиксель
                    if (x1 >= 0 && x1 < bitmap.PixelWidth && y1 >= 0 && y1 < bitmap.PixelHeight)
                    {
                        unsafe
                        {
                            IntPtr pBackBuffer = bitmap.BackBuffer;
                            int stride = bitmap.BackBufferStride;
                            int pixelOffset = y1 * stride + x1 * 4;
                            byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                            
                            pPixel[0] = color.B;
                            pPixel[1] = color.G;
                            pPixel[2] = color.R;
                            pPixel[3] = color.A;
                        }
                    }
                    
                    // Проверяем, достигли ли конечной точки
                    if (x1 == x2 && y1 == y2) break;
                    
                    int e2 = 2 * err;
                    if (e2 > -dy)
                    {
                        err -= dy;
                        x1 += sx;
                    }
                    if (e2 < dx)
                    {
                        err += dx;
                        y1 += sy;
                    }
                }
                
                // Обновляем весь bitmap для простоты
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }
        
        /// <summary>
        /// Отрисовка прямоугольника
        /// </summary>
        public static void DrawRectangle(WriteableBitmap bitmap, int x, int y, int width, int height, Color color, bool fill)
        {
            bitmap.Lock();
            
            try
            {
                if (fill)
                {
                    // Заполненный прямоугольник - рисуем каждую строку
                    for (int i = Math.Max(0, y); i < Math.Min(bitmap.PixelHeight, y + height); i++)
                    {
                        for (int j = Math.Max(0, x); j < Math.Min(bitmap.PixelWidth, x + width); j++)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = i * stride + j * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                }
                else
                {
                    // Пустой прямоугольник - рисуем только контур
                    // Верхняя линия
                    for (int i = Math.Max(0, x); i < Math.Min(bitmap.PixelWidth, x + width); i++)
                    {
                        if (y >= 0 && y < bitmap.PixelHeight)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = y * stride + i * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                    
                    // Нижняя линия
                    for (int i = Math.Max(0, x); i < Math.Min(bitmap.PixelWidth, x + width); i++)
                    {
                        int bottomY = y + height - 1;
                        if (bottomY >= 0 && bottomY < bitmap.PixelHeight)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = bottomY * stride + i * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                    
                    // Левая линия
                    for (int i = Math.Max(0, y); i < Math.Min(bitmap.PixelHeight, y + height); i++)
                    {
                        if (x >= 0 && x < bitmap.PixelWidth)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = i * stride + x * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                    
                    // Правая линия
                    for (int i = Math.Max(0, y); i < Math.Min(bitmap.PixelHeight, y + height); i++)
                    {
                        int rightX = x + width - 1;
                        if (rightX >= 0 && rightX < bitmap.PixelWidth)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = i * stride + rightX * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                }
                
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }
        
        /// <summary>
        /// Отрисовка окружности с использованием алгоритма Брезенхема
        /// </summary>
        public static void DrawCircle(WriteableBitmap bitmap, int centerX, int centerY, int radius, Color color, bool fill)
        {
            bitmap.Lock();
            
            try
            {
                if (fill)
                {
                    // Заполненная окружность
                    // Используем сканирующий подход - для каждой строки рисуем отрезок между точками пересечения
                    for (int y = centerY - radius; y <= centerY + radius; y++)
                    {
                        if (y < 0 || y >= bitmap.PixelHeight) continue;
                        
                        // Вычисляем x-координаты для текущей строки y
                        int dy = y - centerY;
                        int dx = (int)Math.Sqrt(radius * radius - dy * dy);
                        
                        int startX = Math.Max(0, centerX - dx);
                        int endX = Math.Min(bitmap.PixelWidth - 1, centerX + dx);
                        
                        for (int x = startX; x <= endX; x++)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = y * stride + x * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                }
                else
                {
                    // Алгоритм Брезенхема для окружности
                    int x = 0;
                    int y = radius;
                    int d = 3 - 2 * radius;
                    DrawCirclePoints(bitmap, centerX, centerY, x, y, color);
                    
                    while (y >= x)
                    {
                        x++;
                        
                        if (d > 0)
                        {
                            y--;
                            d = d + 4 * (x - y) + 10;
                        }
                        else
                        {
                            d = d + 4 * x + 6;
                        }
                        
                        DrawCirclePoints(bitmap, centerX, centerY, x, y, color);
                    }
                }
                
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }
        
        /// <summary>
        /// Вспомогательный метод для отрисовки 8 симметричных точек окружности
        /// </summary>
        private static void DrawCirclePoints(WriteableBitmap bitmap, int centerX, int centerY, int x, int y, Color color)
        {
            SetPixelSafe(bitmap, centerX + x, centerY + y, color);
            SetPixelSafe(bitmap, centerX - x, centerY + y, color);
            SetPixelSafe(bitmap, centerX + x, centerY - y, color);
            SetPixelSafe(bitmap, centerX - x, centerY - y, color);
            SetPixelSafe(bitmap, centerX + y, centerY + x, color);
            SetPixelSafe(bitmap, centerX - y, centerY + x, color);
            SetPixelSafe(bitmap, centerX + y, centerY - x, color);
            SetPixelSafe(bitmap, centerX - y, centerY - x, color);
        }
        
        /// <summary>
        /// Отрисовка эллипса с использованием алгоритма средней точки
        /// </summary>
        public static void DrawEllipse(WriteableBitmap bitmap, int centerX, int centerY, int radiusX, int radiusY, Color color, bool fill)
        {
            bitmap.Lock();
            
            try
            {
                if (fill)
                {
                    // Заполненный эллипс - используем сканлайн
                    for (int y = centerY - radiusY; y <= centerY + radiusY; y++)
                    {
                        if (y < 0 || y >= bitmap.PixelHeight) continue;
                        
                        // Вычисляем x-координаты для текущей строки y
                        double normalizedY = (double)(y - centerY) / radiusY;
                        double sqY = normalizedY * normalizedY;
                        
                        if (sqY > 1) continue; // За пределами эллипса
                        
                        double dx = radiusX * Math.Sqrt(1 - sqY);
                        
                        int startX = Math.Max(0, (int)(centerX - dx));
                        int endX = Math.Min(bitmap.PixelWidth - 1, (int)(centerX + dx));
                        
                        for (int x = startX; x <= endX; x++)
                        {
                            unsafe
                            {
                                IntPtr pBackBuffer = bitmap.BackBuffer;
                                int stride = bitmap.BackBufferStride;
                                int pixelOffset = y * stride + x * 4;
                                byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                                
                                pPixel[0] = color.B;
                                pPixel[1] = color.G;
                                pPixel[2] = color.R;
                                pPixel[3] = color.A;
                            }
                        }
                    }
                }
                else
                {
                    // Алгоритм средней точки для эллипса (контур)
                    long rx2 = radiusX * radiusX;
                    long ry2 = radiusY * radiusY;
                    long twoRx2 = 2 * rx2;
                    long twoRy2 = 2 * ry2;
                    long p;
                    long x = 0;
                    long y = radiusY;
                    long px = 0;
                    long py = twoRx2 * y;
                    
                    // Рисуем первые точки
                    SetPixelSafe(bitmap, centerX + (int)x, centerY + (int)y, color);
                    SetPixelSafe(bitmap, centerX - (int)x, centerY + (int)y, color);
                    SetPixelSafe(bitmap, centerX + (int)x, centerY - (int)y, color);
                    SetPixelSafe(bitmap, centerX - (int)x, centerY - (int)y, color);
                    
                    // Регион 1
                    p = (long)Math.Round(ry2 - (rx2 * radiusY) + (0.25 * rx2));
                    while (px < py)
                    {
                        x++;
                        px += twoRy2;
                        if (p < 0)
                            p += ry2 + px;
                        else
                        {
                            y--;
                            py -= twoRx2;
                            p += ry2 + px - py;
                        }
                        
                        SetPixelSafe(bitmap, centerX + (int)x, centerY + (int)y, color);
                        SetPixelSafe(bitmap, centerX - (int)x, centerY + (int)y, color);
                        SetPixelSafe(bitmap, centerX + (int)x, centerY - (int)y, color);
                        SetPixelSafe(bitmap, centerX - (int)x, centerY - (int)y, color);
                    }
                    
                    // Регион 2
                    p = (long)Math.Round(ry2 * (x + 0.5) * (x + 0.5) + rx2 * (y - 1) * (y - 1) - rx2 * ry2);
                    while (y > 0)
                    {
                        y--;
                        py -= twoRx2;
                        if (p > 0)
                            p += rx2 - py;
                        else
                        {
                            x++;
                            px += twoRy2;
                            p += rx2 - py + px;
                        }
                        
                        SetPixelSafe(bitmap, centerX + (int)x, centerY + (int)y, color);
                        SetPixelSafe(bitmap, centerX - (int)x, centerY + (int)y, color);
                        SetPixelSafe(bitmap, centerX + (int)x, centerY - (int)y, color);
                        SetPixelSafe(bitmap, centerX - (int)x, centerY - (int)y, color);
                    }
                }
                
                bitmap.AddDirtyRect(new Int32Rect(0, 0, bitmap.PixelWidth, bitmap.PixelHeight));
            }
            finally
            {
                bitmap.Unlock();
            }
        }
        
        /// <summary>
        /// Отрисовка ломаной линии
        /// </summary>
        public static void DrawPolyline(WriteableBitmap bitmap, List<Point> points, Color color)
        {
            if (points.Count < 2) return;
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                DrawLine(bitmap, 
                    (int)points[i].X, (int)points[i].Y, 
                    (int)points[i + 1].X, (int)points[i + 1].Y,
                    color);
            }
        }
        
        /// <summary>
        /// Отрисовка кривой Безье (для кубической кривой Безье)
        /// </summary>
        public static void DrawBezier(WriteableBitmap bitmap, Point p0, Point p1, Point p2, Point p3, Color color)
        {
            // Количество сегментов для аппроксимации кривой
            const int segments = 100;
            
            Point prev = p0;
            
            for (int i = 1; i <= segments; i++)
            {
                double t = (double)i / segments;
                double u = 1 - t;
                double tt = t * t;
                double uu = u * u;
                double uuu = uu * u;
                double ttt = tt * t;
                
                double x = uuu * p0.X + 3 * uu * t * p1.X + 3 * u * tt * p2.X + ttt * p3.X;
                double y = uuu * p0.Y + 3 * uu * t * p1.Y + 3 * u * tt * p2.Y + ttt * p3.Y;
                
                Point current = new Point(x, y);
                
                DrawLine(bitmap, 
                    (int)prev.X, (int)prev.Y, 
                    (int)current.X, (int)current.Y, 
                    color);
                
                prev = current;
            }
        }
        
        /// <summary>
        /// Безопасная установка пикселя с проверкой границ
        /// </summary>
        private static void SetPixelSafe(WriteableBitmap bitmap, int x, int y, Color color)
        {
            if (x >= 0 && x < bitmap.PixelWidth && y >= 0 && y < bitmap.PixelHeight)
            {
                unsafe
                {
                    IntPtr pBackBuffer = bitmap.BackBuffer;
                    int stride = bitmap.BackBufferStride;
                    int pixelOffset = y * stride + x * 4;
                    byte* pPixel = (byte*)pBackBuffer + pixelOffset;
                    
                    pPixel[0] = color.B;
                    pPixel[1] = color.G;
                    pPixel[2] = color.R;
                    pPixel[3] = color.A;
                }
            }
        }
    }
} 