using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace WpfHexaEditor.Core.MethodExtention
{
    public static class StringExtension
    {
        /// <summary>
        /// The screen size of a string
        /// </summary>
        /// <remarks>
        /// Code from :
        /// https://stackoverflow.com/questions/11447019/is-there-any-way-to-find-the-width-of-a-character-in-a-fixed-width-font-given-t
        /// </remarks>
        public static Size GetScreenSize(this string text, FontFamily fontFamily, double fontSize, FontStyle fontStyle,
            FontWeight fontWeight, FontStretch fontStretch)
        {
            fontFamily = fontFamily ?? new TextBlock().FontFamily;
            fontSize = fontSize > 0 ? fontSize : new TextBlock().FontSize;

            var typeface = new Typeface(fontFamily, fontStyle, fontWeight, fontStretch);
#pragma warning disable CS0618 // Le type ou le membre est obsolète
            var ft = new FormattedText(text ?? string.Empty, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, typeface, fontSize, Brushes.Black);
#pragma warning restore CS0618 // Le type ou le membre est obsolète

            return new Size(ft.Width, ft.Height);
        }
    }
}