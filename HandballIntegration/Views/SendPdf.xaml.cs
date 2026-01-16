using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace HandballIntegration.Views
{
    public partial class SendPdf : Page
    {
        private UIElement selectedElement;
        private Point startPoint;

        public SendPdf()
        {
            InitializeComponent();
        }

        // ➕ TEXTE
        private void AddText_Click(object sender, RoutedEventArgs e)
        {
            Border block = CreateBlock();

            TextBlock tb = new TextBlock
            {
                Text = "Nom de la joueuse",
                FontSize = 22,
                FontWeight = FontWeights.Bold
            };

            block.Child = tb;
            AddToCanvas(block, 100, 100);
        }

        // TABLEAU
        private void AddTable_Click(object sender, RoutedEventArgs e)
        {
            Border block = CreateBlock();

            DataGrid grid = new DataGrid
            {
                AutoGenerateColumns = true,
                Height = 150,
                Width = 400,
                CanUserAddRows = false,
                IsReadOnly = true
            };

            // fake data (plus tard ton API)
            grid.ItemsSource = new[]
            {
                new { Match = "M1", Buts = 5, Fautes = 1 },
                new { Match = "M2", Buts = 3, Fautes = 0 },
            };

            block.Child = grid;
            AddToCanvas(block, 100, 200);
        }

        // CRÉATION BLOC
        private Border CreateBlock()
        {
            Border border = new Border
            {
                BorderBrush = Brushes.DodgerBlue,
                BorderThickness = new Thickness(1),
                Background = Brushes.Transparent,
                Padding = new Thickness(5)
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

        // DRAG SYSTEM
        private void Block_MouseDown(object sender, MouseButtonEventArgs e)
        {
            selectedElement = sender as UIElement;
            startPoint = e.GetPosition(EditorCanvas);
            selectedElement.CaptureMouse();
        }

        private void Block_MouseMove(object sender, MouseEventArgs e)
        {
            if (selectedElement == null) return;

            Point pos = e.GetPosition(EditorCanvas);
            double offsetX = pos.X - startPoint.X;
            double offsetY = pos.Y - startPoint.Y;

            Canvas.SetLeft(selectedElement, Canvas.GetLeft(selectedElement) + offsetX);
            Canvas.SetTop(selectedElement, Canvas.GetTop(selectedElement) + offsetY);

            startPoint = pos;
        }

        private void Block_MouseUp(object sender, MouseButtonEventArgs e)
        {
            selectedElement?.ReleaseMouseCapture();
            selectedElement = null;
        }

        // EXPORT (on branchera QuestPDF ensuite)
        private void ExportPdf_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ici on générera le PDF depuis le layout 😉");
        }
    }
}
