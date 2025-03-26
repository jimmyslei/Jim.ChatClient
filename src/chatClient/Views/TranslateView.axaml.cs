using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace chatClient.Views
{
    public partial class TranslateView : UserControl
    {
        public TranslateView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
} 