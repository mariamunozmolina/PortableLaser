using System.Management;
using System.Diagnostics;
using System.IO.Ports;
using System.Windows.Forms;

namespace MobiFreq
{
    // MAIN CLASS //
    public partial class Motor
    {
        //private string puertoCOM;
        private SerialPort serialPort1;
        private double mmVuelta = 10.0; // 10 milímetros por vuelta
        private double pasosVuelta = 200.0; // 1,8º con microstep = 1

        private const double VEL_POS_TO_ZERO = 100.0; // 100 mm/s (sea cual sea el microstep y si llega a esa velocidad máxima)

        private bool finCarreraIzquierdo=false;
        public bool FinCarreraIzquierdo { get => finCarreraIzquierdo; set => finCarreraIzquierdo = value; }
        
        private bool finCarreraDerecho = false;
        public bool FinCarreraDerecho { get => finCarreraDerecho; set => finCarreraDerecho = value; }

        private double posicion = 0.0;
        public double Posicion { get => posicion; set => posicion = value; }

        private int velocidad = 4000;//pasos por segundo
        public int Velocidad { get => velocidad; set => velocidad = value; }

        private int aceleracion = 20000; //pasos por segundo al cuadrado
        public int Aceleracion { get => aceleracion; set => aceleracion = value; }
        
        private string microsteps = "1/16";
        public string Microsteps { get => microsteps; set => microsteps = value; }

        private bool moviendo = false;
        public bool Moviendo { get => moviendo; set => moviendo = value; }
        public double PasosMm { get => pasosVuelta/mmVuelta; }

        public delegate void MensajeHandler(object sender, string mensaje);
        public event MensajeHandler MensajeRecibido; //evento de medida de un LED realizada
        protected virtual void OnMensajeRecibido(string mensaje)
        {
            if (MensajeRecibido != null)
                MensajeRecibido(this, mensaje);
        }

        // FORMULARIO PRINCIPAL //
        public Motor()
        {
            InicializarPuertoCOM();
        }

        // CUANDO SE CIERRA EL FORMULARIO //
        public void CerrarMotor()
        {
            if (serialPort1 != null)
                serialPort1.Close();
        }

        // FUNCIÓN PARA INICIALIZAR EL PRIMER FORMULARIO DE SELECCIÓN DE PUERTOS //
        private void InicializarPuertoCOM()
        {
            // Obtener la lista de puertos COM disponibles
            string puerto = AutodetectArduinoPort();

            if (puerto != null)
            {
                // Inicializar el objeto SerialPort
                serialPort1 = new SerialPort(puerto, 9600);
                serialPort1.DataReceived += serialPort1_DataReceived;
                serialPort1.ErrorReceived += SerialPort1_ErrorReceived;

                serialPort1.Open();
                serialPort1.WriteLine("Comprueba fines de carrera");
            }
            else
            { 
                MessageBox.Show("Motor no encontrado");
            }
        }

        // FUNCIÓN PARA AUTODETECTAR SOLO LOS PUERTOS COM QUE CONTENGAN "ARDUINO" EN SU NOMBRE //
        private string AutodetectArduinoPort()
        {
            string arduinoPort=null;
            
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();

                    if (desc.Contains("Arduino Uno"))
                    {
                        arduinoPort=deviceId;
                        //return $"{desc} ({deviceId})";
                    }
                }
            }
            catch (ManagementException e)
            {
                /* No hacer nada */
                return null;
            }

            return arduinoPort;
        }

        // FUNCIÓN PARA DETECTAR Y TRATAR LA INFORMACIÓN RECIBIDA DEL ARDUINO POR EL PUERTO SERIE //
        private void serialPort1_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                string data = serialPort1.ReadLine();
                Debug.WriteLine(data);
                moviendo = false;
                OnMensajeRecibido(data);
            }
            catch (System.IO.IOException ex)
            {
                // Manejar la excepción de IOException
                Debug.WriteLine($"IOException: {ex.Message}");
                MessageBox.Show("Puerto desconectado!");
            }
            catch (System.InvalidOperationException ex)
            {
                // Manejar la excepción de InvalidOperationException
                Debug.WriteLine($"IOException: {ex.Message}");
                MessageBox.Show("Puerto desconectado!");
            }
        }

        // FUNCIÓN PARA DETECTAR SI HAY UNA PÉRDIDA DE COMUNICACIÓN CON EL ARDUINO POR EL PUERTO SERIE //
        private void SerialPort1_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            // Tratar el error de comunicación con Arduino
            Debug.WriteLine("Puerto COM 'ErrorReceived'.");
            // Mostrar el panel de selección de puerto
            InicializarPuertoCOM();

            // Otra lógica para limpiar o restablecer el estado de la aplicación
            // ...
        }

        internal void Mover(double distancia)
        {
            if (serialPort1 != null)
            {
                serialPort1.WriteLine($"{velocidad};{aceleracion};{microsteps};{distancia}");
                moviendo = true;
            }
        }

        internal void Parar()
        {
            if (serialPort1 != null)
            {
                // Abre el puerto serial si no está abierto
                if (!serialPort1.IsOpen)
                {
                    serialPort1.Open();
                }

                // Envía el comando "STOP" al Arduino
                serialPort1.WriteLine("STOP");
            }
        }

        internal void BuscarOrigen()
        {
            if (serialPort1 != null)
            {
                serialPort1.WriteLine($"Desplazando hasta el origen (izquierda) a 100 mm/s ;{microsteps}");
                moviendo = true;
            }
        }
    }
}
