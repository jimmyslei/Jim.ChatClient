using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using chatClient.Commands;
using chatClient.ViewModels;

namespace chatClient.Views
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
            
            // 添加命令处理
            if (viewModel != null)
            {
                viewModel.CancelCommand = new RelayCommand(() => Close());
                //viewModel.CreateKnowledgeBaseCommand = new RelayCommand(() =>
                //{
                //    viewModel.CreateKnowledgeBase();
                //    Close();
                //});
            }
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}