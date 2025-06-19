using RoSatGCS.Utils.Maps;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RoSatGCS.Controls
{

    /// <summary>
    /// GroundTrackMap.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class GroundTrackMap : UserControl
    {
        private readonly string _defaultMap = @"Images/map.png";
        private GroundTrackMapHandler _handler;
        private DrawingGroup _backingStore = new DrawingGroup();

        private Point _mouseInitPos;
        private Point _mouseCurrentPos;
        private double _deltaX, _deltaY;
        private bool _dragging;

        public GroundTrackMap()
        {
            InitializeComponent();
            _handler = new GroundTrackMapHandler();

            //this.ClipToBounds = true;
        }
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            _handler.Resize(this.Width, this.Height);
            _handler.SetMap(_defaultMap);
            Render();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);
            Render();
            drawingContext.DrawDrawing(_backingStore);
        }

        private void Render()
        {
            DrawingContext drawingContext = _backingStore.Open();
            _handler.Render(ref drawingContext);
            drawingContext.Close();
        }

        private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 1)
            {
                _mouseInitPos = e.GetPosition(canvas);
                _dragging = true;
            }
            e.Handled = true;
        }

        private void OnMouseMove(object sender, MouseEventArgs e)
        {
            if(!_dragging) {
                e.Handled = true;
                return;
            }
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                _mouseCurrentPos = e.GetPosition(canvas);
                _deltaX = _mouseCurrentPos.X - _mouseInitPos.X;
                _deltaY = _mouseCurrentPos.Y - _mouseInitPos.Y;
                _handler.Move(_deltaX);
                Render();
                var p = _handler.MousePosToLatLon(_mouseCurrentPos.X, _mouseCurrentPos.Y);
            }
            else
            {
                _handler.Move(_deltaX, true);
                _dragging = false;
            }
        }

        private void OnMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_dragging)
            {
                _handler.Move(_deltaX, true);
                _dragging = false;
            }
        }
    }
}
