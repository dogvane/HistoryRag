using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HistoryRag
{
    /// <summary>
    /// 实用工具类，提供辅助方法。
    /// </summary>
    internal class Utils
    {
        /// <summary>
        /// 获取书名与其对应文件名的映射字典。
        /// </summary>
        /// <returns>包含文件名和对应书名的字典。</returns>
        static public Dictionary<string, string> GetBookName()
        {
            var mapping = new Dictionary<string, string>
                        {
                            { "baihuabeiqishu.txt", "北齐书" },
                            { "baihuabeishi.txt", "北史" },
                            { "baihuachenshu.txt", "陈书" },
                            { "baihuahanshu.txt", "汉书" },
                            { "baihuahouhanshu.txt", "后汉书" },
                            { "baihuajinshi.txt", "金史" },
                            { "baihuajinshu.txt", "晋书" },
                            { "baihuajiutangshu.txt", "旧唐书" },
                            { "baihuajiuwudaishi.txt", "旧五代史" },
                            { "baihualiangshu.txt", "梁书" },
                            { "baihualiaoshi.txt", "辽史" },
                            { "baihuamingshi.txt", "明史" },
                            { "baihuananqishu.txt", "南齐书" },
                            { "baihuananshi.txt", "南史" },
                            { "baihuasanguozhi.txt", "三国志" },
                            { "baihuashiji.txt", "史记" },
                            { "baihuasongshi.txt", "宋史" },
                            { "baihuasongshu.txt", "宋书" },
                            { "baihuasuishu.txt", "隋史" },
                            { "baihuaweishu.txt", "魏书" },
                            { "baihuaxintangshi.txt", "新唐史" },
                            { "baihuaxinwudaishi.txt", "新五代史" },
                            { "baihuayuanshi.txt", "元史" },
                            { "baihuazhoushu.txt", "周书" }
                        };
            return mapping;
        }

        /// <summary>
        /// 获取书名与其对应文件名的映射字典。
        /// </summary>
        /// <returns>包含文件名和对应书名的字典。</returns>
        static public Dictionary<string, string> GetReversedBookName()
        {
            var original = GetBookName();
            var reversed = original.ToDictionary(pair => pair.Value, pair => pair.Key);

            return reversed;
        }

        public static string FindBooName(string fileName)
        {
            var map = GetBookName();
            if(map.TryGetValue(fileName, out var ret))
            {
                return ret;
            }

            return fileName;
        }
    }

}
