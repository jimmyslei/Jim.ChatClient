using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JIm.ChatClient.Core.Models
{
    public class ToolItem
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string IconData { get; set; }
        public string Type { get; set; } // 工具类型，如PdfToWord、WordToPdf等
    }
}