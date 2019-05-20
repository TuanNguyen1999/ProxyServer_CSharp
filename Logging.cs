using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace ProxyServer
{
    public enum LoggingLevel { Info =0, Warning = 1, Error = 2};
    public static class Logging
    {
        static public void Log(string msg,LoggingLevel level = LoggingLevel.Info)
        {
            if (string.IsNullOrEmpty(msg)) return;

            Brush foreground = null;
            Brush background = null;

            switch (level)
            {
                case LoggingLevel.Info:
                    foreground = Brushes.Black;
                    background = Brushes.Transparent;
                    break;
                case LoggingLevel.Warning:
                    foreground = Brushes.DarkBlue;
                    background = Brushes.Orange;
                    break;
                case LoggingLevel.Error:
                    foreground = Brushes.White;
                    background = Brushes.Red;
                    break;
                default:
                    throw new ArgumentException();
            }
            App.Current.Dispatcher.Invoke(() => { App.GetApp().TheProxyWindow.AddTextBlock(msg, foreground, background); });

            //App.GetApp().TheProxyWindow.AddTextBlock(msg, foreground, background);
        }
    }
}
