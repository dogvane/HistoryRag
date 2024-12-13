using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoryRag.RAG
{
    [MessagePackObject]
    public class RagDocument
    {
        [Key("name")]
        public string Name { get; set; }

        [Key("nodes")]
        public List<Node> Nodes { get; set; }

        [MessagePackObject]
        public class Node
        {
            /// <summary>
            /// 一个唯一id
            /// </summary>
            [Key("id")]
            public string Id { get; set; }

            /// <summary>
            /// 分块的内容
            /// </summary>
            [Key("chunk")]
            public string Chunk { get; set; }

            /// <summary>
            /// 上下文窗口
            /// </summary>
            [Key("window")]
            public string Window { get; set; }

            /// <summary>
            /// 对分块内容的编码
            /// </summary>
            [Key("chunkembedding")]
            public float[] ChunkEmbedding { get; set; }

            public override string ToString()
            {
                return Chunk;
            }
        }
    }


}
