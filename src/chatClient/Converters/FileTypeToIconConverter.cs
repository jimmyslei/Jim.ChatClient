using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Globalization;

namespace chatClient.Converters
{
    public class FileTypeToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string fileType)
            {
                return fileType.ToLowerInvariant() switch
                {
                    var t when t.Contains("pdf") => "📄",
                    var t when t.Contains("word") => "📝",
                    var t when t.Contains("excel") => "📊",
                    var t when t.Contains("powerpoint") => "📑",
                    var t when t.Contains("text") => "📄",
                    var t when t.Contains("markdown") => "📝",
                    var t when t.Contains("json") => "📋",
                    var t when t.Contains("xml") => "📋",
                    var t when t.Contains("html") => "🌐",
                    var t when t.Contains("源代码") => "💻",
                    var t when t.Contains("配置") => "⚙️",
                    var t when t.Contains("日志") => "📜",
                    var t when t.Contains("表格") => "📊",
                    _ => "📄"
                };
            }
            
            return "📄";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 