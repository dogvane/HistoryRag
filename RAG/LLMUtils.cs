using Ollama;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoryRag.RAG
{
    internal class LLMUtils
    {
        static OllamaApiClient api = new OllamaApiClient();

        /// <summary>
        /// 获得文本的编码
        /// bge-m3 的长度是1024 
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public async static Task<float[]> GetEmbedding(string text)
        {
            var embed = await api.Embeddings.GenerateEmbeddingAsync("bge-m3", text);
            var codes = embed.Embedding.Select(o => (float)o).ToArray();
            return codes;
        }

        public static async Task<string> Chat(string prompt)
        {
            var chat = new Chat(api, "glm4:latest");
            var ret = await chat.SendAsync(prompt);
            return ret.Content;
        }

        public static async void ConvertEmbedding(RagDocument doc, IProgress<int> progress)
        {
            var len = doc.Nodes.Count;
            for(var i =0;i < len; i++)
            {
                var item = doc.Nodes[i];
                if (item.ChunkEmbedding != null && item.ChunkEmbedding.Length > 0)
                    continue;

                item.ChunkEmbedding = await GetEmbedding(item.Chunk);

                if (progress != null)
                {
                    // 报告进度
                    int percentComplete = (i + 1) * 100 / len;
                    progress.Report(percentComplete);
                }
            }
        }
    }
}
