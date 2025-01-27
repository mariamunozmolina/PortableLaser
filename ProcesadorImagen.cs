using System;
using System.Collections.Generic;

using System.Drawing.Drawing2D;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//using System.Web.UI.WebControls;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using OpenCvSharp;
using OpenCvSharp.Dnn;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.LinkLabel;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TreeView;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;


namespace MobiFreq
{
    internal class ProcesadorImagen
    {
        const int RADIO = 200;//radio de análisis en torno al centro
        public ProcesadorImagen()
        {
        }

        internal Point BuscarMarca(Mat miImagen, bool marcaInicial, Point centro )
        {
            //La marca mide menos de 50 píxeles en 1920x1080
            //Mat recortada = new Mat();

            //Convertir a escala de grises
            Mat grayImage = new Mat();
            Cv2.CvtColor(miImagen, grayImage, ColorConversionCodes.BGR2GRAY);
            //Cv2.ImShow("Gris", miImagen);

            Mat BW = new Mat();
            //Cv2.Canny(grayImage, BW, 0.05 * 255, 0.15 * 255);
            //Cv2.Threshold(grayImage, BW, 0, 255, ThresholdTypes.Binary | ThresholdTypes.Otsu);
            Cv2.AdaptiveThreshold(grayImage, BW, 255, AdaptiveThresholdTypes.GaussianC, ThresholdTypes.Binary, 73, -30);

            //Mat element = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(3, 3));
            //Cv2.Erode(BW, BW, element);

            LineSegmentPoint[] lines = Cv2.HoughLinesP(BW, 1, Math.PI/2, 20, 20, 5);
            int maxX=0, maxY = 0, minX = 10000, minY=10000;
            bool procesar = true;
            double angulo;
            for (int i = 0; i < lines.Length; i++)
            {
                angulo = Math.Atan2(Math.Abs((lines[i].P1 - lines[i].P2).Y) , Math.Abs((lines[i].P1 - lines[i].P2).X));
                //if (lines[i].P1.X != lines[i].P2.X)
                //    angulo=Math.Atan ((lines[i].P1- lines[i].P2).Y/ (lines[i].P1 - lines[i].P2).X);
                //else
                //    angulo=Math.PI/2;
                if (marcaInicial)
                {
                    procesar = lines[i].Length() < 50; //sólo tiene en cuenta las líneas cortas de la marca
                    //procesar = lines[i].Length() < 50; //sólo tiene en cuenta las líneas cortas de la marca
                    //procesar = procesar && lines[i].P1.Y > 200;
                }
                procesar = (lines[i].P1.DistanceTo(centro) < RADIO) || (lines[i].P2.DistanceTo(centro) < RADIO);//sólo procesa si están cerca del punto iniciaal objetivo
                if (procesar)
                {
                    if (angulo>1.5) //(lines[i].P1.X == lines[i].P2.X)//es una línea vertical
                    {
                        if (lines[i].P1.X > maxX)
                            maxX = lines[i].P1.X;
                        if (lines[i].P1.X < minX)
                            minX = lines[i].P1.X;
                        //// Draws result lines
                        miImagen.Line(lines[i].P1, lines[i].P2, Scalar.Red, 1, LineTypes.AntiAlias, 0);
                    }
                    if (angulo<0.1)//(lines[i].P1.Y == lines[i].P2.Y)//es una línea horizontal
                    {
                        if (lines[i].P1.Y > maxY)
                            maxY = lines[i].P1.Y;
                        if (lines[i].P1.Y < minY)
                            minY = lines[i].P1.Y;
                        //// Draws result lines
                        miImagen.Line(lines[i].P1, lines[i].P2, Scalar.Red, 1, LineTypes.AntiAlias, 0);
                    }
                }
            }
            //Cv2.ImShow("BW", BW);
            Point resultado=new Point((maxX + minX) / 2, (maxY + minY) / 2);
            //if (!imagenEntera)
            //{
            //    if (centro.X > RADIO)
            //        resultado.X = centro.X + resultado.X;
            //    if (centro.Y > RADIO)
            //        resultado.Y = centro.Y+ resultado.Y;
            //}
            System.Windows.Forms.Application.DoEvents();
            return resultado;
        }
    }
}
