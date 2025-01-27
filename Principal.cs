using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using OpenCvSharp;
using OpenCvSharp.Extensions;


namespace MobiFreq
{
    public partial class Principal : Form
    {
        Galvanometro miGalva;
        Laser miLaser;
        VideoCapture miCamara;
        ProcesadorImagen miProcesador = new ProcesadorImagen();
        Thread hiloCamara;
        bool capturarCamara = false;
        Mat imagenCamara = new Mat();
        const int MINIMO = -25000;//-32768;
        const int MAXIMO = 25000; //32767;
        double nivelesMm = 25000.0 / 42.4;
        NumberFormatInfo formato = new NumberFormatInfo();
        OpenCvSharp.Point puntoRef = new OpenCvSharp.Point();
        const int LONGITUD_MARCA = 600;
        public Principal()
        {
            InitializeComponent();
            formato.NumberDecimalSeparator = ".";
            miGalva = new Galvanometro();
            miLaser = new Laser("23100038", ref miGalva);
            timerActualizar.Enabled = true;
        }

       

        private double[][] CalcularCorreccion(double[,] coordenadasX, double[,] coordenadasY, int distanciaRef)
        {
            double[][] correccion = new double[4][];
            double L = coordenadasX[2, 1]; //longitud de referencia en x=max, y=0
            double[,] coordenadasXNiv = new double[3, 3];
            double[,] coordenadasYNiv = new double[3, 3];
            nivelesMm = distanciaRef / L; //FALTA GRABAR ESTO Y LA CALIBRACIÓN PARA QUE QUEDE PARA SIEMPRE!!!!!

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    coordenadasXNiv[i, j] = nivelesMm*coordenadasX[i, j]; //en niveles
                    coordenadasYNiv[i, j] = nivelesMm * coordenadasY[i, j];
                }

            //primer cuadrante(+-)
            correccion[0] = Polinomios(2, 0, coordenadasXNiv, coordenadasYNiv);

            //segundo cuadrante(--)
            correccion[1] = Polinomios(0, 0, coordenadasXNiv, coordenadasYNiv);

            //tercer cuadrante(-+)
            correccion[2] = Polinomios(0, 2, coordenadasXNiv, coordenadasYNiv);

            //cuarto cuadrante(++)
            correccion[3] = Polinomios(2, 2, coordenadasXNiv, coordenadasYNiv);

            return correccion;
        }

        private double[] Polinomios(int indiceX, int indiceY, double[,] coordenadasX, double[,] coordenadasY)
        {
            double L = coordenadasX[2, 1]; //longitud de referencia en x=max, y=0
            double[,] deltaX = new double[3, 3]; //diferencia con posiciones teóricas
            double[,] deltaY = new double[3, 3];

            for (int i = 0; i < 3; i++)
                for (int j = 0; j < 3; j++)
                {
                    deltaX[i, j] = L * (i - 1) - coordenadasX[i, j];
                    deltaY[i, j] = L * (j - 1) - coordenadasY[i, j];
                }

            double a0 = deltaX[indiceX,1] / coordenadasX[indiceX, 1]; //[indiceX,1] es el punto 1 (en el ejeX, y=0) [indiceX, indiceY] el punto 2 y [1, indiceY] el punto 3
            double a1 = -1* deltaX[indiceX, 1] / (coordenadasX[indiceX, 1] * coordenadasY[indiceX, indiceY] * coordenadasY[indiceX, indiceY]) + deltaX[indiceX, indiceY] / (coordenadasX[indiceX, indiceY] * coordenadasY[indiceX, indiceY] * coordenadasY[indiceX, indiceY]);
            double b0 = deltaY[1, indiceY] / (coordenadasY[1, indiceY]);
            double b1 = -1* deltaY[1, indiceY] / (coordenadasY[1, indiceY] * coordenadasX[indiceX, indiceY] * coordenadasX[indiceX, indiceY]) + deltaY[indiceX, indiceY] / (coordenadasY[indiceX, indiceY] * coordenadasX[indiceX, indiceY] * coordenadasX[indiceX, indiceY]);

            return new double[4] { a0, a1, b0, b1 };
        }

