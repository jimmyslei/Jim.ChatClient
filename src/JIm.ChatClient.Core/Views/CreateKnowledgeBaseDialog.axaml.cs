using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JIm.ChatClient.Core.Commands;
using JIm.ChatClient.Core.ViewModels;

namespace JIm.ChatClient.Core.Views
{
    public partial class CreateKnowledgeBaseDialog : Window
    {
        public CreateKnowledgeBaseDialog()
        {
            InitializeComponent();
        }

        public CreateKnowledgeBaseDialog(MainViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            
        }
    }
}