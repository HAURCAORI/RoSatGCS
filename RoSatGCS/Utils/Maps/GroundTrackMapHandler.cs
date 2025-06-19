using RoSatGCS.Utils.Drawing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RoSatGCS.Utils.Maps
{

    public struct LatLng
    {
        public double Lat;
        public double Lng;
    }
    public class GroundTrackMapHandler
    {
        private readonly int mMarginLeft = 50;
        private readonly int mMarginTop = 50;
        private readonly double[] mLatAxisPos = [-60,-30,0, 30,60];
        private readonly double[] mLngAxisPos = [-150,-120,-90,-60, -30, 0, 30, 60,90,120,150,180];
        private double mWidth;
        private double mHeight;
        private Rect mMapArea;
        private BitmapImage? mBaseImage;
        private int mOffset = 0;
        private int mMove = 0;
        private Pen mGridColor = new Pen(new SolidColorBrush(Color.FromArgb(100, 150, 150, 150)), 1);
       
        //private GlyphTypeface mGlyphTypeface;

        public GroundTrackMapHandler() {
            //new Typeface("Consolas").TryGetGlyphTypeface(out this.mGlyphTypeface);
        }

        public void Resize(double width, double height)
        {
            mWidth = width; ;
            mHeight = height;
            mMapArea.X = mMarginLeft;
            mMapArea.Y = mMarginTop;
            mMapArea.Width = width - mMarginLeft;
            mMapArea.Height = height - mMarginTop;
        }

        public void Move(double deltaX, bool set = false)
        {
            if (!set)
            {
                mMove = Convert.ToInt32(deltaX);
                return;
            }
            mOffset += Convert.ToInt32(deltaX);
            mOffset = Convert.ToInt32(mOffset % mMapArea.Width);
            mMove = 0;
        }
        public bool SetMap(string uri)
        {
            try
            {
                mBaseImage = new BitmapImage(new Uri(uri, UriKind.RelativeOrAbsolute));
            }
            catch (System.Exception) { }
            return true;
        }

        public void Render(ref DrawingContext drawingContext)
        {
            DrawMap(ref drawingContext);
            DrawAxis(ref drawingContext);
        }

        private Rect ConvertMapArea(Rect rect)
        {
            rect.X += mMapArea.X;
            rect.Y += mMapArea.Y;
            return rect;
        }
        private Point ConvertMapArea(Point point)
        {
            point.X += mMapArea.X;
            point.Y += mMapArea.Y;
            return point;
        }

        private Point ConvertLatArea(Point point)
        {
            point.Y += mMapArea.Y;
            return point;
        }

        private Point ConvertLngArea(Point point)
        {
            point.X += mMapArea.X;
            return point;
        }

        public LatLng MousePosToLatLon(double mx, double my)
        {
            mx = Math.Max(Math.Min(mx, mWidth), 0);
            my = Math.Max(Math.Min(my, mHeight), 0);
            mx -= mMapArea.X;
            my -= mMapArea.Y;
            return PosToLatLon(mx,my);
        }

        public LatLng PosToLatLon(double x, double y)
        {
            LatLng pos;
            x = Math.Max(Math.Min(x,mMapArea.Width), 0);
            y = Math.Max(Math.Min(y, mMapArea.Height), 0);
            var delta = (mMove + mOffset) % mMapArea.Width;
            pos.Lat = (1 - y / mMapArea.Height * 2) * 90;
            pos.Lng = -(1 - (x-delta)* 2 / mMapArea.Width) * 180;
            if (pos.Lng > 180)
            {
                pos.Lng -= 360;
            }
            else if (pos.Lng <= -180)
            {
                pos.Lng += 360;
            }
            return pos;
        }

        private double LatToY(double deg)
        {
            deg = Math.Max(Math.Min(deg, 90), -90);
            return mMapArea.Height / 2 *( 1 - deg / 90);
        }
        private double LngToX(double deg)
        {
            deg = Math.Max(Math.Min(deg, 180), -180);
            var delta = (mMove + mOffset) % mMapArea.Width;
            var pos = mMapArea.Width / 2 * (1 + deg / 180) + delta;
            pos %= mMapArea.Width;
            if (pos < 0)
            {
                pos += mMapArea.Width;
            }
            return pos;
        }

        public void DrawMap(ref DrawingContext context)
        {
            if (mBaseImage != null)
            {
                var delta = (mMove + mOffset) % mMapArea.Width;
                var ratio = Math.Abs(delta) / mMapArea.Width;

                CroppedBitmap leftImage, rightImage;
                int w1 = Convert.ToInt32(mBaseImage.PixelWidth * ratio);
                int w2 = Convert.ToInt32(mBaseImage.PixelWidth * (1 - ratio));
                if (w1 == 0 || w2 == 0)
                {
                    context.DrawImage(mBaseImage, mMapArea);
                    return;
                }
                if (delta >= 0)
                {
                    leftImage = new CroppedBitmap(mBaseImage, new Int32Rect(w2, 0, w1, mBaseImage.PixelHeight));
                    rightImage = new CroppedBitmap(mBaseImage, new Int32Rect(0, 0, w2, mBaseImage.PixelHeight));
                    context.DrawImage(leftImage, ConvertMapArea(new Rect(0, 0, delta, mMapArea.Height)));
                    context.DrawImage(rightImage, ConvertMapArea(new Rect(delta - 1, 0, mMapArea.Width - delta, mMapArea.Height)));
                }
                else
                {
                    leftImage = new CroppedBitmap(mBaseImage, new Int32Rect(w1, 0, w2, mBaseImage.PixelHeight));
                    rightImage = new CroppedBitmap(mBaseImage, new Int32Rect(0, 0, w1, mBaseImage.PixelHeight));
                    context.DrawImage(leftImage, ConvertMapArea(new Rect(0, 0, mMapArea.Width + delta, mMapArea.Height)));
                    context.DrawImage(rightImage, ConvertMapArea(new Rect(mMapArea.Width + delta - 1, 0, -delta, mMapArea.Height)));
                }

            }
        }
        public void DrawAxis(ref DrawingContext context)
        {
            foreach (var l in mLatAxisPos)
            {
                context.DrawLine(mGridColor, ConvertMapArea(new Point(0, LatToY(l))), ConvertMapArea(new Point(mMapArea.Width, LatToY(l))));
                context.DrawGlyphRun(Brushes.MidnightBlue, BaseDrawing.CreateGlyphRun(l+ "°", 10, ConvertLatArea(new Point(mMarginLeft-10, LatToY(l))), FontOrientation.Right));
            }

            foreach (var l in mLngAxisPos)
            {
                context.DrawLine(mGridColor, ConvertMapArea(new Point(LngToX(l), 0)), ConvertMapArea(new Point(LngToX(l),mMapArea.Height)));
                context.DrawGlyphRun(Brushes.MidnightBlue, BaseDrawing.CreateGlyphRun(l + "°", 10, ConvertLngArea(new Point(LngToX(l), mMarginTop - 10)), FontOrientation.Center));

            }
        }
    }
}
