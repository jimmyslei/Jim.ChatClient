using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Data;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using Color = DocumentFormat.OpenXml.Wordprocessing.Color;
using PageSize = DocumentFormat.OpenXml.Wordprocessing.PageSize;
using Rectangle = iText.Kernel.Geom.Rectangle;
using Run = DocumentFormat.OpenXml.Wordprocessing.Run;
using Text = DocumentFormat.OpenXml.Wordprocessing.Text;

namespace JIm.ChatClient.Core.Services
{
    public class PdfToWordConverter
    {
        /// <summary>
        /// 将PDF文档转换为Word文档，保持原始布局
        /// </summary>
        /// <param name="pdfPath">PDF文件路径</param>
        /// <param name="wordPath">输出Word文件路径</param>
        /// <param name="progressCallback">进度回调</param>
        /// <returns>转换是否成功</returns>
        public async Task<bool> ConvertPdfToWordAsync(string pdfPath, string wordPath, Action<int, string> progressCallback)
        {
            try
            {
                // 报告进度
                progressCallback(0, "正在准备转换...");
                
                // 1. 从PDF提取文本、样式和布局信息
                var pdfContents = await ExtractDetailedContentFromPdfAsync(pdfPath, progressCallback);
                
                // 2. 创建Word文档，保留原始布局和样式
                await CreateEnhancedWordDocumentAsync(pdfContents, wordPath, progressCallback);
                
                // 3. 报告完成
                progressCallback(100, "转换完成！文档布局和样式已保留");
                
                return true;
            }
            catch (Exception ex)
            {
                progressCallback(0, $"转换失败: {ex.Message}");
                return false;
            }
        }
        
        /// <summary>
        /// 从PDF文件中提取详细内容、样式和布局信息
        /// </summary>
        private async Task<List<PdfPageDetailedContent>> ExtractDetailedContentFromPdfAsync(string pdfPath, Action<int, string> progressCallback)
        {
            return await Task.Run(() => 
            {
                var pageContents = new List<PdfPageDetailedContent>();
                
                progressCallback(10, "正在读取PDF文件...");
                
                using (var pdfReader = new PdfReader(pdfPath))
                using (var pdfDoc = new PdfDocument(pdfReader))
                {
                    int totalPages = pdfDoc.GetNumberOfPages();
                    progressCallback(15, $"开始详细分析PDF，共{totalPages}页...");
                    
                    // 处理每一页
                    for (int i = 1; i <= totalPages; i++)
                    {
                        // 更新进度
                        int progress = 15 + (i * 30) / totalPages;
                        progressCallback(progress, $"正在详细分析第{i}页，提取文本和样式...");
                        
                        // 获取页面
                        var page = pdfDoc.GetPage(i);
                        var pageSize = page.GetPageSize();
                        
                        // 为每一页创建一个新的提取策略实例
                        var detailedTextExtractionStrategy = new DetailedTextExtractionStrategy();
                        
                        // 提取页面内容和样式
                        PdfTextExtractor.GetTextFromPage(page, detailedTextExtractionStrategy);
                        
                        // 获取提取的文本块
                        var textBlocks = detailedTextExtractionStrategy.GetTextBlocks();
                        
                        // 创建详细页面内容
                        var pageDetailedContent = new PdfPageDetailedContent
                        {
                            PageNumber = i,
                            Width = pageSize.GetWidth(),
                            Height = pageSize.GetHeight(),
                            TextBlocks = new List<TextBlock>(textBlocks) // 创建文本块列表的副本
                        };
                        
                        pageContents.Add(pageDetailedContent);
                    }
                }
                
                progressCallback(45, "PDF内容和样式分析完成，准备生成Word文档...");
                return pageContents;
            });
        }
        
