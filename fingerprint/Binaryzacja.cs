using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Drawing;

namespace fingerprint
{
    public partial class MainWindow
    {
        private System.Drawing.Bitmap BitmapImage2DBitmap(BitmapImage bitmapImage)
        {
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                System.Drawing.Bitmap bitmap = new System.Drawing.Bitmap(outStream);

                return new System.Drawing.Bitmap(bitmap);
            }
        }
        public BitmapImage ConvertBitmapImage(System.Drawing.Bitmap bitmap)
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
        private void Szarosc(System.Drawing.Bitmap gmp)
        {
            BitmapImage source = obrazek_2.Source as BitmapImage;
            System.Drawing.Bitmap bmp = BitmapImage2DBitmap(source);
            System.Drawing.Color p;
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
        private void BinaryzacjaAutomatyczna(object sender, RoutedEventArgs e)
        {

            BitmapImage source = obrazek_2.Source as BitmapImage;
            System.Drawing.Bitmap b = BitmapImage2DBitmap(source);
            Szarosc(b);

            if (b != null)
            {
                System.Drawing.Color curColor;
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
                        b.SetPixel(i, j, System.Drawing.Color.FromArgb(kolor, kolor, kolor));

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
                {
                    pob[t] += histogram[t1];
                }

                for (int t1 = t + 1; t1 < 256; t1++)
                {
                    pt[t] += histogram[t1];
                }
            }

            double[] srOb = new double[256];
            double[] srT = new double[256];

            for (int t = 0; t < 256; t++)
            {
                for (int k = 0; k <= t; k++)
                {
                    srOb[t] += (k * histogram[k]);// / pob[t];

                }
                for (int k = t + 1; k < 256; k++)
                {

                    srT[t] += (k * histogram[k]);/// pt[t];
                }
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
    }
}
