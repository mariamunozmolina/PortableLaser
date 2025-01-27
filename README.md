# PortableLaser
Código utilizado en el TFM: "Diseño de una máquina portable de grabado láser para superficies selectivas en frecuencia en ventanas bajo-emisivas de ferrocarriles y edificios"
Se divide en tres partes:
  1. Código en C#, contiene los siguientes programas:
     Código formado por la interfaz gráfica:Principal.cs
     Codigo que controla el galvanómetro y el láser: galvanometro.cs y laser.cs
     Código para realizar el procesado de la imagen:procesadorImagen.cs
  2. Código en C++ (Arduino). Mediante el que se controlará el Arduino y el Galvanómetro. Es el archivo: LaserDAC_SPI.ino
  3. Código en python, que corresponde con el archivo TimeGating.py
