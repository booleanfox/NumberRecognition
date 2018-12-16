using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Diagnostics;

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

        public MagicEye()
        { }


        public void ProcessImage(Bitmap bitmap)
        {
            stopByErrors = false;

            //  Минимальная сторона изображения (обычно это высота)    
            if (bitmap.Height > bitmap.Width) throw new Exception("К такой забавной камере меня жизнь не готовила!");
            int side = bitmap.Height;

            original = new Bitmap(bitmap.Width, bitmap.Height);

            Rectangle cropRect = new Rectangle((bitmap.Width - bitmap.Height) / 2 + 40, 40, side, side);

            Graphics g = Graphics.FromImage(original);

            g.DrawImage(bitmap, new Rectangle(0, 0, original.Width, original.Height), cropRect, GraphicsUnit.Pixel);
            

            //  Конвертируем изображение в градации серого
            AForge.Imaging.Filters.Grayscale grayFilter = new AForge.Imaging.Filters.Grayscale(0.2125, 0.7154, 0.0721);
            processed = grayFilter.Apply(AForge.Imaging.UnmanagedImage.FromManagedImage(original));

            //  Масштабируем изображение до 500x500 - этого достаточно
            AForge.Imaging.Filters.ResizeBilinear scaleFilter = new AForge.Imaging.Filters.ResizeBilinear(500, 500);
            original = scaleFilter.Apply(original);


            stopByErrors = false;

            //  Пороговый фильтр применяем. Величина порога берётся из настроек, и меняется на форме
            AForge.Imaging.Filters.BradleyLocalThresholding threshldFilter = new AForge.Imaging.Filters.BradleyLocalThresholding();
            threshldFilter.PixelBrightnessDifferenceLimit = 0.15f;
            threshldFilter.ApplyInPlace(processed);

            cameraImg = processed;

            ///  Инвертируем изображение
            AForge.Imaging.Filters.Invert InvertFilter = new AForge.Imaging.Filters.Invert();
            InvertFilter.ApplyInPlace(cameraImg);
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
           Bitmap pic = cameraImg.ToManagedImage();
           return pic;
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
            double grad = (double)(geom_center.Item2 - mass_center.Item2) / (double)(geom_center.Item1 - mass_center.Item1);
          //  double rad = grad / 57.29;
            double angle = Math.Atan(Math.PI * grad / 180.0);

            //return (float)(angle * (180.0 / Math.PI));
            return (float)(Math.PI * grad / 180.0);

            // Сам поворот
     //       Bitmap result = new Bitmap(cameraImg.Width, cameraImg.Height);
       //    
         //   result = cameraImg.ToManagedImage();
           
            
            // Обрезка повёрнутого числа
        //    AForge.Imaging.Filters.ExtractBiggestBlob extractFfilter = new AForge.Imaging.Filters.ExtractBiggestBlob();
          //  number = extractFfilter.Apply(cameraImg.ToManagedImage());
          
        }
        
    }
}

