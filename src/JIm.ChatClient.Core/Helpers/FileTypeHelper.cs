using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace JIm.ChatClient.Core.Helpers
{
    public static class FileTypeHelper
    {
        private static readonly Dictionary<string, string> _fileTypeMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            // 文本文件
            { ".txt", "文本文件" },
            { ".md", "Markdown文件" },
            { ".json", "JSON文件" },
            { ".xml", "XML文件" },
            { ".html", "HTML文件" },
            { ".htm", "HTML文件" },
            { ".css", "CSS文件" },
            { ".js", "JavaScript文件" },
            { ".ts", "TypeScript文件" },
            { ".cs", "C#源代码" },
            { ".py", "Python源代码" },
            { ".java", "Java源代码" },
            { ".cpp", "C++源代码" },
            { ".c", "C源代码" },
            { ".go", "Go源代码" },
            { ".rs", "Rust源代码" },
            { ".rb", "Ruby源代码" },
            { ".php", "PHP源代码" },
            { ".sql", "SQL脚本" },
            { ".yaml", "YAML文件" },
            { ".yml", "YAML文件" },
            { ".ini", "INI配置文件" },
            { ".config", "配置文件" },
            { ".log", "日志文件" },
            
            // 文档文件
            { ".pdf", "PDF文档" },
            { ".docx", "Word文档" },
            { ".doc", "Word文档" },
            { ".xlsx", "Excel表格" },
            { ".xls", "Excel表格" },
            { ".pptx", "PowerPoint演示文稿" },
            { ".ppt", "PowerPoint演示文稿" },
            { ".rtf", "富文本文档" },
            { ".odt", "OpenDocument文本文档" },
            { ".ods", "OpenDocument电子表格" },
            { ".odp", "OpenDocument演示文稿" },
            
            // 其他常见文件
            { ".csv", "CSV表格数据" },
            { ".tsv", "TSV表格数据" }
        };
        
        public static string GetFileTypeDescription(string fileName)
        {
            string extension = Path.GetExtension(fileName);
            
            if (_fileTypeMap.TryGetValue(extension, out string description))
            {
                return description;
            }
            
            return "未知文件类型";
        }
        
        public static bool IsSupportedFileType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            // 支持的文件类型列表
            var supportedExtensions = new[] 
            { 
                ".txt", ".md", ".json", ".xml", ".html", ".htm", ".css", ".js", 
                ".cs", ".py", ".java", ".cpp", ".c", ".go", ".rs", ".rb", ".php",
                ".sql", ".yaml", ".yml", ".ini", ".config", ".log", ".pdf", ".docx", 
                ".doc", ".xlsx", ".xls", ".csv", ".tsv" 
            };
            
            return supportedExtensions.Contains(extension);
        }
        
        public static string GetContentType(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".doc" => "application/msword",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".xls" => "application/vnd.ms-excel",
                ".pptx" => "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                ".ppt" => "application/vnd.ms-powerpoint",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" => "text/html",
                ".htm" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".csv" => "text/csv",
                ".tsv" => "text/tab-separated-values",
                _ => "application/octet-stream"
            };
        }
    }
} 