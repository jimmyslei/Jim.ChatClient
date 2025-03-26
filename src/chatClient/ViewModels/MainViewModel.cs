using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using chatClient.Models;
using chatClient.Services;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using chatClient.Entity;
using chatClient.Config;
using System.Windows.Input;
using chatClient.Views;
using chatClient.Converters;
using static System.Net.WebRequestMethods;
using chatClient.Functions;

namespace chatClient.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
        private readonly DatabaseService _databaseService;
        private readonly KnowledgeBaseService _knowledgeBaseService;

        [ObservableProperty]
        private string _userInput = string.Empty;

        [ObservableProperty]
        private string _sessionId = string.Empty;

        [ObservableProperty]
        private ObservableCollection<ChatMessage> _messages = new();

        [ObservableProperty]
        private bool _isProcessing;
        
        // 添加计算属性，用于控制发送按钮的启用状态
        public bool CanSendMessage => !string.IsNullOrWhiteSpace(UserInput) && !IsProcessing;
        

        // 在 MainViewModel 类中添加以下属性
        [ObservableProperty]
        private ObservableCollection<AIModel> _availableModels = new();

        [ObservableProperty]
        private AIModel _selectedModel;

        [ObservableProperty]
        private ObservableCollection<ChatSessionEntity> _chatSessions = new();

        [ObservableProperty]
        private ChatSessionEntity _selectedSession;

        public event EventHandler<EventArgs> ScrollToEnd;

        [ObservableProperty]
        private bool _isInternetEnabled;

        // 当前视图类型
        [ObservableProperty]
        private string _currentView = "Chat"; // 默认显示聊天视图

        // 切换视图命令
        [RelayCommand]
        private void SwitchToView(string viewType)
        {
            CurrentView = viewType;
        }

        // 知识库列表
        public ObservableCollection<KnowledgeBase> KnowledgeBases { get; } = new ObservableCollection<KnowledgeBase>();

        // 当前选中的知识库
        private KnowledgeBase _selectedKnowledgeBase;
        public KnowledgeBase SelectedKnowledgeBase
        {
            get => _selectedKnowledgeBase;
            set => SetProperty(ref _selectedKnowledgeBase, value);
        }

        // 是否启用知识库
        private bool _isKnowledgeBaseEnabled;
        public bool IsKnowledgeBaseEnabled
        {
            get => _isKnowledgeBaseEnabled;
            set => SetProperty(ref _isKnowledgeBaseEnabled, value);
        }

        // 新知识库名称
        private string _newKnowledgeBaseName;
        public string NewKnowledgeBaseName
        {
            get => _newKnowledgeBaseName;
            set => SetProperty(ref _newKnowledgeBaseName, value);
        }

        // 新知识库描述
        private string _newKnowledgeBaseDescription;
        public string NewKnowledgeBaseDescription
        {
            get => _newKnowledgeBaseDescription;
            set => SetProperty(ref _newKnowledgeBaseDescription, value);
        }

        // 是否显示知识库创建对话框
        private bool _isCreateKnowledgeBaseDialogOpen;
        public bool IsCreateKnowledgeBaseDialogOpen
        {
            get => _isCreateKnowledgeBaseDialogOpen;
            set => SetProperty(ref _isCreateKnowledgeBaseDialogOpen, value);
        }

        // 知识库搜索相关性阈值
        private double _relevanceThreshold = 0.7;
        public double RelevanceThreshold
        {
            get => _relevanceThreshold;
            set => SetProperty(ref _relevanceThreshold, value);
        }

        // 知识库最大上下文块数
        private int _maxContextChunks = 5;
        public int MaxContextChunks
        {
            get => _maxContextChunks;
            set => SetProperty(ref _maxContextChunks, value);
        }

        // 文档上传进度
        private double _documentUploadProgress;
        public double DocumentUploadProgress
        {
            get => _documentUploadProgress;
            set => SetProperty(ref _documentUploadProgress, value);
        }

        // 是否正在上传文档
        private bool _isUploadingDocument;
        public bool IsUploadingDocument
        {
            get => _isUploadingDocument;
            set => SetProperty(ref _isUploadingDocument, value);
        }

        [RelayCommand]
        private void ToggleInternet()
        {
            IsInternetEnabled = !IsInternetEnabled;
        }

        protected virtual void OnScrollToEnd()
        {
            ScrollToEnd?.Invoke(this, EventArgs.Empty);
        }

        [ObservableProperty]
        private TranslateViewModel _translateViewModel;
        public MainViewModel()
        {
            // 初始化翻译视图模型
            _translateViewModel = new TranslateViewModel();

            _chatService = new ChatService();
            _databaseService = new DatabaseService();
            _knowledgeBaseService = new KnowledgeBaseService(new AIModel
            {
                Id = "bge-m3:latest", //"text-embedding-ada-002", // 默认使用OpenAI的嵌入模型
                DisplayName = "Embedding Model",
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "",
                ApiEndpoint = "http://127.0.0.1:11434/api/embed",
                Type = "local"
            });
            // 添加欢迎消息
            Messages.Add(new ChatMessage(MessageRole.Assistant, "我是JimAi，很高兴见到你！\n\n我可以帮你写代码、读文件、写作各种创意内容，请把你的任务交给我吧~", DateTime.Now));

            // 从注册表加载模型配置
            var configuredModels = RegistryModelConfig.GetModels();
            foreach (var model in configuredModels)
            {
                AvailableModels.Add(model);
            }


            // 设置默认选中的模型
            SelectedModel = AvailableModels[0];
            
            // 监听 UserInput 和 IsProcessing 属性变化，更新 CanSendMessage
            this.PropertyChanged += (sender, e) => 
            {
                if (e.PropertyName == nameof(UserInput) || e.PropertyName == nameof(IsProcessing))
                {
                    OnPropertyChanged(nameof(CanSendMessage));
                }
            };

            // 在构造函数中加载历史记录
            LoadChatSessionsAsync().ConfigureAwait(false);
            LoadKnowledgeBases();

            // 使用默认的嵌入模型初始化知识库服务
            var embeddingModel = new AIModel
            {
                Id = "bge-m3:latest", // 默认使用OpenAI的嵌入模型
                DisplayName = "Embedding Model",
                ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "",
                ApiEndpoint = "http://127.0.0.1:11434/api/embed",
                Type = "local"
                //Id = "text-embedding-ada-002", // 默认使用OpenAI的嵌入模型
                //DisplayName = "Embedding Model",
                //ApiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "",
                //ApiEndpoint = "https://api.openai.com/v1/embeddings",
                //Type = "online"
            };
            
            //// 如果有本地嵌入模型，可以使用本地模型
            //if (AvailableModels.Any(m => m.Type.ToLower() == "local"))
            //{
            //    var localModel = AvailableModels.First(m => m.Type.ToLower() == "local");
            //    embeddingModel = new AIModel
            //    {
            //        Id = localModel.Id,
            //        DisplayName = localModel.DisplayName,
            //        ApiEndpoint = localModel.ApiEndpoint,
            //        Type = "local"
            //    };
            //}
            
            _knowledgeBaseService = new KnowledgeBaseService(embeddingModel);
        }

        private async Task LoadChatSessionsAsync()
        {
            try
            {
                var sessions = await _databaseService.GetHistorySessionAsync();
                ChatSessions.Clear();
                foreach (var session in sessions)
                {
                    ChatSessions.Add(session);
                }

                // // 如果有会话记录，自动选中最新的一条（第一条）
                // if (ChatSessions.Any())
                // {
                //     SelectedSession = ChatSessions.First(); // 因为已经按 CreateTime 降序排序，所以第一条就是最新的
                // }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载历史记录失败: {ex.Message}");
            }
        }

        private async Task LoadSessionMessages(ChatSessionEntity session)
        {
            if (session == null) return;

            try
            {
                // 清空当前消息
                Messages.Clear();
                
                // 加载选中会话的消息
                var messages = await _databaseService.GetHistoryMessagesAsync(session.Id.ToString());
                foreach (var msg in messages.OrderBy(m => m.CreateTime))
                {
                    var role = msg.Role.ToLower() == "user" ? MessageRole.User : 
                              msg.Role.ToLower() == "assistant" ? MessageRole.Assistant : 
                              MessageRole.System;
                    Messages.Add(new ChatMessage(role, msg.Content, msg.CreateTime));
                }
                
                // 更新当前会话ID
                SessionId = session.Id.ToString();
                
                // 滚动到底部
                OnScrollToEnd();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载会话消息失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task SendMessage()
        {
            if (string.IsNullOrWhiteSpace(UserInput) || IsProcessing)
                return;

            var userMessage = new ChatMessage(MessageRole.User, UserInput, DateTime.Now);
            Messages.Add(userMessage);
            OnScrollToEnd();

            var assistantMessage = new ChatMessage(MessageRole.Assistant, string.Empty, DateTime.Now);
            assistantMessage.IsStreaming = true;
            Messages.Add(assistantMessage);
            OnScrollToEnd();

            string userMessageText = UserInput;
            UserInput = string.Empty;
            IsProcessing = true;

            try
            {
                // 如果启用了知识库且选择了知识库，使用知识库上下文
                if (IsKnowledgeBaseEnabled && SelectedKnowledgeBase != null)
                {
                    // 获取与查询相关的知识库上下文
                    string knowledgeBaseContext = await _knowledgeBaseService.GetRelevantContextForQuery(
                        SelectedKnowledgeBase.Id, 
                        userMessageText,
                        MaxContextChunks
                    );
                    
                    // 如果找到相关上下文，使用知识库进行回答
                    if (!string.IsNullOrEmpty(knowledgeBaseContext))
                    {
                        await foreach (var chunk in _chatService.SendKnowledgeBaseMessageAsync(
                            userMessageText, 
                            SessionId, 
                            SelectedModel, 
                            knowledgeBaseContext,
                            IsInternetEnabled))
                        {
                            assistantMessage.Content += chunk;
                            OnPropertyChanged(nameof(Messages));
                            OnScrollToEnd();
                        }
                    }
                    else
                    {
                        // 如果没有找到相关上下文，使用普通对话
                        await foreach (var chunk in _chatService.SendMessageStreamAsync(
                            userMessageText, 
                            SessionId, 
                            SelectedModel, 
                            IsInternetEnabled))
                        {
                            assistantMessage.Content += chunk;
                            OnPropertyChanged(nameof(Messages));
                            OnScrollToEnd();
                        }
                    }
                }
                // 联网搜索
                else if (IsInternetEnabled)
                {
                    var func = new InternetFunctions();
                    var searchResult = await func.ScrapeSearchResults(userMessageText);
                    var searchResultStr = func.FormatForLLM(searchResult);
                    await foreach (var chunk in _chatService.SendSearchMessageAsync(
                            userMessageText,
                            SessionId,
                            SelectedModel,
                            searchResultStr))
                    {
                        assistantMessage.Content += chunk;
                        OnPropertyChanged(nameof(Messages));
                        OnScrollToEnd();
                    }
                }
                else
                {
                    // 原有的消息发送逻辑
                    await foreach (var chunk in _chatService.SendMessageStreamAsync(
                        userMessageText, 
                        SessionId, 
                        SelectedModel, 
                        IsInternetEnabled))
                    {
                        assistantMessage.Content += chunk;
                        OnPropertyChanged(nameof(Messages));
                        OnScrollToEnd();
                    }
                }

                // 在消息发送完成后刷新历史记录列表
                await LoadChatSessionsAsync();
                // 如果有会话记录，自动选中最新的一条（第一条）
                if (ChatSessions.Any())
                {
                    SelectedSession = ChatSessions.First(); // 因为已经按 CreateTime 降序排序，所以第一条就是最新的
                }
                
                // 如果是新对话，确保选中新创建的会话
                if (string.IsNullOrEmpty(SessionId))
                {
                    var newSession = ChatSessions.FirstOrDefault();
                    if (newSession != null)
                    {
                        SelectedSession = newSession;
                        SessionId = newSession.Id.ToString();
                    }
                }
            }
            finally
            {
                assistantMessage.IsStreaming = false;
                IsProcessing = false;
                OnScrollToEnd();
            }
        }

        [RelayCommand]
        private async Task ClearChat()
        {
            // 清空当前消息
            Messages.Clear();
            Messages.Add(new ChatMessage(MessageRole.Assistant, "我是 JimAi，很高兴见到你！\n\n我可以帮你写代码、读文件、写作各种创意内容，请把你的任务交给我吧~", DateTime.Now));
            
            // 清空当前会话
            SessionId = string.Empty;
            SelectedSession = null;

            // 刷新历史记录列表
            await LoadChatSessionsAsync();
        }

        [RelayCommand]
        private async Task UploadFile()
        {
            if (IsProcessing)
                return;

            var fileService = new FileService(new MainWindow());
            var fileContent = await fileService.OpenFileAsync();
            
            if (!string.IsNullOrEmpty(fileContent))
            {
                // 如果文件内容太长，可以截断它
                if (fileContent.Length > 1000)
                {
                    UserInput = $"我上传了一个文件，内容如下：\n\n```\n{fileContent.Substring(0, 1000)}...\n```\n\n(文件内容已截断，共{fileContent.Length}个字符)";
                }
                else
                {
                    UserInput = $"我上传了一个文件，内容如下：\n\n```\n{fileContent}\n```";
                }
            }
        }

        // 添加属性变更通知
        partial void OnSelectedModelChanged(AIModel value)
        {
            // 可以在这里添加模型切换时的逻辑
            // 例如显示提示消息
            if (value != null && Messages.Count > 0)
            {
                Messages.Add(new ChatMessage(MessageRole.System, $"已切换到 {value.DisplayName} 模型", DateTime.Now));
            }
        }

        [RelayCommand]
        private void CopyMessage(string content)
        {
            if (string.IsNullOrEmpty(content))
                return;

            try
            {
                // 发布一个事件，让视图层处理剪贴板操作和Toast显示
                CopyToClipboardRequested?.Invoke(this, content);
                
                // 不再添加系统消息到消息列表中
            }
            catch (Exception ex)
            {
                // 处理异常
                Console.WriteLine($"复制失败: {ex.Message}");
            }
        }

        // 添加一个事件，用于请求复制到剪贴板
        public event EventHandler<string> CopyToClipboardRequested;

        partial void OnSelectedSessionChanged(ChatSessionEntity value)
        {
            if (value != null)
            {
                LoadSessionMessages(value).ConfigureAwait(false);
            }
        }

        // 加载知识库列表
        private void LoadKnowledgeBases()
        {
            KnowledgeBases.Clear();
            foreach (var kb in _knowledgeBaseService.GetAllKnowledgeBases())
            {
                KnowledgeBases.Add(kb);
            }
        }

        // 创建知识库命令
        [RelayCommand]
        public void CreateKnowledgeBase()
        {
            if (string.IsNullOrWhiteSpace(NewKnowledgeBaseName))
                return;
            
            var knowledgeBase = _knowledgeBaseService.CreateKnowledgeBase(
                NewKnowledgeBaseName, 
                NewKnowledgeBaseDescription ?? string.Empty
            );
            
            KnowledgeBases.Add(knowledgeBase);
            SelectedKnowledgeBase = knowledgeBase;
            
            // 清空输入
            NewKnowledgeBaseName = string.Empty;
            NewKnowledgeBaseDescription = string.Empty;
            IsCreateKnowledgeBaseDialogOpen = false;
        }

        // 删除知识库命令
        [RelayCommand]
        private void DeleteKnowledgeBase(KnowledgeBase knowledgeBase)
        {
            if (knowledgeBase == null)
                return;
            
            if (_knowledgeBaseService.DeleteKnowledgeBase(knowledgeBase.Id))
            {
                KnowledgeBases.Remove(knowledgeBase);
                if (SelectedKnowledgeBase == knowledgeBase)
                {
                    SelectedKnowledgeBase = null;
                }
            }
        }

        // 上传文档到知识库命令
        [RelayCommand]
        private async Task UploadDocumentToKnowledgeBase()
        {
            if (SelectedKnowledgeBase == null || IsProcessing || IsUploadingDocument)
                return;
            
            var fileService = new FileService(new MainWindow());
            var file = await fileService.OpenDocumentAsync();
            
            if (file != null)
            {
                try
                {
                    IsUploadingDocument = true;
                    DocumentUploadProgress = 0;
                    
                    // 显示上传开始消息
                    Messages.Add(new ChatMessage(MessageRole.System, $"正在上传文档 {file.Name} 到知识库 {SelectedKnowledgeBase.Name}...", DateTime.Now));
                    
                    // 使用新的方法上传文档到知识库，使用进度报告
                    var document = await _knowledgeBaseService.AddDocumentToKnowledgeBaseWithProperExtraction(
                        SelectedKnowledgeBase.Id,
                        file,
                        progress => DocumentUploadProgress = progress
                    );
                    
                    // 选中刚刚添加文档的知识库
                    SelectedKnowledgeBase = KnowledgeBases.FirstOrDefault(kb => kb.Id == SelectedKnowledgeBase.Id);
                    
                    // 显示上传成功消息
                    Messages.Add(new ChatMessage(MessageRole.System, $"文档 {file.Name} 已成功上传到知识库 {SelectedKnowledgeBase.Name}", DateTime.Now));
                    // 刷新知识库列表
                    LoadKnowledgeBases();
                }
                catch (Exception ex)
                {
                    // 显示错误消息
                    Messages.Add(new ChatMessage(MessageRole.System, $"文档上传失败: {ex.Message}", DateTime.Now));
                }
                finally
                {
                    IsUploadingDocument = false;
                    DocumentUploadProgress = 0;
                }
            }
        }

        // 从知识库中删除文档命令
        [RelayCommand]
        private void RemoveDocumentFromKnowledgeBase(KnowledgeDocument document)
        {
            if (document == null)
                return;
            
            // 确保有选中的知识库
            var knowledgeBase = SelectedKnowledgeBase;
            if (knowledgeBase == null)
            {
                // 添加错误消息
                Messages.Add(new ChatMessage(MessageRole.System, "无法删除文档：未选择知识库", DateTime.Now));
                return;
            }
            
            // 保存知识库ID以便后续查找
            var knowledgeBaseId = knowledgeBase.Id;
            
            if (_knowledgeBaseService.RemoveDocumentFromKnowledgeBase(knowledgeBaseId, document.Id))
            {
                // 刷新知识库列表
                LoadKnowledgeBases();
                
                // 重新选中之前的知识库
                SelectedKnowledgeBase = KnowledgeBases.FirstOrDefault(kb => kb.Id == knowledgeBaseId);
                
                // 显示删除成功消息
                Messages.Add(new ChatMessage(MessageRole.System, $"文档 {document.FileName} 已从知识库中删除", DateTime.Now));
            }
            else
            {
                // 显示删除失败消息
                Messages.Add(new ChatMessage(MessageRole.System, $"删除文档 {document.FileName} 失败", DateTime.Now));
            }
        }

        // 打开创建知识库对话框命令
        [RelayCommand]
        private void CreateKnowledgeBaseDialog()
        {
            var dialog = new CreateKnowledgeBaseDialog(this);
            
            // 获取当前应用程序的主窗口
            var mainWindow = Avalonia.Application.Current.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;
            
            // 使用主窗口作为父窗口显示对话框
            if (mainWindow != null)
            {
                dialog.ShowDialog(mainWindow);
            }
            else
            {
                dialog.Show(); // 如果找不到主窗口，则使用无模态显示
            }
            
            IsCreateKnowledgeBaseDialogOpen = true;
        }

        // 取消命令 - 由对话框设置
        public ICommand CancelCommand { get; set; }

        // 创建知识库命令 - 由对话框设置
        //public ICommand CreateKnowledgeBaseCommand { get; set; }
    }
} 