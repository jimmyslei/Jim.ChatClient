using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using JIm.ChatClient.Core.Models;
using JIm.ChatClient.Core.Services;

namespace JIm.ChatClient.Core.ViewModels
{
    public class ToolItem : ObservableObject
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconData { get; set; }
        public string Type { get; set; } // 工具类型，如PdfToWord、WordToPdf等

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set => SetProperty(ref _isSelected, value);
        }
    }

    public partial class ToolsViewModel : ObservableObject
    {
        private readonly HttpClient _httpClient;
        private readonly string _aiApiEndpoint;
        private readonly string _aiApiKey;
        private readonly PdfToWordConverter _pdfToWordConverter;

        [ObservableProperty]
        private ObservableCollection<ToolItem> _toolItems;

        [ObservableProperty]
        private ToolItem _selectedTool;

        [ObservableProperty]
        private bool _isProcessing;

        [ObservableProperty]
        private double _processProgress;

        [ObservableProperty]
        private string _statusMessage;

        [ObservableProperty]
        private string _selectedFilePath;

        [ObservableProperty]
        private string _outputFilePath;

        public ToolsViewModel()
        {
            _httpClient = new HttpClient();
            _pdfToWordConverter = new PdfToWordConverter();
            
            // 从配置或环境变量获取API Key和Endpoint（这里使用示例值）
            _aiApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "";
            _aiApiEndpoint = "https://api.openai.com/v1/chat/completions"; // 请替换为实际API地址
            
            _toolItems = new ObservableCollection<ToolItem>
            {
                new ToolItem
                {
                    Title = "PDF转Word",
                    Description = "将PDF文档转换为Word文档，保持原始布局",
                    IconData = "M14,2H6A2,2 0 0,0 4,4V20A2,2 0 0,0 6,22H18A2,2 0 0,0 20,20V8L14,2M18,20H6V4H13V9H18V20Z M12,17V15H7V17H12M17,13V11H7V13H17",
                    Type = "PdfToWord"
                },
                new ToolItem
                {
                    Title = "Word转PDF",
                    Description = "将Word文档转换为PDF格式",
                    IconData = "M19,3A2,2 0 0,1 21,5V19A2,2 0 0,1 19,21H5A2,2 0 0,1 3,19V5A2,2 0 0,1 5,3H19M18.5,18.5V5.5H5.5V18.5H18.5M17,6.5H16V11.5H17V6.5M14,6.5A2,2 0 0,0 12,8.5V11.5A2,2 0 0,0 14,13.5H14.5V6.5H14M9.5,7.5V6.5H14V7.5H9.5M9.5,8.5H11V9.5H9.5V8.5M9.5,11.5H11V12.5H9.5V11.5M7,14.5H15V15.5H7V14.5M7,16.5H15V17.5H7V16.5M7,18.5H15V19.5H7V18.5Z",
                    Type = "WordToPdf"
                },
                new ToolItem
                {
                    Title = "PDF文档翻译",
                    Description = "将PDF文档中的文本翻译成其他语言",
                    IconData = "M12.87,15.07L10.33,12.56L10.36,12.53C12.1,10.59 13.34,8.36 14.07,6H17V4H10V2H8V4H1V6H12.17C11.5,7.92 10.44,9.75 9,11.35C8.07,10.32 7.3,9.19 6.69,8H4.69C5.42,9.63 6.42,11.17 7.67,12.56L2.58,17.58L4,19L9,14L12.11,17.11L12.87,15.07M18.5,10H16.5L12,22H14L15.12,19H19.87L21,22H23L18.5,10M15.88,17L17.5,12.67L19.12,17H15.88Z",
                    Type = "PdfTranslate"
                },
                new ToolItem
                {
                    Title = "图片OCR",
                    Description = "从图片中提取文字内容",
                    IconData = "M9.5,3A6.5,6.5 0 0,1 16,9.5C16,11.11 15.41,12.59 14.44,13.73L14.71,14H15.5L20.5,19L19,20.5L14,15.5V14.71L13.73,14.44C12.59,15.41 11.11,16 9.5,16A6.5,6.5 0 0,1 3,9.5A6.5,6.5 0 0,1 9.5,3M9.5,5C7,5 5,7 5,9.5C5,12 7,14 9.5,14C12,14 14,12 14,9.5C14,7 12,5 9.5,5Z",
                    Type = "ImageOcr"
                }
            };
        }

        [RelayCommand]
        private async Task SelectFile()
        {
            try
            {
                var dialog = new OpenFileDialog();
                
                // 根据选中的工具设置文件筛选器
                if (SelectedTool != null)
                {
                    switch (SelectedTool.Type)
                    {
                        case "PdfToWord":
                            dialog.Filters.Add(new FileDialogFilter { Name = "PDF文件", Extensions = { "pdf" } });
                            break;
                        case "WordToPdf":
                            dialog.Filters.Add(new FileDialogFilter { Name = "Word文件", Extensions = { "docx", "doc" } });
                            break;
                        case "PdfTranslate":
                            dialog.Filters.Add(new FileDialogFilter { Name = "PDF文件", Extensions = { "pdf" } });
                            break;
                        case "ImageOcr":
                            dialog.Filters.Add(new FileDialogFilter { Name = "图片文件", Extensions = { "jpg", "jpeg", "png", "bmp" } });
                            break;
                    }
                }
                
                var result = await dialog.ShowAsync(new Window());
                
                if (result != null && result.Length > 0)
                {
                    SelectedFilePath = result[0];
                    StatusMessage = $"已选择文件: {System.IO.Path.GetFileName(SelectedFilePath)}";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"选择文件时发生错误: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task ProcessFile()
        {
            if (string.IsNullOrEmpty(SelectedFilePath) || SelectedTool == null)
            {
                StatusMessage = "请先选择一个文件和工具";
                return;
            }

            try
            {
                IsProcessing = true;
                ProcessProgress = 0;
                StatusMessage = $"开始处理 {System.IO.Path.GetFileName(SelectedFilePath)}...";

                // 设置输出文件路径
                string fileName = System.IO.Path.GetFileNameWithoutExtension(SelectedFilePath);
                string directory = System.IO.Path.GetDirectoryName(SelectedFilePath);

                // 根据选择的工具类型执行不同的处理逻辑
                switch (SelectedTool.Type)
                {
                    case "PdfToWord":
                        OutputFilePath = System.IO.Path.Combine(directory, $"{fileName}.docx");
                        await ProcessPdfToWordAsync(SelectedFilePath, OutputFilePath);
                        break;
                    case "WordToPdf":
                        OutputFilePath = System.IO.Path.Combine(directory, $"{fileName}.pdf");
                        // 这里实现Word转PDF的逻辑
                        await SimulateProcessingAsync();
                        break;
                    case "PdfTranslate":
                        OutputFilePath = System.IO.Path.Combine(directory, $"{fileName}_translated.pdf");
                        // 这里实现PDF翻译的逻辑
                        await SimulateProcessingAsync();
                        break;
                    case "ImageOcr":
                        OutputFilePath = System.IO.Path.Combine(directory, $"{fileName}_ocr.txt");
                        // 这里实现图片OCR的逻辑
                        await SimulateProcessingAsync();
                        break;
                }

                StatusMessage = $"处理完成! 输出文件: {System.IO.Path.GetFileName(OutputFilePath)}";
            }
            catch (Exception ex)
            {
                StatusMessage = $"处理文件时发生错误: {ex.Message}";
            }
            finally
            {
                IsProcessing = false;
                ProcessProgress = 100;
            }
        }
        
        // 模拟处理过程（用于暂未实现的功能）
        private async Task SimulateProcessingAsync()
        {
            for (int i = 0; i <= 100; i += 5)
            {
                ProcessProgress = i;
                await Task.Delay(100); // 模拟处理时间
            }
        }
        
        // PDF转Word实现
        private async Task ProcessPdfToWordAsync(string pdfPath, string wordPath)
        {
            try
            {
                // 使用PdfToWordConverter服务执行转换
                bool success = await _pdfToWordConverter.ConvertPdfToWordAsync(
                    pdfPath, 
                    wordPath, 
                    (progress, message) => 
                    {
                        // 更新进度和状态消息
                        ProcessProgress = progress;
                        StatusMessage = message;
                    });
                
                if (!success)
                {
                    throw new Exception("PDF转换失败");
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"PDF转Word处理失败: {ex.Message}", ex);
            }
        }

        [RelayCommand]
        private void ToolSelected(ToolItem tool)
        {
            // 先将所有工具项的选中状态设为false
            foreach (var item in ToolItems)
            {
                item.IsSelected = false;
            }
            
            // 设置当前选中工具项
            tool.IsSelected = true;
            SelectedTool = tool;
            
            // 重置相关状态
            SelectedFilePath = null;
            OutputFilePath = null;
            StatusMessage = $"已选择工具: {tool.Title}";
        }
    }
} 