        /// <summary>
        /// 创建增强的Word文档，保留原始布局和样式
        /// </summary>
        private async Task CreateEnhancedWordDocumentAsync(List<PdfPageDetailedContent> pdfContents, string wordPath, Action<int, string> progressCallback)
        {
            await Task.Run(() => 
            {
                progressCallback(50, "正在创建Word文档，设置页面和文档属性...");
                
                // 创建Word文档
                using (var wordDoc = WordprocessingDocument.Create(wordPath, WordprocessingDocumentType.Document))
                {
                    // 添加主文档部分
                    var mainPart = wordDoc.AddMainDocumentPart();
                    mainPart.Document = new Document();
                    
                    // 创建文档内容区域
                    var body = new Body();
                    mainPart.Document.Append(body);
                    
                    // 添加字体和样式定义
                    progressCallback(55, "正在设置字体和样式...");
                    AddEnhancedDocumentStyles(mainPart);
                    
                    // 设置文档的段落间距、行距等属性
                    var docDefaults = new DocDefaults();
                    var paragraphPropertiesDefault = new ParagraphPropertiesDefault();
                    var runPropertiesDefault = new RunPropertiesDefault();
                    var runPropertiesBaseStyle = new RunPropertiesBaseStyle();
                    runPropertiesBaseStyle.Append(new FontSize { Val = "24" }); // 默认12pt字体
                    runPropertiesDefault.Append(runPropertiesBaseStyle);
                    docDefaults.Append(runPropertiesDefault);
                    docDefaults.Append(paragraphPropertiesDefault);
                    
                    // 处理每一页内容
                    int totalPages = pdfContents.Count;
                    for (int i = 0; i < totalPages; i++)
                    {
                        var pageContent = pdfContents[i];
                        
                        // 更新进度
                        int progress = 55 + (i * 40) / totalPages;
                        progressCallback(progress, $"正在转换第{pageContent.PageNumber}页，应用样式和布局...");
                        
                        // 在每页前添加页面分隔标记，除了第一页
                        if (i > 0)
                        {
                            var pageBreak = new Paragraph(new Run(new Break { Type = BreakValues.Page }));
                            body.Append(pageBreak);
                        }
                        
                        // 创建一个新的节（section）用于每一页
                        var section = new SectionProperties();
                        
                        // 设置页面大小，匹配PDF页面尺寸
                        // 将PDF单位（点）转换为Word单位（缇）
                        var widthInTwips = (UInt32Value)(pageContent.Width * 1440 / 72);
                        var heightInTwips = (UInt32Value)(pageContent.Height * 1440 / 72);
                        
                        section.Append(new PageSize { Width = widthInTwips, Height = heightInTwips });
                        section.Append(new PageMargin { Top = 720, Right = 720, Bottom = 720, Left = 720 }); // 标准1英寸边距
                        
                        // 处理页面内容，保留布局和样式
                        AddDetailedPageToDocument(body, pageContent);
                        
                        // 如果是最后一页，添加节属性
                        if (i == totalPages - 1)
                        {
                            body.Append(section);
                        }
                    }
                    
                    // 保存文档
                    progressCallback(95, "正在完成Word文档...");
                    mainPart.Document.Save();
                }
                
                progressCallback(100, "Word文档生成完成，已保留原始布局和样式！");
            });
        }
        
        /// <summary>
        /// 添加增强的文档样式
        /// </summary>
        private void AddEnhancedDocumentStyles(MainDocumentPart mainPart)
        {
            // 添加样式部分
            var stylesPart = mainPart.AddNewPart<StyleDefinitionsPart>();
            var styles = new Styles();
            stylesPart.Styles = styles;
            
            // 添加默认段落样式
            var defaultParagraphStyle = new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = "Normal",
                Default = true
            };
            var styleName = new StyleName { Val = "Normal" };
            defaultParagraphStyle.Append(styleName);
            
            // 段落属性
            var paragraphProperties = new ParagraphProperties();
            var paragraphStyleRunProperties = new StyleRunProperties();
            paragraphStyleRunProperties.Append(new RunFonts 
            { 
                Ascii = "SimSun", // 宋体
                HighAnsi = "SimSun",
                ComplexScript = "SimSun"
            });
            // 5号字体对应10.5磅，Word单位为半磅，所以是21
            paragraphStyleRunProperties.Append(new FontSize { Val = "21" });
            paragraphProperties.Append(paragraphStyleRunProperties);
            defaultParagraphStyle.Append(paragraphProperties);
            
            // 添加各种级别的标题样式 - 按照中文字号大小设置
            // 一级标题 - 3号字体(16pt = 32半磅)
            AddHeadingStyle(styles, "Heading1", "标题 1", 32, true); 
            
            // 二级标题 - 4号字体(14pt = 28半磅)
            AddHeadingStyle(styles, "Heading2", "标题 2", 28, true); 
            
