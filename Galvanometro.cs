using MobiFreq.Properties;
using System.Windows.Forms;

namespace MobiFreq
{
    public class Galvanometro
    {
        PuertoComm miPuerto;

        bool correccion=true;
        public bool Correccion //correccion de la deformación del galvanómetro
        {
            get
            {
                return correccion;
            }
            set
            {
                if (value)
                    EnviarYRecibir("C_1");
                else
                    EnviarYRecibir("C_0");
                correccion = value;
            }
        }

        int esperaInicial = 1000;
        public int EsperaInicial //correccion de la deformación del galvanómetro
        {
            get
            {
                return esperaInicial;
            }
            set
            {
                esperaInicial = value;
                EnviarYRecibir("W_" + esperaInicial + "_" + esperaFinal);           
            }
        }

        int esperaFinal = 400;
        public int EsperaFinal //correccion de la deformación del galvanómetro
        {
            get
            {
                return esperaFinal;
            }
            set
            {
                esperaFinal = value;
                EnviarYRecibir("W_" + esperaInicial + "_" + esperaFinal);
            }
        }

        public Galvanometro()
        {
            string puertoArduino = PuertoComm.BuscarPuerto("1", Settings.Default.puertoGalva);
            if (puertoArduino == "VIRTUAL")
                MessageBox.Show("No se encuentra el galvanómetro");
            miPuerto = new PuertoComm(puertoArduino, 115200, "\r\n", false);
            Settings.Default.puertoGalva = puertoArduino;
            Settings.Default.Save();
        }
        internal void HacerLinea(int x0, int y0, int x1, int y1, int nPuntos)
        {
            EnviarYRecibir("L_" + x0 + "_" + y0 + "_" + x1 + "_" + y1 + "_" + nPuntos);
            // enviamos dos comandos porque el tiempo de espera de la recepcion de la respuesta es grande, ya que no responde hasta que no ha acabado de realizar la línea-> cuando responda ya ha acabado el arduino
        }

        internal void Mover(int x, int y)
        {
            EnviarYRecibir("M_" + x + "_" + y);
        }

        internal string EnviarYRecibir(string texto)
        {
            string resp= miPuerto.EnviarYRecibir(texto);
            string resp2=miPuerto.LeeEsperando();
            return resp+" "+resp2;
        }

        internal void CambiarCorreccion(double[][] doubles)
        {
            EnviarYRecibir("P_");
        }
    }
}
