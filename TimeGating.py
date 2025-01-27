#-*- coding: utf-8 -*-
import sys
import numpy as np
import pandas as pd
import matplotlib.pyplot as plt
import skrf as rf
import scipy.io as sio
import tkinter as tk

from PyQt5.QtWidgets import QApplication, QMainWindow,QFileDialog,QGroupBox, QPushButton, QLabel, QVBoxLayout, QWidget,QComboBox, QLineEdit, QHBoxLayout , QSlider
from PyQt5.QtCore import QRect, Qt
from PyQt5.QtGui import QIntValidator
from scipy.signal.windows import hann, kaiser
from scipy.interpolate import interp1d
from scipy import fftpack
from scipy.fft import fft,fftshift,ifft
from matplotlib.backends.backend_qt5agg import FigureCanvasQTAgg as FigureCanvas
from matplotlib.backends.backend_tkagg import FigureCanvasTkAgg



class MyApp(QMainWindow):
    def __init__(self):
        super().__init__()        
        # Variables compartidas para datos
        self.data1 = None
        self.data2 = None
        self.S21_aire = None
        self.S21_vidrio = None
        self.freq = None

        # Simulación
        self.sim = None
        self.freqHz = None
        self.simat = None
        self.N=1

        # Ventanas y parámetros
        self.window_type = 'Hann'
        self.kaiser_beta = 8

        # Definir límites para las líneas (ajustar si es necesario)
        self.k1, self.kk1 = 123, 134  # para aire
        self.k2, self.kk2 = 124, 135  # para vidrio

        # Configuración de la ventana principal
        self.setGeometry(100, 100, 1600, 800)
        self.setWindowTitle("Interfaz Gráfica - Procesamiento de Datos")

        # Widget central y layout principal
        self.central_widget = QWidget(self)
        self.setCentralWidget(self.central_widget)
        self.layout = QHBoxLayout(self.central_widget)

        # Panel izquierdo para controles
        self.controls_layout = QVBoxLayout()
        self.left_panel = QWidget()
        self.left_panel.setLayout(self.controls_layout)

        # Panel derecho para gráficos
        self.right_panel = QVBoxLayout()
        self.plot_widget = QWidget()
        self.plot_widget.setLayout(self.right_panel)

        # Agregar los paneles al layout principal
        self.layout.addWidget(self.left_panel, 1)  # Panel izquierdo (pequeño)
        self.layout.addWidget(self.plot_widget, 3)  # Panel derecho (grande)

        # Elementos del panel izquierdo
        self.initUI()
        # Elementos del panel derecho
        self.init_plots()

        self.is_reference_loaded = False
        self.is_measure_loaded = False
        self.is_simulation_loaded = False


    def initUI(self):
        """Inicializar los controles en el panel izquierdo."""
        # Carga de archivos
        self.add_file_input("Abrir referencia",self.load_file1)
        self.add_file_input( "Abrir medida",self.load_file2)
        self.add_file_input("Abrir simulación",self.load_file3)
        
        # Etiqueta principal
        self.label = QLabel("Presiona un botón para mostrar una gráfica", self)
        self.controls_layout.addWidget(self.label)

        # Selector de ventana
        self.window_selector = QComboBox(self)
        self.window_selector.addItems(["Hann", "Kaiser"])
        self.window_selector.currentTextChanged.connect(self.set_window_type)
        self.controls_layout.addWidget(self.window_selector)

        # Input para parámetro beta de la ventana Kaiser
        self.beta_input = QLineEdit(self)
        self.beta_input.setPlaceholderText("Ingrese el valor de beta para Kaiser")
        self.beta_input.textChanged.connect(self.update_kaiser_beta)
        self.controls_layout.addWidget(self.beta_input)

        # Sliders para k1, kk1, k2 y kk2
        self.k1_slider, self.k1_input = self.add_slider("Aire 1", 0, 200, self.update_k1)
        self.kk1_slider, self.kk1_input = self.add_slider("Aire 2", 0, 200, self.update_kk1)
        self.k2_slider, self.k2_input = self.add_slider("Vidrio 1", 0, 200, self.update_k2)
        self.kk2_slider, self.kk2_input = self.add_slider("Vidrio 2", 0, 200, self.update_kk2)

         # Botón para actualizar las gráficas
        self.update_button = QPushButton("Actualizar Gráficas", self)
        self.update_button.clicked.connect(self.update_plots)
        self.controls_layout.addWidget(self.update_button)
       
       # Botón para guardar los datos
        self.save_button = QPushButton("Guardar", self)
        self.save_button.clicked.connect(self.save_to_excel)
        self.controls_layout.addWidget(self.save_button)



    def init_plots(self):
        """Inicializar las áreas de gráficos en el panel derecho."""
        # Gráfico 1
        self.figure1, self.ax1 = plt.subplots()
        self.canvas1 = FigureCanvas(self.figure1)
        self.canvas1.setFixedHeight(250)  # Fijar altura
        self.right_panel.addWidget(self.canvas1)

        # Gráfico 2
        self.figure2, self.ax2 = plt.subplots()
        self.canvas2 = FigureCanvas(self.figure2)
        self.canvas2.setFixedHeight(250)  # Fijar altura
        self.right_panel.addWidget(self.canvas2)

        # Gráfico 3
        self.figure3, self.ax3 = plt.subplots()
        self.canvas3 = FigureCanvas(self.figure3)
        self.canvas3.setFixedHeight(250)  # Fijar altura
        self.right_panel.addWidget(self.canvas3)

    def update_plots(self):
        """Actualizar las gráficas con los datos cargados."""
        if self.data1 is not None:
            self.ax1.clear()
            self.ax1.plot(self.data1, label="Referencia")
            self.ax1.legend()
            self.canvas1.draw()

        if self.data2 is not None:
            self.ax2.clear()
            self.ax2.plot(self.data2, label="Medida")
            self.ax2.legend()
            self.canvas2.draw()

        if self.sim is not None:
            self.ax3.clear()
            self.ax3.plot(self.sim, label="Simulación")
            self.ax3.legend()
            self.canvas3.draw()


    # Carga de archivos
    def add_file_input(self,button_text, load_file_callback):
        # Botón para abrir un diálogo de archivo
        btn_load_file = QPushButton(button_text, self)
        btn_load_file.clicked.connect(load_file_callback)
        self.controls_layout.addWidget(btn_load_file)

    def load_file1(self):
        """Carga el archivo 1 (.s1p) usando un diálogo de selección."""
        from pathlib import Path
        filepath, _ = QFileDialog.getOpenFileName(self, "Cargar referencia", "", "Ficheros Touchstone (*.s1p *.s2p)")
        if filepath:
            try:
                self.data1 = rf.Network(filepath)
                if Path(filepath).suffix == ".s1p":
                    self.S21_aire = self.data1.s[:, 0, 0]
                else:
                    self.S21_aire = self.data1.s[:, 1, 0]
                self.freq = self.data1.f
                self.label.setText(f'Medida aire cargada')  #Pra saber la ruta añadir: {filepath}
            except Exception as e:
                self.label.setText(f'Error al cargar la medida del aire: {e}')

    def load_file2(self):
        """Carga el archivo 2 (.s1p) usando un diálogo de selección."""
        from pathlib import Path
        filepath, _ = QFileDialog.getOpenFileName(self, "Cargar medida", "", "Ficheros Touchstone (*.s1p *.s2p)")
        if filepath:
            try:
                self.data2 = rf.Network(filepath)
                if Path(filepath).suffix == ".s1p":
                    self.S21_vidrio = self.data2.s[:, 0, 0]
                else:
                    self.S21_vidrio = self.data2.s[:, 1, 0]
                self.label.setText(f'Medida referencia cargada') # {filepath}
                self.run_processing_original() 
                self.run_processing_tiempo()
                self.run_processing_atenuacion()
            except Exception as e:
                self.label.setText(f'Error al cargar la medida de referencia: {e}')

    def load_file3(self):
        """Carga el archivo Excel usando un diálogo de selección."""
        filepath, _ = QFileDialog.getOpenFileName(self, "Cargar simulación", "", "Excel files (*.xlsx)")
        if filepath:
            try:
                self.sim = pd.read_excel(filepath, sheet_name=0).values
                self.freqHz = self.sim[:, 0] * 1e9
                self.simat = self.sim[:, 1]
                self.label.setText(f'Simulación cargada')# {filepath}
                self.run_processing_original() 
                self.run_processing_atenuacion()
            except Exception as e:
                self.label.setText(f'Error al cargar la simulación: {e}')




    #"""Crea un slider con una casilla de entrada al lado."""
    def add_slider(self, label, min_val, max_val, update_func):
        """Agregar un slider con su respectiva etiqueta y sincronizarlo con la casilla de texto."""
        # Crear la etiqueta del slider
        slider_label = QLabel(label, self)
        slider_label.setFixedHeight(20)  # Fijar altura de la etiqueta
        self.controls_layout.addWidget(slider_label)

        # Crear el slider
        slider = QSlider(Qt.Horizontal, self)
        slider.setMinimum(min_val)
        slider.setMaximum(max_val)
        if label == "Aire 1":
            slider.setValue(self.k1)
        elif label == "Aire 2":
            slider.setValue(self.kk1)
        elif label == "Vidrio 1":
            slider.setValue(self.k2)
        elif label == "Vidrio 2":
            slider.setValue(self.kk2)
        slider.setFixedHeight(20)  # Fijar altura del slider
        self.controls_layout.addWidget(slider)

        # Crear la casilla de texto
        slider_input = QLineEdit(self)
        slider_input.setValidator(QIntValidator(min_val, max_val))
        if label == "Aire 1":
            slider_input.setText(str(self.k1))
        elif label == "Aire 2":
            slider_input.setText(str(self.kk1))
        elif label == "Vidrio 1":
            slider_input.setText(str(self.k2))
        elif label == "Vidrio 2":
            slider_input.setText(str(self.kk2))
        slider_input.setFixedHeight(20)  # Fijar altura del campo de texto
        self.controls_layout.addWidget(slider_input)


        # Sincronizar slider con la entrada de texto
        slider.valueChanged.connect(lambda value: self.sync_input_with_slider(slider_input, value))
        slider_input.textChanged.connect(lambda text: self.sync_slider_with_input(slider, text))

        # Conexión del slider con la función de actualización
        slider.valueChanged.connect(update_func)

        return slider, slider_input

    def sync_input_with_slider(self, input_box, value):
        """Actualiza la casilla de texto cuando el slider cambia."""
        input_box.setText(str(value))

    def sync_slider_with_input(self, slider, text):
        """Actualiza el slider cuando el valor de la casilla de texto cambia."""
        try:
            value = int(text)
            if slider.minimum() <= value <= slider.maximum():
                slider.setValue(value)
        except ValueError:
            pass  # Ignorar entradas no válidas



    def update_slider_from_input(self, slider, input_box):
        """Actualiza el valor del slider desde el valor escrito en la casilla de entrada."""
        try:
            value = int(input_box.text())
            slider.setValue(value)
        except ValueError:
            pass  # Ignorar valores no válidos

    # Métodos para actualizar valores de k1, kk1, k2 y kk2
    def update_k1(self, value):
        self.k1 = value
        self.run_processing_tiempo()  # Actualiza la gráfica

    def update_kk1(self, value):
        self.kk1 = value
        self.run_processing_tiempo()  # Actualiza la gráfica

    def update_k2(self, value):
        self.k2 = value
        self.run_processing_tiempo()  # Actualiza la gráfica

    def update_kk2(self, value):
        self.kk2 = value
        self.run_processing_tiempo()  # Actualiza la gráfica

    # Cambiar tipo de ventana
    def set_window_type(self, text):
        # Establece el tipo de ventana seleccionada
        self.window_type = text

    def update_kaiser_beta(self, text):
        # Actualiza el valor de beta desde la entrada
        try:
            self.kaiser_beta = float(text)
        except ValueError:
            # Si el valor ingresado no es un número, usa el valor predeterminado
            self.kaiser_beta = 3


    def save_to_excel(self):
        """Guardar las frecuencias y las medidas en un archivo Excel."""
        if self.freq is None or self.atoriginal is None or self.simat is None or self.at is None:
            self.label.setText("Error: Falta cargar datos o realizar cálculos.")
            return

        # Crear un DataFrame con los datos
        try:
            data = {
                "Frecuencia (Hz)": self.freq,
                "Original ": np.abs(self.atoriginal),
                "Teórica ": np.abs(self.simat),
                #"Frecuencia procesada (Hz)": self.freq[1:-1],
                "Procesada ": np.abs(self.at)
            }
            df = pd.DataFrame(data)

            # Guardar el archivo Excel
            filepath, _ = QFileDialog.getSaveFileName(self, "Guardar datos", "", "Excel files (*.xlsx)")
            if filepath:
                df.to_excel(filepath, index=False)
                self.label.setText("Datos guardados exitosamente.")
        except Exception as e:
            self.label.setText(f"Error al guardar los datos: {e}")



    # Procesos creados
    def run_processing_original(self):
        # Procesa y muestra la gráfica original
        self.process_data_original()  
        self.label.setText('Gráfica Original mostrada')

    def run_processing_tiempo(self):
        self.process_data_tiempo()
        self.label.setText('Gráfica en el Tiempo mostrada')

    def run_processing_atenuacion(self):
        self.process_atenuacion()
        self.label.setText('Gráfica procesada mostrada')

    def process_data_original(self):
        # Procesamiento
        modAire = np.abs(self.S21_aire)
        phAire = np.angle(self.S21_aire)
        modVidrio = np.abs(self.S21_vidrio)
        phVidrio = np.angle(self.S21_vidrio)

        mod = modVidrio / modAire
        ph = phVidrio - phAire
        s21dif = mod * np.exp(1j * ph)
        self.atoriginal = -20 * np.log10(np.abs(s21dif))

        # Gráfica
        self.ax1.clear()
        self.ax1.plot(self.freq, self.atoriginal, label='Original')
        # Solo graficar datos simulados si están disponibles
        if self.simat is not None and self.freqHz is not None:
            self.ax1.plot(self.freqHz, self.simat, label='Teórica')
        self.ax1.legend()
        self.ax1.set_title("Gráfica Original")
        self.ax1.set_xlabel("Frecuencia (Hz)")
        self.ax1.set_ylabel("Atenuación (dB)")
        self.ax1.grid()
        self.canvas1.draw()

    def process_data_tiempo(self):
        # Muestreo
        sample_frequency1 = np.diff(self.freq)[0]  # Paso en frecuencia (primer valor)
        distance1 = 3e8 / (2 * sample_frequency1)  # Cálculo de la distancia
        self.distance_vector = np.linspace(0, distance1, len(self.freq) * self.N)

        # Aplicar ventana de Hann
        Hs=hann(201)
        s21_vidrio2 = self.S21_vidrio * Hs
        s21_aire2 = self.S21_aire * Hs

        # Transformada Inversa de Fourier
        self.s21_vidrio2t = ifft(s21_vidrio2, len(s21_vidrio2) * self.N)
        self.s21_aire2t = ifft(s21_aire2, len(s21_aire2) * self.N)

        # Corrimiento circular
        self.s21_vidrio2t = fftshift(self.s21_vidrio2t)
        self.s21_aire2t = fftshift(self.s21_aire2t)

        # Valores máximos y sus índices
        #m, i = np.max(np.abs(s21_vidrio2t)), np.argmax(np.abs(s21_vidrio2t))
        #m2, i2 = np.max(np.abs(s21_aire2t)), np.argmax(np.abs(s21_aire2t))

        # Graficar en el dominio del tiempo
        self.ax2.clear()
        self.ax2.plot(np.abs(self.s21_vidrio2t), label='Vidrio')
        self.ax2.plot(np.abs(self.s21_aire2t), label='Aire')
        self.ax2.axvline(x=self.k1, linestyle='--', color='red')
        self.ax2.axvline(x=self.kk1, linestyle='--', color='red')
        self.ax2.axvline(x=self.k2, linestyle='--', color='blue')
        self.ax2.axvline(x=self.kk2, linestyle='--', color='blue')
        self.ax2.legend()
        self.ax2.set_xlabel("Índice de Muestra")
        self.ax2.set_ylabel("Amplitud")
        self.ax2.set_title("Comparación de Amplitud en el Tiempo")
        self.ax2.grid()
        self.canvas2.draw()

    def process_atenuacion(self):
        # Parámetros para las ventanas
        target_length = len(self.s21_vidrio2t) # Longitud del vector de distancia
        m, i = np.max(np.abs(self.s21_vidrio2t)), np.argmax(np.abs(self.s21_vidrio2t))
        m2, i2 = np.max(np.abs(self.s21_aire2t)), np.argmax(np.abs(self.s21_aire2t))

        w1 = self.kk1 - self.k1
        w2 = self.kk2 - self.k2

        # Seleccionar la ventana según la opción elegida
        if self.window_type == 'Hann':
            Hs1_1 = hann(w1)
            Hs2_2 = hann(w2)
            Hs1 = np.concatenate([np.zeros((int(i - w1 // 2))-1), Hs1_1 ,np.zeros(int(target_length - i - w1 // 2))]) # Recortar o completar hasta la longitud exacta
            if len(Hs1) > 201:  # Si la longitud es mayor, quitar el último elemento
                Hs1 = Hs1[:-1]
            elif len(Hs1) < 201:  # Si la longitud es menor, añadir un cero al final
                Hs1 = np.concatenate([Hs1, [0]])
            
            Hs2 = np.concatenate([np.zeros((int(i2 - w2 // 2))-1),Hs2_2,np.zeros(int(target_length - i2 - w2 // 2))]) 
            # Ajustar la dimensión si no es 201
            if len(Hs2) > 201:  # Si la longitud es mayor, quitar el último elemento
                Hs2 = Hs2[:-1]
            elif len(Hs2) < 201:  # Si la longitud es menor, añadir un cero al final
                Hs2 = np.concatenate([Hs2, [0]])

            #print(len(self.s21_vidrio2t)) #201
            #print(len(Hs1)) #202 -->201 quitando un cero pasamos a 201
            #print(len(np.zeros((int(i - w1 // 2))-1))) #124 --->123 quitamos un cero para cuadrar dimensiones con matlab
            #print(len(Hs1_1)) #11
            #print(len(np.zeros(int(target_length - i - w1 // 2)))) #67
            
        elif self.window_type == 'Kaiser':
            Hs1_1 = kaiser(w1,  beta=self.kaiser_beta) 
            Hs2_2 = kaiser(w2,  beta=self.kaiser_beta)
            Hs1 = np.concatenate([np.zeros((self.k1)), Hs1_1, np.zeros(target_length  - self.k1 - w1)])
            Hs2 = np.concatenate([np.zeros((self.k2)), Hs2_2, np.zeros(target_length  - self.k2 - w2 )])
            #print(len(np.zeros((self.k1))))  #123
            #print(len(Hs1_1)) #11
            #print(len(np.zeros(target_length  - self.k1 - w1))) #67

        # Aplicación de la ventana al dominio de tiempo
        S21_vidrio_env = self.s21_vidrio2t * Hs2
        S21_aire_env = self.s21_aire2t * Hs1

        # FFT y normalización con ventana de Hann
        Hs5 = hann(201 * self.N)
        S21_vidrio_den = fft(S21_vidrio_env)
        S21_aire_den = fft(S21_aire_env)
        S21_vidrio_den2 = S21_vidrio_den / Hs5
        S21_aire_den2 = S21_aire_den / Hs5

        # Atenuación
        self.at = 20 * np.log10(np.abs(S21_aire_den2)) - 20 * np.log10(np.abs(S21_vidrio_den2))

        # Gráfica
        self.ax3.clear()
        self.ax3.plot(self.freq, self.atoriginal, label='Original')
        if self.simat is not None and self.freqHz is not None:
            self.ax3.plot(self.freqHz, self.simat, label='Teórica')
        self.ax3.plot(self.freq[1:-1], self.at[1:-1], label='Procesada')
        self.ax3.legend()
        self.ax3.axis([6e8, 6e9, 0, 60])
        self.ax3.set_title("Comparación de Atenuación")
        self.ax3.set_xlabel("Frecuencia (Hz)")
        self.ax3.set_ylabel("Atenuación (dB)")
        self.ax3.grid()
        self.canvas3.draw()

if __name__ == '__main__':
    app = QApplication(sys.argv)
    ex = MyApp()
    ex.show()
    sys.exit(app.exec_())