        private void DibujarMalla(double separacion, int puntosMM, bool marcas)
        {
            int nivelesSeparacion = (int)(separacion * nivelesMm);
            int nLineas = (int)Math.Floor((MAXIMO - MINIMO) / (decimal)nivelesSeparacion);
            int minimo = -1*nLineas * nivelesSeparacion/2;
            int maximo = nLineas * nivelesSeparacion / 2; ;//el máximo llega hasta el final del cuadrado, aunque allí no haya línea transversal
            int nPuntos = Math.Max(1, (int)(separacion * nLineas * puntosMM));

            //MALLA
            
            if (checkBoxHorizontales.Checked)
            {
                miGalva.Mover(minimo, minimo);
                Thread.Sleep(100);
                miGalva.HacerLinea(minimo + LONGITUD_MARCA, minimo, maximo + LONGITUD_MARCA, minimo, nPuntos); //primera línea desplazada
                for (int i = 1; i < nLineas; i++)
                {                      
                    if (i % 2 == 0)
                        miGalva.HacerLinea(minimo, minimo + i * nivelesSeparacion, maximo, minimo + i * nivelesSeparacion, nPuntos);
                    else
                        miGalva.HacerLinea(maximo, minimo + i * nivelesSeparacion, minimo, minimo + i * nivelesSeparacion, nPuntos);
                }
            }


            if (checkBoxVerticales.Checked)
            {
                miGalva.Mover(minimo, maximo + LONGITUD_MARCA);
                Thread.Sleep(100);
                miGalva.HacerLinea(minimo, maximo + LONGITUD_MARCA, minimo, minimo + LONGITUD_MARCA, nPuntos);
                for (int i = 1; i < nLineas; i++)
                {                 
                    if (i % 2 == 0)
                        miGalva.HacerLinea(minimo + i * nivelesSeparacion, maximo, minimo + i * nivelesSeparacion, minimo, nPuntos);
                    else
                        miGalva.HacerLinea(minimo + i * nivelesSeparacion, minimo, minimo + i * nivelesSeparacion, maximo, nPuntos);
                }
            }

            if (marcas)
            {
                DibujarMarca(maximo, minimo, LONGITUD_MARCA, puntosMM, false, true);//línea vertical para unir con la siguiente malla
                DibujarMarca(-1 * minimo, minimo, LONGITUD_MARCA, puntosMM, true, false);//sólo la línea horizontal para unir con la siguiente fila
            }
        }

        private void MoverACruz()
        {
            OpenCvSharp.Point posicion = new OpenCvSharp.Point();
            //Buscar posición inicial
            Mat miImagen = new Mat();
            while (posicion.X != puntoRef.X)//luego en los dos ejes
            {
                miCamara.Read(miImagen);
                //Cv2.Rotate(miImagen, miImagen, RotateFlags.Rotate90Clockwise);

                ////////QUITAR
                //miImagen = Cv2.ImRead("Marca prueba2.jpg");
                //pictureBoxCamara.Image = BitmapConverter.ToBitmap(miImagen);
                ///////////////
                posicion = miProcesador.BuscarMarca(miImagen, false, new OpenCvSharp.Point(820, 725));
                miImagen.DrawMarker(posicion, Scalar.Red);
                pictureBoxCamara.Image = BitmapConverter.ToBitmap(miImagen);
                textBoxXCruz.Text = posicion.X.ToString();
                textBoxYCruz.Text = posicion.Y.ToString();
                int distanciaX = -1*(posicion.X - puntoRef.X);
               
            }
        }

        private void TomarReferencia(double separacion, int puntosMM)
        {
            Mat miImagen = new Mat();
            //Hacer marca X
            int nivelesSeparacion = (int)(separacion * nivelesMm);
            int nLineas = (int)Math.Floor((MAXIMO - MINIMO) / (decimal)nivelesSeparacion);
            int minimo = -1 * nLineas * nivelesSeparacion / 2;
            DibujarMarca(minimo, minimo, LONGITUD_MARCA, puntosMM);

            //Buscar posición inicial X
            miLaser.Encendido = false;
            Thread.Sleep(1000);
            for (int i = 0; i < 10; i++)//para quitar imágenes del buffer
                miCamara.Read(miImagen);

            ////////QUITAR
            //miImagen = Cv2.ImRead("Marca prueba.jpg");
            //pictureBoxCamara.Image = BitmapConverter.ToBitmap(miImagen);
            ///////////////


            //Cv2.Rotate(imagenCamara, imagenCamara, RotateFlags.Rotate90Clockwise);
            puntoRef = miProcesador.BuscarMarca(miImagen, true, new OpenCvSharp.Point(820, 725));
            miImagen.DrawMarker(puntoRef, Scalar.Red);
            pictureBoxCamara.Image = BitmapConverter.ToBitmap(miImagen);
            textBoxXCruz.Text = puntoRef.X.ToString();
            textBoxYCruz.Text = puntoRef.Y.ToString();
            miLaser.Encendido = checkBoxLaser.Checked;
        }

        private void DibujarMarca(int x, int y, int L, int puntosMM, bool horizontal=true, bool vertical=true)
        {
            int nPuntos = (int)(LONGITUD_MARCA/nivelesMm * puntosMM);
            miGalva.Mover(x, y);
            Thread.Sleep(100);
            if (vertical)
                miGalva.HacerLinea(x, y , x, y + L, nPuntos); //marca vertical
            if (horizontal)
                miGalva.HacerLinea(x, y, x + L, y, nPuntos); //marca horizontal
        }

