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

namespace ProxyServer
{
    /// <summary>
    /// Interaction logic for BlackList.xaml
    /// </summary>
    public partial class DomainEditor : Window
    {
        static DomainEditor theDomainEditor = null;
        public WebDomainsView ViewModel { get; set; }
        private DomainEditor(WebDomainsView viewmodel = null)
        {
            InitializeComponent();
            Style = (Style)FindResource(typeof(Window));

            if (viewmodel != null)
                ViewModel = viewmodel;
            else ViewModel = new WebDomainsView();
            DataContext = ViewModel;
        }

        static public DomainEditor Create(WebDomainsView items = null)
        {
            return theDomainEditor != null ? theDomainEditor : new DomainEditor(items);
        }
        private void KeyUp_DeleteItems(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete && listboxDisplayDomain.SelectedItems.Count != 0)
            {
                IsEnabled = false;


                int i = listboxDisplayDomain.SelectedIndex;

                while (i >= 0)
                {
                    ViewModel.RemoveAt(i);
                    i = listboxDisplayDomain.SelectedIndex;
                }

                IsEnabled = true;
            }
        }

        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxInput.Text) || string.IsNullOrWhiteSpace(textBoxInput.Text)) return;
            try
            {
                ViewModel.Add(new WebDomain(textBoxInput.Text));
            }
            catch (ArgumentException)
            {
                MessageBox.Show("Invalid Domain", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void ButtonRemove_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(textBoxInput.Text) || string.IsNullOrWhiteSpace(textBoxInput.Text)) return;
            try
            {
                ViewModel.Remove(new WebDomain(textBoxInput.Text));
            }
            catch (ArgumentException)
            {

            }
        }
    }
}
