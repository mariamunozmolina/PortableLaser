
#include <SPI.h>
const int chipSelectPin = 10;
const int modPin = 9;
const int offPin = 8;

//Constantes de corrección: Primer cuadrante  (+-); Segundo cuadrante (--); Tercer cuadrante (-+); Cuarto cuadrante (++)
double a0[4] = { 0, 0.0323, 0.0323, 0 };
double a1[4] = { -1.6491e-11, -1.9124e-11, -2.2743e-11, -1.967e-11 };
double b0[4] = { 0.0316, 0.0316, 0.0379, 0.0379 };
double b1[4] = { 2.9088e-11, 5.2932e-11, 5.3398e-11, 2.4210e-11 };

// //Primer cuadrante (+-)
// const double a0_pn=0;
// const double a1_pn=-1.6491e-11;
// const double b0_pn=0.0316;
// const double b1_pn=2.9088e-11;

// //Segundo cuadrante (--)
// const double a0_nn=0.0323;
// const double a1_nn=-1.9124e-11;
// const double b0_nn=0.0316;
// const double b1_nn=5.2932e-11;

// //Tercer cuadrante (-+)
// const double a0_np=0.0323;
// const double a1_np=-2.2743e-11;
// const double b0_np=0.0379;
// const double b1_np=5.3398e-11;

// //Cuarto cuadrante (++)
// const double a0_pp=0;
// const double a1_pp=-1.967e-11;
// const double b0_pp=0.0379;
// const double b1_pp=2.4210e-11;

const int maxNiveles = 32768;
double Vmin = -10;
double Vmax = 10;
bool correccion = true;
int esperaInicio=1000;
int esperaFin=400;

void setup() {
  Serial.begin(115200);
  pinMode(chipSelectPin, OUTPUT);
  pinMode(modPin, OUTPUT);
  pinMode(offPin, OUTPUT);
  SPI.begin();
  SPI.beginTransaction(SPISettings(5000000, MSBFIRST, SPI_MODE0));
  //cambiarSpan (0x80, 3); //todas las DAC a +-10V
}

