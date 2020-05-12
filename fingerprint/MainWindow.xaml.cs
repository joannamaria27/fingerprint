using Microsoft.Win32;
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

namespace fingerprint
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            obrazek_2.Source = obrazek.Source;
        }
        private void ZaladujZPliku(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.png;*.jpg;*.bmp;*.gif;*.tif;*.tiff;*.jpeg;)|*.png;*.jpg;*.bmp;*.gif;*.tif;*.tiff;*.jpeg; | All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    Uri fileUri = new Uri(openFileDialog.FileName);
                    obrazek.Source = new BitmapImage(fileUri);
                    obrazek_2.Source = obrazek.Source;
                }
                catch (NotSupportedException)
                {
                    MessageBox.Show("Zły format pliku!", "Wczytywanie z pliku", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
        }
    }
}
