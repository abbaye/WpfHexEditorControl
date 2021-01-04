using System;
using System.Reflection;
using System.Windows.Media;

namespace WpfHexEditor.Sample.BinaryFilesDifference
{
    public static class RandomBrushes
    {
        public static SolidColorBrush PickBrush()
        {
            PropertyInfo[] properties = typeof(Brushes).GetProperties();

            int random = new Random().Next(properties.Length);

            return (SolidColorBrush)properties[random].GetValue(null, null);
        }
    }
}
