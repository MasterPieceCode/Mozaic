using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using ColorMine.ColorSpaces;
using ColorMine.ColorSpaces.Comparisons;

namespace Processing
{

    public struct AvgColor
    {
        public AvgColor(double rAvg, double gAvg, double bAvg)
        {
            RAvg = rAvg;
            GAvg = gAvg;
            BAvg = bAvg;
        }

        public double RAvg { get; set; }
        public double GAvg { get; set; }
        public double BAvg { get; set; }

        public override int GetHashCode()
        {
            return RAvg.GetHashCode() ^ GAvg.GetHashCode() ^ BAvg.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var avgColorObg = (AvgColor)obj;
            return RAvg == avgColorObg.RAvg
                && GAvg == avgColorObg.GAvg
                && BAvg == avgColorObg.BAvg;
        }

        public override string ToString()
        {
            return string.Format("{0} {1} {2}", RAvg, GAvg, BAvg);
        }
    }

    public class TileInfo
    {
        public Bitmap TileBitmap { get; set; }
        public AvgColor AvgColor { get; set; }
        public int Id { get; set; }
    }


    public class AvgColorCalculator
    {
        public AvgColor Calculate(Bitmap bitmap)
        {
            double rSum = 0;
            double gSum = 0;
            double bSum = 0;

            for (var y = 0; y < bitmap.Height; y++)
            {
                for (var x = 0; x < bitmap.Width; x++)
                {
                    var pixel = bitmap.GetPixel(x, y);
                    rSum += pixel.R;
                    gSum += pixel.G;
                    bSum += pixel.B;
                }
            }

            var pixelCount = bitmap.Width * bitmap.Height;

            return new AvgColor(rSum / pixelCount, gSum / pixelCount, bSum / pixelCount);
        }
    }

    public class TileContainer
    {
        public static Dictionary<int, TileInfo> TileBase { get; }

        static TileContainer()
        {
            TileBase = new Dictionary<int, TileInfo>();
        }

        public TileContainer()
        {
        }

        public void LoadContainer()
        {
            var directory = new DirectoryInfo("Tiles");

            var avgColorCalculator = new AvgColorCalculator();
            var i = 0;
            foreach (var tileImageFile in directory.GetFiles("*.jpg", SearchOption.TopDirectoryOnly))
            {
                var bitmap = new Bitmap(tileImageFile.FullName);

    /*            // hard code, because tile images has white surrounding area
                var tileBitmap = new Bitmap(240, 240);
                var g = Graphics.FromImage(tileBitmap);
                g.DrawImage(bitmap, 0, 0, new Rectangle(145, 145, 240, 240), GraphicsUnit.Pixel);
                tileBitmap.Save(string.Format("{0}.jpg", i));
                i++;
                //
*/
                var tileInfo = new TileInfo { TileBitmap = bitmap };
                tileInfo.AvgColor = avgColorCalculator.Calculate(bitmap);
                tileInfo.Id = i++;
                TileBase.Add(tileInfo.Id, tileInfo);
            }
        }
    }

    public class ImageConverter
    {
        private readonly Bitmap _image;
        private readonly int _tileSize;
        private readonly int _desirableWidth;
        private readonly int _desirableHeight;
        private readonly AvgColorCalculator _avgColorCalculator;
        private readonly AvgColorSearcher _avgColorSearcher;
        private readonly int _gridHorSteps;
        private readonly int _gridVerSteps;
        private readonly int _gridCellWidth;
        private readonly int _gridCellHeight;
        private readonly Size _gridCellSize;


        public ImageConverter(string imageFile, int tileSize, int desirableWidth, int desirableHeight)
        {
            _image = new Bitmap(imageFile);
            _tileSize = tileSize;
            _desirableWidth = desirableWidth;

            var ratio = (double)_image.Height / _image.Width;

            _desirableHeight = (int)(_desirableWidth * ratio);
            _avgColorCalculator = new AvgColorCalculator();
            _avgColorSearcher = new AvgColorSearcher();
            _gridHorSteps = _desirableWidth / tileSize;
            _gridVerSteps = _desirableHeight / tileSize;

            _gridCellWidth = _image.Width / _gridHorSteps;
            _gridCellHeight = _image.Height / _gridVerSteps;
            _gridCellSize = new Size(_gridCellWidth, _gridCellHeight);
        }


