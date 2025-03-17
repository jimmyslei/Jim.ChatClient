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

namespace chatClient.ViewModels
{
    public partial class MainViewModel : ObservableObject
    {
        private readonly ChatService _chatService;
        private readonly DatabaseService _databaseService;

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

        [RelayCommand]
        private void ToggleInternet()
        {
            IsInternetEnabled = !IsInternetEnabled;
        }

        protected virtual void OnScrollToEnd()
        {
            ScrollToEnd?.Invoke(this, EventArgs.Empty);
        }

        public MainViewModel()
        {
            _chatService = new ChatService();
            _databaseService = new DatabaseService();
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

            // 如果是新对话，创建新的会话
            if (string.IsNullOrWhiteSpace(SessionId))
            {
                // 使用第一条消息作为会话标题
                var title = UserInput.Length > 20 ? UserInput.Substring(0, 20) + "..." : UserInput;
                //var sessionId = await _chatService.SaveGroup(title);
                //SessionId = sessionId.ToString();
                
                // 刷新会话列表
                await LoadChatSessionsAsync();
                SelectedSession = ChatSessions.FirstOrDefault(s => s.Id.ToString() == SessionId);
            }

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
                await foreach (var chunk in _chatService.SendMessageStreamAsync(userMessageText, SessionId, SelectedModel, IsInternetEnabled))
                {
                    assistantMessage.Content += chunk;
                    OnPropertyChanged(nameof(Messages));
                    OnScrollToEnd();
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
    }
} 