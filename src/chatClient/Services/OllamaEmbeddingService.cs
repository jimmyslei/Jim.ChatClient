using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace chatClient.Services
{
    #pragma warning disable SKEXP0001
    public class OllamaTextEmbeddingGeneration : IEmbeddingGenerationService<string, float>
    #pragma warning restore SKEXP0001
    {
        private readonly HttpClient _httpClient;
        private readonly string _modelId;
        
        public OllamaTextEmbeddingGeneration(HttpClient httpClient, string modelId)
        {
            _httpClient = httpClient;
            _modelId = modelId;
        }

        // 实现 IAIService.Attributes 属性
        public IReadOnlyDictionary<string, object?> Attributes => new Dictionary<string, object?>
        {
            { "ModelId", _modelId },
            { "ServiceType", "Ollama Embedding" },
            { "EndpointType", "API" }
        };

        public async Task<IList<ReadOnlyMemory<float>>> GenerateEmbeddingsAsync(
            IList<string> texts, 
            Kernel? kernel = null, 
            CancellationToken cancellationToken = default)
        {
            var results = new List<ReadOnlyMemory<float>>();
            
            foreach (var text in texts)
            {
                var embedding = await GenerateSingleEmbeddingAsync(text, cancellationToken);
                results.Add(embedding);
            }
            
            return results;
        }
        
        private async Task<ReadOnlyMemory<float>> GenerateSingleEmbeddingAsync(
            string text, 
            CancellationToken cancellationToken)
        {
            var requestBody = new OllamaEmbeddingRequest
            {
                Model = _modelId,
                Prompt = text
            };
            
            var response = await _httpClient.PostAsJsonAsync("/api/embeddings", requestBody, cancellationToken);
            response.EnsureSuccessStatusCode();
            
            var result = await response.Content.ReadFromJsonAsync<OllamaEmbeddingResponse>(cancellationToken: cancellationToken);
            
            if (result?.Embedding == null)
                throw new InvalidOperationException("无法从Ollama获取嵌入向量");
                
            return new ReadOnlyMemory<float>(result.Embedding);
        }
        
        private class OllamaEmbeddingRequest
        {
            [JsonPropertyName("model")]
            public string Model { get; set; }
            
            [JsonPropertyName("prompt")]
            public string Prompt { get; set; }
        }
        
        private class OllamaEmbeddingResponse
        {
            [JsonPropertyName("embedding")]
            public float[] Embedding { get; set; }
        }
    }
} 