        public ObservableCollection<ObservableCollection<TileInfo>> Convert()
        {
            var result = new ObservableCollection<ObservableCollection<TileInfo>>();
            //var convertedBitmap = new Bitmap(_gridCellWidth * _gridHorSteps, _gridCellHeight * _gridVerSteps);
            //var convertedImGraphics = Graphics.FromImage(convertedBitmap);
//            convertedImGraphics.CompositingQuality = CompositingQuality.HighQuality;
            //convertedImGraphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            

            for (var y = 0; y < _gridVerSteps; y++)
            {
                var verticalTiles = new ObservableCollection<TileInfo>();
                result.Add(verticalTiles);
                for (var x = 0; x < _gridHorSteps; x++)
                {
                    var bmp = new Bitmap(_gridCellWidth, _gridCellHeight);

                    var imageTileGraphics = Graphics.FromImage(bmp);

                    var point = new Point(_gridCellWidth * x, _gridCellHeight * y);

                    imageTileGraphics.DrawImage(_image, 0, 0, new Rectangle(point, _gridCellSize), GraphicsUnit.Pixel);

                    var findBestMatchTitle = _avgColorSearcher.FindTitle(_avgColorCalculator.Calculate(bmp));
                    verticalTiles.Add(findBestMatchTitle);

              //      convertedImGraphics.DrawImage(findBestMatchTitle.TileBitmap, new Rectangle(point, _gridCellSize), new Rectangle(0, 0, 240, 240), GraphicsUnit.Pixel);

                    imageTileGraphics.Dispose();
                }
            }

            //convertedBitmap.Save("result1.jpg");

            //convertedImGraphics.Dispose();

            return result;
        }
    }

    public class AvgColorSearcher
    {
        public TileContainer TileContainer;
        private readonly IColorSpaceComparison _comparer;
        private readonly List<int> _usedTiles;

        public AvgColorSearcher()
        {
            TileContainer = new TileContainer();
            TileContainer.LoadContainer();
            _comparer = new Cie1976Comparison();
            _usedTiles = new List<int>();
        }

        public TileInfo FindTitle(AvgColor avgColor)
        {
            var threshold = 1;
            TileInfo result = null;
            var hitMatch = false;

            while (result == null)
            {
                hitMatch = false;
                foreach (var title in TileContainer.TileBase)
                {
                    if (GetDifferenceUsingCie1976Comparison(title.Value.AvgColor, avgColor) <= threshold)
                    {
                        // if we use title, then go next
                        if (_usedTiles.Contains(title.Key))
                        {
                            hitMatch = true;
                            continue;
                        }

                        result = title.Value;
                        _usedTiles.Add(title.Key);
                        break;
                    }
                }

                var oldThreshold = threshold;

                if (result == null)
                {
                    if (hitMatch)
                    {
                        foreach (var title in TileContainer.TileBase)
                        {
                            if (!_usedTiles.Contains(title.Key) && GetDifferenceUsingCie1976Comparison(title.Value.AvgColor, avgColor) <= threshold + 5)
                            {
                                result = title.Value;
                                _usedTiles.Add(title.Key);
                                break;
                            }
                        }
                    }

                    if (result == null)
                    {
                        threshold = oldThreshold;
                        var _usedTitlesCopy = _usedTiles.ToList();

                        foreach (var usedTile in _usedTitlesCopy)
                        {
                            var title = TileContainer.TileBase[usedTile];
                            if (GetDifferenceUsingCie1976Comparison(title.AvgColor, avgColor) <= threshold)
                            {
                                result = title;
                                break;
                            }
                        }
                    }
                }

                threshold += 2;
            }

            return result;
        }

        public double GetDifference(AvgColor color1, AvgColor color2)
        {
            var redMass = (color1.RAvg + color2.RAvg)/2;
            var deltaRed = color1.RAvg - color2.RAvg;
            var deltaGreen = color1.GAvg - color2.GAvg;
            var deltaBlue = color1.BAvg - color2.BAvg;

            return Math.Sqrt(Math.Pow(deltaRed, 2) + Math.Pow(deltaGreen, 2) + Math.Pow(deltaBlue, 2));

            /*
                        return Math.Sqrt((2 + redMass / 256) * Math.Pow(deltaRed, 2) + 4 * Math.Pow(deltaGreen, 2) +
                                      (2 + (255 - redMass) / 256) * Math.Pow(deltaBlue, 2));
            */
        }

        public double GetDifferenceUsingCie1976Comparison(AvgColor color1, AvgColor color2)
        {
            var colorRgb1 = new Rgb() {R = color1.RAvg, G = color1.GAvg, B = color1.BAvg};
            var colorRgb2 = new Rgb() {R = color2.RAvg, G = color2.GAvg, B = color2.BAvg};

            return _comparer.Compare(colorRgb1, colorRgb2);
        }
    }

}
