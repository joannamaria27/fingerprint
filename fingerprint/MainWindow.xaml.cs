﻿using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Security.Cryptography.X509Certificates;
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

        #region Odczyt/Zapis
        private void ZaladujZPliku(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "Image files (*.png;*.jpg;*.bmp;*.gif;*.tif;*.tiff;*.jpeg;)|*.png;*.jpg;*.bmp;*.gif;*.tif;*.tiff;*.jpeg; | All files (*.*)|*.*"
            };
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
        private void ZapiszDoPliku(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "All supported graphics|*.jpg;*.jpeg;*.png|" +
                          "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg|" +
                          "Portable Network Graphic (*.png)|*.png"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                FileStream saveStream = new FileStream(saveFileDialog.FileName, FileMode.OpenOrCreate);
                BmpBitmapEncoder encoder = new BmpBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create((BitmapImage)obrazek.Source));
                encoder.Save(saveStream);
                saveStream.Close();
            }
        }
        #endregion

        #region ZamianaBitmap
        private Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            using MemoryStream outStream = new MemoryStream();
            BitmapEncoder enc = new BmpBitmapEncoder();
            enc.Frames.Add(BitmapFrame.Create(bitmapImage));
            enc.Save(outStream);
            Bitmap bitmap = new Bitmap(outStream);

            return new Bitmap(bitmap);
        }
        public BitmapImage BitmapToBitmapImage(Bitmap bitmap)
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

        #region Binaryzacja

        private void ZamianaNaOdcienSzarosci(Bitmap bmp)
        {
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

                    bmp.SetPixel(x, y, Color.FromArgb(a, avg, avg, avg));
                }
            }
            obrazek_2.Source = BitmapToBitmapImage(bmp);
        }
        private void BinaryzacjaAutomatyczna(Bitmap b) //otsu
        {
            ZamianaNaOdcienSzarosci(b);
            if (b != null)
            {
                Color curColor;
                int kolor;
                int prog;
                prog = ObliczanieProgOtsu(b);

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
                obrazek.Source = BitmapToBitmapImage(b);
            }
        }
        private int ObliczanieProgOtsu(Bitmap b)
        {
            int[] histogram = new int[256];
            for (int m = 0; m < b.Width; m++)
            {
                for (int n = 0; n < b.Height; n++)
                {
                    Color pixel = b.GetPixel(m, n);
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
                    srOb[t] += (k * histogram[k]);
                for (int k = t + 1; k < 256; k++)
                    srT[t] += (k * histogram[k]);
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
                wariancjaMiedzy[t] = pob[t] * pt[t] * (srOb[t] - srT[t]) * (srOb[t] - srT[t]);
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
        #endregion

        #region Szkieletyzacja
        private void BinaryzacjaISzkieletyzacja(object sender, RoutedEventArgs e)
        {
            BitmapImage source = obrazek_2.Source as BitmapImage;
            Bitmap b = BitmapImageToBitmap(source);
            BinaryzacjaAutomatyczna(b);
            KMM(b);
        }
        private void KMM(Bitmap b)
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
                    if (koloryOb.R == 0) nowePixele[x, y] = 1;
                    else nowePixele[x, y] = 0;
                }
            }

            Boolean zmiana = false;
            do
            {
                zmiana = false;
                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 1)
                        {
                            if (nowePixele[x + 1, y] == 0 || nowePixele[x, y + 1] == 0 || nowePixele[x, y - 1] == 0 || nowePixele[x - 1, y] == 0)
                                nowePixele[x, y] = 2;
                        }
                    }
                }

                for (int x = dlugosc; x < b.Width - dlugosc; x++)
                {
                    for (int y = dlugosc; y < b.Height - dlugosc; y++)
                    {
                        if (nowePixele[x, y] == 1)
                        {
                            if (nowePixele[x + 1, y + 1] == 0 || nowePixele[x - 1, y + 1] == 0 || nowePixele[x - 1, y - 1] == 0 || nowePixele[x + 1, y - 1] == 0)
                                nowePixele[x, y] = 3;
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
                                    if (nowePixele[x + i, y + j] == 4 || nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 2 || nowePixele[x + i, y + j] == 3)
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];
                                }
                            }
                            if (czworki.Contains(nowePixele[x, y]))
                            {
                                nowePixele[x, y] = 4;
                                zmiana = true;
                            }
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
                                    if (nowePixele[x + i, y + j] == 4 || nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 2 || nowePixele[x + i, y + j] == 3)
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];
                                }
                            }
                            if (wciecia.Contains(nowePixele[x, y]))
                            {
                                nowePixele[x, y] = 0;
                                zmiana = true;
                            }
                            else
                            {
                                nowePixele[x, y] = 1;
                                // zmiana = true;
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
                                    if (nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 4 || nowePixele[x + i, y + j] == 3 || nowePixele[x + i, y + j] == 2)
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];
                                }
                            }
                            if (wciecia.Contains(nowePixele[x, y]))
                            {
                                nowePixele[x, y] = 0;
                                zmiana = true;
                            }
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
                                    if (nowePixele[x + i, y + j] == 3 || nowePixele[x + i, y + j] == 1 || nowePixele[x + i, y + j] == 2 || nowePixele[x + i, y + j] == 4)
                                        nowePixele[x, y] += maksaSprawdzajaca[1 + i, 1 + j];
                                }
                            }
                            if (wciecia.Contains(nowePixele[x, y]))
                            {
                                nowePixele[x, y] = 0;
                                zmiana = true;
                            }
                            else
                                nowePixele[x, y] = 1;
                        }
                    }
                }
            } while (zmiana == true);

            for (int x = dlugosc; x < b.Width - dlugosc; x++)
            {
                for (int y = dlugosc; y < b.Height - dlugosc; y++)
                {
                    if (nowePixele[x, y] == 0) nowePixele[x, y] = 255;
                    if (nowePixele[x, y] == 1) nowePixele[x, y] = 0;
                    b.SetPixel(x, y, Color.FromArgb(nowePixele[x, y], nowePixele[x, y], nowePixele[x, y]));
                }
            }
            obrazek.Source = BitmapToBitmapImage(b);
        }

        #endregion


        private void Rozwidlenia(object sender, RoutedEventArgs e)  //razem z binaryzacja i szkieletyzacją  ------ póżniej zmienić i przycisk aktywny tylko po wykonaniu szkieletyczacji czy coś takiego --- do przemyslenia
        {
            BitmapImage source = obrazek_2.Source as BitmapImage;
            Bitmap b = BitmapImageToBitmap(source);
            BinaryzacjaAutomatyczna(b);
            KMM(b);
            SzukanieMinucji(b);
        }

        private void SzukanieMinucji(Bitmap b)
        {
            Bitmap bb = b;
            int dlugosc = 2;
            int[,] cn = new int[b.Width, b.Height];
            int[,] nowePixele = new int[b.Width, b.Height];
            int[,,] filtr = new int[10000, b.Width, b.Height];
            for (int x = dlugosc; x < b.Width - dlugosc; x++)
            {
                for (int y = dlugosc; y < b.Height - dlugosc; y++)
                {
                    Color koloryOb = b.GetPixel(x, y);
                    if (koloryOb.R == 0) nowePixele[x, y] = 1;
                    else nowePixele[x, y] = 0;
                }
            }

            for (int x = dlugosc; x < b.Width - dlugosc; x++)
            {
                for (int y = dlugosc; y < b.Height - dlugosc; y++)
                {
                    if (nowePixele[x, y] == 1)
                    {
                        cn[x, y] = Math.Abs(((nowePixele[x, y + 1] - nowePixele[x - 1, y + 1]) + //1-2

                              (nowePixele[x - 1, y + 1] - nowePixele[x - 1, y]) + //2-3
                              (nowePixele[x - 1, y] - nowePixele[x - 1, y - 1]) + //3-4
                              (nowePixele[x - 1, y - 1] - nowePixele[x, y - 1]) + //4-5
                              (nowePixele[x, y - 1] - nowePixele[x + 1, y - 1]) + //5-6
                              (nowePixele[x + 1, y - 1] - nowePixele[x + 1, y]) + //6-7
                              (nowePixele[x + 1, y] - nowePixele[x + 1, y + 1]) + //7-8
                             (nowePixele[x + 1, y + 1] - nowePixele[x, y + 1])) / 2); //8-1    
                    }
                }
            }

            cn = distancefilter(cn, 0);
            cn = distancefilter(cn, 1);
            cn = distancefilter(cn, 3);
            cn = distancefilter(cn, 4);


            for (int x = dlugosc; x < b.Width - dlugosc; x++)
            {
                for (int y = dlugosc; y < b.Height - dlugosc; y++)
                {
                    if (cn[x, y] == 0) //pojedyńczy punkt - minucja
                    {
                        bb.SetPixel(x - 2, y - 2, Color.FromArgb(0, 0, 255));  //obrysowanie - kwadrat o sodku 3x3   ------ czy obrysowanie czy może zmiana koloru tych minucji z czarnego na inny?????
                        bb.SetPixel(x - 2, y - 1, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x - 2, y, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x - 2, y + 1, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x - 2, y + 2, Color.FromArgb(0, 0, 255));

                        bb.SetPixel(x - 1, y - 2, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x - 1, y + 2, Color.FromArgb(0, 0, 255));

                        bb.SetPixel(x, y - 2, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x, y + 2, Color.FromArgb(0, 0, 255));

                        bb.SetPixel(x + 1, y - 2, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x + 1, y + 2, Color.FromArgb(0, 0, 255));

                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x + 2, y - 1, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x + 2, y, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x + 2, y + 1, Color.FromArgb(0, 0, 255));
                        bb.SetPixel(x + 2, y + 2, Color.FromArgb(0, 0, 255));

                    }
                    if (cn[x, y] == 1) //zakończenie krawędzi - minucja
                    {
                        bb.SetPixel(x - 2, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x - 2, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x - 2, y, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x - 2, y + 1, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x - 2, y + 2, Color.FromArgb(255, 0, 255));


                        bb.SetPixel(x - 1, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x - 1, y + 2, Color.FromArgb(255, 0, 255));

                        bb.SetPixel(x, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x, y + 2, Color.FromArgb(255, 0, 255));

                        bb.SetPixel(x + 1, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x + 1, y + 2, Color.FromArgb(255, 0, 255));


                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x + 2, y, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x + 2, y + 1, Color.FromArgb(255, 0, 255));
                        bb.SetPixel(x + 2, y + 2, Color.FromArgb(255, 0, 255));
                    }
                    if (cn[x, y] == 3) //rozwidlenie - minucja

                    {
                        /*for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (nowePixele[x + i, y + j] == 1)
                                    bb.SetPixel(x + i, y + j, Color.FromArgb(255, 0, 0));
                            }
                        }*/

                        bb.SetPixel(x - 2, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x - 2, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x - 2, y, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x - 2, y + 1, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x - 2, y + 2, Color.FromArgb(0, 255, 0));

                        bb.SetPixel(x - 1, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x - 1, y + 2, Color.FromArgb(0, 255, 0));

                        bb.SetPixel(x, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x, y + 2, Color.FromArgb(0, 255, 0));

                        bb.SetPixel(x + 1, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x + 1, y + 2, Color.FromArgb(0, 255, 0));

                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x + 2, y, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x + 2, y + 1, Color.FromArgb(0, 255, 0));
                        bb.SetPixel(x + 2, y + 2, Color.FromArgb(0, 255, 0));
                    }
                    if (cn[x, y] == 4) //skrzyżowanie - minucja
                    {
                        /*for (int i = -1; i <= 1; i++)
                        {
                            for (int j = -1; j <= 1; j++)
                            {
                                if (nowePixele[x + i, y + j] == 1)
                                    bb.SetPixel(x + i, y + j, Color.FromArgb(255, 0, 0));
                            }
                        }*/

                        b.SetPixel(x - 2, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x - 2, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x - 2, y, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x - 2, y + 1, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x - 2, y + 2, Color.FromArgb(255, 255, 0));

                        bb.SetPixel(x - 1, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x - 1, y + 2, Color.FromArgb(255, 255, 0));

                        bb.SetPixel(x, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x, y + 2, Color.FromArgb(255, 255, 0));

                        bb.SetPixel(x + 1, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x + 1, y + 2, Color.FromArgb(255, 255, 0));

                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x + 2, y - 2, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x + 2, y, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x + 2, y + 1, Color.FromArgb(255, 255, 0));
                        bb.SetPixel(x + 2, y + 2, Color.FromArgb(255, 255, 0));
                    }
                }
            }
            obrazek.Source = BitmapToBitmapImage(bb);
        }
        private int[,] distancefilter(int[,] cn, int choice)
        {
            for (int i = 0; i < cn.GetLength(0); i++)
            {
                for (int j = 0; j < cn.GetLength(1); j++)
                {
                    if (cn[i, j] == choice) cn = maskSeventoSeven(cn, i, j, choice);
                }

            }
            return cn;
        }
        private int[,] maskSeventoSeven(int[,] cn, int x, int y, int choice)
        {
            int edgeLeft = x - 3, edgeRight = x + 3;
            if (edgeLeft < 0) edgeLeft = 0;
            int sizeX = cn.GetLength(0);
            if (edgeRight >= cn.GetLength(0)) edgeRight = (sizeX - 1);

            int edgeTop = y - 3, edgeDown = y + 3;
            if (edgeTop < 0) edgeTop = 0;
            if (edgeDown >= cn.GetLength(1)) edgeDown = (cn.GetLength(1) - 1);
            for (int i = edgeLeft; i <= edgeRight; i++)
            {
                for (int j = edgeTop; j <= edgeDown; j++)
                {
                    if (cn[i, j] == choice && i != x || j != y)
                        cn[i, j] = 2;
                }
            }
            return cn;
        }
        private void Przefiltrowanie(object sender, RoutedEventArgs e)
        {
            BitmapImage source = obrazek_2.Source as BitmapImage;
            Bitmap b = BitmapImageToBitmap(source);
            BinaryzacjaAutomatyczna(b);
            KMM(b);
            SzukanieMinucji(b);
            UsuwanieFalszywychMinucji(b);
        }


        private void UsuwanieFalszywychMinucji(Bitmap b)
        {

            obrazek.Source = BitmapToBitmapImage(b);
        }

    }
}






