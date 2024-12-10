using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace HistoryRag.RAG
{
    /// <summary>
    /// 句子窗口节点解析器，用于将文档分割成带有窗口上下文的节点。
    /// </summary>
    public class SentenceWindowNodeParser
    {
        private int windowSize;
        private int chunkSize;

        /// <summary>
        /// 初始化 <see cref="SentenceWindowNodeParser"/> 类的新实例。
        /// </summary>
        /// <param name="sentenceSplitter">用于分割句子的函数委托。</param>
        /// <param name="windowSize">窗口大小，即每个节点包含的上下文句子数量。</param>
        /// <param name="includeMetadata">是否包含元数据。</param>
        /// <param name="includePrevNextRel">是否包含前后关系信息。</param>
        public SentenceWindowNodeParser(
            int windowSize = 3,
            int chunkSize = 300
            )
        {
            this.windowSize = windowSize;
            this.chunkSize = chunkSize;
        }


        /// <summary>
        /// 从文档集合中获取节点列表。
        /// </summary>
        /// <param name="documents">要处理的文档集合。</param>
        /// <param name="showProgress">是否显示处理进度。</param>
        /// <returns>包含所有节点的列表。</returns>
        public RagDocument GetNodesFromDocuments(string text)
        {
            var doc = new RagDocument();
            doc.Nodes = new List<RagDocument.Node>();

                var textSplits = SplitBySentenceTokenizer(text);
                var nodes = BuildNodesFromSplits(textSplits);

                for (int i = 0; i < nodes.Count; i++)
                {
                    var start = Math.Max(0, i - windowSize);
                    var end = Math.Min(i + windowSize + 1, nodes.Count);
                    var windowNodes = nodes.Skip(start).Take(end - start).ToList();

                    nodes[i].Window = string.Join(" ", windowNodes.Select(n => n.Chunk));
                }

                doc.Nodes.AddRange(nodes);

            return doc;
        }

        /// <summary>
        /// 根据文本分割结果和文档信息构建节点。
        /// </summary>
        /// <param name="textSplits">分割后的句子列表。</param>
        /// <param name="doc">当前处理的文档。</param>
        /// <returns>构建的节点列表。</returns>
        private List<RagDocument.Node> BuildNodesFromSplits(List<string> textSplits)
        {
            var nodes = new List<RagDocument.Node>();
            var currentChunk = new StringBuilder();

            foreach (var sentence in textSplits)
            {
                if (string.IsNullOrWhiteSpace(sentence))
                    continue;

                // 如果当前块的长度加上新句子的长度超过了 chunkSize，则创建一个新的节点
                if (currentChunk.Length + sentence.Length > chunkSize)
                {
                    var node = new RagDocument.Node
                    {
                        Chunk = currentChunk.ToString().Trim(),
                    };
                    nodes.Add(node);
                    currentChunk.Clear();
                }

                // 将句子添加到当前块中
                currentChunk.Append(sentence).Append(" ");
            }

            // 如果当前块中仍有内容，则创建一个新的节点
            if (currentChunk.Length > 0)
            {
                var node = new RagDocument.Node
                {
                    Chunk = currentChunk.ToString().Trim(),
                };
                nodes.Add(node);
            }

            return nodes;
        }

        /// <summary>
        /// 使用句子分词器将文本分割为句子列表。
        /// </summary>
        /// <param name="text">要分割的文本。</param>
        /// <returns>分割后的句子列表。</returns>
        private List<string> SplitBySentenceTokenizer(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return new List<string>();

            // 使用正则表达式根据中文和英文的句子结束符进行分割
            // 如果。符号之后跟着中文的“”符号，则不视为断句
            var pattern = @"[^,.;。？！“”]+[,.;。？！](?![""“”])";
            var sentences = Regex.Matches(text, pattern)
                                 .Cast<Match>()
                                 .Select(m => m.Value.Trim())
                                 .ToList();
            return sentences;
        }
    }
}