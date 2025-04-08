using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using JIm.ChatClient.Core.Services;
using JIm.ChatClient.Core.IServices;
using JIm.ChatClient.Core.Models;

namespace JIm.ChatClient.Core.ViewModels
{
    public partial class TranslateViewModel : ObservableObject
    {
        private readonly IChatService _chatService;

        [ObservableProperty]
        private int _sourceLanguageIndex = 0;

        [ObservableProperty]
        private int _targetLanguageIndex = 1; // 默认选择第二个语言

        [ObservableProperty]
        private string _sourceText = string.Empty;

        [ObservableProperty]
        private string _translatedText = string.Empty;

        [ObservableProperty]
        private bool _isTranslating = false;

        public ObservableCollection<string> AvailableLanguages { get; } = new ObservableCollection<string>();

        public TranslateViewModel(IChatService chatService)
        {
            _chatService = chatService ?? new ChatService();

            // 初始化可用语言列表
            InitializeLanguages();
        }

        private void InitializeLanguages()
        {
            // 添加支持的语言
            AvailableLanguages.Add("中文");
            AvailableLanguages.Add("英语");
            AvailableLanguages.Add("日语");
            AvailableLanguages.Add("韩语");
            AvailableLanguages.Add("法语");
            AvailableLanguages.Add("德语");
            AvailableLanguages.Add("西班牙语");
            AvailableLanguages.Add("俄语");
            // 可以根据需要添加更多语言
        }

        [RelayCommand]
        private async Task TranslateAsync()
        {
            if (string.IsNullOrWhiteSpace(SourceText))
                return;

            try
            {
                TranslatedText = string.Empty;
                IsTranslating = true;

                // 获取源语言和目标语言的代码
                string sourceLanguage = GetLanguageCode(SourceLanguageIndex);
                string targetLanguage = GetLanguageCode(TargetLanguageIndex);

                var aiModel = new AIModel
                {
                    Id = "glm-4-plus", 
                    DisplayName = "glm-4-plus",
                    ApiKey = "886f0982df504caa9670294e87cc509a.VTJYiOaqF1eKvgKI",
                    ApiEndpoint = "https://open.bigmodel.cn/api/paas/v4/chat/completions",
                    Type = "online"
                };
                // 调用翻译服务
                await foreach (var chunk in _chatService.TranslateMessageAsync(
                            targetLanguage, aiModel, SourceText))
                {
                    TranslatedText += chunk;
                }
               
            }
            catch (Exception ex)
            {
                // 处理异常
                TranslatedText = $"翻译出错: {ex.Message}";
            }
            finally
            {
                IsTranslating = false;
            }
        }

        [RelayCommand]
        private void SwapLanguages()
        {
            // 交换源语言和目标语言
            int temp = SourceLanguageIndex;
            SourceLanguageIndex = TargetLanguageIndex;
            TargetLanguageIndex = temp;

            // 如果已经有翻译结果，也交换文本
            if (!string.IsNullOrEmpty(TranslatedText))
            {
                string tempText = SourceText;
                SourceText = TranslatedText;
                TranslatedText = tempText;
            }
        }

        [RelayCommand]
        private void ClearTranslation()
        {
            SourceText = string.Empty;
            TranslatedText = string.Empty;
        }

        [RelayCommand]
        private async Task UploadFileForTranslationAsync()
        {
            try
            {
                string fileContent = "";
                if (!string.IsNullOrEmpty(fileContent))
                {
                    SourceText = fileContent;
                }
            }
            catch (Exception ex)
            {
                // 处理异常
                SourceText = $"文件加载失败: {ex.Message}";
            }
        }

        [RelayCommand]
        private void CopyTranslatedText()
        {
            if (!string.IsNullOrEmpty(TranslatedText))
            {
                //todo: 复制翻译结果到剪贴板
                // 可以添加一个通知提示复制成功
            }
        }

        [RelayCommand]
        private async Task ExportTranslatedTextAsync()
        {
            if (string.IsNullOrEmpty(TranslatedText))
                return;

            try
            {
                // todo: 导出翻译结果到文件
            }
            catch (Exception ex)
            {
                // 处理异常
                // 可以添加一个通知提示保存失败
            }
        }

        private string GetLanguageCode(int languageIndex)
        {
            // 根据语言索引返回对应的语言代码
            // 这里使用简化的映射，实际应用中可能需要更复杂的映射
            switch (languageIndex)
            {
                case 0: return "中文"; // zh 中文
                case 1: return "英语"; // en 英语
                case 2: return "日语"; // ja 日语
                case 3: return "韩语"; // ko 韩语
                case 4: return "法语"; // fr 法语
                case 5: return "德语"; // de 德语
                case 6: return "西班牙语"; // es 西班牙语
                case 7: return "俄语"; // ru 俄语
                default: return "英语"; // en 默认英语
            }
        }
    }

}
