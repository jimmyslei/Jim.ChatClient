using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Microsoft.SemanticKernel.Embeddings;
using DocumentFormat.OpenXml.Spreadsheet;

namespace JIm.ChatClient.Core.Services
{
    public class DocumentProcessingService
    {
        // 文档块的最大字符数
        private const int MaxChunkSize = 1000;
        // 文档块的重叠字符数
        private const int ChunkOverlap = 200;

        // 添加进度报告委托
        public delegate void ProgressReportHandler(double progress);

        public async Task<List<DocumentChunk>> ProcessDocumentAsync(
            string filePath, 
            string content = null, 
            ProgressReportHandler progressReport = null)
        {
            string fileExtension = Path.GetExtension(filePath).ToLowerInvariant();
            string documentContent;

            // 报告开始处理
            progressReport?.Invoke(0.1);

            if (content != null)
            {
                // 如果已提供内容，直接使用
                documentContent = content;
            }
            else
            {
                // 根据文件类型提取内容
                documentContent = fileExtension switch
                {
                    ".pdf" => ExtractTextFromPdf(filePath, progressReport),
                    ".docx" => ExtractTextFromDocx(filePath, progressReport),
                    ".xlsx" => ExtractTextFromExcel(filePath, progressReport),
                    ".xls" => ExtractTextFromExcel(filePath, progressReport),
                    ".txt" => await File.ReadAllTextAsync(filePath),
                    ".md" => await File.ReadAllTextAsync(filePath),
                    ".json" => await File.ReadAllTextAsync(filePath),
                    ".xml" => await File.ReadAllTextAsync(filePath),
                    ".html" => await File.ReadAllTextAsync(filePath),
                    ".css" => await File.ReadAllTextAsync(filePath),
                    ".js" => await File.ReadAllTextAsync(filePath),
                    ".cs" => await File.ReadAllTextAsync(filePath),
                    ".py" => await File.ReadAllTextAsync(filePath),
                    _ => throw new NotSupportedException($"不支持的文件类型: {fileExtension}")
                };
            }

            // 报告内容提取完成
            progressReport?.Invoke(0.5);

            // 将文档分块
            var chunks = ChunkText(documentContent);
            
            // 报告分块完成
            progressReport?.Invoke(0.8);
            
            return chunks;
        }

        private string ExtractTextFromPdf(string filePath, ProgressReportHandler progressReport = null)
        {
            StringBuilder text = new StringBuilder();
            using (PdfReader pdfReader = new PdfReader(filePath))
            {
                using (PdfDocument pdfDoc = new PdfDocument(pdfReader))
                {
                    int totalPages = pdfDoc.GetNumberOfPages();
                    for (int i = 1; i <= totalPages; i++)
                    {
                        var strategy = new SimpleTextExtractionStrategy();
                        string pageText = PdfTextExtractor.GetTextFromPage(pdfDoc.GetPage(i), strategy);
                        text.AppendLine(pageText);
                        
                        // 报告PDF处理进度
                        progressReport?.Invoke(0.1 + 0.4 * (double)i / totalPages);
                    }
                }
            }
            return text.ToString();
        }

        private string ExtractTextFromDocx(string filePath, ProgressReportHandler progressReport = null)
        {
            StringBuilder text = new StringBuilder();
            using (WordprocessingDocument wordDoc = WordprocessingDocument.Open(filePath, false))
            {
                Body body = wordDoc.MainDocumentPart.Document.Body;
                var paragraphs = body.Elements<Paragraph>().ToList();
                int totalParagraphs = paragraphs.Count;
                
                for (int i = 0; i < totalParagraphs; i++)
                {
                    text.AppendLine(paragraphs[i].InnerText);
                    
                    // 报告Word处理进度
                    progressReport?.Invoke(0.1 + 0.4 * (double)i / totalParagraphs);
                }
            }
            return text.ToString();
        }

