using System.Windows;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.IO;
using Path = System.IO.Path;
using HistoryRag.RAG;
using System.Text.Json.Serialization;
using System.Text.Json;
using MessagePack;
using static System.Reflection.Metadata.BlobBuilder;
using System.Numerics.Tensors;
using System.Windows.Media;
using System.Windows.Input;
using System.Globalization;
using System.Windows.Data;
using System.Text;
using Ollama;

namespace HistoryRag;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        dataDirectory = Path.Combine(AppContext.BaseDirectory, "data/history_24");
        if(!Directory.Exists(dataDirectory))
        {
            dataDirectory = Path.Combine(AppContext.BaseDirectory, "../../../data/history_24");
        }

        EnvCheck();

        LoadBooks();
    }

    RagDocument doc;

    string dataDirectory;

    private void LoadBooks()
    {
        if (Directory.Exists(dataDirectory))
        {
            var files = Directory.GetFiles(dataDirectory, "*.txt");
            foreach (var file in files)
            {
                var fileName = new FileInfo(file).Name;

                BooksComboBox.Items.Add(Utils.FindBooName(fileName));
            }
        }
    }

    private void BooksComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // 在这里处理选择更改事件
        string selectedBook = BooksComboBox.SelectedItem.ToString();
        string txtFilePath = Path.Combine(dataDirectory, selectedBook);
        if (!File.Exists(txtFilePath))
        {
            txtFilePath = Path.Combine(dataDirectory, Utils.GetReversedBookName()[selectedBook]);
        }

        var ragFile = Path.Combine(dataDirectory, "msgpack", txtFilePath.Replace(".txt", ".rag"));

        if (File.Exists(ragFile))
        {
            doc = MessagePackSerializer.Deserialize<RagDocument>(File.OpenRead(ragFile));
        }
        else
        {
            var bookTxt = File.ReadAllText(txtFilePath);
            doc = new SentenceWindowNodeParser(chunkSize: 100).GetNodesFromDocuments(bookTxt);
            doc.Name = selectedBook;

            var bytes = MessagePackSerializer.Serialize(doc);
            File.WriteAllBytes(ragFile, bytes);
        }

        var embedding = doc.Nodes.Count(o => o.ChunkEmbedding != null);

        // 显示文档信息
        DocumentInfoTextBlock.Text = $"书名：{doc.Name}\n" +
            $"分块数量：{doc.Nodes.Count}\n" +
            $"已向量化：{embedding}\n" +
            $"总字数：{doc.Nodes.Sum(n => n.Chunk.Length)}";
    }

    private async void Embedding_Click(object sender, RoutedEventArgs e)
    {
        // 对书籍做向量化操作
        if (BooksComboBox.SelectedItem == null)
        {
            MessageBox.Show("请选择一本书籍。");
            return;
        }

        string selectedBook = BooksComboBox.SelectedItem.ToString();
        string txtFilePath = Path.Combine(dataDirectory, selectedBook);
        if (!File.Exists(txtFilePath))
        {
            txtFilePath = Path.Combine(dataDirectory, Utils.GetReversedBookName()[selectedBook]);
        }

        var ragFile = Path.Combine(dataDirectory, "msgpack", txtFilePath.Replace(".txt", ".rag"));

        if (File.Exists(ragFile))
        {
            using var stream = File.OpenRead(ragFile);
            doc = MessagePackSerializer.Deserialize<RagDocument>(stream);
        }
        else
        {
            MessageBox.Show("书籍打开失败。");
            return;
        }

        var embedding = doc.Nodes.Count(o => o.ChunkEmbedding != null);

        if (embedding < doc.Nodes.Count)
        {
            // 还一些文本没有完成编码，需要进行一次异步编码
            var progress = new Progress<int>(p =>
            {
                // 更新界面上的进度信息
                EncodingProgressTextBlock.Text = $"编码中：{p}%";
            });

            await Task.Run(() =>
            {
                LLMUtils.ConvertEmbedding(doc, progress);
            });

            var bytes = MessagePackSerializer.Serialize(doc);
            File.WriteAllBytes(ragFile, bytes);
        }
    }
    private void MessageTextBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            Button_Click(BtnSend, new RoutedEventArgs());
        }
    }

    /// <summary>
    /// 用户点击发送按钮
    /// 构建提示词，并发送给模型
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private async void Button_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(MessageTextBox.Text))
            return;

        if (doc == null)
        {
            MessageBox.Show("请先选择一本书籍。");
            return;
        }

        try
        {
            BtnSend.IsEnabled = false;            
            MessageTextBox.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait; // 设置鼠标指针为等待手势

            var queryTxt = MessageTextBox.Text;

            ChatListBox.Items.Add(new ChatMessage
            {
                Message = queryTxt,
                IsQuestion = true,
            });

            // 先走一个不带RAG的提问
            var zeroRet = await LLMUtils.Chat(queryTxt);
            ChatListBox.Items.Add(new ChatMessage
            {
                Message = zeroRet,
                IsQuestion = false,
            });

            // 查询当前选择的历史书
            var findItem = await Query(queryTxt, doc);

            // 构建第一次查询提示词模板
            var firstQuery = Prompts.QAPromptTemplate(findItem.Window, queryTxt);
            var firstRet = await LLMUtils.Chat(firstQuery);

            ChatListBox.Items.Add(new ChatMessage
            {
                Message = firstRet,
                IsQuestion = false,
                ToolTipContent = findItem.Window
            });

            // 构建二次查询提示词模板，对第一次的结果做确认或者微调
            var secendQuery = Prompts.RefinePromptTemplate(findItem.Window, queryTxt, firstRet);
            var result = await LLMUtils.Chat(secendQuery);

            ChatListBox.Items.Add(new ChatMessage
            {
                Message = result,
                IsQuestion = false,
                ToolTipContent = secendQuery
            });

            MessageTextBox.Text = string.Empty;
        }
        finally
        {
            BtnSend.IsEnabled = true;
            MessageTextBox.IsEnabled = true;
            Mouse.OverrideCursor = null; // 恢复默认鼠标指针
        }
    }

    public class ChatMessage
    {
        public string Message { get; set; }
        public bool IsQuestion { get; set; }
        public int Column => IsQuestion ? 0 : 1;
        public Brush Background => IsQuestion ? Brushes.LightGray : Brushes.LightBlue;
        public string ToolTipContent { get; set; }
    }

    private async Task<RagDocument.Node> Query(string queryTxt, RagDocument ragDocument)
    {
        var embed = await LLMUtils.GetEmbedding(queryTxt);
        RagDocument.Node findItem = null;
        float current_distance = float.MaxValue;

        foreach (var item in ragDocument.Nodes)
        {
            if (item.ChunkEmbedding == null || item.ChunkEmbedding.Length == 0)
                continue;

            var distance = TensorPrimitives.Distance(embed.AsSpan(), item.ChunkEmbedding.AsSpan());

            if (distance < current_distance)
            {
                current_distance = distance;
                findItem = item;
            }
        }

        return findItem;
    }

    void EnvCheck()
    {
        Task.Run(() =>
        {
            try
            {
                var envCheck = _envCheck();
                Dispatcher.Invoke(() =>
                {
                    ChatListBox.Items.Add(new ChatMessage
                    {
                        Message = envCheck,
                        IsQuestion = false,
                    });
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        });
    }
    string _envCheck()
    {
        // 对系统进行自检
        StringBuilder ret = new StringBuilder();

        if (!Directory.Exists(dataDirectory))
        {
            ret.AppendLine("数据目录不存在：" + dataDirectory);
        }
        else
        {
            var files = Directory.GetFiles(dataDirectory, "*.txt");
            ret.AppendLine("找到 " + files.Length + " 本书籍。");
        }

        OllamaApiClient api = new OllamaApiClient();
        var models = api.Models.ListModelsAsync();
        models.Wait();
        if (models.Result.Models.Count > 0)
        {
            ret.AppendLine("找到 " + models.Result.Models.Count + " 个模型。");

            foreach (var model in models.Result.Models)
            {
                ret.AppendLine("模型：" + model.Model1);
            }
        }
        else
        {
            ret.AppendLine("未找到任何模型。");
        }
        return ret.ToString();
    }
}
public class WidthConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values[0] is double actualWidth)
        {
            return actualWidth - 20;
        }
        return 0;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}