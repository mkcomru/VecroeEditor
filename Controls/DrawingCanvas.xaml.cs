using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using VectorEditor.Models;

namespace VectorEditor.Controls
{
    public partial class DrawingCanvas : UserControl
    {
        private readonly ShapeEditor editor;
        private readonly DrawingVisual drawingVisual;
        private RenderTargetBitmap? renderBitmap;
        private bool isShiftPressed = false;
        
        public event EventHandler<Shape>? ShapeSelected;

        public DrawingCanvas()
        {
            InitializeComponent();
            
            editor = new ShapeEditor();
            drawingVisual = new DrawingVisual();
            
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
                
                // Показываем подсказки при переключении на инструменты
                if (value == DrawingMode.Polyline)
                {
                    MessageBox.Show("Чтобы завершить создание ломаной линии, дважды щелкните мышью или нажмите Enter", 
                        "Подсказка", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (value == DrawingMode.Line)
                {
                    MessageBox.Show("После создания линии вы можете выбрать её и изменить длину и угол, перетаскивая маркеры на концах линии. Удерживайте Shift для ограничения угла до 45 градусов.",
                        "Подсказка", MessageBoxButton.OK, MessageBoxImage.Information);
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

        public void DeleteSelectedShape()
        {
            editor.DeleteSelectedShape();
            Render();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Создаем RenderTargetBitmap после инициализации размеров
                int width = Math.Max(1, (int)ActualWidth);
                int height = Math.Max(1, (int)ActualHeight);
                
                renderBitmap = new RenderTargetBitmap(
                    width, height, 
                    96, 96, PixelFormats.Pbgra32);
                
                DrawingImage.Source = renderBitmap;
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
            if (renderBitmap == null) return;

            using (var dc = drawingVisual.RenderOpen())
            {
                // Очищаем холст
                dc.DrawRectangle(Brushes.White, null, new Rect(0, 0, ActualWidth, ActualHeight));
                
                // Рисуем все фигуры
                foreach (var shape in editor.Shapes)
                {
                    shape.Draw(dc);
                }
            }
            
            renderBitmap.Render(drawingVisual);
            DrawingImage.Source = renderBitmap;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            
            try
            {
                if (sizeInfo.NewSize.Width > 0 && sizeInfo.NewSize.Height > 0)
                {
                    var newBitmap = new RenderTargetBitmap(
                        (int)sizeInfo.NewSize.Width, 
                        (int)sizeInfo.NewSize.Height, 
                        96, 96, PixelFormats.Pbgra32);
                    
                    DrawingImage.Source = newBitmap;
                    
                    if (drawingVisual != null)
                    {
                        newBitmap.Render(drawingVisual);
                    }

                    renderBitmap = newBitmap;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при изменении размера: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 