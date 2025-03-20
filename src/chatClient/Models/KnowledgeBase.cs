using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace chatClient.Models
{
    public class KnowledgeBase
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public ObservableCollection<KnowledgeDocument> Documents { get; set; } = new ObservableCollection<KnowledgeDocument>();
        
        // 统计属性
        public int DocumentCount => Documents?.Count ?? 0;
        
        public int TotalChunks => Documents?.Sum(d => d.ChunkCount) ?? 0;
        
        public string TotalSize
        {
            get
            {
                long totalBytes = Documents?.Sum(d => d.FileSize) ?? 0;
                
                if (totalBytes < 1024)
                    return $"{totalBytes} B";
                else if (totalBytes < 1024 * 1024)
                    return $"{totalBytes / 1024.0:F2} KB";
                else if (totalBytes < 1024 * 1024 * 1024)
                    return $"{totalBytes / (1024.0 * 1024.0):F2} MB";
                else
                    return $"{totalBytes / (1024.0 * 1024.0 * 1024.0):F2} GB";
            }
        }
    }

    public class KnowledgeDocument
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.Now;
        
        // 文档元数据
        public int ChunkCount { get; set; }
        public string FileType { get; set; }
        public string Title { get; set; }
        
        // 显示文件大小的格式化字符串
        public string FormattedSize
        {
            get
            {
                if (FileSize < 1024)
                    return $"{FileSize} B";
                else if (FileSize < 1024 * 1024)
                    return $"{FileSize / 1024.0:F2} KB";
                else
                    return $"{FileSize / (1024.0 * 1024.0):F2} MB";
            }
        }
    }
} 