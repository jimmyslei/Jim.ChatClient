using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using JIm.ChatClient.Core.ViewModels;
using SkiaSharp;
using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        var _viewModel = new MainViewModel();
        DataContext = _viewModel;
        
    }
}
