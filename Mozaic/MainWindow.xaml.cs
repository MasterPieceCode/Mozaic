using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Processing;

namespace Mozaic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private IdToImageConverter _idToImageConverter;

        public MainWindow()
        {
            InitializeComponent();
            //var bitmapImage = new BitmapImage(new Uri(@"C:\Sources\Spikes\Mozaic\Mozaic\bin\Debug\Penguins.jpg"));
            // ImageCanvas.Source = bitmapImage;

            MainPanel.Child = BuildGrid(new ImageConverter(@"C:\Sources\Spikes\Mozaic\Mozaic\bin\Debug\Penguins.jpg", 5, 2000, 1500).Convert());
            MainPanel.MouseWheel += MainPanel_MouseWheel;

            /*
                        TextBlock txtBlock2 = new TextBlock();
                        txtBlock2.Text = "Age";
                        txtBlock2.VerticalAlignment = VerticalAlignment.Stretch;
                        txtBlock2.HorizontalAlignment = HorizontalAlignment.Stretch;

                        MainPanel.Children.Add(txtBlock2);
                         new IdToImageConverter()
            */
        }

        private void MainPanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var panel = (UIElement)sender;

            if (!MainPanel.IsMouseOver)
            {
                return;
            }

            Point p = e.MouseDevice.GetPosition(panel);

            Matrix m = panel.RenderTransform.Value;
            if (e.Delta > 0)
                m.ScaleAtPrepend(1.1, 1.1, p.X, p.Y);
            else
                m.ScaleAtPrepend(1 / 1.1, 1 / 1.1, p.X, p.Y);

            panel.RenderTransform = new MatrixTransform(m);
        }

        private UIElement BuildGrid(ObservableCollection<ObservableCollection<TileInfo>> tileCollection)
        {
            var grid = new Grid();
            grid.HorizontalAlignment = HorizontalAlignment.Stretch;
            grid.VerticalAlignment = VerticalAlignment.Stretch;
            grid.ShowGridLines = false;
            grid.Background = new SolidColorBrush(Colors.Gray);


            var observableCollection = tileCollection.First();

            foreach (var tileInfo in observableCollection)
            {
                var tileColumn = CreateTileColumn(tileInfo);
                tileColumn.Width = GridLength.Auto;
                grid.ColumnDefinitions.Add(tileColumn);
            }

            foreach (var tileInfo in tileCollection)
            {
                var tileRow = new RowDefinition();
                tileRow.Height = GridLength.Auto;
                grid.RowDefinitions.Add(tileRow);

            }

            var rowInd = 0;

            _idToImageConverter = new IdToImageConverter();

            var maxHeight = (double)1000 / observableCollection.Count;;
            var maxWidth = maxHeight;

            foreach (var tileInfoRow in tileCollection)
            {
                var colInd = 0;

                foreach (var tileInfoColumn in tileInfoRow)
                {

                    var image = new Image();
                    image.MaxHeight = maxHeight;
                    image.MaxWidth = maxWidth;
                    //image.Source = bi;

                    var border = new Border();
                    border.BorderThickness = new Thickness(0.2);
                    border.BorderBrush = new SolidColorBrush(Colors.LightGray);
                    border.Child = image;

                    var dataBinding = new Binding()
                    {
                        Converter = _idToImageConverter,
/*                        ConverterParameter = tileInfoColumn.TileBitmap,*/
                        Source = tileInfoColumn,
                    };

                    image.SetBinding(Image.SourceProperty, dataBinding);
                    image.DataContext = tileInfoColumn;
/*


                    txtBlock2.Text = "Age";
                    txtBlock2.VerticalAlignment = VerticalAlignment.Stretch;
                    txtBlock2.HorizontalAlignment = HorizontalAlignment.Stretch;
*/
                    Grid.SetRow(border, rowInd);
                    Grid.SetColumn(border, colInd);
                    grid.Children.Add(border);
                    colInd++;
                }

                rowInd++;
            }

            return grid;
        }

        private static ColumnDefinition CreateTileColumn(TileInfo tileInfo)
        {
            return new ColumnDefinition();
        }
    }

    public class IdToImageConverter : IValueConverter

    {
        public Dictionary<int, ImageSource> _proccesItems { get; set; }

        public IdToImageConverter()
        {
            _proccesItems = new Dictionary<int, ImageSource>();
        }

        public Object Convert(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            var tileInfo = (TileInfo)value;

            if (!_proccesItems.ContainsKey(tileInfo.Id))
            {
                var tileBase = TileContainer.TileBase[tileInfo.Id];

                var ms = new MemoryStream();
                tileBase.TileBitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                ms.Position = 0;
                var bi = new BitmapImage();
                bi.BeginInit();
                bi.StreamSource = ms;
                bi.EndInit();
                _proccesItems.Add(tileInfo.Id, bi);
            }


            return _proccesItems[tileInfo.Id];
        }

        public Object ConvertBack(Object value, Type targetType, Object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
