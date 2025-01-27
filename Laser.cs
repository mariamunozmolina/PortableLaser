using MobiFreq.Properties;
using System;
using System.Globalization;
using System.Windows.Forms;

namespace MobiFreq
{
    public class Laser
    {
        PuertoComm miPuerto;
        Galvanometro miGalva;
        NumberFormatInfo formato = new NumberFormatInfo();
        
        public bool RojoEncendido //encender o apagar el láser rojo
        {
            get
            {
                uint estado=Convert.ToUInt32(EnviarYRecibir("$11"));
                return ((estado & 32) > 0); //mira si el bit 5 está a 1
            }

            set
            {
                if (value)
                    EnviarYRecibir("$40");
                else
                    EnviarYRecibir("$41");
            }
        }

        public bool Encendido //encender o apagar el láser
        {
            get
            {
                uint estado = Convert.ToUInt32(EnviarYRecibir("$11"));
                return ((estado & 32768) > 0); //mira si el bit 15 está a 1
            }

            set
            {
                if (value)
                    EnviarYRecibir("$42");
                else
                    EnviarYRecibir("$43");
            }
        }

        public bool Modulacion //encender o apagar la modulación
        {
            get
            {
                uint estado = Convert.ToUInt32(EnviarYRecibir("$10"));
                return ((estado & 0x40000) > 0); //mira si el bit 18 está a 1
            }

            set
            {
                if (value)
                    miGalva.EnviarYRecibir("E_1");
                else
                    miGalva.EnviarYRecibir("E_0");
            }
        }

        public float Frecuencia //modificar valor frecuencia
        {
            get
            {
                return (float)Convert.ToDouble(EnviarYRecibir("$29"), formato);
            }
            set
            {
                EnviarYRecibir("$28;"+ value);
                System.Threading.Thread.Sleep(1000);
            }
        }

        public float Potencia //modificar valor potencia
        {
            get
            {
                return (float)Convert.ToDouble(EnviarYRecibir("$34"), formato);
            }

            set
            {
                EnviarYRecibir("$32;"+value);
                System.Threading.Thread.Sleep(1000);
            }
        }

        public string Alarmas //resetear alarmas
        {
            get
            {
                return EnviarYRecibir("$4");
            }

        }

        public Laser(string numSerie, ref Galvanometro vGalva)
        {
            formato.NumberDecimalSeparator = ".";
            miGalva = vGalva;
            string puertoLaser = PuertoComm.BuscarPuerto("$2", "2;"+numSerie, 57600, "\r", Settings.Default.puertoLaser);
            if (puertoLaser == "VIRTUAL")
                MessageBox.Show("No se encuentra el láser");
            miPuerto = new PuertoComm(puertoLaser, 57600, "\r", false);
            Settings.Default.puertoLaser = puertoLaser;
            Settings.Default.Save();
            Modulacion = false;
            Encendido = false;
            RojoEncendido = false;
            Frecuencia = 150;
            Potencia = 0;
            ResetAlarmas();
        }

        public void ResetAlarmas()
        {
            EnviarYRecibir("$50");
        }

        internal string EnviarYRecibir(string texto)
        {
            return miPuerto.EnviarYRecibir(texto).Split(';')[1];
        }
    }
}
