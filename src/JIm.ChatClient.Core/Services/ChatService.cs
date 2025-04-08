using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JIm.ChatClient.Core.Models;
using System.Net.Http.Headers;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.Ollama;
using System.Threading;
using Azure.Core;
using JIm.ChatClient.Core.AiHandler;
using OllamaSharp;
using FreeSql;
using JIm.ChatClient.Core.Entity;
using System.Linq;
using System.ComponentModel;
using JIm.ChatClient.Core.Functions;
using System.Reflection;
using Newtonsoft.Json;
using System.ClientModel.Primitives;
using System.Text.Json.Nodes;
using JIm.ChatClient.Core.IServices;
using JIm.ChatClient.Core.MCP;

namespace JIm.ChatClient.Core.Services
{
    public class ChatService: IChatService
    {
        private readonly HttpClient _httpClient;
        private readonly DatabaseService _dbService;

        public ChatService()
        {
            _httpClient = new HttpClient();
            _dbService = new DatabaseService();
        }

        /// <summary>
        /// 发送消息
        /// </summary>
        /// <param name="message">消息</param>
        /// <param name="sessionId">会话id</param>
        /// <param name="model"></param>
        /// <param name="IsInternetEnabled">是否联网</param>
        /// <returns></returns>
        public async IAsyncEnumerable<string> SendMessageStreamAsync(string message,string sessionId, AIModel model, bool IsInternetEnabled)
        {
            var historyMessages = new List<ChatMessageContent>();
            if (!string.IsNullOrEmpty(sessionId))
            {
                var messages = await _dbService.GetMessageListAsync(sessionId);
                messages.Reverse();
                historyMessages = messages.Select(m => new ChatMessageContent
                {
                    Role = new AuthorRole(m.Role),
                    Content = m.Content,
                }).ToList();
            }
            historyMessages.Add(new ChatMessageContent(AuthorRole.User, message));
            // 保存用户消息
            await _dbService.SaveMessageAsync(new ChatMessageEntity
            {
                SessionId = sessionId,
                Role = "user",
                Content = message,
                CreateTime = DateTime.Now,
                ModelId = model.Id
            });

            // 用于收集AI完整回复
            var aiResponse = new StringBuilder();
            switch (model.Type.ToLower())
            {
                case "local":
                    await foreach (var chunk in SendLocalMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "online":
                    await foreach (var chunk in SendSemanticKernelMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "weather":
                    await foreach (var chunk in SendMessageWithFunctionsAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "mcp":
                    await foreach (var chunk in SendMessageWithMCPServiceAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                default:
                    await foreach (var chunk in SendHttpMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
            }

            // 保存AI回复
            await _dbService.SaveMessageAsync(new ChatMessageEntity
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponse.ToString(), // 使用收集到的完整回复
                CreateTime = DateTime.Now,
                ModelId = model.Id
            });

            if (!string.IsNullOrEmpty(sessionId))
            {
                await _dbService.UpdateModifyTimeAsync(sessionId);
            }
            if (string.IsNullOrEmpty(sessionId))
            {
                var messages = await _dbService.GetNoSessionMessageListAsync();
                var chatMessages = messages.Select(m => new ChatMessageContent
                {
                    Role = new AuthorRole(m.Role),
                    Content = m.Content,
                }).ToList();
                chatMessages.Add(new ChatMessageContent(AuthorRole.User, "用一句话为这段对话取个标题，字数不超过30个字"));
                var titleCotext = new StringBuilder(); ;
                await foreach (var chunk in SendSemanticKernelMessageAsync(chatMessages, model))
                {
                    titleCotext.Append(chunk);
                }
                // 保存会话信息
                var chatSessionId = await _dbService.SaveMessageAsync(new ChatSessionEntity
                {
                    Title = titleCotext.ToString(), //message.Length > 50 ? message.Substring(0, 50) + "..." : message, // 使用用户消息的前50个字符作为标题
                    CreateTime = DateTime.Now,
                    ModifyTime = DateTime.Now
                });

                await _dbService.UpdateMessageSessionIdAsync(chatSessionId.ToString());
            }
        }

        private async IAsyncEnumerable<string> SendLocalMessageAsync(List<ChatMessageContent> messages, AIModel model)
        {
            // 创建 Ollama 内核
            var builder = Kernel.CreateBuilder();

            var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint))
            {
                BaseAddress = new Uri(model.ApiEndpoint)
            };

            //var ollamaclient = new OllamaApiClient(httpClient, model.Id);

            // 添加详细的请求头，确保与 OpenAI API 兼容
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

#pragma warning disable SKEXP0070 // 类型仅用于评估，在将来的更新中可能会被更改或删除
            builder.AddOllamaChatCompletion(model.Id, httpClient);
#pragma warning restore SKEXP0070 // 类型仅用于评估，在将来的更新中可能会被更改或删除
            var kernel = builder.Build();

            // 获取聊天服务
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            // 创建聊天历史
            ChatHistory history = new ChatHistory(messages);
            //history.AddSystemMessage("你是一个智能助手，可以回答用户问题。当用户询问天气相关信息时，请使用GetWeather函数获取准确信息。");
            //history.AddUserMessage(message);
            //builder.Plugins.AddFromType<WeatherFunctions>();
            // 设置参数
            OpenAIPromptExecutionSettings settings = new() { Temperature = 0.7, TopP = 0.9 };

            // 获取流式响应
            await foreach (var content in chat.GetStreamingChatMessageContentsAsync(
                history, settings, kernel))
            {
                yield return content.ToString();
            }
        }

        private async IAsyncEnumerable<string> SendSemanticKernelMessageAsync(List<ChatMessageContent> messages, AIModel model)
        {
            //var internetFunctions = new InternetFunctions();
            //// 直接从类型获取函数
            //var kernelFunctions = new List<KernelFunction>();
            //foreach (var method in typeof(InternetFunctions).GetMethods())
            //{
            //    var kernelFunctionAttribute = method.GetCustomAttribute<KernelFunctionAttribute>();
            //    if (kernelFunctionAttribute != null)
            //    {
            //        var function = KernelFunctionFactory.CreateFromMethod(method, internetFunctions);
            //        kernelFunctions.Add(function);
            //    }
            //}

            // 创建 OpenAI 内核
            var builder = Kernel.CreateBuilder();

            // 根据模型类型添加不同的服务
            if (model.Id.Contains("gpt"))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: model.Id,
                    apiKey: model.ApiKey);
            }
            else
            {
                var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint));
                //{
                //    BaseAddress = new Uri(endpoint)
                //};

