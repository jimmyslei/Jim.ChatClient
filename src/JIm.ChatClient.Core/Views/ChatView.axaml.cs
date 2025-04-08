using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using JIm.ChatClient.Core.ViewModels;
using System;

namespace JIm.ChatClient.Core.Views
{
    public partial class ChatView : UserControl
    {
        private DispatcherTimer _toastTimer;
        public ChatView()
        {
            InitializeComponent();
            
            // 在DataContextChanged事件中订阅ViewModel的事件
            this.DataContextChanged += ChatView_DataContextChanged;
            
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
        
        private void ChatView_DataContextChanged(object sender, EventArgs e)
        {
            // 订阅ViewModel的事件
            if (this.DataContext is MainViewModel viewModel)
            {
                // 订阅滚动事件
                viewModel.ScrollToEnd += ViewModel_ScrollToEnd;
                
                // 订阅复制到剪贴板事件
                viewModel.CopyToClipboardRequested += ViewModel_CopyToClipboardRequested;
            }
        }
        
        private void ViewModel_ScrollToEnd(object sender, EventArgs e)
        {
            if (ChatScrollViewer != null)
            {
                ChatScrollViewer.ScrollToEnd();
            }
        }
        
        private async void ViewModel_CopyToClipboardRequested(object sender, string content)
        {
            var topLevel = TopLevel.GetTopLevel(this);
            if (topLevel != null)
            {
                await topLevel.Clipboard.SetTextAsync(content);
                ShowToast("内容已复制到剪贴板");
            }
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
    }
} 