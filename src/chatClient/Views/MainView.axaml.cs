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
    public MainView()
    {
        InitializeComponent();
        var _viewModel = new ViewModels.MainViewModel();
        this.DataContext = _viewModel;
        
    }
}
