using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace HandballIntegration.Views
{
    public partial class SendPdf : Page
    {
        private static readonly Brush BlockBorderBrush = new SolidColorBrush(Color.FromRgb(46, 125, 107));
        private static readonly Brush BlockSelectedBrush = new SolidColorBrush(Color.FromRgb(197, 97, 63));
        private static readonly Brush BlockBackgroundBrush = new SolidColorBrush(Color.FromRgb(250, 248, 244));

        private UIElement selectedElement;
        private Point startPoint;

        public SendPdf()
        {
            InitializeComponent();
        }

        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            Border block = CreateBlock();

            TextBlock textBlock = new TextBlock
            {
                Text = "Fiche joueuse",
                FontSize = 24,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(Color.FromRgb(25, 40, 35))
            };

            block.Child = textBlock;
            AddToCanvas(block, 96, 92);
        }

        private void AddTable_Click(object sender, RoutedEventArgs e)
        {
            Border block = CreateBlock();

            DataGrid grid = new DataGrid
            {
                AutoGenerateColumns = true,
                Height = 170,
                Width = 420,
                CanUserAddRows = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                GridLinesVisibility = DataGridGridLinesVisibility.None,
                Background = Brushes.White,
                BorderBrush = new SolidColorBrush(Color.FromRgb(226, 216, 205)),
                BorderThickness = new Thickness(1)
            };

            grid.ItemsSource = new[]
            {
                new { Match = "M1", Buts = 5, Fautes = 1 },
                new { Match = "M2", Buts = 3, Fautes = 0 },
            };

            block.Child = grid;
            AddToCanvas(block, 96, 190);
        }

        private Border CreateBlock()
        {
            Border border = new Border
            {
                BorderBrush = BlockBorderBrush,
                BorderThickness = new Thickness(1.5),
                Background = BlockBackgroundBrush,
                Padding = new Thickness(14),
                CornerRadius = new CornerRadius(16),
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    Color = Color.FromRgb(32, 58, 51),
                    Opacity = 0.08,
                    ShadowDepth = 0
                }
            };

            border.MouseLeftButtonDown += Block_MouseDown;
            border.MouseMove += Block_MouseMove;
            border.MouseLeftButtonUp += Block_MouseUp;

            return border;
        }

        private void AddToCanvas(UIElement element, double x, double y)
        {
            Canvas.SetLeft(element, x);
            Canvas.SetTop(element, y);
            EditorCanvas.Children.Add(element);
        }

        private void Block_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border border)
            {
                return;
            }

            selectedElement = border;
            startPoint = e.GetPosition(EditorCanvas);
            border.BorderBrush = BlockSelectedBrush;
            border.CaptureMouse();
        }

        private void Block_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedElement == null)
            {
                return;
            }

            Point position = e.GetPosition(EditorCanvas);
            double offsetX = position.X - startPoint.X;
            double offsetY = position.Y - startPoint.Y;

            Canvas.SetLeft(selectedElement, Canvas.GetLeft(selectedElement) + offsetX);
            Canvas.SetTop(selectedElement, Canvas.GetTop(selectedElement) + offsetY);

            startPoint = position;
        }

        private void Block_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (selectedElement is Border border)
            {
                border.ReleaseMouseCapture();
                border.BorderBrush = BlockBorderBrush;
            }

            selectedElement = null;
        }

        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Le gabarit PDF sera généré depuis cette mise en page dès que l’export QuestPDF sera branché.",
                "Export PDF",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
    }
}
