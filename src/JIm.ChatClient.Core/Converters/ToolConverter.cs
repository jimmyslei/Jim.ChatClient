using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;
using JIm.ChatClient.Core.Models;
using Avalonia;

namespace JIm.ChatClient.Core.Converters
{
    public class ToolItemEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToolItem selectedTool && parameter is ToolItem item)
            {
                // 如果当前工具项与选中的工具项相同，则返回选中的背景颜色
                return selectedTool.Type == item.Type ? "#4D4D4D" : "#2D2D2D";
            }
            
            // 默认返回未选中的背景颜色
            return "#2D2D2D";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SelectedBorderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ToolItem selectedTool && parameter is ToolItem item)
            {
                // 如果当前工具项与选中的工具项相同，则返回左侧边框粗细
                return selectedTool.Type == item.Type ? new Thickness(3, 1, 1, 1) : new Thickness(1);
            }
            
            // 默认返回普通边框粗细
            return new Thickness(1);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isSelected = value is bool b && b;
            string paramStr = parameter as string;
            
            if (paramStr != null)
            {
                string[] parts = paramStr.Split(',');
                if (parts.Length == 2)
                {
                    string brush = isSelected ? parts[0] : parts[1];
                    return SolidColorBrush.Parse(brush);
                }
            }
            
            // 默认颜色
            return isSelected ? new SolidColorBrush(Color.Parse("#3D6392")) : new SolidColorBrush(Color.Parse("#2D2D2D"));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

}