            // 三级标题 - 小4号字体(12pt = 24半磅)
            AddHeadingStyle(styles, "Heading3", "标题 3", 24, true); 
            
            // 四级标题 - 5号字体(10.5pt = 21半磅)
            AddHeadingStyle(styles, "Heading4", "标题 4", 21, true);
            
            // 添加特殊样式，如粗体、斜体等
            AddCharacterStyle(styles, "Bold", "Bold", true, false);
            AddCharacterStyle(styles, "Italic", "Italic", false, true);
            AddCharacterStyle(styles, "BoldItalic", "Bold Italic", true, true);
            
            // 保存样式
            styles.Append(defaultParagraphStyle);
            stylesPart.Styles.Save();
        }
        
        /// <summary>
        /// 添加标题样式
        /// </summary>
        private void AddHeadingStyle(Styles styles, string styleId, string styleName, int fontSize, bool isBold)
        {
            var headingStyle = new Style
            {
                Type = StyleValues.Paragraph,
                StyleId = styleId,
                Default = false
            };
            var headingStyleName = new StyleName { Val = styleName };
            headingStyle.Append(headingStyleName);
            
            var headingParagraphProperties = new ParagraphProperties();
            var headingStyleRunProperties = new StyleRunProperties();
            headingStyleRunProperties.Append(new RunFonts 
            { 
                Ascii = "Arial", 
                HighAnsi = "Arial",
                ComplexScript = "Arial"
            });
            
            if (isBold)
            {
                headingStyleRunProperties.Append(new Bold());
            }
            
            headingStyleRunProperties.Append(new FontSize { Val = fontSize.ToString() });
            headingParagraphProperties.Append(headingStyleRunProperties);
            headingStyle.Append(headingParagraphProperties);
            
            styles.Append(headingStyle);
        }
        
        /// <summary>
        /// 添加字符样式（粗体、斜体等）
        /// </summary>
        private void AddCharacterStyle(Styles styles, string styleId, string styleName, bool isBold, bool isItalic)
        {
            var characterStyle = new Style
            {
                Type = StyleValues.Character,
                StyleId = styleId,
                Default = false
            };
            var characterStyleName = new StyleName { Val = styleName };
            characterStyle.Append(characterStyleName);
            
            var characterStyleRunProperties = new StyleRunProperties();
            
            if (isBold)
            {
                characterStyleRunProperties.Append(new Bold());
            }
            
            if (isItalic)
            {
                characterStyleRunProperties.Append(new Italic());
            }
            
            characterStyle.Append(characterStyleRunProperties);
            styles.Append(characterStyle);
        }
        
