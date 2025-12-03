using System;
using static Frugalia.Global;



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
        /// costo de escritura de caché entonces se prestan para el truco del relleno de la instrucción de sistema para lograr ahorros. 
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
        internal int TókenesEntradaMáximos { get; }

        /// <summary>
        /// Descuento aplicable por operaciones en lote (no en tiempo real) que típicamente se tardan 24 horas. Los descuentos aplican para los PreciosEntrada 
        /// y PreciosSalida, pero no para PreciosEscrituraManualCaché.
        /// </summary>
        internal decimal FracciónDescuentoEntradaYSalidaPorLote { get; }

        internal decimal? FracciónDescuentoEscrituraCachéPorLote { get; }

        internal decimal FracciónDescuentoLecturaCachePorLote { get; } // Gemini y OpenAI no aplican descuento, pero Claude sí.


        internal Modelo(string nombre, Familia familia, decimal precioEntradaNoCaché, decimal precioEntradaCaché, decimal precioSalidaNoRazonamiento,
            decimal precioSalidaRazonamiento, decimal? precioEscrituraManualCachéRefrescablePor5Minutos, decimal? precioEscrituraManualCachéRefrescablePor60Minutos,
            decimal? precioAlmacenamientoCachéPorHora, int? límiteTókenesActivaciónCachéAutomática, int tókenesEntradaMáximos,
            decimal fracciónDescuentoEntradaYSalidaPorLote, decimal fracciónDescuentoLecturaCachePorLote, decimal? fracciónDescuentoEscrituraCachéPorLote,
            string nombreModelo1NivelSuperior = "", string nombreModelo2NivelesSuperior = "", string nombreModelo3NivelesSuperior = "") {

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
            FracciónDescuentoEntradaYSalidaPorLote = fracciónDescuentoEntradaYSalidaPorLote;
            FracciónDescuentoEscrituraCachéPorLote = fracciónDescuentoEscrituraCachéPorLote;
            FracciónDescuentoLecturaCachePorLote = fracciónDescuentoLecturaCachePorLote;

        } // ModeloIA>


        internal static Modelo? ObtenerModeloNulable(string nombreModelo) {

            if (Modelos.ContainsKey(nombreModelo)) {
                return Modelos[nombreModelo];
            } else {
                return null;
            }

        } // ObtenerModeloNulable>


        public static Modelo ObtenerModelo(string nombreModelo) {

            var modelo = ObtenerModeloNulable(nombreModelo);
            if (modelo == null) {
                throw new Exception($"No se encontró el modelo: {nombreModelo}");
            } else {
                return (Modelo)modelo;
            }

        } // ObtenerModelo>


        internal static double ObtenerFactorDescuentoCaché(string nombreModelo) {

            var modelo = ObtenerModeloNulable(nombreModelo);
            if (modelo == null) {
                return 1;
            } else {
                return (double)(((Modelo)modelo).PrecioEntradaCaché / ((Modelo)modelo).PrecioEntradaNoCaché);
            }

        } // ObtenerFactorDescuentoCaché>


        internal static string ObtenerModeloMejorado(string nombreModeloOriginal, int nivelesMejoramiento) {

            if (nivelesMejoramiento <= 0 || nivelesMejoramiento >= 3) throw new Exception("Parámetro incorrecto nivelesMejoramiento. Solo puede ser 1 o 2.");
            var modeloNulable = ObtenerModeloNulable(nombreModeloOriginal);
            if (modeloNulable == null) {
                throw new Exception("Modelo original no encontrado en tabla de modelos. No se puede encontrar el modelo mejorado");
            } else {

                var modelo = (Modelo)modeloNulable;
                reintentar:
                var nombreModeloMejorado = nivelesMejoramiento == 2 ? modelo.NombreModelo2NivelesSuperior : modelo.NombreModelo1NivelSuperior;
                if (nombreModeloMejorado.Contains("[deshabilitado]")) nombreModeloMejorado = "";
                var modeloMejorado = ObtenerModeloNulable(nombreModeloMejorado); // Aquí podría buscar con un nombre de modelo vacío y está bien porque se controla posteriormente.
                if (modeloMejorado == null && nivelesMejoramiento == 2) {
                    nivelesMejoramiento = 1; // Si no hay un modelo 2 niveles superior, se usa el que es un nivel superior.
                    goto reintentar;
                }

                if (modeloMejorado == null) {

                    if (string.IsNullOrEmpty(nombreModeloMejorado)) {
                        return ""; // Se acepta que sea vacío porque es posible que no haya modelos superiores a este.
                    } else {
                        throw new Exception("Nombre del modelo mejorado no encontrado en tabla de modelos.");
                    }

                } else {
                    return ((Modelo)modeloMejorado).Nombre;
                }

            }

        } // ObtenerModeloMejorado>


    } // Modelo>



} // Frugalia>