        private void CerrarCamara()
        {
            capturarCamara = false;
            if (miCamara!=null)
                miCamara.Release();
        }

        private void IniciarCamara()
        {
            miCamara = new VideoCapture(0);
            miCamara.FrameWidth = 1600;// 1920; //
            miCamara.FrameHeight = 1200;// 1080; //

            //miCamara.Open(1);
            for (int i = 0;i< 10;i++) //para que coja bien la exposición
                miCamara.Read(imagenCamara);
            miCamara.Exposure = -6;
        }

        #region Eventos del formulario
        private void Principal_Load(object sender, EventArgs e)
        {
            IniciarCamara();

            miGalva.Correccion = checkBoxCorreccion.Checked;

           
        }

        private void ActualizarPanel()
        {
            if (!textBoxFrecuencia.Focused)
                textBoxFrecuencia.Text = miLaser.Frecuencia.ToString(formato);
            if (!textBoxPotencia.Focused)
                textBoxPotencia.Text = miLaser.Potencia.ToString(formato);
            checkBoxLaser.Checked = miLaser.Encendido;
            checkBoxModulacion.Checked = miLaser.Modulacion;
            checkBoxLaserRojo.Checked = miLaser.RojoEncendido;
            toolStripStatusLabelAlarmas.Text = "Alarmas: " + miLaser.Alarmas;
        }

        private void buttonEnviarLaser_Click(object sender, EventArgs e)
        {
            textBoxRespuestaLaser.Text = miLaser.EnviarYRecibir(textBoxMensajeLaser.Text);
        }

        private void buttonLineas_Click(object sender, EventArgs e)
        {
            double separacion = Convert.ToDouble(textBoxSeparacion.Text, formato);
            int puntosMM = Convert.ToInt32(textBoxPuntos.Text);

            timerActualizar.Enabled = false;
            DateTime inicio = DateTime.Now;

            DibujarMalla(separacion, puntosMM, true);

            double tiempo = (DateTime.Now - inicio).TotalMilliseconds;
            timerActualizar.Enabled = true;
            MessageBox.Show("Tiempo: " + tiempo.ToString(formato));
        }

        private void checkBoxLaser_CheckedChanged(object sender, EventArgs e)
        {
            miLaser.Encendido = checkBoxLaser.Checked;
            if (checkBoxLaser.Checked)
                checkBoxLaser.BackColor = Color.Red;
            else
                checkBoxLaser.BackColor = SystemColors.Control;
        }

        private void checkBoxModulacion_CheckedChanged(object sender, EventArgs e)
        {
            miLaser.Modulacion = checkBoxModulacion.Checked;
            if (checkBoxModulacion.Checked)
                checkBoxModulacion.BackColor = Color.Red;
            else
                checkBoxModulacion.BackColor = SystemColors.Control;
        }

        private void checkBoxLaserRojo_CheckedChanged(object sender, EventArgs e)
        {
            miLaser.RojoEncendido = checkBoxLaserRojo.Checked;
            if (checkBoxLaserRojo.Checked)
                checkBoxLaserRojo.BackColor = Color.Red;
            else
                checkBoxLaserRojo.BackColor = SystemColors.Control;
        }

        private void buttonEnviarArduino_Click(object sender, EventArgs e)
        {
            timerActualizar.Enabled = false;
            textBoxRespuestaArduino.Text = "";
            string[] comandos = textBoxMensajeArduino.Text.Split('\r');
            foreach (string comando in comandos)
            {
                string comandot = comando.Trim();
                textBoxRespuestaArduino.Text = textBoxRespuestaArduino.Text + miGalva.EnviarYRecibir(comandot) + "\r\n";
                Thread.Sleep(100);
            }
            timerActualizar.Enabled = true;
        }

        private void buttonFrecuencia_Click(object sender, EventArgs e)
        {
            miLaser.Frecuencia = (float)Convert.ToDouble(textBoxFrecuencia.Text, formato);
            //double prueba=miLaser.Frecuencia;
        }

        private void buttonPotencia_Click(object sender, EventArgs e)
        {
            miLaser.Potencia = (float)Convert.ToDouble(textBoxPotencia.Text, formato);
        }

        private void buttonResetAlarma_Click(object sender, EventArgs e)
        {
            miLaser.ResetAlarmas();
        }

        private void timerActualizar_Tick(object sender, EventArgs e)
        {
            ActualizarPanel();
        }

        private void textBoxPotencia_Leave(object sender, EventArgs e)
        {
            trackBarPotencia.Value = int.Parse(textBoxPotencia.Text);
            miLaser.Potencia = Convert.ToSingle(textBoxPotencia.Text, formato);
        }