        /// <summary>
        /// 添加详细页面内容到Word文档
        /// </summary>
        private void AddDetailedPageToDocument(Body body, PdfPageDetailedContent pageContent)
        {
            // 按照Y坐标（垂直位置）对文本块排序，模拟阅读顺序
            var sortedTextBlocks = pageContent.TextBlocks
                .OrderBy(b => -b.Rectangle.GetY())
                .ThenBy(b => b.Rectangle.GetX())
                .ToList();
            
            // 分析页面内容，预处理标题级别
            AnalyzeAndAssignHeadingLevels(sortedTextBlocks);
            
            // 上一个文本块的底部位置
            float? lastBottomPosition = null;
            Paragraph currentParagraph = null;
            
            // 处理每个文本块
            foreach (var textBlock in sortedTextBlocks)
            {
                // 检查是否需要创建新段落
                bool newParagraph = true;
                
                if (lastBottomPosition.HasValue)
                {
                    // 计算与上一个文本块的垂直距离
                    float verticalGap = Math.Abs(lastBottomPosition.Value - textBlock.Rectangle.GetY());
                    
                    // 如果垂直距离小于阈值，则认为是同一段落
                    if (verticalGap < 1.2 * textBlock.FontSize)
                    {
                        newParagraph = false;
                    }
                }
                
                // 创建新段落或继续使用现有段落
                if (newParagraph || currentParagraph == null)
                {
                    currentParagraph = new Paragraph();
                    
                    // 设置段落属性
                    var paragraphProperties = new ParagraphProperties();
                    
                    // 判断是否为标题及标题级别
                    if (textBlock.HeadingLevel > 0)
                    {
                        // 根据标题级别应用样式
                        paragraphProperties.Append(new ParagraphStyleId { Val = $"Heading{textBlock.HeadingLevel}" });
                        
                        // 标题段落间距调整
                        paragraphProperties.Append(new SpacingBetweenLines { Before = "240", After = "120" });
                    }
                    else
                    {
                        // 非标题文本使用普通样式
                        paragraphProperties.Append(new ParagraphStyleId { Val = "Normal" });
                        
                        // 正文段落间距
                        paragraphProperties.Append(new SpacingBetweenLines { After = "80" });
                    }
                    
                    // 根据位置设置对齐方式
                    if (textBlock.IsRightAligned)
                    {
                        paragraphProperties.Append(new Justification { Val = JustificationValues.Right });
                    }
                    else if (textBlock.IsCentered)
                    {
                        paragraphProperties.Append(new Justification { Val = JustificationValues.Center });
                    }
                    
                    currentParagraph.Append(paragraphProperties);
                    body.Append(currentParagraph);
                }
                
                // 创建Run元素
                var run = new Run();
                
                // 设置Run属性
                var runProperties = new RunProperties();
                
                // 设置字体
                runProperties.Append(new RunFonts
                {
                    Ascii = textBlock.FontName ?? "SimSun",
                    HighAnsi = textBlock.FontName ?? "SimSun",
                    ComplexScript = textBlock.FontName ?? "SimSun"
                });
                
                // 根据标题级别设置字体大小
                int fontSize;
                
                if (textBlock.HeadingLevel == 1)
                {
                    // 一级标题 - 3号字体(16pt = 32半磅)
                    fontSize = 32;
                }
                else if (textBlock.HeadingLevel == 2)
                {
                    // 二级标题 - 4号字体(14pt = 28半磅)
                    fontSize = 28;
                }
                else if (textBlock.HeadingLevel == 3)
                {
                    // 三级标题 - 小4号字体(12pt = 24半磅)
                    fontSize = 24;
                }
                else if (textBlock.HeadingLevel == 4)
                {
                    // 四级标题 - 5号字体(10.5pt = 21半磅)
                    fontSize = 21;
                }
                else
                {
                    // 正文内容统一使用5号字体(10.5pt = 21半磅)
                    fontSize = 21;
                }
                
                runProperties.Append(new FontSize { Val = fontSize.ToString() });
                
                // 设置字体样式
                if (textBlock.IsBold || textBlock.HeadingLevel > 0)
                {
                    runProperties.Append(new Bold());
                }
                
                if (textBlock.IsItalic)
                {
                    runProperties.Append(new Italic());
                }
                
                // 设置字体颜色
                if (textBlock.FontColor != null)
                {
                    string colorHex = textBlock.FontColor.Substring(1); // 移除#前缀
                    runProperties.Append(new Color { Val = colorHex });
                }
                
                run.Append(runProperties);
                
                // 添加文本
                var text = new Text(textBlock.Text.Trim()) { Space = SpaceProcessingModeValues.Preserve };
                run.Append(text);
                
                // 将Run添加到段落
                currentParagraph.Append(run);
                
                // 更新上一个文本块的底部位置
                lastBottomPosition = textBlock.Rectangle.GetY() - textBlock.Rectangle.GetHeight();
            }
        }
        
        /// <summary>
        /// 分析文本块并分配标题级别
        /// </summary>
        private void AnalyzeAndAssignHeadingLevels(List<TextBlock> textBlocks)
        {
            // 首先将所有文本块初始化为非标题
            foreach (var block in textBlocks)
            {
                block.HeadingLevel = 0;
            }
            
            // 计算页面上文本的平均字体大小
            float avgFontSize = textBlocks.Average(b => b.FontSize);
            float maxFontSize = textBlocks.Max(b => b.FontSize);
            
            // 按字体大小排序，找出所有可能的标题
            var potentialHeadings = textBlocks
                .Where(b => IsHeading(b, textBlocks))
                .OrderByDescending(b => b.FontSize)
                .ToList();
            
            // 如果没有检测到标题，直接返回
            if (potentialHeadings.Count == 0)
                return;
            
            // 按字体大小分配级别 - 最多支持4级标题
            float[] fontSizeThresholds = new float[4];
            
            // 如果只有一种字体大小的标题，则全部标记为一级标题
            if (potentialHeadings.Select(h => h.FontSize).Distinct().Count() == 1)
            {
                foreach (var heading in potentialHeadings)
                {
                    heading.HeadingLevel = 1;
                }
                return;
            }
            
            // 否则，根据字体大小的分布将标题分成几个级别
            var distinctFontSizes = potentialHeadings
                .Select(h => h.FontSize)
                .Distinct()
                .OrderByDescending(size => size)
                .ToList();
            
            // 根据不同字体大小的数量决定标题级别
            for (int i = 0; i < Math.Min(distinctFontSizes.Count, 4); i++)
            {
                fontSizeThresholds[i] = distinctFontSizes[i];
            }
            
            // 为每个潜在标题分配级别
            foreach (var heading in potentialHeadings)
            {
                for (int level = 0; level < 4; level++)
                {
                    if (level < distinctFontSizes.Count && 
                        Math.Abs(heading.FontSize - fontSizeThresholds[level]) < 0.5)
                    {
                        heading.HeadingLevel = level + 1;
                        break;
                    }
                }
                
                // 默认为4级标题
                if (heading.HeadingLevel == 0)
                {
                    heading.HeadingLevel = 4;
                }
            }
        }
        
