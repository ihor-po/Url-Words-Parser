using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
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

        delegate void NewLine(TextBox tb, string line);

        NewLine anrl;

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
           

            Thread mainWorkingThread = new Thread(SendRequest);
            mainWorkingThread.Start(context);

        }

        private void SendRequest(object param)
        {
            try
            {
                SynchronizationContext _context = param as SynchronizationContext;

                _context.Send(ClearViewResult, null);
                //Создаем объект запроса
                //WebRequest request = WebRequest.Create(userUrl.Text);
                _context.Send(AddNewLine, ">>> Создаем объект запроса...");
                request = WebRequest.Create(url);

                //Получаем ответ от сервера
                _context.Send(AddNewLine, ">>> Получаем ответ от сервера...");
                response = request.GetResponse();

                if (response != null)
                {
                    _context.Send(ReadingRequest, _context);
                }
            }
            catch (Exception ex)
            {
                ShowErrorMessage(ex.Message);
            }
        }

        private void ReadingRequest(object obj)
        {
            SynchronizationContext _context = obj as SynchronizationContext;
           
            using (StreamReader reader = new StreamReader(response.GetResponseStream()))
            {
                _context.Send(ClearViewResult, null);

                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    _context.Send(AddNewLine, line);
                }
            }
        }

        //private void AddNewLine (TextBox tb ,string line)
        //{
        //    tb.AppendText(line + Environment.NewLine);
        //}
        private void AddNewLine (object line)
        {
            string str = line as string;
            viewResult.AppendText(line + Environment.NewLine);
        }

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
