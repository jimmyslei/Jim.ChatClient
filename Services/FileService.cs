using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace chatClient.Services
{
    public class FileService
    {
        private readonly Window _parentWindow;

        public FileService(Window parentWindow)
        {
            _parentWindow = parentWindow;
        }

        public async Task<string> OpenFileAsync()
        {
            var fileTypes = new FilePickerFileType("文本文件")
            {
                Patterns = new[] { "*.txt", "*.md", "*.json", "*.xml", "*.html", "*.css", "*.js", "*.cs", "*.py" },
                MimeTypes = new[] { "text/*" }
            };

            var options = new FilePickerOpenOptions
            {
                Title = "选择文件",
                AllowMultiple = false,
                FileTypeFilter = new[] { fileTypes }
            };

            var result = await _parentWindow.StorageProvider.OpenFilePickerAsync(options);
            
            if (result.Count > 0)
            {
                var file = result[0];
                using var stream = await file.OpenReadAsync();
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }

            return string.Empty;
        }
    }
} 