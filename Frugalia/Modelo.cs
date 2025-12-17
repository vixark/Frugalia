// Copyright Notice:
//
// Frugalia® is a free LLM wrapper library that includes smart mechanisms to minimize AI API costs.
// Copyright© 2025 Vixark (vixark@outlook.com).
// For more information about Frugalia®, see https://frugalia.org.
//
// This program is free software: you can redistribute it and/or modify it under the terms of the GNU Affero
// General Public License as published by the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful, but without any warranty, without even the
// implied warranty of merchantability or fitness for a particular purpose. See the GNU Affero General Public
// License for more details.
//
// You should have received a copy of the GNU Affero General Public License along with this program. If not,
// see https://www.gnu.org/licenses.
//
// This License does not grant permission to use the trade names, trademarks, service marks, or product names
// of the Licensor, except as required for reasonable and customary use in describing the origin of the work
// and reproducing the content of the notice file.
//
// When redistributing this file, preserve this notice, as required by the GNU Affero General Public License.
//

using System;
using System.Collections.Generic;
using System.Text;
using static Frugalia.General;
using static Frugalia.GlobalFrugalia;


namespace Frugalia {


    public readonly struct Modelo {


        internal decimal PrecioEntradaNoCaché { get; }

        internal decimal PrecioEntradaCaché { get; }

        internal decimal PrecioSalidaNoRazonamiento { get; }

        internal decimal PrecioSalidaRazonamiento { get; }

        /// <summary>
        /// La caché se mantiene activa indefinidamente si se consulta en menos de los 5 minutos de límite. El precio de escritura de los tókenes es único, 
        /// es decir no se le suma precio de entrada a los tókenes que se escribirán en la caché. Claude funciona así.
        /// </summary>
        internal decimal? PrecioEscrituraManualCachéRefrescablePor5Minutos { get; } // Aplicable para modelos como Claude que no tienen caché automática si no manual. Si la usas constantemente (cada <5 min), la caché puede vivir “indefinidamente”.

        /// <summary>
        /// La caché se mantiene activa indefinidamente si se consulta en menos de la hora de límite. El precio de escritura de los tókenes es único, 
        /// es decir no se le suma precio de entrada a los tókenes que se escribirán en la caché. Claude funciona así.
        /// </summary>
        internal decimal? PrecioEscrituraManualCachéRefrescablePor60Minutos { get; } // Aplicable para modelos como Claude que no tienen caché automática si no manual.

        /// <summary>
        /// Costo de almacenar 1 millón de tókenes durante una hora. También se puede solicitar almacenamiento por menos de una hora y se cobra proporcional.
        /// Así funciona Gemini. No se ahorra el costo de entrada, igual se cobra. Por ejemplo, el costo de enviar a caché los 1M tókenes por 15 minutos es
        /// de 2 USD + 4,5 USD / 4 (15 minutos de almacenamiento en caché a 4,5 USD PrecioAlmacenamientoCachéPorHora.
        /// </summary>
        internal decimal? PrecioAlmacenamientoCachéPorHora { get; }

        /// <summary>
        /// Aplicable para modelos como GPT que tienen un límite a partir del cual activan la caché automática. Estos modelos no cobran el 
        /// costo de escritura de caché entonces se prestan para el truco del relleno de la instrucción del sistema para lograr ahorros. 
        /// </summary>
        internal int? LímiteTókenesActivaciónCachéAutomática { get; }

        public string Nombre { get; }

        internal string NombreModelo1NivelSuperior { get; } // Un modelo un nivel más grande que el actual.

        internal string NombreModelo2NivelesSuperior { get; } // Un modelo dos niveles más grande que el actual.

        internal string NombreModelo3NivelesSuperior { get; } // Un modelo tres niveles más grande que el actual. Se guarda principalmente detectar que el modelo que tenga un modelo tres niveles superior es un modelo muy pequeño. Por el momento no se hacen saltos de modelo de tres niveles con la función modo calidad adaptable.

        public Familia Familia { get; }

        /// <summary>
        /// Máximo de tókenes de entrada que la librería permitirá para este modelo. Se usa para no exceder los límites del modelo y en casos como Claude Sonnet,
        /// evitar cruzar el umbral a modos de precio más caros (por ejemplo Sonnet en versión de 1M tókenes de entrada, a la que se le llama Sonnet+).
        /// En el caso de los otros modelos no es tan exacto porque el límite en GPT por ejemplo incluye la salida, entonces el límite real estaría un poco
        /// antes del especificado en TókenesEntradaMáximos.
        /// </summary>
        private int TókenesEntradaMáximos { get; }

        /// <summary>
        /// Límite seguro de tókenes de entrada para evitar acercarse demasiado al límite máximo del modelo. Usado internamente para evitar errores por 
        /// exceder el límite.
        /// </summary>
        internal double TókenesEntradaLímiteSeguro => FactorSeguridadTókenesEntradaMáximos * TókenesEntradaMáximos;

        /// <summary>
        /// Descuento aplicable por operaciones en lote (no en tiempo real) que típicamente se tardan 24 horas. Los descuentos aplican para los PreciosEntrada 
        /// y PreciosSalida, pero no para PreciosEscrituraManualCaché.
        /// </summary>
        internal decimal FactorDescuentoEntradaYSalidaPorLote { get; }

        internal decimal? FactorDescuentoEscrituraCachéPorLote { get; }

        internal decimal FactorDescuentoLecturaCachePorLote { get; } // Gemini y OpenAI no aplican descuento, pero Claude sí.

        internal double? FactorÉxitoCaché { get; } // Si OpenAI funcionara bien no debería pasar que no se active la caché en el segundo mensaje de una conversación que tiene instrucciones del sistema rellenadas desde el primer mensaje, pero sí pasa. En algunos casos OpenAI simplemente ignora la caché. Se hizo un experimento rellenando más las instrucciones del sistema y queda demostrado que no es cuestión del tamaño de tokenes de la función ni que se cambie ni nada, simplemente a veces no lo coge, con 1294 tókenes hay más que suficiente para garantizar que toda la instruccion sistema es superior a 1024 . 1294 - 10 (o lo que sea del mensaje del usuario) - 73 de la función  = 1211 1: ENC = 1294 EC = 0 SNR = 103 SR = 0 2: ENC = 1402 EC = 0 SNR = 222 SR = 0 OpenAI describe el prompt caching como una optimización de “best effort”, no determinista como un contrato fuerte tipo: “si el prefijo coincide, 100 % te voy a servir desde caché”. Así que básicamente dicen, si no funciona, no me culpen. Lo mejor entonces es asumir un % de éxito que se incorporá en la fórmula para solo engordar las instrucciones del sistema que considerando ese porcentaje de éxito de uso de la caché logren ahorros. 0.8 es un valor que se tira al aire basado en un pequeño experimento limitado: se ejecutó 10 veces una conversación de 6-7 mensajes y se obtuvo una tasa de fallo de 13%, es decir un factor de éxito de 0.87. Después se hizo otro ensayo y se encontró que en algunos casos podría bajar hasta 75%, entonces este es un valor que puede estar entre 75% y 85%. Se elige el valor de 0.75 como límite inferior del rango observado, adoptando un enfoque conservador para evitar sobreestimar la efectividad de la caché, especialmente porque rellenar las instrucciones del sistema puede empeorar ligeramente el comportamiento del agente. Así, se prefiere asumir el peor caso razonable (0.75) para proteger costos y evitar optimizaciones demasiado agresivas. Se usa el mismo valor para todas las familias de modelos porque no se conoce aún su funcionamiento interno respecto a la caché. Actualización: Después de muchos ensayos se descubrió que los modelos diferentes tienen diferente factor de éxito de caché y además también depende de si se tiene activada la función grupo de caché, así que estos valores pasaron a ser propiedades de los modelos en vez de factores generales.

        internal double? FactorÉxitoCachéConGrupoCaché { get; }

        public string Descripción => $"{Familia} {Nombre}";

        internal List<RazonamientoEfectivo> RazonamientosEfectivosPermitidos { get; }

        internal List<Verbosidad> VerbosidadesPermitidas { get; }

        internal bool UsaCachéExtendida { get; }// Funcionalidad de modelos que activan la caché automáticamente (Por ejemplo, GPT) que permiten establecer la duración de la caché por varias horas sin costo adicional. En el caso de GPT por 24 horas.


        internal Modelo(string nombre, Familia familia, decimal precioEntradaNoCaché, decimal precioEntradaCaché, decimal precioSalidaNoRazonamiento,
            decimal precioSalidaRazonamiento, decimal? precioEscrituraManualCachéRefrescablePor5Minutos, decimal? precioEscrituraManualCachéRefrescablePor60Minutos,
            decimal? precioAlmacenamientoCachéPorHora, int? límiteTókenesActivaciónCachéAutomática, int tókenesEntradaMáximos,
            decimal factorDescuentoEntradaYSalidaPorLote, decimal factorDescuentoLecturaCachePorLote, decimal? factorDescuentoEscrituraCachéPorLote,
            bool usaCachéExtendida, string nombreModelo1NivelSuperior = "", string nombreModelo2NivelesSuperior = "", 
            string nombreModelo3NivelesSuperior = "", List<RazonamientoEfectivo> razonamientosEfectivosPermitidos = null, 
            List<RazonamientoEfectivo> razonamientosEfectivosNoPermitidos = null, List<Verbosidad> verbosidadesPermitidas = null, 
            List<Verbosidad> verbosidadesNoPermitidas = null, double? factorÉxitoCaché = null, double? factorÉxitoCachéConGrupoCaché = null) {

            Nombre = nombre;
            PrecioEntradaNoCaché = precioEntradaNoCaché;
            PrecioEntradaCaché = precioEntradaCaché;
            PrecioSalidaNoRazonamiento = precioSalidaNoRazonamiento;
            PrecioSalidaRazonamiento = precioSalidaRazonamiento;
            Familia = familia;
            NombreModelo1NivelSuperior = nombreModelo1NivelSuperior;
            NombreModelo2NivelesSuperior = nombreModelo2NivelesSuperior;
            NombreModelo3NivelesSuperior = nombreModelo3NivelesSuperior;
            PrecioEscrituraManualCachéRefrescablePor5Minutos = precioEscrituraManualCachéRefrescablePor5Minutos;
            PrecioEscrituraManualCachéRefrescablePor60Minutos = precioEscrituraManualCachéRefrescablePor60Minutos;
            PrecioAlmacenamientoCachéPorHora = precioAlmacenamientoCachéPorHora;
            LímiteTókenesActivaciónCachéAutomática = límiteTókenesActivaciónCachéAutomática;
            TókenesEntradaMáximos = tókenesEntradaMáximos;
            FactorDescuentoEntradaYSalidaPorLote = factorDescuentoEntradaYSalidaPorLote;
            FactorDescuentoEscrituraCachéPorLote = factorDescuentoEscrituraCachéPorLote;
            FactorDescuentoLecturaCachePorLote = factorDescuentoLecturaCachePorLote;
            UsaCachéExtendida = usaCachéExtendida; // Debido a que se encontró que no necesariamente habilitan la caché extendida para modelos nuevos como gpt-5-mini y si para viejos como gpt-4.1, se prefiere establecer por defecto usaCachéExtendida = falso, además porque principalmente aplica para modelos de la familia GPT. https://platform.openai.com/docs/guides/prompt-caching.
            
            if (LímiteTókenesActivaciónCachéAutomática != null && límiteTókenesActivaciónCachéAutomática > 0 
                && (factorÉxitoCaché == null || factorÉxitoCachéConGrupoCaché == null)) 
                    throw new Exception($"El modelo {nombre} que con LímiteTókenesActivaciónCachéAutomática > 0 debe establecer " +
                        $"su factorÉxitoCaché y factorÉxitoCachéConGrupoCaché.");
            
            RazonamientosEfectivosPermitidos = CompilarElementosPermitidos(razonamientosEfectivosPermitidos, razonamientosEfectivosNoPermitidos);
            VerbosidadesPermitidas = CompilarElementosPermitidos(verbosidadesPermitidas, verbosidadesNoPermitidas);
            FactorÉxitoCaché = factorÉxitoCaché;
            FactorÉxitoCachéConGrupoCaché = factorÉxitoCachéConGrupoCaché;

        } // Modelo>


        public override string ToString() => Nombre;


        public static Modelo? ObtenerModelo(string nombreModelo) {

            if (Modelos.TryGetValue(nombreModelo, out Modelo modelo)) {
                return modelo;
            } else {
                return null;
            }

        } // ObtenerModelo>


        internal static double ObtenerFactorDescuentoCaché(Modelo modelo) => (double)(modelo.PrecioEntradaCaché / (modelo).PrecioEntradaNoCaché);


        internal static Modelo? ObtenerModeloMejorado(Modelo modeloOriginal, CalidadAdaptable calidadAdaptable, int nivelMejoramientoSugerido,
            ref StringBuilder información) {

            var nivelMejoramiento = ObtenerNivelMejoramientoModeloEfectivo(calidadAdaptable, nivelMejoramientoSugerido);

            if (nivelMejoramiento == 0) { información.AgregarLínea($"No se mejoró el modelo."); return modeloOriginal; }
            if (nivelMejoramiento < 0 || nivelMejoramiento >= 3) throw new Exception("Parámetro incorrecto nivelesMejoramiento. Solo puede ser 1 o 2.");

            reintentar:
            var nombreModeloMejorado = nivelMejoramiento == 2 ? modeloOriginal.NombreModelo2NivelesSuperior : modeloOriginal.NombreModelo1NivelSuperior;
            if (nombreModeloMejorado.Contains("[deshabilitado]")) nombreModeloMejorado = "";
            var modeloMejorado = ObtenerModelo(nombreModeloMejorado); // Aquí podría buscar con un nombre de modelo vacío y está bien porque se controla posteriormente.
            if (modeloMejorado == null && nivelMejoramiento == 2) {
                información.AgregarLínea($"No se encontró un modelo dos niveles superior a {modeloOriginal} entonces se usó un modelo un nivel superior.");
                nivelMejoramiento = 1; // Si no hay un modelo dos niveles superior, se usa el que es un nivel superior.
                goto reintentar;
            }

            if (modeloMejorado == null) {

                if (string.IsNullOrEmpty(nombreModeloMejorado)) {
                    información.AgregarLínea($"No habían modelos superiores a {modeloOriginal} entonces no se pudo mejorar el modelo.");
                    return null; // Se acepta que sea vacío porque es posible que no haya modelos superiores a este.
                } else {
                    throw new Exception("Nombre del modelo mejorado no encontrado en tabla de modelos.");
                }

            } else {
                información.AgregarLínea($"Se mejoró el modelo {nivelMejoramiento.ALetras()} nivel{(nivelMejoramiento > 1 ? "es" : "")} de " +
                    $"{modeloOriginal} a {nombreModeloMejorado}.");
                return modeloMejorado;
            }

        } // ObtenerModeloMejorado>


        internal Tamaño ObtenerTamaño() {

            if (!string.IsNullOrEmpty(NombreModelo3NivelesSuperior)) {
                return Tamaño.MuyPequeño;
            } else if (!string.IsNullOrEmpty(NombreModelo2NivelesSuperior)) {
                return Tamaño.Pequeño;
            } else if (!string.IsNullOrEmpty(NombreModelo1NivelSuperior)) {
                return Tamaño.Medio;
            } else {
                return Tamaño.Grande;
            }

        } // ObtenerTamaño>


    } // Modelo>


} // Frugalia>