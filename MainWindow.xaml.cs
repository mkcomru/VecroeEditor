using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using VectorEditor.Models;

namespace VectorEditor;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        try
        {
            InitializeComponent();
            
            // Заполняем комбобоксы цветов
            FillColors();
            
            // Устанавливаем значения по умолчанию
            FillColorComboBox.SelectedIndex = 0;  // Белый
            StrokeColorComboBox.SelectedIndex = 1;  // Черный
            ThicknessComboBox.SelectedIndex = 0;  // 1
            PolygonSidesComboBox.SelectedIndex = 2;  // 5 углов по умолчанию
            
            // Добавляем обработчик закрытия окна
            Closing += MainWindow_Closing;
            
            // Загружаем примеры после инициализации
            Loaded += MainWindow_Loaded;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при инициализации: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            // Загружаем примеры фигур
            if (DrawingCanvas != null)
            {
                DrawingCanvas.LoadExampleShapes();
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка при загрузке: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        // Здесь можно выполнить действия при закрытии окна
    }

    private void FillColors()
    {
        var colors = new List<Brush>
        {
            Brushes.White,
            Brushes.Black,
            Brushes.Red,
            Brushes.Green,
            Brushes.Blue,
            Brushes.Yellow,
            Brushes.Orange,
            Brushes.Purple,
            Brushes.Gray
        };
        
        FillColorComboBox.ItemsSource = colors;
        StrokeColorComboBox.ItemsSource = colors;
    }

    private void SelectButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Select;
    }

    private void RectangleButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Rectangle;
    }

    private void EllipseButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Ellipse;
    }

    private void LineButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Line;
    }

    private void BezierButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Bezier;
    }

    private void PolylineButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Polyline;
    }

    private void PolygonButton_Checked(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DrawingMode = DrawingMode.Polygon;
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
            DrawingCanvas.DeleteSelectedShape();
    }

    private void FillColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (FillColorComboBox.SelectedItem is Brush brush && DrawingCanvas != null)
        {
            DrawingCanvas.ChangeShapeFill(brush);
        }
    }

    private void StrokeColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (StrokeColorComboBox.SelectedItem is Brush brush && DrawingCanvas != null)
        {
            DrawingCanvas.ChangeShapeStroke(brush);
        }
    }

    private void ThicknessComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThicknessComboBox.SelectedItem is ComboBoxItem item && 
            double.TryParse(item.Content.ToString(), out double thickness) &&
            DrawingCanvas != null)
        {
            DrawingCanvas.ChangeStrokeThickness(thickness);
        }
    }

    private void PolygonSidesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (PolygonSidesComboBox.SelectedItem is ComboBoxItem item && 
            int.TryParse(item.Content.ToString(), out int sides) &&
            DrawingCanvas != null)
        {
            DrawingCanvas.ChangePolygonSides(sides);
        }
    }

    private void ClosePolylineButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
        {
            DrawingCanvas.ClosePolyline();
        }
    }

    private void OpenPolylineButton_Click(object sender, RoutedEventArgs e)
    {
        if (DrawingCanvas != null)
        {
            DrawingCanvas.OpenPolyline();
        }
    }

    private void NewFile_Click(object sender, RoutedEventArgs e)
    {
        // Здесь можно реализовать создание нового файла
        MessageBox.Show("Создание нового файла");
    }

    private void Exit_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}