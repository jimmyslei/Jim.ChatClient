using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace chatClient.Services
{
    public class FileService
    {
        private readonly Window _parentWindow;
        public IStorageFile LastFile { get; private set; }

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
                LastFile = result[0];
                using var stream = await result[0].OpenReadAsync();
                using var reader = new StreamReader(stream);
                return await reader.ReadToEndAsync();
            }

            return string.Empty;
        }
        
        public async Task<IStorageFile> OpenDocumentAsync()
        {
            var fileTypes = new List<FilePickerFileType>
            {
                new FilePickerFileType("文本文件")
                {
                    Patterns = new[] { "*.txt", "*.md", "*.json", "*.xml", "*.html", "*.css", "*.js", "*.cs", "*.py" },
                    MimeTypes = new[] { "text/*" }
                },
                new FilePickerFileType("PDF文件")
                {
                    Patterns = new[] { "*.pdf" },
                    MimeTypes = new[] { "application/pdf" }
                },
                new FilePickerFileType("Word文档")
                {
                    Patterns = new[] { "*.docx", "*.doc" },
                    MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "application/msword" }
                },
                new FilePickerFileType("Excel表格")
                {
                    Patterns = new[] { "*.xlsx", "*.xls" },
                    MimeTypes = new[] { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "application/vnd.ms-excel" }
                }
            };

            var options = new FilePickerOpenOptions
            {
                Title = "选择文档",
                AllowMultiple = false,
                FileTypeFilter = fileTypes
            };

            var result = await _parentWindow.StorageProvider.OpenFilePickerAsync(options);
            
            if (result.Count > 0)
            {
                LastFile = result[0];
                return result[0];
            }

            return null;
        }
    }
} 