                // 添加详细的请求头，确保与 OpenAI API 兼容
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // 使用完整配置的 HttpClient
                builder.AddOpenAIChatCompletion(
                    modelId: model.Id,
                    apiKey: model.ApiKey,
                    httpClient: httpClient);
            }

            //var plugin = KernelPluginFactory.CreateFromFunctions("InternetPlugin", kernelFunctions);
            //builder.Plugins.Add(plugin);

            var kernel = builder.Build();

            // 获取聊天服务
            var chat = kernel.GetRequiredService<IChatCompletionService>();

            // 创建聊天历史
            ChatHistory history = new ChatHistory(messages);
            //history.AddSystemMessage(@"<optimized_prompt>
            //                            <role>
            //                            你是一款专业的搜索引擎助手，用户提出的每一个问题你都需要联网查询。
            //                            </role>

            //                            <capabilities>
            //                            - 解析html中标签生成对应的md。
            //                            - 将提取的信息准确地总结为一段简洁的文本。
            //                            - 不属于用户提问的数据则不用整理。
            //                            </capabilities>

            //                            <instructions>
            //                            1. 解析html标签:
            //                               - 这是一个完整的html标签，您需要根据标签生成对应的md格式。
            //                               - 只包含关键信息，尽量减少非主要信息的出现。
            //                               - 完成总结后，立即向用户提供，不需要询问用户是否满意或是否需要进一步的修改和优化。

            //                            2. 回答规范:
            //                               - 严格聚焦于用户问题的相关内容
            //                               - 不要添加无关的额外内容
            //                            </instructions>

            //                            <response_format>
            //                            简洁、准确、友好的回答
            //                            </response_format>
            //                            </optimized_prompt>");


