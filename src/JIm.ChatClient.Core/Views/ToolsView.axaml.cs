using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace JIm.ChatClient.Core.Views
{
    public partial class ToolsView : UserControl
    {
        public ToolsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 