using Avalonia.Data.Converters;
using Avalonia.Media;
using JIm.ChatClient.Core.Models;
using System;
using System.Globalization;

namespace JIm.ChatClient.Core.Converters
{
    public class RoleToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => new SolidColorBrush(Color.Parse("#34E3F2FD")),
                    MessageRole.Assistant => new SolidColorBrush(Color.Parse("#2CF5F5F5")),
                    MessageRole.System => new SolidColorBrush(Color.Parse("#3AFFF8E1")),
                    _ => new SolidColorBrush(Colors.Transparent)
                };
            }
            return new SolidColorBrush(Colors.Transparent);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RoleToAvatarBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => new SolidColorBrush(Color.Parse("#2196F3")),
                    MessageRole.Assistant => new SolidColorBrush(Color.Parse("#4CAF50")),
                    MessageRole.System => new SolidColorBrush(Color.Parse("#FF9800")),
                    _ => new SolidColorBrush(Colors.Gray)
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RoleToAvatarTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => "用",
                    MessageRole.Assistant => "AI",
                    MessageRole.System => "系",
                    _ => "?"
                };
            }
            return "?";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RoleToDisplayNameConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role switch
                {
                    MessageRole.User => "用户",
                    MessageRole.Assistant => "DeepSeek",
                    MessageRole.System => "系统",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RoleIsAssistantConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is MessageRole role)
            {
                return role == MessageRole.Assistant;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled && parameter is string colors)
            {
                var colorParts = colors.Split(';');
                if (colorParts.Length == 2)
                {
                    return isEnabled ? colorParts[0] : colorParts[1];
                }
            }
            return "#CCCCCC";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ViewTypeEqualityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string viewType && parameter is string paramType)
            {
                return viewType == paramType;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEqual && isEqual && parameter is string paramType)
            {
                return paramType;
            }
            return string.Empty;
        }
    }

    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue && parameter is string options)
            {
                string[] parts = options.Split('|');
                if (parts.Length == 2)
                {
                    return boolValue ? parts[0] : parts[1];
                }
            }
            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}