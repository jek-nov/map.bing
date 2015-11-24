using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Maps.MapControl.WPF;

namespace WpfBingMaps
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public class WgsPoint
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public WgsPoint(double Latitude, double Longitude)
        {
            this.Latitude = Latitude;
            this.Longitude = Longitude;
        }
    }

    public class MapView
    {
        public WgsPoint Point { get; set; }
        public double Zoom { get; set; }
    }

    class ScreenPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
    }

    class BingMapPointData
    {
        public double X { get; set; }
        public double Y { get; set; }
        public uint pixelCoordinatesRange { get; set; }
    }

    public partial class MainWindow : Window
    {
        private const double MinLatitude = -85.05112878;
        private const double MaxLatitude = 85.05112878;
        private const double MinLongitude = -180;
        private const double MaxLongitude = 180;
        //WgsPoint testPoint = new WgsPoint(35.19176694773939, 33.733520547391024);
        const int earthRadius = 6378137;
        const int tileSize = 256;
        const int zoomLevel = 18;

        BingMapPointData bingMapPoint { get; set; }
        Pushpin pin { get; set; }
        Rectangle rectangle { get; set; }
        double pixelGlobeSize { get; set; }
        double pixelsDegreesRatio { get; set; }
        double pixelsRadiansRatio { get; set; }
        float halfPixelGlobeSize { get; set; }
        System.Drawing.PointF pixelGlobeCenter { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            SetEvents();

            //var point = new Pushpin();
            //point.Location = new Location(testPoint.Latitude, testPoint.Longitude);
            //MapObject.Children.Add(point);

            //MapObject.SetView(point.Location, 7);

            //this.Loaded += MainWindow_Loaded;
        }

        void SetEvents()
        {
            MapObject.MouseRightButtonDown += (sender, args) =>
            {
                //Get mouseClick coordinates
                Point mousePosition = args.GetPosition(this);
                //convet coordinates to a location on the map
                Location pinLocation = MapObject.ViewportPointToLocation(mousePosition);

                // The pushpin to add to the map.
                if (pin == null)
                {
                    pin = new Pushpin();
                }
                else 
                {
                    MapObject.Children.Remove(pin);
                }

                pin.Location = pinLocation;
                // Adds the pushpin to the map.
                MapObject.Children.Add(pin);

                CalculateBingMapPoint(pinLocation);
                DrawScreenPoint(pin.Location);

                TextBlockLatitude.Text = pinLocation.Latitude.ToString();
                TextBlockLongitude.Text = pinLocation.Longitude.ToString();
                TextBlockX.Text = bingMapPoint.X.ToString();
                TextBlockY.Text = bingMapPoint.Y.ToString();
            };

            MapObject.ViewChangeOnFrame += (sender, arguments) =>
            {
                if (pin == null)
                    return;

                CalculateBingMapPoint(pin.Location);

                TextBlockLatitude.Text = pin.Location.Latitude.ToString();
                TextBlockLongitude.Text = pin.Location.Longitude.ToString();
                TextBlockX.Text = bingMapPoint.X.ToString();
                TextBlockY.Text = bingMapPoint.Y.ToString();
            };
        }

        BingMapPointData CalculateBingMapPoint(Location location)
        {
            var latitude = Clip(location.Latitude, MinLatitude, MaxLatitude);
            var longitude = Clip(location.Longitude, MinLongitude, MaxLongitude);

            double x = (longitude + 180) / 360;
            double sinLatitude = Math.Sin(latitude * Math.PI / 180);
            double y = 0.5 - Math.Log((1 + sinLatitude) / (1 - sinLatitude)) / (4 * Math.PI);

            uint mapSize = MapSize(Convert.ToInt32(MapObject.ZoomLevel));
            var pixelX = (int)Clip(x * mapSize + 0.5, 0, mapSize - 1);
            var pixelY = (int)Clip(y * mapSize + 0.5, 0, mapSize - 1);

            if (bingMapPoint == null)
            {
                bingMapPoint = new BingMapPointData()
                {
                    X = pixelX,
                    Y = pixelY,
                    pixelCoordinatesRange = mapSize
                };
            }
            else
            {
                bingMapPoint.X = pixelX;
                bingMapPoint.Y = pixelY;
                bingMapPoint.pixelCoordinatesRange = mapSize;
            }

            return bingMapPoint;
        }

        uint MapSize(int levelOfDetail)
        {
            return (uint)256 << levelOfDetail;
        }

        double Clip(double n, double minValue, double maxValue)
        {
            return Math.Min(Math.Max(n, minValue), maxValue);
        }

        void DrawScreenPoint(Location objectLocation)
        {
            var mapBoudary = MapObject.BoundingRectangle;

            var Xmax = mapBoudary.East;
            var Xmin = mapBoudary.West;
            var deltaX = Xmax - Xmin;

            var Ymax = mapBoudary.North;
            var Ymin = mapBoudary.South;
            var deltaY = Ymax - Ymin;

            var Xobj = (objectLocation.Longitude - Xmin) * (MapObject.ActualWidth / deltaX);
            var Yobj = (objectLocation.Latitude - Ymin) * (MapObject.ActualHeight / deltaY);
            var realY = MapObject.ActualHeight - Yobj;

            if (rectangle == null)
            {
                rectangle = new Rectangle()
                {
                    Stroke = Brushes.Black,
                    Fill = Brushes.Red,
                    Width = Convert.ToDouble(10),
                    Height = Convert.ToDouble(10),
                    Margin = new Thickness(left: Xobj, top: realY, right: 0, bottom: 0),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Top,
                };
            }
            else
            {
                MainGrid.Children.Remove(rectangle);
                rectangle.Margin = new Thickness(left: Xobj, top: realY, right: 0, bottom: 0);
            }

            MainGrid.Children.Add(rectangle);
        }

        //void MainWindow_Loaded(object sender, RoutedEventArgs e)
        //{
        //    pixelGlobeSize = tileSize * Math.Pow(2d, zoomLevel);
        //    pixelsDegreesRatio = pixelGlobeSize / 360.0d;
        //    pixelsRadiansRatio = pixelGlobeSize / (2d * Math.PI);

        //    halfPixelGlobeSize = Convert.ToSingle(pixelGlobeSize / 2d);
        //    pixelGlobeCenter = new System.Drawing.PointF(halfPixelGlobeSize, halfPixelGlobeSize);

        //    var result = FromCoordinatesToPixel(new System.Drawing.PointF { X = (float)testPoint.Longitude, Y = (float)testPoint.Latitude });

        //    var screenPoint = CalculateScreenPoint(
        //        new MapView {
        //            Point = testPoint,
        //            Zoom = zoomLevel
        //        }
        //    );

        //    var rectangle = new Rectangle()
        //    {
        //        Stroke = Brushes.Black,
        //        Fill = Brushes.Red,
        //        Width = Convert.ToDouble(10),
        //        Height = Convert.ToDouble(10),
        //        Margin = new Thickness(left: screenPoint.X, top: screenPoint.Y, right: 0, bottom: 0),
        //        HorizontalAlignment = HorizontalAlignment.Left,
        //        VerticalAlignment = VerticalAlignment.Top,
        //    };

        //    MainGrid.Children.Add(rectangle);
        //}

        private ScreenPoint CalculateScreenPoint(MapView mapView)
        {
            var latitude = Math.PI * mapView.Point.Latitude / 180;
            var longitude = Math.PI * mapView.Point.Longitude / 180;

            var scale = Math.Min(MainGrid.ActualWidth, MainGrid.ActualHeight) / (2.0 * earthRadius);
            var offset = Math.Min(MainGrid.ActualWidth, MainGrid.ActualHeight) / 2.0;

            var x = earthRadius * Math.Cos(latitude) * Math.Cos(longitude) * scale + offset;
            var y = earthRadius * Math.Cos(latitude) * Math.Sin(longitude) * scale + offset;
            
            return new ScreenPoint { X = x, Y = y };
        }

        public System.Drawing.PointF FromCoordinatesToPixel(System.Drawing.PointF coordinates)
        {
            var x = Math.Round(pixelGlobeCenter.X + (coordinates.X * pixelsDegreesRatio));
            var f = Math.Min(Math.Max(Math.Sin(coordinates.Y * pixelsDegreesRatio), -0.9999d), 0.9999d);
            var y = Math.Round(pixelGlobeCenter.Y + .5d * Math.Log((1d + f) / (1d - f)) * pixelsRadiansRatio);
            return new System.Drawing.PointF(Convert.ToSingle(x), Convert.ToSingle(y));
        }
    }
}