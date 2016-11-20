using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace misoiLab1
{
    public partial class Form1 : Form
    {
        public Bitmap openImage;
        private Bitmap lastStateImage;
        public Bitmap outputImageMedF;
        public Bitmap outputImageMonF;
        public Bitmap outputNegative;
        private Bitmap lastStateNegative;
        public Color[,] allPixels;
        String filename;
        public int width;
        public int height;
            
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            openFileDialog1.Filter = "Image Files(*.BMP;*.JPG;*.GIF;*.PNG;*.JPEG)|*.BMP;*.JPG;*.GIF;*.PNG;*.JPEG|All files (*.*)|*.*";
            if (openFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                filename = openFileDialog1.FileName;
                openImage = new Bitmap(openFileDialog1.FileName);
                width = openImage.Width;
                height = openImage.Height;
                lastStateImage = openImage;
                pictureBox1.Image = openImage;
                pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
                pictureBox1.Invalidate();
            }
        }

        public Color[,] GetPixels(Bitmap image)
        {
            Color[,] allPixels = new Color[image.Width - 1, image.Height - 1];
            for (int i = 0; i < (image.Width - 1); i++)
            {
                for (int j = 0; j < image.Height - 1; j++)
                {
                    allPixels[i, j] = image.GetPixel(i, j);
                }
            }
            return allPixels;
        }
        public Color SearchMedianPixel(int x, int y, int size)
        {
            Color pixel;
            int[] arrayR = new int[size * size];
            int[] arrayG = new int[size * size];
            int[] arrayB = new int[size * size];
            int countPixel = 0;
            for (int i = x-size/2; i < x+size/2+1; i++)
            {
                for (int j = y-size/2; j < y+size/2+1; j++)
                {
                    arrayR[countPixel] = allPixels[i, j].R;
                    arrayG[countPixel] = allPixels[i, j].G;
                    arrayB[countPixel] = allPixels[i, j].B;
                    countPixel++;
                }
            }
            Array.Sort(arrayR);
            Array.Sort(arrayG);
            Array.Sort(arrayB);
            pixel = Color.FromArgb(arrayR[size * size/2], arrayG[size * size/2], arrayB[size * size/2]);
            return pixel;
        }

        public Bitmap ApplyMedianFilter(Color[,] pixels, int size)
        {
            outputImageMedF = new Bitmap(lastStateImage.Width - size/2 +1, lastStateImage.Height - size / 2 + 1);
            Color medianPixel;
            for (int i = size / 2; i < lastStateImage.Width - (size / 2 + 1); i++)
            {
                for (int j = size / 2; j < lastStateImage.Height - (size / 2 + 1) ; j++)
                {
                    medianPixel = SearchMedianPixel(i, j, size);
                    outputImageMedF.SetPixel(i- size / 2, j- size / 2, medianPixel);
                }
            }
            return outputImageMedF;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            allPixels = GetPixels(lastStateImage);
            int size;
            try
            {
                size = Convert.ToInt32(textBox1.Text);
            } catch
            {
                size = 7;
            }
            lastStateImage = ApplyMedianFilter(allPixels, size);
            pictureBox2.Image = lastStateImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox2.Invalidate();
        }

        ///////////////////////////////////////// FOR OTSU
        private float Px(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += hist[i];

            return (float)sum;
        }

        // function is used to compute the mean values in the equation (mu)
        private float Mx(int init, int end, int[] hist)
        {
            int sum = 0;
            int i;
            for (i = init; i <= end; i++)
                sum += i * hist[i];

            return (float)sum;
        }

        // finds the maximum element in a vector
        private int findMax(float[] vec, int n)
        {
            float maxVec = 0;
            int idx = 0;
            int i;

            for (i = 1; i < n - 1; i++)
            {
                if (vec[i] > maxVec)
                {
                    maxVec = vec[i];
                    idx = i;
                }
            }
            return idx;
        }

        // simply computes the image histogram
        unsafe private void getHistogram(byte* p, int w, int h, int ws, int[] hist)
        {
            hist.Initialize();
            for (int i = 0; i < h; i++)
            {
                for (int j = 0; j < w * 3; j += 3)
                {
                    int index = i * ws + j;
                    hist[p[index]]++;
                }
            }
        }

        // find otsu threshold
        public int getOtsuThreshold(Bitmap bmp)
        {
            byte t = 0;
            float[] vet = new float[256];
            int[] hist = new int[256];
            vet.Initialize();

            float p1, p2, p12;
            int k;

            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();

                getHistogram(p, bmp.Width, bmp.Height, bmData.Stride, hist);

                // loop through all possible t values and maximize between class variance
                for (k = 1; k != 255; k++)
                {
                    p1 = Px(0, k, hist);
                    p2 = Px(k + 1, 255, hist);
                    p12 = p1 * p2;
                    if (p12 == 0)
                        p12 = 1;
                    float diff = (Mx(0, k, hist) * p2) - (Mx(k + 1, 255, hist) * p1);
                    vet[k] = (float)diff * diff / p12;
                    
                }
            }
            bmp.UnlockBits(bmData);

            t = (byte)findMax(vet, 256);

            return t;
        }

        // simple routine to convert to gray scale
        public void Convert2GrayScaleFast(Bitmap bmp)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
                    ImageLockMode.ReadWrite, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();
                int stopAddress = (int)p + bmData.Stride * bmData.Height;
                while ((int)p != stopAddress)
                {
                    p[0] = (byte)(.299 * p[2] + .587 * p[1] + .114 * p[0]);
                    p[1] = p[0];
                    p[2] = p[0];
                    p += 3;
                }
            }
            bmp.UnlockBits(bmData);
        }

        // simple routine for thresholdin
        public void threshold(Bitmap bmp, int thresh)
        {
            BitmapData bmData = bmp.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height),
            ImageLockMode.ReadOnly, PixelFormat.Format24bppRgb);
            unsafe
            {
                byte* p = (byte*)(void*)bmData.Scan0.ToPointer();
                int h = bmp.Height;
                int w = bmp.Width;
                int ws = bmData.Stride;

                for (int i = 0; i < h; i++)
                {
                    byte* row = &p[i * ws];
                    for (int j = 0; j < w * 3; j += 3)
                    {
                        row[j] = (byte)((row[j] > (byte)thresh) ? 255 : 0);
                        row[j + 1] = (byte)((row[j + 1] > (byte)thresh) ? 255 : 0);
                        row[j + 2] = (byte)((row[j + 2] > (byte)thresh) ? 255 : 0);
                    }
                }
            }
            bmp.UnlockBits(bmData);
        }
        ///////////////////////////////////////////////

        public Bitmap ApplyMonochrom(int t)
        {
            int threshold;
            if (t != 256) {
                threshold = t;
            } else
            {
                threshold = getOtsuThreshold(lastStateImage);
            }


            outputImageMonF = new Bitmap(lastStateImage.Width, lastStateImage.Height);
            
            Color color = new Color();
            try
            {
                for (int i = 0; i < lastStateImage.Width; i++) 
                {
                    for (int j = 0; j < lastStateImage.Height; j++)
                    {
                        //color = openImage.GetPixel(i, j);
                        //color = outputImageMedF.GetPixel(i, j);
                        color = lastStateImage.GetPixel(i, j);
                        int medianValueColors = (color.R + color.G + color.B) / 3;
                        outputImageMonF.SetPixel(i, j, (medianValueColors <= threshold ? Color.Black : Color.White));
                    }   
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return outputImageMonF;
        }
        private void button3_Click(object sender, EventArgs e)
        {
            try {
                lastStateImage = ApplyMonochrom(Convert.ToInt32(textBox2.Text));
            } catch
            {
                lastStateImage = ApplyMonochrom(256);
            }
            pictureBox2.Image = lastStateImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
            //allPixels = GetPixels(outputImage);
          
        }

        public Bitmap ApplyNegativeFilter()
        {
            outputNegative = new Bitmap(lastStateImage);
            Color color = new Color();
            Color negativeColor = new Color();

            for (int i=0; i<lastStateImage.Width; i++)
            {
                for (int j=0; j<lastStateImage.Height; j++)
                {
                    //color = openImage.GetPixel(i, j);
                    color = lastStateImage.GetPixel(i, j);
                    negativeColor = Color.FromArgb(255 - color.R, 255 - color.G, 255 - color.B);
                    outputNegative.SetPixel(i, j, negativeColor);
                }
            }

            return outputNegative;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            pictureBox2.Image = ApplyNegativeFilter();
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            outputNegative.Save("D:\\9сем\\misoiLab1\\new_.jpg", ImageFormat.Jpeg);



        }

        private Bitmap sobel(Bitmap image)
        {
            //Bitmap b = new Bitmap(pictureBox1.Image);
            //Bitmap bb = new Bitmap(pictureBox1.Image);
            Bitmap b = image;
            Bitmap bb = new Bitmap(image.Width, image.Height);
            int width = b.Width;
            int height = b.Height;
            int[,] gx = new int[,] { { -1, 0, 1 }, { -2, 0, 2 }, { -1, 0, 1 } };
            int[,] gy = new int[,] { { 1, 2, 1 }, { 0, 0, 0 }, { -1, -2, -1 } };

            int[,] allPixR = new int[width, height];
            int[,] allPixG = new int[width, height];
            int[,] allPixB = new int[width, height];

            int limit = 128 * 128;

            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    allPixR[i, j] = b.GetPixel(i, j).R;
                    allPixG[i, j] = b.GetPixel(i, j).G;
                    allPixB[i, j] = b.GetPixel(i, j).B;
                }
            }

            int new_rx = 0, new_ry = 0;
            int new_gx = 0, new_gy = 0;
            int new_bx = 0, new_by = 0;
            int rc, gc, bc;
            for (int i = 1; i < b.Width - 1; i++)
            {
                for (int j = 1; j < b.Height - 1; j++)
                {

                    new_rx = 0;
                    new_ry = 0;
                    new_gx = 0;
                    new_gy = 0;
                    new_bx = 0;
                    new_by = 0;
                    rc = 0;
                    gc = 0;
                    bc = 0;

                    for (int wi = -1; wi < 2; wi++)
                    {
                        for (int hw = -1; hw < 2; hw++)
                        {
                            rc = allPixR[i + hw, j + wi];
                            new_rx += gx[wi + 1, hw + 1] * rc;
                            new_ry += gy[wi + 1, hw + 1] * rc;

                            gc = allPixG[i + hw, j + wi];
                            new_gx += gx[wi + 1, hw + 1] * gc;
                            new_gy += gy[wi + 1, hw + 1] * gc;

                            bc = allPixB[i + hw, j + wi];
                            new_bx += gx[wi + 1, hw + 1] * bc;
                            new_by += gy[wi + 1, hw + 1] * bc;
                        }
                    }
                    if (new_rx * new_rx + new_ry * new_ry > limit || new_gx * new_gx + new_gy * new_gy > limit || new_bx * new_bx + new_by * new_by > limit)
                        bb.SetPixel(i, j, Color.White);
                    //bb.SetPixel (i, j, Color.FromArgb(allPixR[i,j],allPixG[i,j],allPixB[i,j]));
                    else
                        bb.SetPixel(i, j, Color.Black);
                }
            }
            return bb;
            
        }
        private void button6_Click(object sender, EventArgs e)
        {
            //sobel(outputImageMonF);
            lastStateImage = sobel(lastStateImage);
            pictureBox2.Image = lastStateImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        }

        private void button7_Click(object sender, EventArgs e)
        {
            lastStateImage = openImage;
            pictureBox2.Image = lastStateImage;
            pictureBox2.SizeMode = PictureBoxSizeMode.StretchImage;
        }
    }
}
