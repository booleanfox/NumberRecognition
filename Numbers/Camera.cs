using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;
using AForge.Imaging;
using AForge.Video;
using AForge.Video.DirectShow;

namespace Numbers
{
   

    class MagicEye
    {
        /// <summary>
        /// Изображение, получаемое из кадра
        /// </summary>
        public AForge.Imaging.UnmanagedImage cameraImg;

        /// <summary>
        /// Оригинальное изображение и обработанное
        /// </summary>
        public AForge.Imaging.UnmanagedImage processed;
        public Bitmap original, number;

        public int errorCount = 0;
        public bool stopByErrors = false;

        public float ThresholdValue = 0;

        public MagicEye()
        { }


        public void ProcessImage(Bitmap bitmap)
        {
            stopByErrors = false;

            //  Минимальная сторона изображения (обычно это высота)    
            //if (bitmap.Height > bitmap.Width) throw new Exception("К такой забавной камере меня жизнь не готовила!");
            int side =Math.Min(bitmap.Height,bitmap.Width);

            // обрезка фида с камера
            original = new Bitmap(bitmap.Width, bitmap.Height);
            Rectangle cropRect = new Rectangle(0, 0 ,side, side);
            Graphics g = Graphics.FromImage(original);
            g.DrawImage(bitmap, new Rectangle(0, 0, bitmap.Width, bitmap.Height), cropRect, GraphicsUnit.Pixel);
            

            //  Конвертируем изображение в градации серого
            AForge.Imaging.Filters.Grayscale grayFilter = new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);
            var processed = grayFilter.Apply(AForge.Imaging.UnmanagedImage.FromManagedImage(original));

            //  Масштабируем изображение до 300x300 - этого достаточно
            AForge.Imaging.Filters.ResizeBilinear scaleFilter = new AForge.Imaging.Filters.ResizeBilinear(300, 300);
            original = scaleFilter.Apply(original);


            stopByErrors = false;

            //  Пороговый фильтр применяем. Величина порога берётся из настроек, и меняется на форме
            AForge.Imaging.Filters.BradleyLocalThresholding threshldFilter = new AForge.Imaging.Filters.BradleyLocalThresholding();
            threshldFilter.PixelBrightnessDifferenceLimit = ThresholdValue;
            threshldFilter.ApplyInPlace(processed);

            

            //  Инвертируем изображение, пустота черная
            AForge.Imaging.Filters.Invert InvertFilter = new AForge.Imaging.Filters.Invert();
            InvertFilter.ApplyInPlace(processed);

            

            cameraImg = processed;

        }

        public Bitmap recogniseNumber()
        {
            Bitmap pic;

            AForge.Imaging.Filters.ExtractBiggestBlob extractFfilter = new AForge.Imaging.Filters.ExtractBiggestBlob();
            pic = extractFfilter.Apply(cameraImg.ToManagedImage());
            number = pic;
            return pic;
        }

        
        // Запасной вариант, нехороший
        public Bitmap recogniseNumber2()
        {
            Bitmap pic;

            AForge.Imaging.BlobCounterBase bc = new AForge.Imaging.BlobCounter();

            bc.FilterBlobs = true;
            bc.MinWidth = 5;
            bc.MinHeight = 5;
      
            bc.ObjectsOrder = AForge.Imaging.ObjectsOrder.Size;
  
            bc.ProcessImage(cameraImg);
            AForge.Imaging.Blob[] blobs = bc.GetObjectsInformation();
      
            if (blobs.Length > 0)
            {
                bc.ExtractBlobsImage(cameraImg, blobs[0], true);
            }

            pic = cameraImg.ToManagedImage();
            return pic;
        }

        public Bitmap showPicture()
        {
            //if(cameraImg!= null)
                return  cameraImg.ToManagedImage();
           // return new Bitmap(1, 1);
        }

        public float rotateAngle()
        {
            recogniseNumber();

            // Центр масс
            Tuple<int, int> mass_center;
            // Геометрический центр
            Tuple<int, int> geom_center;
  
            int w = number.Width;
            int h = number.Height;

            geom_center = new Tuple<int, int>(w / 2, h / 2);

            int x = 0, y = 0;
            for (int i = 0; i < w; i++)
                for (int j = 0; j < h; j++)
                {
                    Color clr = number.GetPixel(i, j);
                    if (clr.R == 255 && clr.G == 255 && clr.B == 255)
                    {
                        x += i / w; 
                        y += j / h;
                    }
                }
            mass_center = new Tuple<int, int>(x, y);

            // Угол наклона вектора <геом.центр -- центр масс>
            double k = (double)(geom_center.Item2 - mass_center.Item2) / (double)(geom_center.Item1 - mass_center.Item1);
            return (float)(Math.Atan(k) * 180 / Math.PI);

 
           
            
            // Обрезка повёрнутого числа
        //    AForge.Imaging.Filters.ExtractBiggestBlob extractFfilter = new AForge.Imaging.Filters.ExtractBiggestBlob();
          //  number = extractFfilter.Apply(cameraImg.ToManagedImage());
          
        }
        
    }
}

