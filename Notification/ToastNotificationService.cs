// using Avalonia;
// using Avalonia.Controls;
// using Avalonia.Media;
// using Avalonia.Threading;
// using System;
// using System.Collections.Generic;
// using System.Linq;
// using System.Threading.Tasks;

// namespace chatClient.Notification
// {
//     public interface INotificationService
//     {
//         void ShowNotification(string message);
//     }

//     public class ToastNotificationService : INotificationService
//     {
//         private readonly Window _window;
        
//         public ToastNotificationService(Window window)
//         {
//             _window = window;
//         }
        
//         public void ShowNotification(string message)
//         {
//             var notification = new Border
//             {
//                 Background = new SolidColorBrush(Color.Parse("#333333")),
//                 CornerRadius = new CornerRadius(4),
//                 Padding = new Thickness(12, 8),
//                 HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
//                 VerticalAlignment = Avalonia.Layout.VerticalAlignment.Bottom,
//                 Margin = new Thickness(0, 0, 0, 20),
//                 Child = new TextBlock
//                 {
//                     Text = message,
//                     Foreground = Brushes.White,
//                     TextAlignment = TextAlignment.Center
//                 }
//             };
            
//             // 添加到窗口
//             var overlay = new Panel
//             {
//                 Children = { notification },
//                 IsHitTestVisible = false
//             };
            
//             _window.Content = new Grid
//             {
//                 Children = 
//                 {
//                     //_window.Content,
//                     overlay
//                 }
//             };
            
//             // 3秒后移除
//             var timer = new DispatcherTimer
//             {
//                 Interval = TimeSpan.FromSeconds(3)
//             };
            
//             timer.Tick += (s, e) =>
//             {
//                 timer.Stop();
//                 (_window.Content as Grid)?.Children.Remove(overlay);
//             };
            
//             timer.Start();
//         }
//     }
// }