        private string ExtractTextFromExcel(string filePath, ProgressReportHandler progressReport = null)
        {
            StringBuilder text = new StringBuilder();
            using (SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Open(filePath, false))
            {
                WorkbookPart workbookPart = spreadsheetDocument.WorkbookPart;
                SharedStringTablePart sharedStringTablePart = workbookPart.SharedStringTablePart;
                SharedStringTable sharedStringTable = sharedStringTablePart.SharedStringTable;
                
                // 获取所有工作表
                var sheets = workbookPart.Workbook.Descendants<Sheet>();
                int totalSheets = sheets.Count();
                int currentSheet = 0;
                
                foreach (Sheet sheet in sheets)
                {
                    currentSheet++;
                    WorksheetPart worksheetPart = (WorksheetPart)workbookPart.GetPartById(sheet.Id);
                    Worksheet worksheet = worksheetPart.Worksheet;
                    
                    // 添加工作表名称作为标题
                    text.AppendLine($"工作表: {sheet.Name}");
                    text.AppendLine(new string('-', 50));
                    
                    // 获取所有单元格
                    var cells = worksheet.Descendants<Cell>();
                    int totalCells = cells.Count();
                    int currentCell = 0;
                    
                    // 创建一个字典来存储单元格的值
                    Dictionary<string, string> cellValues = new Dictionary<string, string>();
                    
                    // 提取所有单元格的值
                    foreach (Cell cell in cells)
                    {
                        currentCell++;
                        string cellValue = GetCellValue(cell, sharedStringTable);
                        if (!string.IsNullOrEmpty(cellValue))
                        {
                            cellValues[cell.CellReference.Value] = cellValue;
                        }
                        
                        // 报告Excel处理进度
                        double sheetProgress = (double)currentSheet / totalSheets;
                        double cellProgress = (double)currentCell / totalCells;
                        double combinedProgress = (sheetProgress + cellProgress / totalSheets) * 0.4; // 0.1-0.5范围内
                        progressReport?.Invoke(0.1 + combinedProgress);
                    }
                    
                    // 获取工作表的数据区域
                    SheetData sheetData = worksheet.GetFirstChild<SheetData>();
                    
                    // 按行组织单元格内容
                    foreach (Row row in sheetData.Elements<Row>())
                    {
                        StringBuilder rowText = new StringBuilder();
                        
                        foreach (Cell cell in row.Elements<Cell>())
                        {
                            if (cellValues.TryGetValue(cell.CellReference.Value, out string value))
                            {
                                // 添加单元格值，用制表符分隔
                                rowText.Append(value + "\t");
                            }
                            else
                            {
                                rowText.Append("\t");
                            }
                        }
                        
                        // 添加行文本
                        if (rowText.Length > 0)
                        {
                            text.AppendLine(rowText.ToString());
                        }
                    }
                    
                    text.AppendLine();
                }
            }
            
            return text.ToString();
        }

        private string GetCellValue(Cell cell, SharedStringTable sharedStringTable)
        {
            if (cell.CellValue == null)
            {
                return string.Empty;
            }
            
            string value = cell.CellValue.Text;
            
            // 如果单元格类型是SharedString，则从共享字符串表中获取实际值
            if (cell.DataType != null && cell.DataType.Value == CellValues.SharedString)
            {
                return sharedStringTable.ChildElements[int.Parse(value)].InnerText;
            }
            
            return value;
        }

        private List<DocumentChunk> ChunkText(string text)
        {
            List<DocumentChunk> chunks = new List<DocumentChunk>();
            
            // 按段落分割文本
            string[] paragraphs = text.Split(new[] { "\r\n\r\n", "\n\n" }, StringSplitOptions.RemoveEmptyEntries);
            
            StringBuilder currentChunk = new StringBuilder();
            int chunkIndex = 0;
            
            foreach (var paragraph in paragraphs)
            {
                // 如果段落本身超过最大块大小，需要进一步分割
                if (paragraph.Length > MaxChunkSize)
                {
                    // 处理当前累积的块
                    if (currentChunk.Length > 0)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = currentChunk.ToString(),
                            ChunkIndex = chunkIndex++
                        });
                        currentChunk.Clear();
                    }
                    
                    // 分割大段落
                    for (int i = 0; i < paragraph.Length; i += MaxChunkSize - ChunkOverlap)
                    {
                        int length = Math.Min(MaxChunkSize, paragraph.Length - i);
                        string chunkText = paragraph.Substring(i, length);
                        
                        chunks.Add(new DocumentChunk
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = chunkText,
                            ChunkIndex = chunkIndex++
                        });
                    }
                }
                else
                {
                    // 如果添加当前段落会超过最大块大小，先保存当前块
                    if (currentChunk.Length + paragraph.Length + 2 > MaxChunkSize)
                    {
                        chunks.Add(new DocumentChunk
                        {
                            Id = Guid.NewGuid().ToString(),
                            Content = currentChunk.ToString(),
                            ChunkIndex = chunkIndex++
                        });
                        currentChunk.Clear();
                    }
                    
                    // 添加段落到当前块
                    if (currentChunk.Length > 0)
                    {
                        currentChunk.AppendLine();
                        currentChunk.AppendLine();
                    }
                    currentChunk.Append(paragraph);
                }
            }
            
            // 添加最后一个块
            if (currentChunk.Length > 0)
            {
                chunks.Add(new DocumentChunk
                {
                    Id = Guid.NewGuid().ToString(),
                    Content = currentChunk.ToString(),
                    ChunkIndex = chunkIndex
                });
            }
            
            return chunks;
        }
    }

    public class DocumentChunk
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public int ChunkIndex { get; set; }
        public float[] Embedding { get; set; }
    }
} 