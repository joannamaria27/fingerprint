using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;


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

        #region ZamianaBitmap
        private Bitmap BitmapImage2DBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);

                return new Bitmap(bitmap);
            }
        }
        public BitmapImage ConvertBitmapImage(Bitmap bitmap)
        {
            MemoryStream ms = new MemoryStream();
            bitmap.Save(ms, System.Drawing.Imaging.ImageFormat.Bmp);
            BitmapImage image = new BitmapImage();
            image.BeginInit();
            ms.Seek(0, SeekOrigin.Begin);
            image.StreamSource = ms;
            image.EndInit();

            return image;
        }
        #endregion

        private void BinaryzacjaAutomatyczna(Bitmap b)
        {
            //BitmapImage source = obrazek_2.Source as BitmapImage;
            //Bitmap b = BitmapImage2DBitmap(source);
            Szarosc(b);

            if (b != null)
            {
                Color curColor;
                int kolor = 0;
                int prog;
                prog = ProgOtsu(b);

                for (int i = 0; i < b.Width; i++)
                {
                    for (int j = 0; j < b.Height; j++)
                    {
                        curColor = b.GetPixel(i, j);
                        kolor = curColor.R;

                        if (kolor > prog)
                        {
                            kolor = 255;
                        }
                        else
                            kolor = 0;
                        b.SetPixel(i, j, Color.FromArgb(kolor, kolor, kolor));
                    }
                }
                obrazek.Source = ConvertBitmapImage(b);
            }
        }

        private int ProgOtsu(Bitmap b)
        {
            int[] histogram = new int[256];

            for (int m = 0; m < b.Width; m++)
            {
                for (int n = 0; n < b.Height; n++)
                {
                    System.Drawing.Color pixel = b.GetPixel(m, n);
                    histogram[pixel.R]++;
                }
            }

            long[] pob = new long[256];
            long[] pt = new long[256];

            for (int t = 0; t < 256; t++)
            {
                for (int t1 = 0; t1 <= t; t1++)
                    pob[t] += histogram[t1];
                for (int t1 = t + 1; t1 < 256; t1++)
                    pt[t] += histogram[t1];
            }

            double[] srOb = new double[256];
            double[] srT = new double[256];

            for (int t = 0; t < 256; t++)
            {
                for (int k = 0; k <= t; k++)
                    srOb[t] += (k * histogram[k]);// / pob[t];
                for (int k = t + 1; k < 256; k++)
                    srT[t] += (k * histogram[k]);/// pt[t];
            }

            for (int t = 0; t < 256; t++)
            {
                if (pob[t] != 0)
                    srOb[t] = srOb[t] / pob[t];
                if (pt[t] != 0)
                    srT[t] = srT[t] / pt[t];
            }

            double[] wariancjaMiedzy = new double[256];
            double maks = 0;

            for (int t = 0; t < 256; t++)
            {
                wariancjaMiedzy[t] = pob[t] * pt[t] * (srOb[t] - srT[t]) * (srOb[t] - srT[t]);
                //(pob[t] * Math.Pow(warOb[t], 2)) + (pt[t] * Math.Pow(warT[t], 2));
            }

            maks = 0;
            int x = 0;
            for (int w = 0; w < 256; w++)
            {
                if (wariancjaMiedzy[w] > maks)
                {
                    maks = wariancjaMiedzy[w];
                    x = w;
                }
            }
            return x;
        }
        private void Szarosc(Bitmap gmp)
        {
            BitmapImage source = obrazek_2.Source as BitmapImage;
            Bitmap bmp = BitmapImage2DBitmap(source);
            Color p;
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    p = bmp.GetPixel(x, y);

                    int a = p.A;
                    int r = p.R;
                    int g = p.G;
                    int b = p.B;
                    int avg = (r + g + b) / 3;

                    bmp.SetPixel(x, y, System.Drawing.Color.FromArgb(a, avg, avg, avg));
                }
            }
            obrazek_2.Source = ConvertBitmapImage(bmp);
            //obrazek.Source = ConvertBitmapImage(bmp);
        }


        private void Szkieletyzacja(object sender, RoutedEventArgs e)
        {
            BitmapImage source = obrazek_2.Source as BitmapImage;
            Bitmap b = BitmapImage2DBitmap(source);
            BinaryzacjaAutomatyczna(b);
            SzkieletyzacjaKMM(b);
        }
        private void SzkieletyzacjaKMM(Bitmap b) //po binaryzacji
        {
            int[] listaCzworek = { 3, 6, 7, 12, 14, 15, 24, 28, 30, 48, 56, 60, 96, 112, 120, 129, 131, 135, 192, 193, 195, 224, 225, 240 };
            int[,] maksaSprawdzajaca = { { 128, 64, 32 }, { 1, 0, 16 }, { 2, 4, 8 } };
            int[] maskaWyciec = { 3, 5, 7, 12, 13, 14, 15, 20, 21, 22, 23, 28, 29, 30, 31, 48, 52, 53, 54, 55, 56, 60, 61, 62, 63, 65, 67, 69, 71, 77, 79, 80, 81, 83, 84, 85, 86, 87, 88, 89, 91, 92, 93, 94, 95, 97, 99, 101, 103, 109, 111, 112, 113, 115, 116, 117, 118, 119, 120, 121, 123, 124, 125, 126, 127, 131, 133, 135, 141, 143, 149, 151, 157, 159, 181, 183, 189, 191, 192, 193, 195, 197, 199, 205, 207, 208, 209, 211, 212, 213, 214, 215, 216, 217, 219, 220, 221, 222, 223, 224, 225, 227, 229, 231, 237, 239, 240, 241, 243, 244, 245, 246, 247, 248, 249, 251, 252, 253, 254, 255 };
            List<int> czworki = new List<int>(listaCzworek); 
            List<int> wciecia = new List<int>(maskaWyciec);

            int dlugosc = 1;
            int[,] nowePixele = new int[b.Width, b.Height];
            for (int x = dlugosc; x < b.Width - dlugosc; x++)
            {
                for (int y = dlugosc; y < b.Height - dlugosc; y++)
                {
                    Color koloryOb = b.GetPixel(x, y);
                    if (koloryOb.R == 0) nowePixele[x, y] = 0;
                    else nowePixele[x, y] = 1;
                }
            }

            //int[,] nowszePixele= { { 0 } };
            // bool flaga = true;
            int r = 0;
            while (r != 155)
            {
                r++;

                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 1)
                        {
                            if (nowePixele[x + 1, y] == 0 || nowePixele[x, y + 1] == 0 || nowePixele[x, y - 1] == 0 || nowePixele[x - 1, y] == 0)
                            {
                                nowePixele[x, y] = 2;

                            }
                            else
                            {
                                if (nowePixele[x + 1, y + 1] == 0 || nowePixele[x - 1, y + 1] == 0 || nowePixele[x - 1, y - 1] == 0 || nowePixele[x + 1, y - 1] == 0)
                                {
                                    nowePixele[x, y] = 3;
                                }

                            }
                          
                        }
                    }
                }

                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 2)
                        {
                            nowePixele[x, y] = 0;
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    
                                    if (nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 2 || nowePixele[x + i, y + j] == 3)
                                    {
                                        
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];


                                    }
                                }
                            }
                           
                            if (czworki.Contains(nowePixele[x, y]))
                                nowePixele[x, y] = 4;
                            else
                                nowePixele[x, y] = 2;
                        }
                    }
                }

                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 4)
                        {
                            nowePixele[x, y] = 0;
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    if (nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 2 || nowePixele[x + i, y + j] == 3)
                                    {
                                        
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];


                                    }
                                  
                                }
                            }
                            if (wciecia.Contains(nowePixele[x, y]))
                                nowePixele[x, y] = 0;
                            else
                                nowePixele[x, y] = 1;
                        }

                    }
                }

                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 2)
                        {
                            nowePixele[x, y] = 0;
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    if (nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 4 || nowePixele[x + i, y + j] == 3)
                                    {
                                        
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];


                                    }
                                }
                            }
                            if (wciecia.Contains(nowePixele[x, y]))
                                nowePixele[x, y] = 0;
                            else
                                nowePixele[x, y] = 1;
                        }

                    }
                }

                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 3)
                        {
                            nowePixele[x, y] = 0;
                            for (int i = -1; i <= 1; i++)
                            {
                                for (int j = -1; j <= 1; j++)
                                {
                                    if (nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 2 || nowePixele[x + i, y + j] == 4)
                                    {
                                       
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];


                                    }
                                }
                            }
                            if (wciecia.Contains(nowePixele[x, y]))
                                nowePixele[x, y] = 0;
                            else
                                nowePixele[x, y] = 1;
                        }

                    }
                }




            }

            for (int x = dlugosc; x < b.Width - dlugosc; x++)
            {
                for (int y = dlugosc; y < b.Height - dlugosc; y++)
                {
                    if (nowePixele[x, y] == 1) nowePixele[x, y] = 255;
                    b.SetPixel(x, y, Color.FromArgb(nowePixele[x, y], nowePixele[x, y], nowePixele[x, y]));

                }
            }
            obrazek.Source = ConvertBitmapImage(b);

        }
    }
}
