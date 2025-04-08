using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;
using JIm.ChatClient.Core.Models;
using System.Threading;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Connectors.AzureOpenAI;

namespace JIm.ChatClient.Core.Services
{
    public class EmbeddingService
    {
#pragma warning disable SKEXP0001
        private readonly IEmbeddingGenerationService<string, float> _embeddingService;
#pragma warning restore SKEXP0001

        public EmbeddingService(AIModel embeddingModel)
        {
            // 创建嵌入生成服务
            if (embeddingModel.Type.ToLower() == "local")
            {
                // 使用本地模型生成嵌入
                var httpClient = new System.Net.Http.HttpClient
                {
                    BaseAddress = new Uri(embeddingModel.ApiEndpoint)
                };

                _embeddingService = new OllamaTextEmbeddingGeneration(
                    httpClient,
                    embeddingModel.Id);
            }
            else
            {
                // 使用在线模型生成嵌入
                var httpClient = new System.Net.Http.HttpClient();
                httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {embeddingModel.ApiKey}");

                // 使用最新的 OpenAI 嵌入生成类
#pragma warning disable SKEXP0010
                _embeddingService = new AzureOpenAITextEmbeddingGenerationService(
                    deploymentName: embeddingModel.Id,
                    endpoint: embeddingModel.ApiEndpoint,
                    apiKey: embeddingModel.ApiKey);
#pragma warning restore SKEXP0010
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            try
            {
                var embeddings = await _embeddingService.GenerateEmbeddingsAsync(new List<string> { text });
                return embeddings[0].ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"生成嵌入向量时出错: {ex.Message}");
                return new float[1536]; // 返回空向量
            }
        }

        public float CalculateCosineSimilarity(float[] vector1, float[] vector2)
        {
            if (vector1.Length != vector2.Length)
                throw new ArgumentException("向量维度不匹配");

            float dotProduct = 0;
            float magnitude1 = 0;
            float magnitude2 = 0;

            for (int i = 0; i < vector1.Length; i++)
            {
                dotProduct += vector1[i] * vector2[i];
                magnitude1 += vector1[i] * vector1[i];
                magnitude2 += vector2[i] * vector2[i];
            }

            magnitude1 = (float)Math.Sqrt(magnitude1);
            magnitude2 = (float)Math.Sqrt(magnitude2);

            if (magnitude1 == 0 || magnitude2 == 0)
                return 0;

            return dotProduct / (magnitude1 * magnitude2);
        }
    }

}