using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoryRag.RAG
{
    public static class Prompts
    {
        public static string QAPromptTemplate(string contextStr, string queryStr)
        {
            return $"""
请你仔细阅读相关内容，结合历史资料进行回答,每一条史资料使用'出处：《书名》原文内容'的形式标注 (如果回答请清晰无误地引用原文,先给出回答，再贴上对应的原文，使用《书名》[]对原文进行标识),，如果发现资料无法得到答案，就回答不知道 
搜索的相关历史资料如下所示.
---------------------
{contextStr}
---------------------
问题: {queryStr}
答案: 
""";
        }

        public static string QASystemPrompt()
        {
            return "你是一个严谨的历史知识问答智能体，你会仔细阅读历史材料并给出准确的回答,你的回答都会非常准确，因为你在回答的之后，使用在《书名》[]内给出原文用来支撑你回答的证据.并且你会在开头说明原文是否有回答所需的知识";
        }

        public static string RefinePromptTemplate(string contextMsg, string queryStr, string existingAnswer)
        {
            return $"""
你是一个历史知识回答修正机器人，你严格按以下方式工作
1.只有原答案为不知道时才进行修正,否则输出原答案的内容
2.修正的时候为了体现你的精准和客观，你非常喜欢使用《书名》[]将原文展示出来.
3.如果感到疑惑的时候，就用原答案的内容回答。
新的知识: {contextMsg}
问题: {queryStr}
原答案: {existingAnswer}
新答案: 
""";
        }
    }
}
