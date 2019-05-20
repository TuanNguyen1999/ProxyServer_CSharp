using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.IO;
using System.Security.Cryptography;
using System.Collections.ObjectModel;

namespace ProxyServer
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public const string FileBannedDomains = "blacklist.conf";
        public const string CacheFolder = "cache";
        public const string TempFolder = "temp";

        public ProxyWindow TheProxyWindow { get; private set; } = null;
        public ObservableCollection<WebDomain> BannedDomains { get; set; } = new ObservableCollection<WebDomain>();
        private void Application_Startup(object sender, StartupEventArgs e)
        {
            try
            {
                //HashAlgorithm hash = HashAlgorithm.Create("SHA256");

                // Get a list of banned domains
                FileStream stream = File.OpenRead(FileBannedDomains);
                StreamReader ReadStream = new StreamReader(stream);
                
                while(!ReadStream.EndOfStream)
                {
                    string domain = ReadStream.ReadLine();
                    BannedDomains.Add(new WebDomain(domain));
                }
                ReadStream.Close();

                // Create cache folder
                if (!Directory.Exists(CacheFolder))
                    Directory.CreateDirectory(CacheFolder);

            }
            catch (FileNotFoundException)
            {
                File.OpenWrite(FileBannedDomains);
            }
            TheProxyWindow = ProxyWindow.Create();
            TheProxyWindow.ShowDialog();

            // Write Domains to file
            StreamWriter WriteStream = new StreamWriter(FileBannedDomains);
            
            foreach (var item in BannedDomains)
            {
                WriteStream.WriteLine(item.ToString());
            }
            WriteStream.Close();

        }

        public static App GetApp()
        {
            return (App)Current;
            
        }
    }
}