            // 设置参数，添加更多配置以确保兼容性
            OpenAIPromptExecutionSettings settings = new()
            {
                Temperature = 0.7,
                MaxTokens = 2000,
                //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                //ExtensionData = new Dictionary<string, object>
                //{
                //    { "tools_choice", "auto" }  // 使用DeepSeek的自动工具选择
                //}
                // 如果您的 API 需要特定的请求格式，可以在这里配置
            };

            // 获取流式响应
            await foreach (var update in chat.GetStreamingChatMessageContentsAsync(
                history, settings, kernel, CancellationToken.None))
            {
                if (string.IsNullOrEmpty(update.ToString()))
                {
                    var jsonContent = JsonNode.Parse(ModelReaderWriter.Write(update.InnerContent!));
                    var choices = jsonContent!["choices"];
                    if (choices.ToString() != "[]")
                    {
                        var reasoningUpdate = jsonContent!["choices"]![0]!["delta"]!["reasoning_content"];

                        if (reasoningUpdate != null)
                            yield return reasoningUpdate.ToString();
                    }
                }
                else yield return update.ToString();
            }
        }

        private async IAsyncEnumerable<string> SendHttpMessageAsync(List<ChatMessageContent> messages, AIModel model)
        {
            // 配置 HTTP 客户端
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.ApiKey}");
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            var msgs = new List<object>();
            messages.ForEach(msg =>
            {
                msgs.Add(new { role = msg.Role.ToString(), content = msg.Content });
            });

            var requestBody = new
            {
                model = model.Id,
                messages = msgs,// new[] { new { role = "user", content = "你是谁" } },
                stream = true
            };

            var content = new StringContent(
                JsonConvert.SerializeObject(requestBody),
                Encoding.UTF8,
                "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, model.ApiEndpoint)
            {
                Content = content
            };

            var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();

            using var stream = await response.Content.ReadAsStreamAsync();
            using var reader = new System.IO.StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var data = line.Substring(6);
                    if (data == "[DONE]")
                        break;

                    var jsonResponse = JsonDocument.Parse(data);
                    var responseStr = jsonResponse.RootElement
                        .GetProperty("choices")[0]
                        .GetProperty("delta")
                        .GetProperty("content")
                        .GetString();

                    if (!string.IsNullOrEmpty(responseStr))
                        yield return responseStr;
                }
                else
                {
                    if (!string.IsNullOrEmpty(line))
                    {
                        if (line == "[DONE]")
                            break;
                        var jsonResponse = JsonDocument.Parse(line);
                        var responseStr = jsonResponse.RootElement
                            .GetProperty("message")
                            .GetProperty("content")
                            .GetString();

                        if (!string.IsNullOrEmpty(responseStr))
                            yield return responseStr;
                    }
                }
            }
        }

        // 添加函数调用支持
        public async IAsyncEnumerable<string> SendMessageWithFunctionsAsync(
            List<ChatMessageContent> messages, 
            AIModel model)
        {
            // 函数集合
            var weatherFunctions = new WeatherFunctions();
            // 直接从类型获取函数
            var functions = new List<KernelFunction>();
            foreach (var method in typeof(WeatherFunctions).GetMethods())
            {
                var kernelFunctionAttribute = method.GetCustomAttribute<KernelFunctionAttribute>();
                if (kernelFunctionAttribute != null)
                {
                    var function = KernelFunctionFactory.CreateFromMethod(method, weatherFunctions);
                    functions.Add(function);
                }
            }

            // 创建内核
            var builder = Kernel.CreateBuilder();
            
            if (model.Type.ToLower() == "local")
            {
                // var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint))
                // {
                //     BaseAddress = new Uri(model.ApiEndpoint)
                // };
#pragma warning disable SKEXP0070 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                builder.AddOllamaChatCompletion(model.Id, new Uri(model.ApiEndpoint));
#pragma warning restore SKEXP0070 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            }
            else
            {
                var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint));
                builder.AddOpenAIChatCompletion(
                    modelId: model.Id,
                    apiKey: model.ApiKey,
                    httpClient: httpClient);
            }

            //builder.Plugins.AddFromType<WeatherFunctions>();

            // 正确注册函数
            var plugin = KernelPluginFactory.CreateFromFunctions("WeatherPlugin", functions);
            builder.Plugins.Add(plugin);

            var kernel = builder.Build();
            
            // 创建聊天历史
            ChatHistory history = new ChatHistory(messages);
            history.AddSystemMessage(@"<optimized_prompt>
                                        <role>
                                        你是一个专业的智能天气助手，擅长提供准确的天气信息和时间查询服务。
                                        </role>

                                        <capabilities>
                                        - 查询当前时间和日期
                                        - 提供精确的天气预报信息
                                        - 使用可爱友好的语气回应用户
                                        </capabilities>

                                        <instructions>
                                        1. 当用户询问天气相关信息时:
                                           - 首先使用GetCurrentTime函数获取当前准确时间
                                           - 然后使用GetWeather函数查询相应日期的天气数据
                                           - 基于当前日期，精确定位用户询问的具体日期的天气信息

                                        2. 当用户仅询问当前时间或日期时:
                                           - 只调用GetCurrentTime函数
                                           - 不要调用GetWeather函数

                                        3. 回答规范:
                                           - 严格聚焦于用户问题的相关内容
                                           - 可以添加温馨提示或天气相关建议
                                           - 使用可爱活泼的语气回应
                                           - 不要添加无关的额外内容
                                        </instructions>

                                        <response_format>
                                        简洁、准确、友好的回答，包含:
                                        - 时间/日期信息(如适用)
                                        - 天气数据(如适用)
                                        - 相关温馨提示(可选)
                                        </response_format>
                                        </optimized_prompt>");
            //history.AddUserMessage(message);
            
            // 设置参数，启用函数调用
            OpenAIPromptExecutionSettings settings = new() 
            { 
                Temperature = 0.7,
                ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                ExtensionData = new Dictionary<string, object>
                {
                    { "tools_choice", "auto" }  // 使用DeepSeek的自动工具选择
                }
            };
            
            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // 获取流式响应
            await foreach (var content in chatCompletionService.GetStreamingChatMessageContentsAsync(
                history, settings, kernel, CancellationToken.None))
            {
                yield return content.ToString();
            }
        }

        public async IAsyncEnumerable<string> SendMessageWithMCPServiceAsync(
            List<ChatMessageContent> messages,
            AIModel model)
        {
            //// 函数集合
            //var weatherFunctions = new WeatherFunctions();
            //// 直接从类型获取函数
            //var functions = new List<KernelFunction>();
            //foreach (var method in typeof(WeatherFunctions).GetMethods())
            //{
            //    var kernelFunctionAttribute = method.GetCustomAttribute<KernelFunctionAttribute>();
            //    if (kernelFunctionAttribute != null)
            //    {
            //        var function = KernelFunctionFactory.CreateFromMethod(method, weatherFunctions);
            //        functions.Add(function);
            //    }
            //}

            // 创建内核
            var builder = Kernel.CreateBuilder();

            if (model.Type.ToLower() == "local")
            {
                // var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint))
                // {
                //     BaseAddress = new Uri(model.ApiEndpoint)
                // };
#pragma warning disable SKEXP0070 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                builder.AddOllamaChatCompletion(model.Id, new Uri(model.ApiEndpoint));
#pragma warning restore SKEXP0070 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            }
            else
            {
                var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint));
                builder.AddOpenAIChatCompletion(
                    modelId: model.Id,
                    apiKey: model.ApiKey,
                    httpClient: httpClient);
            }

            //builder.Plugins.AddFromType<WeatherFunctions>();

            // 正确注册函数
            await builder.Plugins.AddMcpFunctionsFromSseServerAsync("http://127.0.0.1:5050/sse", "ssetool");
            //var plugin = KernelPluginFactory.CreateFromFunctions("WeatherPlugin", functions);
            //builder.Plugins.Add(plugin);

            var kernel = builder.Build();

            // 创建聊天历史
            ChatHistory history = new ChatHistory(messages);
            history.AddSystemMessage(@"你是一个函数调用助手，我所提问的所有问题都通过调用我提供的函数返回。
            当用户让你创建一个任务时，有两种情况：
            --一、如果用户提到了给具体某人发送任务，
                需要先调用getOrgUser函数获取用户，再根据返回的数据匹配是否有相同名字的用户，
                如果有相同用户需用户确认，
                如果没有相同用户，提示用户选择需要发送任务的参与人；
            --二、如果用户没有提到给具体某人发送任务，而是直接让你发一个任务，则依次往下按步骤进行
            
            先需要提供两个选项
            1.快速任务
            2.模板任务
            -1、当用户输入快速任务时，你需要提示用户创建快速任务的标题是什么，任务的开始时间和结束时间是多少，
            
            (备注：如果为第一种情况跳过getOrgUser获取人员选择的步骤)
            然后调用getOrgUser的toolfun获取有哪些用户，给用户列出来，
            然后让用户输入任务的参与人有哪些，从你提供的人员里面选择；


            然后提示用户输入任务的要求，如果用回复没有要求则根据以上用户回复的信息组成一个json对象，
            -json示例：
            ```
            {""title"":""11"",""planTimeline"":[""2025-04-03 08:00:00"",""2025-04-03 18:00:00""],""participants"":[{""id"":""438954639166021637"",""type"":""user"",""name"":""张三/zs""}]}
            ```
            最后让用户确认一下最终信息是否正确，用户确认后，调用launchTask的toolfun，把json数据传入方法中发起任务
            -2、当用户输入模板任务时，你需要从tools里面调用getTaskType方法，获取模板分类有哪些，给用户罗列出来，让用户选择其中一个分类，
            如果有多个下级分类，你需要把上一个分类的id作为入参同样请求getTaskType方法，当这个分类下有模板时，需需要列出模板有哪些，展示给用户选择，
            用户选择模板后，根据选择的模板id调用getTaskForm方法获取模板任务的表单信息返回给用户，
            然后提示用户输入创建模板任务的标题是什么，任务的开始时间和结束时间是多少，工时多少小时，
            
            (备注：如果为第一种情况跳过getOrgUser获取人员选择的步骤)
            然后调用getOrgUser的toolfun获取有哪些用户，给用户列出来，
            然后让用户输入任务的参与人有哪些，从你提供的人员里面选择；

            然后根据getTaskForm方法返回的数据中找到launchFields集合，
            再根据集合中required字段是否为true提示用户必须输入，当用户输入完所有信息后，把用户输入的信息整理为一个json对象，提示用户确认信息，如下示例：
            -json示例：
            ```
            {""title"":""458"",""typeId"":""606979677491428613"",""planTimeline"":[""2025-04-03 08:00:00"",""2025-04-03 18:00:00""],""workload"":1,""content"":null,""participants"":[{""id"":""436391321196825029"",""type"":""user"",""name"":""张三/zs""}],""taskTemplateVersionId"":""606985705931018566"",""formData"":{""name"":""11"",""position"":""11"",""entryDate"":""2025-04-30"",""manager"":""""}}
            ```
            formData数据是根据launchFields返回的字段组装而成的
            当用户确认信息后调用launchTask,把json数据传入方法中发起任务
            ");
            //history.AddUserMessage(message);

            // 设置参数，启用函数调用
            OpenAIPromptExecutionSettings settings = new()
            {
                Temperature = 0.7,
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto(),
                //ToolCallBehavior = ToolCallBehavior.AutoInvokeKernelFunctions,
                //ExtensionData = new Dictionary<string, object>
                //{
                //    { "tools_choice", "auto" }  // 使用DeepSeek的自动工具选择
                //}
            };

            var chatCompletionService = kernel.GetRequiredService<IChatCompletionService>();

            // 获取流式响应
            await foreach (var content in chatCompletionService.GetStreamingChatMessageContentsAsync(
                history, settings, kernel, CancellationToken.None))
            {
                yield return content.ToString();
            }
        }


        public async IAsyncEnumerable<string> SendKnowledgeBaseMessageAsync(
            string message, 
            string sessionId, 
            AIModel model, 
            string knowledgeBaseContext,
            bool isInternetEnabled)
        {
            var historyMessages = new List<ChatMessageContent>();
            if (!string.IsNullOrEmpty(sessionId))
            {
                var messages = await _dbService.GetMessageListAsync(sessionId);
                messages.Reverse();
                historyMessages = messages.Select(m => new ChatMessageContent
                {
                    Role = new AuthorRole(m.Role),
                    Content = m.Content,
                }).ToList();
            }
            
            // 添加知识库上下文作为系统消息
            if (!string.IsNullOrEmpty(knowledgeBaseContext))
            {
                historyMessages.Insert(0, new ChatMessageContent(
                    AuthorRole.System,
                    $@"
                        #提示词：
                        ##角色：
                            -专业知识库助手
                        ##能力：
                        请基于提供的知识库内容回答用户问题。遵循以下原则：

                        -1. 仅使用知识库中的信息进行回答
                        -2. 如发现知识库中没有相关信息，明确告知用户""无法回答此问题""，不进行信息编造
                        -3. 回答时引用相关知识库内容作为依据
                        -4. 确保回答准确、简洁、有条理
                        
                        ##知识库内容
                        -{knowledgeBaseContext}
                        "
                ));
            }
            
            historyMessages.Add(new ChatMessageContent(AuthorRole.User, message));
            
            // 保存用户消息
            await _dbService.SaveMessageAsync(new ChatMessageEntity
            {
                SessionId = sessionId,
                Role = "user",
                Content = message,
                CreateTime = DateTime.Now,
                ModelId = model.Id
            });

            // 用于收集AI完整回复
            var aiResponse = new StringBuilder();
            
            // 根据模型类型选择不同的处理方式
            switch (model.Type.ToLower())
            {
                case "local":
                    await foreach (var chunk in SendLocalMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "online":
                    await foreach (var chunk in SendSemanticKernelMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "weather":
                    await foreach (var chunk in SendMessageWithFunctionsAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                default:
                    await foreach (var chunk in SendHttpMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
            }

            // 保存AI回复
            await _dbService.SaveMessageAsync(new ChatMessageEntity
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponse.ToString(),
                CreateTime = DateTime.Now,
                ModelId = model.Id
            });

            if (!string.IsNullOrEmpty(sessionId))
            {
                await _dbService.UpdateModifyTimeAsync(sessionId);
            }
            
            // 处理新会话的情况
            if (string.IsNullOrEmpty(sessionId))
            {
                var messages = await _dbService.GetNoSessionMessageListAsync();
                var chatMessages = messages.Select(m => new ChatMessageContent
                {
                    Role = new AuthorRole(m.Role),
                    Content = m.Content,
                }).ToList();
                chatMessages.Add(new ChatMessageContent(AuthorRole.User, "用一句话为这段对话取个标题，字数不超过30个字"));
                var titleCotext = new StringBuilder();
                await foreach (var chunk in SendSemanticKernelMessageAsync(chatMessages, model))
                {
                    titleCotext.Append(chunk);
                }
                
                // 保存会话信息
                var chatSessionId = await _dbService.SaveMessageAsync(new ChatSessionEntity
                {
                    Title = titleCotext.ToString(),
                    CreateTime = DateTime.Now,
                    ModifyTime = DateTime.Now
                });

                await _dbService.UpdateMessageSessionIdAsync(chatSessionId.ToString());
            }
        }

        public async IAsyncEnumerable<string> SendSearchMessageAsync(
            string message,
            string sessionId,
            AIModel model,
            string searchContext)
        {
            var historyMessages = new List<ChatMessageContent>();
            if (!string.IsNullOrEmpty(sessionId))
            {
                var messages = await _dbService.GetMessageListAsync(sessionId);
                messages.Reverse();
                historyMessages = messages.Select(m => new ChatMessageContent
                {
                    Role = new AuthorRole(m.Role),
                    Content = m.Content,
                }).ToList();
            }

            historyMessages.Insert(0, new ChatMessageContent(
                AuthorRole.System,
                $@"
                        # 提示词：
                        ## 角色：
                            -你是一款专业的搜索引擎助手
                        ## 能力：
                        请基于搜索结果回答用户问题。遵循以下原则：

                        -1. 如果用户提问的问题在搜索结果中，请参考搜索结果回答用户问题，在回复用户时请标注搜索引擎搜索结果来源
                        -2. 如发现搜索结果中没有相关信息，明确告知用户""无法回答此问题""，不进行信息编造
                        -3. 确保回答准确、简洁、有条理
                        
                        ## 搜索结果
                        -{searchContext}
                        "
            ));

            historyMessages.Add(new ChatMessageContent(AuthorRole.User, message));

            // 保存用户消息
            await _dbService.SaveMessageAsync(new ChatMessageEntity
            {
                SessionId = sessionId,
                Role = "user",
                Content = message,
                CreateTime = DateTime.Now,
                ModelId = model.Id
            });

            // 用于收集AI完整回复
            var aiResponse = new StringBuilder();

            // 根据模型类型选择不同的处理方式
            switch (model.Type.ToLower())
            {
                case "local":
                    await foreach (var chunk in SendLocalMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "online":
                    await foreach (var chunk in SendSemanticKernelMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                case "weather":
                    await foreach (var chunk in SendMessageWithFunctionsAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
                default:
                    await foreach (var chunk in SendHttpMessageAsync(historyMessages, model))
                    {
                        aiResponse.Append(chunk);
                        yield return chunk;
                    }
                    break;
            }

            // 保存AI回复
            await _dbService.SaveMessageAsync(new ChatMessageEntity
            {
                SessionId = sessionId,
                Role = "assistant",
                Content = aiResponse.ToString(),
                CreateTime = DateTime.Now,
                ModelId = model.Id
            });

            if (!string.IsNullOrEmpty(sessionId))
            {
                await _dbService.UpdateModifyTimeAsync(sessionId);
            }

            // 处理新会话的情况
            if (string.IsNullOrEmpty(sessionId))
            {
                var messages = await _dbService.GetNoSessionMessageListAsync();
                var chatMessages = messages.Select(m => new ChatMessageContent
                {
                    Role = new AuthorRole(m.Role),
                    Content = m.Content,
                }).ToList();
                chatMessages.Add(new ChatMessageContent(AuthorRole.User, "用一句话为这段对话取个标题，字数不超过30个字"));
                var titleCotext = new StringBuilder();
                await foreach (var chunk in SendSemanticKernelMessageAsync(chatMessages, model))
                {
                    titleCotext.Append(chunk);
                }

                // 保存会话信息
                var chatSessionId = await _dbService.SaveMessageAsync(new ChatSessionEntity
                {
                    Title = titleCotext.ToString(),
                    CreateTime = DateTime.Now,
                    ModifyTime = DateTime.Now
                });

                await _dbService.UpdateMessageSessionIdAsync(chatSessionId.ToString());
            }
        }

        public async IAsyncEnumerable<string> TranslateMessageAsync(
           string lan,
           AIModel model,
           string originalContext)
        {
            var content = """
                                {{$input}}

                                你是一个专业的翻译助手,将上面的输入翻译成{{$language}}，无需任何其他内容
                                """;

            // 用于收集AI完整回复
            var aiResponse = new StringBuilder();

            // 根据模型类型选择不同的处理方式
            // 创建 OpenAI 内核
            var builder = Kernel.CreateBuilder();

            // 根据模型类型添加不同的服务
            if (model.Id.Contains("gpt"))
            {
                builder.AddOpenAIChatCompletion(
                    modelId: model.Id,
                    apiKey: model.ApiKey);
            }
            else
            {
                var httpClient = new HttpClient(new AiHttpClientHandler(model.ApiEndpoint));

                // 添加详细的请求头，确保与 OpenAI API 兼容
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // 使用完整配置的 HttpClient
                builder.AddOpenAIChatCompletion(
                    modelId: model.Id,
                    apiKey: model.ApiKey,
                    httpClient: httpClient);
            }

            var kernel = builder.Build();

            // 获取聊天服务
            await foreach (var update in kernel.InvokePromptStreamingAsync(
               content, new() { ["input"] = originalContext, ["language"] = lan }))
            {
                if (string.IsNullOrEmpty(update.ToString()))
                {
                    var jsonContent = JsonNode.Parse(ModelReaderWriter.Write(update.InnerContent!));
                    var choices = jsonContent!["choices"];
                    if (choices.ToString() != "[]")
                    {
                        var reasoningUpdate = jsonContent!["choices"]![0]!["delta"]!["reasoning_content"];

                        if (reasoningUpdate != null)
                            yield return reasoningUpdate.ToString();
                    }
                }
                else yield return update.ToString();
            }

        }

    }
}