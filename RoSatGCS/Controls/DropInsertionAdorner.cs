using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace RoSatGCS.Controls
{
    public class DropInsertionAdorner : Adorner
    {
        // Pen for the line (orange, 2px thick)
        private static readonly Pen InsertionPen = new Pen(Brushes.Orange, 1);
        // Size of the little triangles
        private const double TriangleSize = 3;

        // Vertical offset where we’ll draw the insertion line
        private double _insertionY;

        // Constructor
        public DropInsertionAdorner(UIElement adornedElement)
            : base(adornedElement)
        {
            // Typically you do not want to be hit‐test visible
            IsHitTestVisible = false;

            // If you use a Pen or geometry repeatedly, consider calling Freeze() for performance
            InsertionPen.Freeze();
        }

        /// <summary>
        /// Call this to position the insertion line *below* the specified item.
        /// </summary>
        public void Show(ListBoxItem item, bool isTop = false)
        {
            // Transform the bottom of the item into coordinates of the AdornedElement
            GeneralTransform transform = item.TransformToAncestor(AdornedElement);
            Point bottomOfItem = transform.Transform(new Point(0, isTop ? 0 : item.ActualHeight));

            // Store the vertical offset; force re‐render
            _insertionY = bottomOfItem.Y;
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            base.OnRender(drawingContext);

            // The total width to draw across
            double adornerWidth = AdornedElement.RenderSize.Width;
            double lineY = _insertionY;

            // Draw the horizontal line
            Point start = new Point(0, lineY);
            Point end = new Point(adornerWidth, lineY);
            drawingContext.DrawLine(InsertionPen, start, end);

            // Draw the left triangle
            var leftTriangle = CreateTriangleGeometry(
                new Point(TriangleSize, lineY),
                new Point(0, lineY - TriangleSize),
                new Point(0, lineY + TriangleSize)
            );
            drawingContext.DrawGeometry(InsertionPen.Brush, null, leftTriangle);

            // Draw the right triangle
            var rightTriangle = CreateTriangleGeometry(
                new Point(adornerWidth - TriangleSize, lineY),
                new Point(adornerWidth, lineY - TriangleSize),
                new Point(adornerWidth, lineY + TriangleSize)
            );
            drawingContext.DrawGeometry(InsertionPen.Brush, null, rightTriangle);
        }

        /// <summary>
        /// Utility to create a simple closed triangle geometry.
        /// </summary>
        private static Geometry CreateTriangleGeometry(params Point[] points)
        {
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(points[0], isFilled: true, isClosed: true);
                ctx.LineTo(points[1], isStroked: false, isSmoothJoin: false);
                ctx.LineTo(points[2], isStroked: false, isSmoothJoin: false);
            }
            geometry.Freeze();
            return geometry;
        }
    }
}
