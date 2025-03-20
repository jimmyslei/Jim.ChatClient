using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using chatClient.ViewModels;
using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace chatClient.Views;

public partial class MainView : UserControl
{
    private DispatcherTimer _toastTimer;
    
    public MainView()
    {
        InitializeComponent();
        var _viewModel = new ViewModels.MainViewModel();
        this.DataContext = _viewModel;
        
        // 订阅滚动事件
        _viewModel.ScrollToEnd += (s, e) =>
        {
            if (ChatScrollViewer != null)
            {
                ChatScrollViewer.ScrollToEnd();
            }
        };
        
        // 订阅复制到剪贴板事件
        _viewModel.CopyToClipboardRequested += async (s, content) =>
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                await topLevel.Clipboard.SetTextAsync(content);
                ShowToast("内容已复制到剪贴板");
            }
        };
        
        // 初始化Toast计时器
        _toastTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(3)
        };
        
        _toastTimer.Tick += (s, e) =>
        {
            _toastTimer.Stop();
            if (ToastNotification != null)
            {
                ToastNotification.IsVisible = false;
            }
        };
    }
    
    // 显示Toast通知
    private void ShowToast(string message)
    {
        if (ToastNotification != null && ToastText != null)
        {
            ToastText.Text = message;
            ToastNotification.IsVisible = true;
            
            // 重置计时器
            _toastTimer.Stop();
            _toastTimer.Start();
        }
    }

    private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is MainViewModel viewModel)
        {
            // SelectedSession 的绑定会自动更新，不需要在这里手动设置
            // 所有加载消息的逻辑都在 ViewModel 的 OnSelectedSessionChanged 中处理
        }
    }
}
