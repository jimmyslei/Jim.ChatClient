using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Collections.Generic;

namespace JIm.ChatClient.Core.Views
{
    public partial class SettingsView : UserControl
    {
        private Button _currentSelectedButton;
        private Dictionary<string, Control> _viewMap;

        public SettingsView()
        {
            InitializeComponent();
            
            // 初始化视图映射
            _viewMap = new Dictionary<string, Control>
            {
                { "ModelSettings", this.FindControl<Control>("ModelSettingsView") },
                { "KnowledgeBaseSettings", this.FindControl<Control>("KnowledgeBaseSettingsView") },
                { "UISettings", this.FindControl<Control>("UISettingsView") },
                { "About", this.FindControl<Control>("AboutView") }
            };
            
            // 设置默认选中按钮
            _currentSelectedButton = this.FindControl<Button>("ModelSettings");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void OnMenuButtonClick(object sender, RoutedEventArgs e)
        {
            if (sender is Button clickedButton)
            {
                // 移除之前选中按钮的Selected类
                if (_currentSelectedButton != null)
                {
                    _currentSelectedButton.Classes.Remove("Selected");
                }
                
                // 添加Selected类到当前点击的按钮
                clickedButton.Classes.Add("Selected");
                _currentSelectedButton = clickedButton;
                
                // 隐藏所有视图
                foreach (var view in _viewMap.Values)
                {
                    view.IsVisible = false;
                }
                
                // 显示对应的视图
                string buttonName = clickedButton.Name?.ToString();
                if (!string.IsNullOrEmpty(buttonName) && _viewMap.ContainsKey(buttonName))
                {
                    _viewMap[buttonName].IsVisible = true;
                }
            }
        }

        private async void OnGitHubLinkClick(object sender, RoutedEventArgs e)
        {
            // GitHub项目地址
            string githubUrl = "https://github.com/jimmyslei/Jim.ChatClient";
            
            try
            {
                // 使用系统默认浏览器打开URL
                if (OperatingSystem.IsWindows())
                {
                    using var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = githubUrl;
                    process.StartInfo.UseShellExecute = true;
                    process.Start();
                }
                else if (OperatingSystem.IsLinux())
                {
                    using var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "xdg-open";
                    process.StartInfo.Arguments = githubUrl;
                    process.Start();
                }
                else if (OperatingSystem.IsMacOS())
                {
                    using var process = new System.Diagnostics.Process();
                    process.StartInfo.FileName = "open";
                    process.StartInfo.Arguments = githubUrl;
                    process.Start();
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"无法打开URL: {ex.Message}");
            }
        }
    }
}