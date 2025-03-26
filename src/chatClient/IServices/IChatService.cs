using Avalonia.Animation;
using chatClient.Models;
using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace chatClient.IServices
{
    public interface IChatService
    {
        IAsyncEnumerable<string> SendMessageStreamAsync(string message, string sessionId, AIModel model, bool IsInternetEnabled);

        IAsyncEnumerable<string> SendMessageWithFunctionsAsync(
            List<ChatMessageContent> messages,
            AIModel model);

        IAsyncEnumerable<string> SendKnowledgeBaseMessageAsync(
            string message,
            string sessionId,
            AIModel model,
            string knowledgeBaseContext,
            bool isInternetEnabled);

        IAsyncEnumerable<string> SendSearchMessageAsync(
            string message,
            string sessionId,
            AIModel model,
            string searchContext);

        IAsyncEnumerable<string> TranslateMessageAsync(
            string lan,
            AIModel model,
            string originalContext);
    }
}
