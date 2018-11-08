using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace wordsParser
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        WebResponse response;
        WebRequest request;
        SynchronizationContext context; //Контекст синхронизации потока
        string url;

        List<Word> wd;

        const string tags = @"(<.*?>)|(<img.*?>)|<img.*>|(&ndash;)|(&copy;)|(&nbsp;)|(&bull;)|<!--.*-->|(&quot;)|(&gt;)";
        const string script = @"<script\b[^<]*(?:(?!<\/script>)<[^<]*)*<\/script>";
        const string style = @"<style\b[^<]*(?:(?!<\/style>)<[^<]*)*<\/style>";
        const string path = @"<path\b[^<]*(?:<[^<]*)*\/>";

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        /// <summary>
        /// Loaded main page
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            context = SynchronizationContext.Current;

            wd = new List<Word>();

            // Add columns
            var gridView = new GridView();
            viewWords.View = gridView;
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Слова",
                DisplayMemberBinding = new Binding("WD"),
                Width = 200
                
            });
            gridView.Columns.Add(new GridViewColumn
            {
                Header = "Количество повторений",
                DisplayMemberBinding = new Binding("Quantity")
            });

            goBtn.Click += GoBtn_Click;
        }

        /// <summary>
        /// Sending request to user url
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GoBtn_Click(object sender, RoutedEventArgs e)
        {
            if (userUrl.Text.Length == 0)
            {
                ShowErrorMessage("Вы не ввели адрес для запроса");
                return;
            }

            url = userUrl.Text;
           
            if (wd.Count > 0)
            {
                wd.Clear();
                viewWords.Items.Clear();
            }

            Thread mainWorkingThread = new Thread(SendRequest);
            mainWorkingThread.Start(context);
        }

        /// <summary>
        /// Отправка запроса
        /// </summary>
        /// <param name="param"></param>
        private void SendRequest(object param)
        {
            SynchronizationContext _context = param as SynchronizationContext;
            try
            {
                _context.Post(_ => viewResult.Clear(), null);
                //
                //Создаем объект запроса
                _context.Post(AddNewLine, ">>> Создаем объект запроса ...");
                request = WebRequest.Create(url);
                request.Timeout = 10000;

                SynchronizationContext c = SynchronizationContext.Current;

                //Получаем ответ от сервера
                _context.Post(AddNewLine, ">>> Получаем ответ от сервера ...");
                response = request.GetResponse();

                if  (response != null)
                {
                    _context.Post(AddNewLine, ">>> Обработка данных ...");
                    _context.Post(ReadingRequest, _context);
                }
                
            }
            catch (Exception ex)
            {
                _context.Post(AddNewLine, ex.Message);
                Thread.Sleep(5000);
                ShowErrorMessage(ex.Message);
            }
        }

        /// <summary>
        /// Чтение ответа
        /// </summary>
        /// <param name="obj"></param>
        private void ReadingRequest(object obj)
        {
            SynchronizationContext _context = obj as SynchronizationContext;
            try
            {
                using (StreamReader reader = new StreamReader(response.GetResponseStream()))
                {
                    _context.Post(ClearViewResult, null);

                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        _context.Post(AddNewLine, line);
                    }
                }

                _context.Post( GetWordFromText, _context);
            }
            catch (Exception ex)
            {
                _context.Post(AddNewLine, ex.Message);
                Thread.Sleep(5000);
            }
            
        }

        private void GetWordFromText(object obj)
        {
            SynchronizationContext _context = obj as SynchronizationContext;

            string pageText = viewResult.Text;
            pageText = Regex.Replace(pageText, script, " ");
            pageText = Regex.Replace(pageText, style, " ");
            pageText = Regex.Replace(pageText, tags, " ");
            pageText = Regex.Replace(pageText, path, " ");
            pageText = Regex.Replace(pageText, @"{.*}", " ");
            SplitWords(pageText);
            ShowWords();
        }

        /// <summary>
        /// Show words with quantity
        /// </summary>
        private void ShowWords()
        {
            foreach (var item in wd)
            {
                viewWords.Items.Add(item);
            }
        }

        /// <summary>
        /// Spliting word
        /// </summary>
        /// <param name="text"></param>
        private void SplitWords(string text)
        {
            //array of symbols for split text
            char[] chrs = {
                ',', ':', '.', ';', ' ', '\n', '\r', '\t', '!', '%', '^', '*', '-', '_', '=', '?',
                '<', '>', '/', '\\', '|', '[', ']', '{', '}', '\'', '(', ')', '"'
            };

            string[] words = text.Split(chrs);

            foreach (string item in words)
            {
                if (item != "" && item != " ")
                {
                    Word w = wd.FirstOrDefault(_item => _item.WD.ToUpper() == item.ToUpper());

                    if (w == null)
                    {
                        wd.Add(new Word(item));
                    }
                    else
                    {
                        w.Quantity++;
                    }
                }
            }

            wd = wd.OrderByDescending(q => q.Quantity).ToList();
        }

        /// <summary>
        /// Add new line to the result field
        /// </summary>
        /// <param name="line"></param>
        private void AddNewLine (object line)
        {
            string str = line as string;
            viewResult.AppendText(line + Environment.NewLine);
        }

        ///// <summary>
        ///// Add new line to the field with words
        ///// </summary>
        ///// <param name="line"></param>
        //private void AddResLine(object line)
        //{
        //    string str = line as string;
        //    viewWords.AppendText(line + Environment.NewLine);
        //}

        private void ClearViewResult(object obj) { viewResult.Clear(); }

        /// <summary>
        /// Show Error message
        /// </summary>
        /// <param name="error"></param>
        private void ShowErrorMessage(string error)
        {
            MessageBox.Show(error, this.Title, MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}
