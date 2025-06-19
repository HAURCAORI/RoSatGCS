using System;
using System.Collections.Generic;
using System.Windows;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Diagnostics;
using System.Reflection;
using System.Windows.Controls;

namespace RoSatGCS.Utils.Drawing
{

    internal enum FontOrientation
    {
        None,
        Center,
        Left,
        Right
    }
    internal class BaseDrawing
    {   
        static Typeface _basetType = new Typeface(new FontFamily(new Uri("pack://application:,,,/"), "./Fonts/#KoPubWorldDotum"), FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        internal static GlyphRun? CreateGlyphRun(string text, double size, int x, int y, FontOrientation orientation = FontOrientation.None)
        {
            return CreateGlyphRun(_basetType, text, size, new Point(x, y), orientation);
        }

        internal static GlyphRun? CreateGlyphRun(string text, double size, Point origin, FontOrientation orientation = FontOrientation.None)
        {
            return CreateGlyphRun(_basetType, text, size, origin, orientation);
        }
        internal static GlyphRun? CreateGlyphRun(Typeface typeface, string text, double size, int x, int y, FontOrientation orientation = FontOrientation.None)
        {
            return CreateGlyphRun(typeface, text, size, new Point(x, y), orientation);
        }

        internal static GlyphRun? CreateGlyphRun(Typeface typeface, string text, double size, Point origin, FontOrientation orientation = FontOrientation.None)
        {
            if (text.Length == 0)
                return null;

            GlyphTypeface glyphTypeface;

            if (!typeface.TryGetGlyphTypeface(out glyphTypeface))
            {
                return null;
            }

            var glyphIndexes = new ushort[text.Length];
            var advanceWidths = new double[text.Length];

            for (int n = 0; n < text.Length; n++)
            {
                try
                {
                    var glyphIndex = glyphTypeface.CharacterToGlyphMap[text[n]];
                    glyphIndexes[n] = glyphIndex;
                    advanceWidths[n] = glyphTypeface.AdvanceWidths[glyphIndex] * size;
                }
                 catch {
                    glyphIndexes[n] = '\0';
                    advanceWidths[n] = glyphTypeface.AdvanceWidths['\0'] * size;
                }
            }

            var total = advanceWidths.Sum();
            switch (orientation)
            {
                case FontOrientation.Center:
                    origin.X = origin.X - total / 2;
                    break;
                case FontOrientation.Right:
                    origin.X = origin.X - total;
                    break;
            }

            return new GlyphRun(glyphTypeface, 0, false, size, glyphIndexes, origin, advanceWidths, null, null,
                                        null,
                                        null, null, null);
        }
    }
}
