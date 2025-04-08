using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JIm.ChatClient.Core.Models;
using Avalonia.Platform.Storage;
using JIm.ChatClient.Core.Helpers;
using static JIm.ChatClient.Core.Services.DocumentProcessingService;
using DocumentFormat.OpenXml.Packaging;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Newtonsoft.Json;

namespace JIm.ChatClient.Core.Services
{
    public class KnowledgeBaseService
    {
        private readonly string _knowledgeBasePath;
        private readonly string _documentsPath;
        private readonly string _vectorDbPath;
        private List<KnowledgeBase> _knowledgeBases;
        private readonly DocumentProcessingService _documentProcessor;
        private VectorDatabaseService _vectorDb;
        private readonly AIModel _embeddingModel;
        
        public KnowledgeBaseService(AIModel embeddingModel)
        {
            _embeddingModel = embeddingModel;
            
            // 在应用程序目录下创建知识库存储目录
            _knowledgeBasePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "KnowledgeBases");
            _documentsPath = Path.Combine(_knowledgeBasePath, "Documents");
            _vectorDbPath = Path.Combine(_knowledgeBasePath, "VectorDb", "knowledge_vectors.db");
            
            // 确保目录存在
            Directory.CreateDirectory(_knowledgeBasePath);
            Directory.CreateDirectory(_documentsPath);
            Directory.CreateDirectory(Path.Combine(_knowledgeBasePath, "VectorDb"));
            
            // 初始化文档处理服务
            _documentProcessor = new DocumentProcessingService();
            
            // 初始化向量数据库
            _vectorDb = new VectorDatabaseService(_vectorDbPath, embeddingModel);
            
            // 加载知识库列表
            LoadKnowledgeBases();
        }
        
        private void LoadKnowledgeBases()
        {
            string configFile = Path.Combine(_knowledgeBasePath, "knowledge_bases.json");
            if (File.Exists(configFile))
            {
                string json = File.ReadAllText(configFile);
                _knowledgeBases = JsonConvert.DeserializeObject<List<KnowledgeBase>>(json)?? new List<KnowledgeBase>(); //JsonSerializer.Deserialize<List<KnowledgeBase>>(json) ?? new List<KnowledgeBase>();
            }
            else
            {
                _knowledgeBases = new List<KnowledgeBase>();
                SaveKnowledgeBases();
            }
        }
        
        private void SaveKnowledgeBases()
        {
            string configFile = Path.Combine(_knowledgeBasePath, "knowledge_bases.json");
            string json = JsonConvert.SerializeObject(_knowledgeBases); //JsonSerializer.Serialize(_knowledgeBases, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(configFile, json);
        }
        
        public List<KnowledgeBase> GetAllKnowledgeBases()
        {
            return _knowledgeBases;
        }
        
        public KnowledgeBase GetKnowledgeBase(string id)
        {
            return _knowledgeBases.FirstOrDefault(kb => kb.Id == id);
        }
        
        public KnowledgeBase CreateKnowledgeBase(string name, string description)
        {
            var knowledgeBase = new KnowledgeBase
            {
                Name = name,
                Description = description
            };
            
            _knowledgeBases.Add(knowledgeBase);
            SaveKnowledgeBases();
            
            return knowledgeBase;
        }
        