        private void textBoxFrecuencia_Leave(object sender, EventArgs e)
        {
            miLaser.Frecuencia = Convert.ToSingle(textBoxFrecuencia.Text, formato);
        }

        private void trackBarPotencia_MouseUp(object sender, MouseEventArgs e)
        {
            textBoxPotencia.Text = trackBarPotencia.Value.ToString();
            miLaser.Potencia = Convert.ToSingle(textBoxPotencia.Text, formato);
        }

        private void trackBarPotencia_KeyDown(object sender, KeyEventArgs e)
        {
            textBoxPotencia.Text = trackBarPotencia.Value.ToString();
            miLaser.Potencia = Convert.ToSingle(textBoxPotencia.Text, formato);
        }

        private void textBoxFrecuencia_KeyPress(object sender, KeyPressEventArgs e)
        {
            miLaser.Frecuencia = Convert.ToSingle(textBoxFrecuencia.Text, formato);
        }

        private void textBoxPotencia_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Return)
            {
                trackBarPotencia.Value = int.Parse(textBoxPotencia.Text);
                miLaser.Potencia = Convert.ToSingle(textBoxPotencia.Text, formato);
            }
        }

        private void checkBoxCamara_CheckedChanged(object sender, EventArgs e)
        {
            capturarCamara = checkBoxCamara.Checked;
            if (checkBoxCamara.Checked)
            {
                checkBoxCamara.BackColor = Color.Red;
                hiloCamara = new Thread(new ThreadStart(CaptureCameraCallback));
                hiloCamara.Start();
            }
            else
                checkBoxCamara.BackColor = SystemColors.Control;
        }

        private void CaptureCameraCallback()
        {
            while (capturarCamara)
            {
                miCamara.Read(imagenCamara);
                //Cv2.Rotate(imagenCamara, imagenCamara, RotateFlags.Rotate90Clockwise);
                //imagenCamara = Cv2.ImRead("Marca prueba.jpg");
                pictureBoxCamara.Image = BitmapConverter.ToBitmap(imagenCamara);
            }
        }
      
        private void buttonProcesar_Click(object sender, EventArgs e)
        {
            checkBoxCamara.Checked = false;
            miCamara.Read(imagenCamara);
            //Cv2.Rotate(imagenCamara, imagenCamara, RotateFlags.Rotate90Clockwise);
            OpenCvSharp.Point posicion = miProcesador.BuscarMarca(imagenCamara, true, new OpenCvSharp.Point(820, 725));
            imagenCamara.DrawMarker(posicion, Scalar.Red);
            pictureBoxCamara.Image = BitmapConverter.ToBitmap(imagenCamara);
            textBoxXCruz.Text = posicion.X.ToString();
            textBoxYCruz.Text = posicion.Y.ToString();
        }

        private void Principal_FormClosing(object sender, FormClosingEventArgs e)
        {
            CerrarCamara();
        }


        private void buttonTomarRef_Click(object sender, EventArgs e)
        {
            double separacion = Convert.ToDouble(textBoxSeparacion.Text);
            int puntosMM = Convert.ToInt32(textBoxPuntos.Text);
            checkBoxCamara.Checked = false;
            TomarReferencia(separacion, puntosMM);
        }

        private void buttonBuscarRef_Click(object sender, EventArgs e)
        {
            checkBoxCamara.Checked = false;
            MoverACruz();
        }

        #endregion region

        #region Motor

     
        // FUNCIÓN QUE DETECTA UN CAMBIO EN EL SELECTOR DE DISTANCIA A RECORRER CON EL CARRO //
        private void numericUpDownDistancia_ValueChanged(object sender, EventArgs e)
        {
            NumericUpDown numericUpDown = (NumericUpDown)sender;
            double value = (double)numericUpDown.Value;

            if (value < 0.1 || value > 600) // Rango para "distancia"
            {
                MessageBox.Show("El valor debe estar entre 0,1 y 600.");
                ((System.Windows.Forms.TextBox)sender).Text = "0,1";
            }
        }

        // FUNCIÓN QUE DETECTA UN CAMBIO EN EL DESPLEGABLE DE SENTIDO DE GIRO DEL MOTOR //
       
        // FUNCIÓN PARA ACTUALIZAR LA POSICIÓN "X" ACTUAL DEL CARRITO //
       
        // FUNCIÓN PARA RESETEAR EL VALOR DE LA POSICIÓN "X" DEL CARRITO //
        private void ResetPosX()
        {
            posXLabel.Text = "0.0 mm";
        }

        #endregion

        private void checkBoxCorreccion_CheckedChanged(object sender, EventArgs e)
        {
            miGalva.Correccion=checkBoxCorreccion.Checked;
        }

       
        private void pictureBoxCamara_Click(object sender, EventArgs e)
        {

        }
    }
}