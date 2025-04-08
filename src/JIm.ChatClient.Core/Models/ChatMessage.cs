using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;

namespace JIm.ChatClient.Core.Models
{
    public enum MessageRole
    {
        User,
        Assistant,
        System
    }

    public class ChatMessage : INotifyPropertyChanged
    {
        private string _content;
        
        public event PropertyChangedEventHandler PropertyChanged;
        
        public MessageRole Role { get; }
        
        public string Content
        {
            get => _content;
            set
            {
                if (_content != value)
                {
                    _content = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Content)));
                }
            }
        }
        
        private bool _isStreaming;
        public bool IsStreaming
        {
            get => _isStreaming;
            set
            {
                if (_isStreaming != value)
                {
                    _isStreaming = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsStreaming)));
                }
            }
        }
        
        public DateTime Timestamp { get; set; } = DateTime.Now;
        
        public bool AutoRemove { get; set; }

        public ChatMessage(MessageRole role, string content, DateTime chatTime)
        {
            Role = role;
            _content = content;
            Timestamp = chatTime;
        }
    }
}