        public bool DeleteKnowledgeBase(string id)
        {
            var knowledgeBase = _knowledgeBases.FirstOrDefault(kb => kb.Id == id);
            if (knowledgeBase == null)
                return false;
            
            // 删除知识库中的所有文档文件
            foreach (var document in knowledgeBase.Documents)
            {
                string filePath = Path.Combine(_documentsPath, document.FilePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }
            
            // 删除向量数据库中的知识库数据
            _vectorDb.DeleteKnowledgeBaseChunks(id);
            
            _knowledgeBases.Remove(knowledgeBase);
            SaveKnowledgeBases();
            
            return true;
        }
        
        public async Task<KnowledgeDocument> AddDocumentToKnowledgeBase(
            string knowledgeBaseId, 
            IStorageFile file,
            ProgressReportHandler progressReport = null)
        {
            var knowledgeBase = _knowledgeBases.FirstOrDefault(kb => kb.Id == knowledgeBaseId);
            if (knowledgeBase == null)
                throw new ArgumentException("Knowledge base not found");
            
            // 报告开始上传
            progressReport?.Invoke(0.05);
            
            // 读取文件内容
            string content;
            using (var stream = await file.OpenReadAsync())
            using (var reader = new StreamReader(stream))
            {
                content = await reader.ReadToEndAsync();
            }
            
            // 报告文件读取完成
            progressReport?.Invoke(0.2);
            
            // 创建文档对象
            var document = new KnowledgeDocument
            {
                FileName = file.Name,
                ContentType = FileTypeHelper.GetContentType(file.Name),
                FileSize = content.Length,
                UploadedAt = DateTime.Now,
                FileType = FileTypeHelper.GetFileTypeDescription(file.Name),
                Title = Path.GetFileNameWithoutExtension(file.Name)
            };
            
            // 保存文档内容到文件
            string documentFileName = $"{document.Id}_{file.Name}";
            string documentPath = Path.Combine(_documentsPath, documentFileName);
            document.FilePath = documentFileName;
            
            await File.WriteAllTextAsync(documentPath, content);
            
            // 报告文件保存完成
            progressReport?.Invoke(0.3);
            
            // 处理文档内容，分块并生成嵌入向量
            var chunks = await _documentProcessor.ProcessDocumentAsync(
                file.Name, 
                content, 
                progress => progressReport?.Invoke(0.3 + progress * 0.4) // 30%-70%的进度用于文档处理
            );
            
            document.ChunkCount = chunks.Count;
            
            // 报告开始生成向量
            progressReport?.Invoke(0.7);
            
            // 存储文档块到向量数据库
            await _vectorDb.StoreDocumentChunksAsync(
                knowledgeBaseId, 
                document.Id, 
                chunks, 
                progress => progressReport?.Invoke(0.7 + progress * 0.25) // 70%-95%的进度用于向量存储
            );
            
            // 添加文档到知识库
            knowledgeBase.Documents.Add(document);
            SaveKnowledgeBases();
            
            // 报告完成
            progressReport?.Invoke(1.0);
            
            return document;
        }
        
        private string GetContentTypeFromFileName(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            return extension switch
            {
                ".pdf" => "application/pdf",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".txt" => "text/plain",
                ".md" => "text/markdown",
                ".json" => "application/json",
                ".xml" => "application/xml",
                ".html" => "text/html",
                ".css" => "text/css",
                ".js" => "application/javascript",
                ".cs" => "text/plain",
                ".py" => "text/plain",
                _ => "application/octet-stream"
            };
        }
        
        public bool RemoveDocumentFromKnowledgeBase(string knowledgeBaseId, string documentId)
        {
            var knowledgeBase = _knowledgeBases.FirstOrDefault(kb => kb.Id == knowledgeBaseId);
            if (knowledgeBase == null)
                return false;
            
            var document = knowledgeBase.Documents.FirstOrDefault(d => d.Id == documentId);
            if (document == null)
                return false;
            
            // 删除文档文件
            string documentPath = Path.Combine(_documentsPath, document.FilePath);
            if (File.Exists(documentPath))
            {
                File.Delete(documentPath);
            }
            
            // 从向量数据库中删除文档块
            _vectorDb.DeleteDocumentChunks(documentId);
            
            // 从知识库中移除文档
            knowledgeBase.Documents.Remove(document);
            SaveKnowledgeBases();
            
            return true;
        }
        
        public async Task<string> GetRelevantContextForQuery(string knowledgeBaseId, string query, int maxChunks = 5)
        {
            // 搜索与查询相关的文档块
            var relevantChunks = await _vectorDb.SearchSimilarChunksAsync(knowledgeBaseId, query, maxChunks);
            
            if (relevantChunks.Count == 0)
                return string.Empty;
            
            // 构建上下文
            StringBuilder context = new StringBuilder();
            context.AppendLine("以下是与您的问题相关的信息：\n");
            
            foreach (var chunk in relevantChunks)
            {
                // 获取文档信息
                var knowledgeBase = _knowledgeBases.FirstOrDefault(kb => kb.Id == knowledgeBaseId);
                var document = knowledgeBase?.Documents.FirstOrDefault(d => d.Id == chunk.DocumentId);
                
                if (document != null)
                {
                    context.AppendLine($"--- 来自文档: {document.FileName} ---");
                    context.AppendLine(chunk.Content);
                    context.AppendLine();
                }
            }
            
            return context.ToString();
        }
        
        public async Task<KnowledgeDocument> AddDocumentToKnowledgeBaseWithProperExtraction(
            string knowledgeBaseId, 
            IStorageFile file, 
            Action<double> progressReport = null)
        {
            var knowledgeBase = _knowledgeBases.FirstOrDefault(kb => kb.Id == knowledgeBaseId);
            if (knowledgeBase == null)
                throw new ArgumentException("Knowledge base not found");
            
            // 报告开始上传
            progressReport?.Invoke(0.05);
            
            // 创建文档对象
            var document = new KnowledgeDocument
            {
                Id = Guid.NewGuid().ToString(),
                FileName = file.Name,
                ContentType = FileTypeHelper.GetContentType(file.Name),
                UploadedAt = DateTime.Now,
                FileType = FileTypeHelper.GetFileTypeDescription(file.Name),
                Title = Path.GetFileNameWithoutExtension(file.Name)
            };
            
            // 保存原始文件
            string originalFileName = $"{document.Id}_original_{file.Name}";
            string originalFilePath = Path.Combine(_documentsPath, originalFileName);
            
            using (var sourceStream = await file.OpenReadAsync())
            using (var destinationStream = File.Create(originalFilePath))
            {
                await sourceStream.CopyToAsync(destinationStream);
            }
            
            // 报告文件保存完成
            progressReport?.Invoke(0.15);
            
            // 提取文档内容
            string content = await ExtractDocumentContentWithDotNet(originalFilePath, file.Name);
            document.FileSize = content.Length;
            
            // 保存提取的文本内容到文件，确保使用UTF-8编码
            string documentFileName = $"{document.Id}_{file.Name}.txt";
            string documentPath = Path.Combine(_documentsPath, documentFileName);
            document.FilePath = documentFileName;
            
            await File.WriteAllTextAsync(documentPath, content, Encoding.UTF8);
            
            // 报告文件内容提取完成
            progressReport?.Invoke(0.3);
            
            // 处理文档内容，分块并生成嵌入向量
            var chunks = await _documentProcessor.ProcessDocumentAsync(
                file.Name, 
                content, 
                progress => progressReport?.Invoke(0.3 + progress * 0.4) // 30%-70%的进度用于文档处理
            );
            
            document.ChunkCount = chunks.Count;
            
            // 报告开始生成向量
            progressReport?.Invoke(0.7);
            
            // 存储文档块到向量数据库
            await _vectorDb.AddDocumentChunksAsync(
                knowledgeBaseId,
                document.Id,
                chunks,
                progress => progressReport?.Invoke(0.7 + progress * 0.25) // 70%-95%的进度用于向量存储
            );
            
            // 添加文档到知识库
            knowledgeBase.Documents.Add(document);
            
            // 保存知识库信息
            SaveKnowledgeBases();
            
            // 报告完成
            progressReport?.Invoke(1.0);
            
            return document;
        }
        
        private async Task<string> ExtractDocumentContentWithDotNet(string filePath, string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            try
            {
                switch (extension)
                {
                    case ".docx":
                        return ExtractTextFromDocx(filePath);
                        
                    case ".doc":
                        return ExtractTextFromDoc(filePath);
                        
                    case ".xlsx":
                    case ".xls":
                        return ExtractTextFromExcel(filePath);
                        
                    case ".pptx":
                        return ExtractTextFromPptx(filePath);
                        
                    case ".pdf":
                        return ExtractTextFromPdf(filePath);
                        
                    case ".txt":
                    case ".md":
                    case ".json":
                    case ".xml":
                    case ".html":
                    case ".css":
                    case ".js":
                    case ".cs":
                    case ".py":
                        // 文本文件直接读取
                        return await File.ReadAllTextAsync(filePath, DetectFileEncoding(filePath));
                        
                    default:
                        // 尝试作为文本文件读取
                        try
                        {
                            return await File.ReadAllTextAsync(filePath, DetectFileEncoding(filePath));
                        }
                        catch
                        {
                            return $"[无法提取内容: {fileName}]";
                        }
                }
            }
            catch (Exception ex)
            {
                return $"[文档内容提取失败: {ex.Message}]";
            }
        }
        
        // 使用 DocumentFormat.OpenXml 提取 DOCX 文档内容
        private string ExtractTextFromDocx(string filePath)
        {
            // 需要添加 NuGet 包: DocumentFormat.OpenXml
            using var doc = WordprocessingDocument.Open(filePath, false);
            var body = doc.MainDocumentPart?.Document.Body;
            
            if (body == null)
                return string.Empty;
            
            StringBuilder text = new StringBuilder();
            foreach (var para in body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Paragraph>())
            {
                text.AppendLine(para.InnerText);
            }
            
            return text.ToString();
        }
        
        // 替代方案：处理DOC文档内容
        private string ExtractTextFromDoc(string filePath)
        {
            try
            {
                // 尝试使用NPOI的其他类
                using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                
                // 尝试使用POIFSFileSystem
                var poifs = new NPOI.POIFS.FileSystem.POIFSFileSystem(fs);
                var director = poifs.Root;
                
                // 尝试提取文本内容
                StringBuilder text = new StringBuilder();
                
                // 遍历文档结构
                foreach (var entry in director)
                {
                    if (entry is NPOI.POIFS.FileSystem.DocumentEntry docEntry)
                    {
                        try
                        {
                            using var stream = director.CreateDocumentInputStream(entry.Name);
                            // 修复 Available 方法调用
                            int available = stream.Available();
                            byte[] buffer = new byte[available];
                            stream.Read(buffer, 0, buffer.Length);
                            
                            // 尝试将内容转换为文本
                            string content = Encoding.UTF8.GetString(buffer);
                            if (!string.IsNullOrWhiteSpace(content) && !IsGibberish(content))
                            {
                                text.AppendLine(content);
                            }
                        }
                        catch (Exception ex) 
                        {
                            // 记录异常但继续处理
                            Console.WriteLine($"处理文档条目时出错: {ex.Message}");
                        }
                    }
                }
                
                if (text.Length > 0)
                {
                    return text.ToString();
                }
                
                // 如果上述方法失败，尝试直接读取文件
                return $"[Word文档(.doc)内容提取受限: {Path.GetFileName(filePath)}]\n请考虑将文档转换为.docx格式以获得更好的支持";
            }
            catch (Exception ex)
            {
                // 如果NPOI提取失败，尝试使用其他方法
                try
                {
                    // 尝试直接读取文件内容
                    using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                    using var reader = new StreamReader(stream);
                    return reader.ReadToEnd();
                }
                catch
                {
                    return $"[无法提取Word文档内容: {Path.GetFileName(filePath)}]\n建议将文档转换为.docx格式";
                }
            }
        }
        
        // 辅助方法：检查文本是否为乱码
        private bool IsGibberish(string text)
        {
            // 简单检查：如果包含太多不可打印字符，可能是乱码
            int unprintableCount = 0;
            foreach (char c in text)
            {
                if (c < 32 || c > 126)
                {
                    unprintableCount++;
                }
            }
            
            // 如果不可打印字符超过20%，认为是乱码
            return (double)unprintableCount / text.Length > 0.2;
        }
        
        // 使用 NPOI 提取 Excel 内容
        private string ExtractTextFromExcel(string filePath)
        {
            // 需要添加 NuGet 包: NPOI
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            
            // 根据文件扩展名选择不同的工作簿
            var workbook = Path.GetExtension(filePath).ToLower() == ".xlsx" 
                ? new NPOI.XSSF.UserModel.XSSFWorkbook(fs) 
                : (NPOI.SS.UserModel.IWorkbook)new NPOI.HSSF.UserModel.HSSFWorkbook(fs);
            
            StringBuilder text = new StringBuilder();
            
            for (int i = 0; i < workbook.NumberOfSheets; i++)
            {
                var sheet = workbook.GetSheetAt(i);
                text.AppendLine($"--- 工作表: {sheet.SheetName} ---");
                
                for (int rowIdx = 0; rowIdx <= sheet.LastRowNum; rowIdx++)
                {
                    var row = sheet.GetRow(rowIdx);
                    if (row != null)
                    {
                        List<string> cellValues = new List<string>();
                        for (int cellIdx = 0; cellIdx < row.LastCellNum; cellIdx++)
                        {
                            var cell = row.GetCell(cellIdx);
                            if (cell != null)
                            {
                                cellValues.Add(cell.ToString());
                            }
                        }
                        
                        if (cellValues.Count > 0)
                        {
                            text.AppendLine(string.Join("\t", cellValues));
                        }
                    }
                }
                
                text.AppendLine();
            }
            
            return text.ToString();
        }
        
        // 使用 DocumentFormat.OpenXml 提取 PowerPoint 内容
        private string ExtractTextFromPptx(string filePath)
        {
            // 需要添加 NuGet 包: DocumentFormat.OpenXml
            using var presentation = PresentationDocument.Open(filePath, false);
            StringBuilder text = new StringBuilder();
            
            var slideIdList = presentation.PresentationPart.Presentation.SlideIdList;
            if (slideIdList != null)
            {
                int slideNumber = 1;
                foreach (var slideId in slideIdList.ChildElements)
                {
                    var slidePartId = ((DocumentFormat.OpenXml.Presentation.SlideId)slideId).RelationshipId;
                    var slidePart = presentation.PresentationPart.GetPartById(slidePartId) as DocumentFormat.OpenXml.Packaging.SlidePart;
                    
                    if (slidePart != null)
                    {
                        text.AppendLine($"--- 幻灯片 {slideNumber} ---");
                        
                        // 提取文本
                        var textElements = slidePart.Slide.Descendants<DocumentFormat.OpenXml.Drawing.Text>();
                        foreach (var textElement in textElements)
                        {
                            text.AppendLine(textElement.Text);
                        }
                        
                        text.AppendLine();
                        slideNumber++;
                    }
                }
            }
            
            return text.ToString();
        }

        // 使用 itext7 提取 PDF 内容
        private string ExtractTextFromPdf(string filePath)
        {
            // 使用 itext7
            StringBuilder text = new StringBuilder();

            using (var pdfReader = new PdfReader(filePath))
            using (var pdfDocument = new PdfDocument(pdfReader))
            {
                int numberOfPages = pdfDocument.GetNumberOfPages();
                for (int i = 1; i <= numberOfPages; i++)
                {
                    text.AppendLine($"--- 页面 {i} ---");

                    // 获取页面
                    var page = pdfDocument.GetPage(i);

                    // 提取文本
                    var strategy = new LocationTextExtractionStrategy();
                    string currentText = PdfTextExtractor.GetTextFromPage(page, strategy);

                    text.AppendLine(currentText);
                    text.AppendLine();
                }
            }

            return text.ToString();
        }

        // 检测文件编码
        private Encoding DetectFileEncoding(string filePath)
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            return DetectEncoding(fs);
        }
        
        // 添加检测文件编码的辅助方法
        private Encoding DetectEncoding(Stream stream)
        {
            // 尝试检测文件编码
            byte[] buffer = new byte[Math.Min(stream.Length, 4096)];
            stream.Read(buffer, 0, buffer.Length);
            
            // 检查BOM标记
            if (buffer.Length >= 3 && buffer[0] == 0xEF && buffer[1] == 0xBB && buffer[2] == 0xBF)
                return Encoding.UTF8;
            if (buffer.Length >= 2 && buffer[0] == 0xFE && buffer[1] == 0xFF)
                return Encoding.BigEndianUnicode;
            if (buffer.Length >= 2 && buffer[0] == 0xFF && buffer[1] == 0xFE)
                return Encoding.Unicode;
            if (buffer.Length >= 4 && buffer[0] == 0 && buffer[1] == 0 && buffer[2] == 0xFE && buffer[3] == 0xFF)
                return Encoding.UTF32;
            
            // 如果没有BOM标记，默认使用UTF-8
            return Encoding.UTF8;
        }
    }
} 