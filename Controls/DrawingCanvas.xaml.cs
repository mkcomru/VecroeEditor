using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Models;
using VectorEditor.Graphics;

namespace VectorEditor.Controls
{
    public partial class DrawingCanvas : UserControl
    {
        private readonly ShapeEditor editor;
        private WriteableBitmap? drawingBitmap;
        private bool isShiftPressed = false;
        
        public event EventHandler<Shape>? ShapeSelected;

        public DrawingCanvas()
        {
            InitializeComponent();
            
            editor = new ShapeEditor();
            
            // Регистрируем обработчики событий
            Loaded += OnLoaded;
            MouseDown += OnMouseDown;
            MouseMove += OnMouseMove;
            MouseUp += OnMouseUp;
            MouseDoubleClick += OnMouseDoubleClick;
            
            // Добавляем обработку клавиш
            Focusable = true;
            KeyDown += OnKeyDown;
            KeyUp += OnKeyUp;
            
            this.FocusVisualStyle = null; // Убираем визуальное отображение фокуса
        }

        public DrawingMode DrawingMode
        {
            get => editor.CurrentMode;
            set 
            { 
                // Если меняем режим с Polyline на другой, завершаем ломаную
                if (editor.CurrentMode == DrawingMode.Polyline && value != DrawingMode.Polyline)
                {
                    editor.CompletePolyline();
                    Render();
                }
                
                editor.CurrentMode = value; 
            }
        }

        public void LoadExampleShapes()
        {
            try
            {
                // Очистим текущие фигуры
                editor.Shapes.Clear();
                
                // Добавляем примеры фигур
                foreach (var shape in ExampleShapes.CreateExamples())
                {
                    editor.Shapes.Add(shape);
                }
                
                Render();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке примеров: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void ChangeShapeFill(Brush brush)
        {
            editor.ChangeShapeColor(brush);
            Render();
        }

        public void ChangeShapeStroke(Brush brush)
        {
            editor.ChangeShapeStroke(brush);
            Render();
        }

        public void ChangeStrokeThickness(double thickness)
        {
            editor.ChangeStrokeThickness(thickness);
            Render();
        }

        public void ChangePolygonSides(int sides)
        {
            editor.ChangePolygonSides(sides);
            Render();
        }

        public void ClosePolyline()
        {
            editor.CloseSelectedPolyline();
            Render();
        }

        public void OpenPolyline()
        {
            editor.OpenSelectedPolyline();
            Render();
        }

        public void DeleteSelectedShape()
        {
            editor.DeleteSelectedShape();
            Render();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем WriteableBitmap после инициализации размеров
                int width = Math.Max(1, (int)ActualWidth);
                int height = Math.Max(1, (int)ActualHeight);
                
                drawingBitmap = new WriteableBitmap(
                    width, height, 
                    96, 96, PixelFormats.Pbgra32, null);
                
                CanvasImage.Source = drawingBitmap;
                Render();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OnMouseDown(object sender, MouseButtonEventArgs e)
        {
            // Получаем фокус для обработки клавиш
            Focus();
            
            var position = e.GetPosition(this);
            editor.StartDrawing(position, isShiftPressed);
            Render();
            
            if (editor.SelectedShape != null)
            {
                ShapeSelected?.Invoke(this, editor.SelectedShape);
            }
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                var position = e.GetPosition(this);
                editor.ContinueDrawing(position, isShiftPressed);
                Render();
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            var position = e.GetPosition(this);
            editor.EndDrawing(position, isShiftPressed);
            Render();
        }

        private void OnMouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (editor.CurrentMode == DrawingMode.Polyline)
            {
                editor.CompletePolyline();
                Render();
            }
        }
        
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Отмена текущего рисования
                editor.CancelDrawing();
                Render();
            }
            else if (e.Key == Key.Delete)
            {
                // Удаление выбранной фигуры
                DeleteSelectedShape();
            }
            else if (e.Key == Key.Enter && editor.CurrentMode == DrawingMode.Polyline)
            {
                // Завершение ломаной по Enter
                editor.CompletePolyline();
                Render();
            }
            else if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                isShiftPressed = true;
                // Обновляем редактирование, если есть активная операция с зажатым Shift
                if (editor.CurrentMode == DrawingMode.Select && Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    var position = Mouse.GetPosition(this);
                    editor.ContinueDrawing(position, true);
                    Render();
                }
                e.Handled = true;
            }
        }
        
        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift)
            {
                isShiftPressed = false;
                // Обновляем редактирование при отпускании Shift
                if (editor.CurrentMode == DrawingMode.Select && Mouse.LeftButton == MouseButtonState.Pressed)
                {
                    var position = Mouse.GetPosition(this);
                    editor.ContinueDrawing(position, false);
                    Render();
                }
                e.Handled = true;
            }
        }

        private void Render()
        {
            if (drawingBitmap == null) return;

            // Очищаем bitmap
            drawingBitmap.Lock();
            
            try 
            {
                // Заполняем белым цветом
                GraphicsAlgorithms.DrawRectangle(
                    drawingBitmap, 
                    0, 0, 
                    drawingBitmap.PixelWidth, drawingBitmap.PixelHeight, 
                    Colors.White, true);
                
                // Рисуем все фигуры
                foreach (var shape in editor.Shapes)
                {
                    shape.Draw(drawingBitmap);
                }
            }
            finally 
            {
                drawingBitmap.Unlock();
                
                // Не нужно вызывать AddDirtyRect после разблокировки, 
                // так как все алгоритмы рисования уже вызывают его.
                // drawingBitmap.AddDirtyRect(new Int32Rect(0, 0, drawingBitmap.PixelWidth, drawingBitmap.PixelHeight));
            }
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            try
            {
                if (sizeInfo.NewSize.Width > 0 && sizeInfo.NewSize.Height > 0)
                {
                    drawingBitmap = new WriteableBitmap(
                        (int)sizeInfo.NewSize.Width, 
                        (int)sizeInfo.NewSize.Height, 
                        96, 96, PixelFormats.Pbgra32, null);
                    
                    CanvasImage.Source = drawingBitmap;
                    Render();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении размера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 