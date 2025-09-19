using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RoSatGCS.Controls
{
    public class DragAdorner : Adorner
    {
        private readonly VisualBrush _visualBrush;
        private readonly UIElement _adornedElement;
        private readonly Size _adornerSize;
        private Point _offset;

        public DragAdorner(UIElement adornedElement, UIElement dragElement)
            : base(adornedElement)
        {
            _adornedElement = adornedElement;
            _visualBrush = new VisualBrush(dragElement)
            {
                Opacity = 0.3
            };

            IsHitTestVisible = false;

            if (dragElement is FrameworkElement element)
            {
                //element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));//element.DesiredSize;
                _adornerSize = new Size(element.ActualWidth, element.ActualHeight);
            }
            else
            {
                _adornerSize = new Size(50, 20); // Default fallback size
            }

        }

        public void UpdatePosition(Point position)
        {
            _offset = position;
            AdornerLayer.GetAdornerLayer(_adornedElement)?.Update(_adornedElement);
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            Rect rect = new Rect(_offset, _adornerSize);
            drawingContext.DrawRectangle(_visualBrush, null, rect);
        }
    }
}