        /// <summary>
        /// 判断文本块是否为标题
        /// </summary>
        private bool IsHeading(TextBlock textBlock, List<TextBlock> allTextBlocks)
        {
            // 非常明显的标题特征：单独一行并且文本较短
            if (textBlock.Text.Length < 30 && textBlock.IsBold && textBlock.IsCentered)
            {
                return true;
            }
            
            // 计算页面上文本的平均字体大小
            float avgFontSize = allTextBlocks.Average(b => b.FontSize);
            float stdDevFontSize = (float)Math.Sqrt(allTextBlocks.Average(b => Math.Pow(b.FontSize - avgFontSize, 2)));
            
            // 字体显著大于平均值并且是粗体，非常可能是标题
            if (textBlock.FontSize > avgFontSize * 1.4 && textBlock.IsBold && textBlock.Text.Length < 60)
            {
                return true;
            }
            
            // 对于其他情况，使用更严格的判断标准
            // 1. 文本长度适中（标题通常不会太长）
            bool appropriateLength = textBlock.Text.Length < 40;
            
            // 2. 字体大小特征：标题通常比周围文本大
            bool largerFont = textBlock.FontSize > (avgFontSize + 1.2 * stdDevFontSize);
            
            // 3. 样式特征 - 必须是粗体
            bool isBold = textBlock.IsBold;
            
            // 4. 位置特征：居中、靠左且大于平均字体
            bool positionHint = (textBlock.IsCentered || 
                                 (!textBlock.IsRightAligned && textBlock.FontSize > avgFontSize));
            
            // 5. 不包含常见的非标题特征
            bool notParagraphText = !textBlock.Text.EndsWith("。") && 
                                    !textBlock.Text.EndsWith(".") && 
                                    !textBlock.Text.Contains("，") &&
                                    textBlock.Text.Count(c => c == '，' || c == ',') < 2;
            
            // 6. 文本内容特征 - 特定格式（如数字序号开头可能是标题）
            bool hasSpecialFormat = System.Text.RegularExpressions.Regex.IsMatch(textBlock.Text, @"^\d+[\.\s]") ||
                                   System.Text.RegularExpressions.Regex.IsMatch(textBlock.Text, @"^第[一二三四五六七八九十]+[章节]");
            
            // 必须同时满足以下条件才认为是标题：
            // 1. 字体大于平均值或有特殊格式
            // 2. 长度适中
            // 3. 不是明显的段落文本
            // 4. 要么是粗体，要么有良好的位置
            return (largerFont || hasSpecialFormat) && 
                   appropriateLength && 
                   notParagraphText &&
                   (isBold || positionHint);
        }
    }
    
    /// <summary>
    /// PDF页面详细内容模型
    /// </summary>
    public class PdfPageDetailedContent
    {
        public int PageNumber { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public List<TextBlock> TextBlocks { get; set; } = new List<TextBlock>();
    }
    
    /// <summary>
    /// 文本块，包含位置、样式信息
    /// </summary>
    public class TextBlock
    {
        public string Text { get; set; }
        public Rectangle Rectangle { get; set; }
        public float FontSize { get; set; }
        public string FontName { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsRightAligned { get; set; }
        public bool IsCentered { get; set; }
        public string FontColor { get; set; } = "#000000"; // 默认黑色
        public int HeadingLevel { get; set; } = 0; // 0表示非标题，1-4表示标题级别
    }
    
    /// <summary>
    /// 详细文本提取策略，用于获取文本块的样式和位置信息
    /// </summary>
    public class DetailedTextExtractionStrategy : LocationTextExtractionStrategy
    {
        private readonly List<TextBlock> _textBlocks = new List<TextBlock>();
        
        public void Reset()
        {
            _textBlocks.Clear();
        }
        
        public List<TextBlock> GetTextBlocks()
        {
            return _textBlocks;
        }
        
        public override void EventOccurred(IEventData data, EventType type)
        {
            if (type == EventType.RENDER_TEXT)
            {
                var renderInfo = (TextRenderInfo)data;
                
                // 获取文本内容
                string text = renderInfo.GetText();
                
                if (string.IsNullOrEmpty(text.Trim()))
                    return;
                
                // 获取文本的边界框
                var baseline = renderInfo.GetBaseline();
                var rectangle = renderInfo.GetDescentLine().GetBoundingRectangle();
                
                // 获取字体信息
                var font = renderInfo.GetFont();
                string fontName = font != null ? font.GetFontProgram()?.GetFontNames()?.GetFontName() : "SimSun";
                
                // 更精确地检测粗体和斜体
                bool isBold = IsTextBold(fontName, renderInfo);
                bool isItalic = IsTextItalic(fontName, renderInfo);
                
                // 获取字体大小
                float fontSize = renderInfo.GetFontSize();
                
                // 获取字体颜色
                var fontColor = "#000000"; // 默认黑色
                var fillColor = renderInfo.GetFillColor();
                if (fillColor != null)
                {
                    if (fillColor is DeviceRgb rgb)
                    {
                        fontColor = $"#{(int)(rgb.GetColorValue()[0] * 255):X2}{(int)(rgb.GetColorValue()[1] * 255):X2}{(int)(rgb.GetColorValue()[2] * 255):X2}";
                    }
                }
                
                // 判断对齐方式
                bool isRightAligned = false;
                bool isCentered = false;
                
                // 使用基线的起始点X值判断对齐方式
                float startX = baseline.GetStartPoint().Get(0);
                float pageWidth = rectangle.GetWidth() * 100; // 放大以获得更好的相对比例
                
                if (startX > pageWidth * 0.7)
                {
                    isRightAligned = true;
                }
                else if (startX > pageWidth * 0.3 && startX < pageWidth * 0.7)
                {
                    isCentered = true;
                }
                
                // 创建文本块
                var textBlock = new TextBlock
                {
                    Text = text,
                    Rectangle = rectangle,
                    FontSize = fontSize,
                    FontName = fontName,
                    IsBold = isBold,
                    IsItalic = isItalic,
                    IsRightAligned = isRightAligned,
                    IsCentered = isCentered,
                    FontColor = fontColor
                };
                
                _textBlocks.Add(textBlock);
            }
            
            base.EventOccurred(data, type);
        }
        
        /// <summary>
        /// 更精确判断文本是否为粗体
        /// </summary>
        private bool IsTextBold(string fontName, TextRenderInfo renderInfo)
        {
            if (fontName == null)
                return false;
            
            // 通过字体名称判断
            string nameLower = fontName.ToLower();
            if (nameLower.Contains("bold") || nameLower.Contains("heavy") || nameLower.Contains("black"))
                return true;
            
            // 中文字体名称判断
            if (nameLower.Contains("粗体") || nameLower.Contains("黑体"))
                return true;
            
            // 通过字形宽度判断 - 粗体通常有更大的描边宽度
            float thickness = renderInfo.GetFont().GetFontProgram()?.GetAvgWidth() ?? 0;
            if (thickness > 0.3) // 这个阈值需要根据实际情况调整
                return true;
            
            return false;
        }
        
        /// <summary>
        /// 更精确判断文本是否为斜体
        /// </summary>
        private bool IsTextItalic(string fontName, TextRenderInfo renderInfo)
        {
            if (fontName == null)
                return false;
            
            // 主要通过字体名称判断斜体
            string nameLower = fontName.ToLower();
            
            // 这些关键词通常表示斜体字体
            return nameLower.Contains("italic") || 
                   nameLower.Contains("oblique") || 
                   nameLower.Contains("斜体");
        }
    }
} 