void loop() {
  String mensaje = Serial.readStringUntil('\n');
  if (mensaje.length() == 0)
    return;

  //////////////Dividir mensaje
  int numValores = 0;
  int valores[5];
  byte comando = mensaje[0];       //la primera letra es el comando
  mensaje = mensaje.substring(2);  //quita el comando
  if (comando != 'P')              //si es P deja el resto del mensaje intacto
  {
    while ((mensaje.length() > 0) & numValores < 5)
    {
      int indice = mensaje.indexOf('_');
      if (indice == -1)  // No se encuentra _
      {
        valores[numValores++] = (mensaje.toInt());
        break;
      } else {
        valores[numValores++] = (mensaje.substring(0, indice).toInt());
        mensaje = mensaje.substring(indice + 1);
      }
    }
  }

  switch (comando)  //ejecutar el comando
  {
    case 'S':  //número de serie
      {
        Serial.println("1");
        Serial.println("OK");
        break;
      }
    case 'E':  //Emisión (modulación)
      {
        if (numValores > 0)
        {
          cambiarModulacion(valores[0] > 0);
          Serial.println(valores[0]);
          Serial.println("OK");
        }
        else
        {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'M':  //Mover
      {
        if (numValores > 1) {
          Serial.println("mover");
          mover(valores[0], valores[1]);
          Serial.println("movido");
        } else {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'X':  //línea horizontal
      {
        if (numValores > 2) {
          Serial.println("lineaX");
          lineaXY(valores[0], valores[1], true, valores[2]);
          Serial.println("trazada");
        } else {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'Y':  //línea vertical
      {
        if (numValores > 2) {
          Serial.println("lineaY");
          lineaXY(valores[0], valores[1], false, valores[2]);
          Serial.println("trazada");
        } else {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'L':  //línea cualquiera
      {
        if (numValores > 4) {
          Serial.println("linea");
          linea(valores[0], valores[1], valores[2], valores[3], valores[4]);
          Serial.println("trazada");
        } else {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'W': //cambiar las esperas en las líneas
    {
      if (numValores > 1)
      {
        esperaInicio=valores[0];
        esperaFin=valores[1];
        Serial.println(valores[0]);
        Serial.println("OK");
      } 
      else 
      {
        Serial.println("ERROR");
        Serial.println("OK");
      }
      break;
    }
    case 'C':  //activar o desactivar corrección
      {
        if (numValores > 0) {
          Serial.println("correccion");
          correccion = (valores[0] == 1);
          Serial.println(correccion);
        } else {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'P':  //parámetros de corrección
      {
        if (actualizarParametros(mensaje))
        {
          Serial.println("parametros");
          Serial.println("cambiados");
        }
        else
        {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    case 'O':  //Optotune. De 0 a 16383 es de 0 a 5V
      {
        if (numValores > 0) {
          if (valores[0] < 16384)
            escribirDatoActualizarDato(2, valores[0]);
          Serial.println(valores[0]);
          Serial.println("OK");
        } else {
          Serial.println("ERROR");
          Serial.println("OK");
        }
        break;
      }
    default:
      {
        Serial.println("ERROR");
        Serial.println("OK");
        break;
      }
  }
}

void mover(int x, int y) {
  escribirDato(0, corregirX(x, y));
  escribirDatoActualizarTodo(1, corregirY(x, y));
}

void cambiarModulacion(bool encendida) {
  if (encendida)
    digitalWrite(modPin, HIGH);
  else
    digitalWrite(modPin, LOW);
}

void lineaXY(int p0, int p1, bool X, int numPuntos) {
  double v = p1 - p0;  //vector director
  int datos[numPuntos];

  cambiarModulacion(true);
  for (int i = 0; i < numPuntos; i++) {
    datos[i] = p0 + v * i / (numPuntos - 1);
    //Serial.println(datos[i]);
  }

  if (X)
    for (int i = 0; i < numPuntos; i++)
      escribirDatoActualizarTodo(0, datos[i]);
  else
    for (int i = 0; i < numPuntos; i++)
      escribirDatoActualizarTodo(1, datos[i]);
  cambiarModulacion(true);
}

void linea(int x0, int y0, int x1, int y1, int numPuntos) {
  int vy = y1 - y0;  //vector director
  int vx = x1 - x0;
  double x;
  double y;
  int datosX;
  int datosY;

  // mover al punto incial
  escribirDato(0, corregirX(x0, y0));
  escribirDatoActualizarTodo(1, corregirY(x0, y0));
  delayMicroseconds(esperaInicio);
  cambiarModulacion(true);
  for (int i = 0; i < numPuntos; i++)
  {
    x = x0 + vx * i / (numPuntos - 1);
    y = y0 + vy * i / (numPuntos - 1);
    datosX = corregirX(x, y);
    datosY = corregirY(x, y);
    escribirDato(0, datosX);
    escribirDatoActualizarTodo(1, datosY);
  }
  delayMicroseconds(esperaFin);
  cambiarModulacion(false);
}

int corregirX(int x, int y) {
  double dx = 0;
  int i = 0;  //1er cuadrante (x>=0, y<=0)
  if (correccion) {
    if (x < 0 & y < 0)  //2º cuadrante
      i = 1;
    else if (x<0 & y> 0)  //3er cuadrante
      i = 2;
    else if (x > 0 & y > 0)  //4º cuadrante
      i = 3;
    dx = a0[i] * x + a1[i] * x * y * y;
  }

  return (int)(x + dx + maxNiveles);
}

int corregirY(int x, int y) {
  double dy = 0;
  int i = 0;  //1er cuadrante (x>=0, y<=0)
  if (correccion) {
    if (x < 0 & y < 0)  //2º cuadrante
      i = 1;
    else if (x<0 & y> 0)  //3er cuadrante
      i = 2;
    else if (x > 0 & y > 0)  //4º cuadrante
      i = 3;
    dy = b0[i] * y + b1[i] * y * x * x;
  }
  return (int)(y + dy + maxNiveles);
}

bool actualizarParametros(String mensaje)
{
  double parametros[16];
  int numValores=0;
  while ((mensaje.length() > 0) & numValores < 16)
  {
    int indice = mensaje.indexOf('_');
    if (indice == -1)  // No se encuentra _
    {
      parametros[numValores++] = (mensaje.toDouble());
      break;
    }
    else
    {
      parametros[numValores++] = (mensaje.substring(0, indice).toDouble());
      mensaje = mensaje.substring(indice + 1);
    }
  }
  if (numValores < 16)
    return false;
  else
  {
    for (int i = 0; i < 4; i++)
    {
      a0[i] = parametros[i];
      a1[i] = 4 + parametros[i];
      b0[i] = 8 + parametros[i];
      b1[i] = 12 + parametros[i];
    }
  }
  return true;
}

int voltios2valor(double voltios) {
  return 65535 * (voltios - Vmin) / (Vmax - Vmin);
}

void escribirDato(byte nDAC, int value) {
  byte comando = 0x00 + nDAC;
  byte MSB = (byte)(value >> 8);
  byte LSB = (byte)(value & 0x00FF);
  escribirSPI(comando, MSB, LSB);
}

void escribirDatoActualizarTodo(byte nDAC, int value) {
  byte comando = 0x20 + nDAC;
  byte MSB = (byte)(value >> 8);
  byte LSB = (byte)(value & 0x00FF);
  escribirSPI(comando, MSB, LSB);
}

void escribirDatoActualizarDato(byte nDAC, int value) {
  byte comando = 0x30 + nDAC;
  byte MSB = (byte)(value >> 8);
  byte LSB = (byte)(value & 0x00FF);
  escribirSPI(comando, MSB, LSB);
}

void cambiarSpan(byte nDAC, int value)  //nDAC=0x80 o 128, TODOS; value: 0 0-5V, 1 0-10V, 2 +-5V, 3 +-10V, 4 +-2.5V
{
  byte comando = 0x60 + nDAC;
  byte MSB = 0x00;
  byte LSB = (byte)(value & 0x00FF);
  escribirSPI(comando, MSB, LSB);
}

void escribirSPI(byte comando, byte MSB, byte LSB)  //nDAC=0x80 o 128, TODOS; value: 0 0-5V, 1 0-10V, 2 +-5V, 3 +-10V, 4 +-2.5V
{
  digitalWrite(chipSelectPin, LOW);
  //delay(100);
  SPI.transfer(comando);
  SPI.transfer(MSB);
  SPI.transfer(LSB);
  //delay(100);
  digitalWrite(chipSelectPin, HIGH);
}
