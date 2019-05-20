using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
using System.Threading;

namespace ProxyServer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ProxyWindow : Window
    {
        static ProxyWindow theProxyWindow = null;
        ProxyListener theProxyListener = null;
        public string TextCtrl { get; set; } = string.Empty;
        private ProxyWindow()
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));

            theProxyListener = new ProxyListener();
            theProxyListener.Start();
        }

        static public ProxyWindow Create()
        {
            return theProxyWindow != null ? theProxyWindow : new ProxyWindow();
        }

        private void Click_ShowBlackList(object sender, RoutedEventArgs e)
        {
            WebDomainsView BannedDomains = new WebDomainsView(App.GetApp().BannedDomains);
            DomainEditor theDomainEditor = DomainEditor.Create(BannedDomains);
            theDomainEditor.Show();
        }

        private void Click_ShowLog(object sender, RoutedEventArgs e)
        {
            if(LoggingSession.IsEnabled)
            {
                // Collapse
                LoggingSession.Visibility = Visibility.Collapsed;
                LoggingSession.IsEnabled = false;
                buttonShowLog.Content = "Expand";
                ResizeAndCenter(500, 100);
            }
            else
            {
                // Expand
                LoggingSession.Visibility = Visibility.Visible;
                LoggingSession.IsEnabled = true;
                buttonShowLog.Content = "Collapse";
                ResizeAndCenter(1024, 768);
            }

        }
        protected void ResizeAndCenter(int width, int height)
        {
            Height = height;
            Width = width;
            Rect workArea = System.Windows.SystemParameters.WorkArea;
            Left = (workArea.Width - Width) / 2 + workArea.Left;
            Top = (workArea.Height - Height) / 2 + workArea.Top;
        }

        public void AddTextBlock(string msg, Brush foreground, Brush background)
        {
            // Create new text block
            TextBlock block = new TextBlock();
            block.Text = msg;
            block.Background = background;
            block.Foreground = foreground;
            block.Margin = new Thickness(0, 5, 0, 0);
            block.Style = (Style)FindResource("LogginggText");

            lock (stkLogging.Children)
            {
                stkLogging.Children.Add(block);
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < 100; i++)
                AddTextBlock("def", Brushes.Black, Brushes.Transparent);

            for (int i = stkLogging.Children.Count - 1; i >= 0; i--)
            {
                TextBlock child = (TextBlock)stkLogging.Children[i];
                if (child.Background == Brushes.Transparent)
                    child.Visibility = Visibility.Collapsed;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            theProxyListener.Stop();
        }
    }
}
