using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace VectorEditor.Models
{
    public class ShapeEditor
    {
        public ObservableCollection<Shape> Shapes { get; } = new ObservableCollection<Shape>();
        public Shape? SelectedShape { get; private set; }
        public DrawingMode CurrentMode { get; set; } = DrawingMode.Select;
        
        private Point startPoint;
        private bool isDragging;
        private Shape? drawingShape;
        private BezierShape? editingBezier;
        private RectangleShape? editingRectangle;
        private EllipseShape? editingEllipse;
        
        // Переменная для хранения ломаной во время рисования
        private PolylineShape? tempPolyline;

        public void StartDrawing(Point position, bool isShiftPressed)
        {
            // Если режим изменился с Polyline на что-то другое, сбрасываем tempPolyline
            if (tempPolyline != null && CurrentMode != DrawingMode.Polyline)
            {
                CompletePolyline();
            }
            
            if (CurrentMode == DrawingMode.Select)
            {
                // Сбрасываем все редактируемые фигуры
                editingBezier = null;
                editingRectangle = null;
                editingEllipse = null;
                
                // Проверка на выделение или выбор точки кривой Безье
                Shape? newSelection = Shapes.FirstOrDefault(s => s.Contains(position));
                
                if (SelectedShape is BezierShape bezier && bezier.IsSelected)
                {
                    // Проверяем, не выбрана ли точка кривой Безье
                    bezier.SelectPoint(position);
                    if (bezier.HasSelectedPoint)
                    {
                        editingBezier = bezier;
                        isDragging = true;
                        startPoint = position;
                        return;
                    }
                }
                else if (SelectedShape is RectangleShape rect && rect.IsSelected)
                {
                    // Проверяем, не выбран ли маркер изменения размера
                    rect.SelectResizeHandle(position);
                    if (rect.SelectedResizeHandle != null)
                    {
                        editingRectangle = rect;
                        isDragging = true;
                        startPoint = position;
                        return;
                    }
                }
                else if (SelectedShape is EllipseShape ellipse && ellipse.IsSelected)
                {
                    // Проверяем, не выбран ли маркер изменения размера
                    ellipse.SelectResizeHandle(position);
                    if (ellipse.SelectedResizeHandle != null)
                    {
                        editingEllipse = ellipse;
                        isDragging = true;
                        startPoint = position;
                        return;
                    }
                }
                
                // Обычное выделение фигуры
                SelectedShape = newSelection;
                if (SelectedShape != null)
                {
                    foreach (var shape in Shapes)
                    {
                        shape.IsSelected = (shape == SelectedShape);
                    }
                    
                    isDragging = true;
                    startPoint = position;
                }
                else
                {
                    // Если клик не на фигуре, снимаем все выделения
                    foreach (var shape in Shapes)
                    {
                        shape.IsSelected = false;
                        if (shape is BezierShape bezierShape)
                        {
                            bezierShape.ClearPointSelection();
                        }
                        else if (shape is RectangleShape rectangleShape)
                        {
                            rectangleShape.ClearResizeHandleSelection();
                        }
                        else if (shape is EllipseShape ellipseShape)
                        {
                            ellipseShape.ClearResizeHandleSelection();
                        }
                    }
                    SelectedShape = null;
                }
                return;
            }
            
            // Для ломаной линии особая логика
            if (CurrentMode == DrawingMode.Polyline)
            {
                // Если это первая точка или предыдущая ломаная завершена
                // Строго создаем новую ломаную, если предыдущая не null, значит рисование не было корректно завершено
                // или мы переключились с другого инструмента
                if (tempPolyline == null)
                {
                    tempPolyline = new PolylineShape();
                    tempPolyline.Points.Add(position);
                    tempPolyline.Position = position;
                    Shapes.Add(tempPolyline);
                }
                else
                {
                    // Добавляем новую точку к существующей ломаной
                    tempPolyline.Points.Add(position);
                }
                
                drawingShape = tempPolyline;
                return;
            }
            
            // Создание новой фигуры других типов
            startPoint = position;
            drawingShape = CreateShape(position);
            
            if (drawingShape != null)
            {
                Shapes.Add(drawingShape);
            }
        }

        public void ContinueDrawing(Point position, bool isShiftPressed)
        {
            if (CurrentMode == DrawingMode.Select && isDragging)
            {
                if (editingBezier != null && editingBezier.HasSelectedPoint)
                {
                    // Перетаскивание точки кривой Безье
                    editingBezier.MoveSelectedPoint(position);
                }
                else if (editingRectangle != null && editingRectangle.SelectedResizeHandle != null)
                {
                    // Изменение размера прямоугольника
                    editingRectangle.Resize(position, isShiftPressed);
                }
                else if (editingEllipse != null && editingEllipse.SelectedResizeHandle != null)
                {
                    // Изменение размера эллипса
                    editingEllipse.Resize(position, isShiftPressed);
                }
                else if (SelectedShape != null)
                {
                    // Перетаскивание фигуры
                    Vector delta = new Vector(position.X - startPoint.X, position.Y - startPoint.Y);
                    SelectedShape.Move(delta);
                    startPoint = position;
                }
                return;
            }
            
            if (drawingShape == null) return;
            
            // Для ломаной не делаем ничего при движении мыши
            if (CurrentMode == DrawingMode.Polyline)
            {
                return;
            }
            
            switch (CurrentMode)
            {
                case DrawingMode.Rectangle:
                    if (drawingShape is RectangleShape rect)
                    {
                        if (isShiftPressed)
                        {
                            // Создаем квадрат
                            double size = Math.Max(Math.Abs(position.X - startPoint.X), Math.Abs(position.Y - startPoint.Y));
                            rect.Width = size;
                            rect.Height = size;
                            
                            // Определяем направление от начальной точки
                            double dirX = Math.Sign(position.X - startPoint.X);
                            double dirY = Math.Sign(position.Y - startPoint.Y);
                            if (dirX == 0) dirX = 1;
                            if (dirY == 0) dirY = 1;
                            
                            rect.Position = new Point(
                                dirX > 0 ? startPoint.X : startPoint.X - size,
                                dirY > 0 ? startPoint.Y : startPoint.Y - size);
                        }
                        else
                        {
                            rect.Width = Math.Abs(position.X - startPoint.X);
                            rect.Height = Math.Abs(position.Y - startPoint.Y);
                            rect.Position = new Point(
                                Math.Min(startPoint.X, position.X),
                                Math.Min(startPoint.Y, position.Y));
                        }
                    }
                    break;
                    
                case DrawingMode.Ellipse:
                    if (drawingShape is EllipseShape ellipse)
                    {
                        if (isShiftPressed)
                        {
                            // Создаем круг
                            double radius = Math.Max(Math.Abs(position.X - startPoint.X), Math.Abs(position.Y - startPoint.Y)) / 2;
                            ellipse.RadiusX = radius;
                            ellipse.RadiusY = radius;
                            
                            // Определяем направление от начальной точки
                            double dirX = Math.Sign(position.X - startPoint.X);
                            double dirY = Math.Sign(position.Y - startPoint.Y);
                            if (dirX == 0) dirX = 1;
                            if (dirY == 0) dirY = 1;
                            
                            ellipse.Position = new Point(
                                dirX > 0 ? startPoint.X + radius : startPoint.X - radius,
                                dirY > 0 ? startPoint.Y + radius : startPoint.Y - radius);
                        }
                        else
                        {
                            ellipse.RadiusX = Math.Abs(position.X - startPoint.X) / 2;
                            ellipse.RadiusY = Math.Abs(position.Y - startPoint.Y) / 2;
                            ellipse.Position = new Point(
                                Math.Min(startPoint.X, position.X) + ellipse.RadiusX,
                                Math.Min(startPoint.Y, position.Y) + ellipse.RadiusY);
                        }
                    }
                    break;
                    
                case DrawingMode.Line:
                    if (drawingShape is LineShape line)
                    {
                        if (isShiftPressed)
                        {
                            // Ограничиваем под 45-градусные углы
                            Vector delta = new Vector(position.X - startPoint.X, position.Y - startPoint.Y);
                            if (Math.Abs(delta.X) > Math.Abs(delta.Y))
                            {
                                // Горизонтальная линия с уклоном 45 градусов
                                double sign = Math.Sign(delta.Y);
                                if (sign == 0) sign = 1;
                                line.EndPoint = new Point(
                                    position.X, 
                                    startPoint.Y + sign * Math.Abs(delta.X));
                            }
                            else
                            {
                                // Вертикальная линия с уклоном 45 градусов
                                double sign = Math.Sign(delta.X);
                                if (sign == 0) sign = 1;
                                line.EndPoint = new Point(
                                    startPoint.X + sign * Math.Abs(delta.Y), 
                                    position.Y);
                            }
                        }
                        else
                        {
                            line.EndPoint = position;
                        }
                    }
                    break;
                    
                case DrawingMode.Bezier:
                    if (drawingShape is BezierShape bezier)
                    {
                        // Автоматическое размещение контрольных точек
                        bezier.EndPoint = position;
                        bezier.ControlPoint1 = new Point(
                            startPoint.X + (position.X - startPoint.X) / 3,
                            startPoint.Y + (position.Y - startPoint.Y) / 3);
                        bezier.ControlPoint2 = new Point(
                            startPoint.X + (position.X - startPoint.X) * 2 / 3,
                            startPoint.Y + (position.Y - startPoint.Y) * 2 / 3);
                    }
                    break;
            }
        }

        public void EndDrawing(Point position, bool isShiftPressed)
        {
            if (CurrentMode == DrawingMode.Select)
            {
                isDragging = false;
                return;
            }
            
            // Для ломаной особая логика - не завершаем рисование
            if (CurrentMode == DrawingMode.Polyline)
            {
                return;
            }
            
            if (drawingShape == null) return;

            // Применяем final корректировки для шифт
            ContinueDrawing(position, isShiftPressed);
            
            // Для других фигур завершаем рисование
            drawingShape = null;
        }

        public void CompletePolyline()
        {
            if (tempPolyline == null) return;
            
            if (tempPolyline.Points.Count >= 2)
            {
                // Ломаная уже добавлена в коллекцию, просто отмечаем завершение
                // и сбрасываем ссылки
                drawingShape = null;
                tempPolyline = null;
            }
            else
            {
                // Если менее двух точек, удаляем неполноценную ломаную
                if (Shapes.Contains(tempPolyline))
                {
                    Shapes.Remove(tempPolyline);
                }
                drawingShape = null;
                tempPolyline = null;
            }
        }

        public void CancelDrawing()
        {
            if (drawingShape != null)
            {
                if (Shapes.Contains(drawingShape))
                {
                    Shapes.Remove(drawingShape);
                }
                drawingShape = null;
            }
            
            if (tempPolyline != null)
            {
                if (Shapes.Contains(tempPolyline))
                {
                    Shapes.Remove(tempPolyline);
                }
                tempPolyline = null;
            }
        }

        public void ChangeShapeColor(Brush color)
        {
            if (SelectedShape != null)
            {
                SelectedShape.Fill = color;
            }
        }

        public void ChangeShapeStroke(Brush color)
        {
            if (SelectedShape != null)
            {
                SelectedShape.Stroke = color;
            }
        }

        public void ChangeStrokeThickness(double thickness)
        {
            if (SelectedShape != null)
            {
                SelectedShape.StrokeThickness = thickness;
            }
        }

        public void DeleteSelectedShape()
        {
            if (SelectedShape != null)
            {
                Shapes.Remove(SelectedShape);
                SelectedShape = null;
            }
        }

        private Shape? CreateShape(Point position)
        {
            return CurrentMode switch
            {
                DrawingMode.Rectangle => new RectangleShape { Position = position, Width = 0, Height = 0 },
                DrawingMode.Ellipse => new EllipseShape { Position = position, RadiusX = 0, RadiusY = 0 },
                DrawingMode.Line => new LineShape { Position = position, EndPoint = position },
                DrawingMode.Bezier => new BezierShape 
                { 
                    Position = position, 
                    EndPoint = position,
                    ControlPoint1 = position,
                    ControlPoint2 = position
                },
                DrawingMode.Polyline => new PolylineShape { Points = new List<Point> { position } },
                _ => null
            };
        }
    }

    public enum DrawingMode
    {
        Select,
        Rectangle,
        Ellipse,
        Line,
        Bezier,
        Polyline
    }
} 