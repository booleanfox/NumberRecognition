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
        public Bitmap original;

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

            ///    Создаём BlobCounter, выдёргиваем самый большой кусок, масштабируем, пересечение и сохраняем
            ///    изображение в эксклюзивном использовании
            AForge.Imaging.BlobCounterBase bc = new AForge.Imaging.BlobCounter();

            bc.FilterBlobs = true;
            bc.MinWidth = 3;
            bc.MinHeight = 3;

            // Упорядочиваем по размеру
            bc.ObjectsOrder = AForge.Imaging.ObjectsOrder.Size;
            // Обрабатываем картинку

            bc.ProcessImage(cameraImg);

            // Извлекаем самый большой блоб
            AForge.Imaging.Blob[] blobs = bc.GetObjectsInformation();
            if (blobs.Length > 0)
            {
                bc.ExtractBlobsImage(cameraImg, blobs[0], true);
            }

        }
        
    }
}

