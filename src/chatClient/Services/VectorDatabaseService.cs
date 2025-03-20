using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using LiteDB;
using chatClient.Models;
using static chatClient.Services.DocumentProcessingService;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;

namespace chatClient.Services
{
    public class VectorDatabaseService
    {
        private readonly string _dbPath;
        private readonly EmbeddingService _embeddingService;
        
        public VectorDatabaseService(string dbPath, AIModel embeddingModel)
        {
            _dbPath = dbPath;
            _embeddingService = new EmbeddingService(embeddingModel);
            
            // 确保数据库目录存在
            Directory.CreateDirectory(Path.GetDirectoryName(dbPath));
            
            // 初始化数据库
            using (var db = new LiteDatabase(_dbPath))
            {
                // 创建必要的集合和索引
                var chunks = db.GetCollection<DocumentChunkEntity>("chunks");
                chunks.EnsureIndex(x => x.KnowledgeBaseId);
                chunks.EnsureIndex(x => x.DocumentId);
            }
        }
        
        public async Task StoreDocumentChunksAsync(
            string knowledgeBaseId, 
            string documentId, 
            List<DocumentChunk> chunks,
            ProgressReportHandler progressReport = null)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var chunksCollection = db.GetCollection<DocumentChunkEntity>("chunks");
                
                // 删除文档的现有块
                chunksCollection.DeleteMany(c => c.DocumentId == documentId);
                
                // 存储新块
                int totalChunks = chunks.Count;
                for (int i = 0; i < totalChunks; i++)
                {
                    var chunk = chunks[i];
                    
                    // 生成嵌入向量
                    if (chunk.Embedding == null)
                    {
                        chunk.Embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);
                    }
                    
                    var chunkEntity = new DocumentChunkEntity
                    {
                        Id = chunk.Id,
                        KnowledgeBaseId = knowledgeBaseId,
                        DocumentId = documentId,
                        Content = chunk.Content,
                        ChunkIndex = chunk.ChunkIndex,
                        Embedding = chunk.Embedding
                    };
                    
                    chunksCollection.Insert(chunkEntity);
                    
                    // 报告进度
                    progressReport?.Invoke((double)(i + 1) / totalChunks);
                }
            }
        }
        
        public async Task<List<RelevantChunk>> SearchSimilarChunksAsync(string knowledgeBaseId, string query, int topK = 5)
        {
            // 为查询生成嵌入向量
            float[] queryEmbedding = await _embeddingService.GenerateEmbeddingAsync(query);
            
            using (var db = new LiteDatabase(_dbPath))
            {
                var chunksCollection = db.GetCollection<DocumentChunkEntity>("chunks");
                
                // 获取知识库中的所有块
                var allChunks = chunksCollection.Find(c => c.KnowledgeBaseId == knowledgeBaseId).ToList();
                
                // 计算相似度并排序
                var rankedChunks = allChunks
                    .Select(chunk => new RelevantChunk
                    {
                        ChunkId = chunk.Id,
                        DocumentId = chunk.DocumentId,
                        Content = chunk.Content,
                        Similarity = _embeddingService.CalculateCosineSimilarity(queryEmbedding, chunk.Embedding)
                    })
                    .OrderByDescending(c => c.Similarity)
                    .Take(topK)
                    .ToList();
                
                return rankedChunks;
            }
        }
        
        public void DeleteDocumentChunks(string documentId)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var chunksCollection = db.GetCollection<DocumentChunkEntity>("chunks");
                chunksCollection.DeleteMany(c => c.DocumentId == documentId);
            }
        }
        
        public void DeleteKnowledgeBaseChunks(string knowledgeBaseId)
        {
            using (var db = new LiteDatabase(_dbPath))
            {
                var chunksCollection = db.GetCollection<DocumentChunkEntity>("chunks");
                chunksCollection.DeleteMany(c => c.KnowledgeBaseId == knowledgeBaseId);
            }
        }

        // 使用LiteDB实现AddDocumentChunksAsync方法
        public async Task AddDocumentChunksAsync(
            string knowledgeBaseId,
            string documentId,
            List<DocumentChunk> chunks,
            Action<double> progressReport = null)
        {
            int total = chunks.Count;
            int processed = 0;
            
            using (var db = new LiteDatabase(_dbPath))
            {
                var chunksCollection = db.GetCollection<DocumentChunkEntity>("chunks");
                
                // 删除文档的现有块
                chunksCollection.DeleteMany(c => c.DocumentId == documentId);

                // 存储新块
                foreach (var chunk in chunks)
                {
                    try
                    {
                        // 生成嵌入向量
                        float[] embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Content);

                        var chunkEntity = new DocumentChunkEntity
                        {
                            Id = chunk.Id,
                            KnowledgeBaseId = knowledgeBaseId,
                            DocumentId = documentId,
                            Content = chunk.Content,
                            ChunkIndex = chunk.ChunkIndex,
                            Embedding = embedding,
                            //Metadata = Newtonsoft.Json.JsonSerializer.Serialize(chunk.Metadata)
                        };

                        chunksCollection.Insert(chunkEntity);

                        processed++;
                        progressReport?.Invoke((double)processed / total);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error processing chunk: {ex.Message}");
                        // 继续处理其他块
                    }
                }
            }
        }

        // 添加向量序列化和反序列化的辅助方法
        private byte[] SerializeVector(float[] vector)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(ms))
                {
                    // 写入向量长度
                    writer.Write(vector.Length);
                    
                    // 写入每个浮点数
                    foreach (float value in vector)
                    {
                        writer.Write(value);
                    }
                }
                return ms.ToArray();
            }
        }

        private float[] DeserializeVector(byte[] bytes)
        {
            using (MemoryStream ms = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(ms))
                {
                    // 读取向量长度
                    int length = reader.ReadInt32();
                    
                    // 读取每个浮点数
                    float[] vector = new float[length];
                    for (int i = 0; i < length; i++)
                    {
                        vector[i] = reader.ReadSingle();
                    }
                    
                    return vector;
                }
            }
        }
    }
    
    public class DocumentChunkEntity
    {
        public string Id { get; set; }
        public string KnowledgeBaseId { get; set; }
        public string DocumentId { get; set; }
        public string Content { get; set; }
        public int ChunkIndex { get; set; }
        public float[] Embedding { get; set; }
        public string Metadata { get; set; } // 存储序列化后的元数据
    }
    
    public class RelevantChunk
    {
        public string ChunkId { get; set; }
        public string DocumentId { get; set; }
        public string Content { get; set; }
        public float Similarity { get; set; }
    }
} 