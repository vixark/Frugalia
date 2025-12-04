using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;


namespace Frugalia {


    public enum Razonamiento {
        Ninguno, Bajo, Medio, Alto, // Se configuran solo 4 niveles de razonamiento, en línea con GPT 5.1. El valor Ninguno se mapea al nivel más bajo disponible en otros modelos como 'minimal' en GPT 5 o su equivalentes en otras familias.
        NingunoOMayor, BajoOMayor, MedioOMayor, // Adaptables: Los modelos de OpenAI ya adaptan internamente cuántos tókenes de razonamiento usan según la dificultad de la tarea, pero aquí se añade una capa extra de control para proteger costos. Por ejemplo, con NingunoOMayor se usa Ninguno (más barato) y sólo se sube a Bajo o Medio cuando la entrada es muy larga, de modo que el modelo tenga más margen de razonamiento cuando realmente lo necesita, sin arriesgarse a gastar de más en casos simples. Esto logra que si se estima que una tarea puede funcionar con Ninguno, se puede poner en NingunoOMayor para que la mayoría de las veces se ejecute con Ninguno y no hayan tókenes de razonamiento, y que cuando excepcionalmente ser requiera procesar textos más largos, que podrían requerir más razonamiento, se adapte a uno de los niveles de razonamiento superiores.
    }

    public enum Verbosidad { Baja, Media, Alta }

    public enum CalidadAdaptable { Ninguna, MejorarModelo, MejorarModeloYRazonamiento }; // Si se usa un modo de calidad adaptable, el modelo contestará con alguno de estos textos [LoHiceBien], [ModeloMedioRecomendado] o [ModeloGrandeRecomendado]. Si se tiene el modo MejorarModelo se realizará nuevamente la consulta usando un modelo inmediatamente superior al actual (si es posible). Si se tiene el modo MejorarModeloYRazonamiento se mejora tanto el modelo como el razonamiento (si es posible). Si responde  [ModeloGrandeRecomendado] se hacen dos incrementos de nivel de golpe.

    public enum RestricciónRazonamiento { // Al cambiar de gpt-5.1 a gpt-5-nano manteniendo Razonamiento = Alto en un experimento se disparó la cantidad de tókenes de salida, incluso con respuestas muy cortas, y la calidad fue peor. Esto pasa porque el razonamiento genera muchos pasos de pensamiento internos (tókenes ocultos pero facturados). En modelos muy pequeños como nano, el modelo necesita más pasos para llegar a la misma conclusión, gastando muchos tókenes y contrarrestando en parte el ahorro esperado por el menor precio del modelo. Por eso, para gpt-5-nano se debe considerar restringir el razonamiento alto.
        Ninguna,
        ModelosMuyPequeños,
        ModelosPequeños
    }

    public enum Tamaño {
        MuyPequeño, // Modelos que tienen 3 niveles de modelos superiores a ellos en su familia.
        Pequeño, // Modelos que tienen 2 niveles de modelos superiores a ellos en su familia.
        Medio, // Modelos que tienen 1 modelo superior a ellos en su familia.
        Grande // Modelos que no tienen modelos superiores a ellos en su familia.
    }

    public enum TipoArchivo { Pdf, Imagen }

    public enum Familia {
        GPT, // Familia de OpenAI, referencia generalista con muy buen rendimiento en razonamiento y código.
        Claude, // Modelos de Anthropic, muy fuertes en razonamiento largo, código y uso corporativo.
        Gemini, // Familia multimodal de Google, buena integración con el ecosistema y servicios de Google Cloud.
        Mistral, // Modelos ligeros y eficientes, muy competitivos en relación coste/rendimiento.
        Llama, // Familia pesos-abiertos de Meta, estándar de facto para despliegues autoalojados.
        DeepSeek, // Modelos tipo mezcla de expertos muy potentes y baratos, especialmente fuertes en razonamiento y generación de código.
        Qwen, // Familia multilingüe de Alibaba, muy buenos resultados en chino y otros idiomas como el español.
        GLM // Familia de Zhipu, centrada en razonamiento y agentes, alternativa china de bajo costo.
    }

    public enum TratamientoNegritas {
        Ninguno, // Mantiene el formato de negritas que devuelve el modelo. Por ejemplo en el caso de ChatGPT mantiene los dobles asteriscos: **negrita**.
        Eliminar,
        ConvertirEnHtml
    }


    public static class Global {


        public static string DobleLínea = $"{Environment.NewLine}{Environment.NewLine}";

        internal const string LoHiceBien = "[lo-hice-bien]";

        internal const string MedioRecomendado = "[modelo-medio-recomendado]";

        internal const string GrandeRecomendado = "[modelo-grande-recomendado]";

        internal const string Deshabilitado = "[deshabilitado]";

        internal const string Fin = ".[fin].";

        internal static readonly Dictionary<string, Modelo> Modelos = new Dictionary<string, Modelo>() { // Para control de costos por el momento se deshabilita el modelo gpt-5-pro. Los que se quieran deshabilitar silenciosamente se les pone {Deshabilitado} en el nombre de modelos mejorados (no en la clave del diccionario) para que no saque excepción y lo ignore como si no existiera.
            { "gpt-5-pro", new Modelo("gpt-5-pro", Familia.GPT, 15, 15, 120, 120, null, null, null, null, 400000, 0.5m, 1, null) }, // https://openai.com/api/pricing/. No tiene descuento para tókenes de entrada de caché y por lo tanto tampoco tiene límite de tókenes para activación automática de caché.
            { "gpt-5.1", new Modelo("gpt-5.1", Familia.GPT, 1.25m, 0.125m, 10, 10, null, null, null, 1024, 400000, 0.5m, 1, null, // A Noviembre 2025 ChatGPT cobra igual los tókenes de salida de razonamiento que los de salida de no razonamiento.
                $"gpt-5-pro{Deshabilitado}") },
            { "gpt-5-mini", new Modelo("gpt-5-mini", Familia.GPT, 0.25m, 0.025m, 2, 2, null, null, null, 1024, 400000, 0.5m, 1, null,
                "gpt-5.1", $"gpt-5-pro{Deshabilitado}") },
            { "gpt-5-nano", new Modelo("gpt-5-nano", Familia.GPT, 0.05m, 0.005m, 0.4m, 0.4m, null, null, null, 1024, 400000, 0.5m, 1, null,
                "gpt-5-mini", "gpt-5.1", $"gpt-5-pro{Deshabilitado}") },
            { "claude-opus-4-5", new Modelo("claude-opus-4-5", Familia.Claude, 5, 0.5m, 25, 25, 6.25m, 10, null, null, 200000, 0.5m, 0.5m, 0.5m) }, // https://claude.com/pricing#api.
            { "claude-sonnet-4-5", new Modelo("claude-sonnet-4-5", Familia.Claude, 3, 0.3m, 15, 15, 3.75m, 6, null, null, 200000, 0.5m, 0.5m, 0.5m,
                "claude-opus-4-5") },
            { "claude-sonnet+-4-5" , new Modelo("claude-sonnet+-4-5", Familia.Claude, 6, 0.6m, 22.5m, 22.5m, 7.5m, 12, null, null, 1000000, 0.5m, 0.5m, 0.5m) }, // Modelo de contexto muy grande, útil para el procesamiento de textos muy grandes. 
            { "claude-haiku-4-5", new Modelo("claude-haiku-4-5", Familia.Claude, 1, 0.1m, 5, 5, 1.25m, 2, null, null, 200000, 0.5m, 0.5m, 0.5m,
                "claude-sonnet-4-5", "claude-opus-4-5") },
            { "gemini-3-pro-preview", new Modelo("gemini-3-pro-preview", Familia.Gemini, 2, 0.2m, 12, 12, null, null, 4.5m, null, 200000, 0.5m, 1, null) }, // https://ai.google.dev/gemini-api/docs/pricing.
            { "gemini-3-pro+-preview", new Modelo("gemini-3-pro+-preview", Familia.Gemini, 4, 0.4m, 18, 18, null, null, 4.5m, null, 1048576, 0.5m, 1, null) },
        };


        internal static string LeerClave(string rutaArchivo, out string error) {

            error = null;
            if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo)) {
                error = "No se encontró el archivo con la clave de la API.";
                return null;
            }
            var contenido = File.ReadAllText(rutaArchivo).Trim();
            if (string.IsNullOrWhiteSpace(contenido)) {
                error = "El archivo de la API key está vacío.";
                return null;
            }
            return contenido;

        } // LeerClave>


        internal static int ObtenerLargoInstrucciónÚtil(string últimaInstrucción, string instrucciónSistema, string rellenoInstrucciónSistema)
            => Math.Max((últimaInstrucción?.Length ?? 0) + (instrucciónSistema?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0), 0);


        internal static Razonamiento ObtenerRazonamientoMejorado(Razonamiento razonamiento, int nivelesMejoramiento) {

            if (nivelesMejoramiento <= 0 || nivelesMejoramiento >= 3) throw new Exception("Parámetro incorrecto nivelesMejoramiento. Solo puede ser 1 o 2.");

            if (nivelesMejoramiento == 1) {

                switch (razonamiento) {
                case Razonamiento.Ninguno:
                    return Razonamiento.Bajo;
                case Razonamiento.Bajo:
                    return Razonamiento.Medio;
                case Razonamiento.Medio:
                    return Razonamiento.Alto;
                case Razonamiento.Alto:
                    return Razonamiento.Alto; // Permanece igual.
                case Razonamiento.NingunoOMayor:
                    return Razonamiento.BajoOMayor;
                case Razonamiento.BajoOMayor:
                    return Razonamiento.MedioOMayor;
                case Razonamiento.MedioOMayor:
                    return Razonamiento.Alto; // No hay adaptable con Alto. Va directo a Alto.
                default:
                    throw new Exception("Valor de razonamiento incorrecto.");
                }

            } else { // Caso 2.

                switch (razonamiento) {
                case Razonamiento.Ninguno:
                    return Razonamiento.Medio;
                case Razonamiento.Bajo:
                    return Razonamiento.Alto;
                case Razonamiento.Medio:
                    return Razonamiento.Alto; // No se puede subir dos niveles entonces se queda en Alto.
                case Razonamiento.Alto:
                    return Razonamiento.Alto; // Permanece igual.
                case Razonamiento.NingunoOMayor:
                    return Razonamiento.MedioOMayor;
                case Razonamiento.BajoOMayor:
                    return Razonamiento.Alto; // No se puede subir dos niveles entonces se queda en Alto.
                case Razonamiento.MedioOMayor:
                    return Razonamiento.Alto; // No hay adaptable con Alto. Va directo a Alto.
                default:
                    throw new Exception("Valor de razonamiento incorrecto.");
                }

            }

        } // ObtenerRazonamientoMejorado>


        internal static Tamaño ObtenerTamaño(string nombreModelo) {

            var modelo = Modelo.ObtenerModeloNulable(nombreModelo) ?? throw new Exception($"No se pudo obtener el tamaño porque el modelo {nombreModelo} no existe.");
            if (!string.IsNullOrEmpty(modelo.NombreModelo3NivelesSuperior)) {
                return Tamaño.MuyPequeño;
            } else if (!string.IsNullOrEmpty(modelo.NombreModelo2NivelesSuperior)) {
                return Tamaño.Pequeño;
            } else if (!string.IsNullOrEmpty(modelo.NombreModelo1NivelSuperior)) {
                return Tamaño.Medio;
            } else {
                return Tamaño.Grande;
            }

        } // ObtenerTamaño>


        internal static Razonamiento ObtenerRazonamientoEfectivo(Razonamiento razonamiento, RestricciónRazonamiento restricciónRazonamientoAlto,
            RestricciónRazonamiento restricciónRazonamientoMedio, string nombreModelo, int largoInstrucciónÚtil) {

            var largoLímite1 = 500; // Aproximadamente 166 tókenes. Los límites de 500 y 2000 caracteres son a criterio. Se prefiere subir el razonamiento un poco antes (pagando algo más) para reducir errores, repreguntas y consultas repetidas (que valen más), que a la larga también consumen tókenes y empeoran la experiencia de usuario. Se encontró que cuando los textos son muy largos el agente se confunde y olvida cosas como preguntar un dato necesario para la función, al subir el nivel de razonamiento disminuye un poco este efecto.
            var largoLímite2 = 2000; // Aproximadamente 666 tokenes.

            var razonamientoEfectivo = razonamiento; // Se hace esta copia porque se reescribirá en esta función.
            if (razonamientoEfectivo == Razonamiento.NingunoOMayor) {

                if (largoInstrucciónÚtil < largoLímite1) {
                    razonamientoEfectivo = Razonamiento.Ninguno;
                } else if (largoInstrucciónÚtil < largoLímite2) {
                    razonamientoEfectivo = Razonamiento.Bajo;
                } else {
                    razonamientoEfectivo = Razonamiento.Medio;
                }

            } else if (razonamientoEfectivo == Razonamiento.BajoOMayor) {

                if (largoInstrucciónÚtil < largoLímite1) {
                    razonamientoEfectivo = Razonamiento.Bajo;
                } else if (largoInstrucciónÚtil < largoLímite2) {
                    razonamientoEfectivo = Razonamiento.Medio;
                } else {
                    razonamientoEfectivo = Razonamiento.Alto;
                }

            } else if (razonamientoEfectivo == Razonamiento.MedioOMayor) {

                if (largoInstrucciónÚtil < largoLímite1) {
                    razonamientoEfectivo = Razonamiento.Medio;
                } else {
                    razonamientoEfectivo = Razonamiento.Alto;
                }

            }

            var tamaño = ObtenerTamaño(nombreModelo);

            if (razonamientoEfectivo == Razonamiento.Alto && restricciónRazonamientoAlto != RestricciónRazonamiento.Ninguna) {

                if (restricciónRazonamientoAlto == RestricciónRazonamiento.ModelosPequeños) {
                    if (tamaño == Tamaño.MuyPequeño || tamaño == Tamaño.Pequeño) razonamientoEfectivo = Razonamiento.Medio;
                } else if (restricciónRazonamientoAlto == RestricciónRazonamiento.ModelosMuyPequeños) {
                    if (tamaño == Tamaño.MuyPequeño) razonamientoEfectivo = Razonamiento.Medio;
                }

            }

            if (razonamientoEfectivo == Razonamiento.Medio && restricciónRazonamientoMedio != RestricciónRazonamiento.Ninguna) { // Se debe poner después de la revisión de razonamiento Alto porque es posible que tenga doble restricción, entonces el anterior código lo pasa a razonamiento medio y el siguiente verifica si se debe pasar a razonamiento bajo.

                if (restricciónRazonamientoMedio == RestricciónRazonamiento.ModelosPequeños) {
                    if (tamaño == Tamaño.MuyPequeño || tamaño == Tamaño.Pequeño) razonamientoEfectivo = Razonamiento.Bajo;
                } else if (restricciónRazonamientoMedio == RestricciónRazonamiento.ModelosMuyPequeños) {
                    if (tamaño == Tamaño.MuyPequeño) razonamientoEfectivo = Razonamiento.Bajo;
                }

            }

            return razonamientoEfectivo;

        } // ObtenerRazonamientoEfectivo>


        public static Dictionary<string, Tókenes> AgregarSumando(this Dictionary<string, Tókenes> diccionario, Tókenes tókenes) {

            if (diccionario == null) diccionario = new Dictionary<string, Tókenes>();
            var clave = tókenes.Clave;
            if (diccionario.ContainsKey(clave)) {
                diccionario[clave] += tókenes;
            } else {
                diccionario.Add(clave, tókenes);
            }
            return diccionario;

        } // AgregarSumando>


        internal static List<(string Nombre, string Valor)> ExtraerParámetros(JsonDocument json) {

            if (json == null) return new List<(string Nombre, string Valor)> { };
            var resultado = new List<(string Nombre, string Valor)>();
            foreach (var propiedad in json.RootElement.EnumerateObject()) {
                var nombre = propiedad.Name.ToLowerInvariant();
                var valor = ATexto(propiedad.Value);
                resultado.Add((nombre, valor));
            }
            return resultado;

        } // ExtraerParámetros>


        private static string ATexto(JsonElement elemento) {

            switch (elemento.ValueKind) {
            case JsonValueKind.String:
                return elemento.GetString();
            case JsonValueKind.Number:
                Suspender(); // Verificar cuando suceda.
                return elemento.GetRawText(); // Devuelve el texto crudo: 123, 3.14, etc.
            case JsonValueKind.True:
            case JsonValueKind.False:
                Suspender(); // Verificar cuando suceda.
                return elemento.GetBoolean().ToString();
            case JsonValueKind.Null:
                Suspender(); // Verificar cuando suceda.
                return null;
            default:
                Suspender(); // Verificar cuando suceda.
                return elemento.GetRawText(); // Objetos, vectores, etc: se devuelve crudo.
            }

        } // ATexto>


        /// <summary>
        /// Ayudante para evitar el estado inestable en el que puede quedar el depurador después de una excepción en algunos casos.
        /// No se recomienda usar frecuentemente. Lo recomendable es usar en la medida de lo posible las excepciones normales. 
        /// Solo usar esta función cuando el editor esté poniendo problema.
        /// </summary>
        /// <param name="mensaje"></param>
        internal static void LanzarExcepción(string mensaje) {

            Suspender();
            #if !DEBUG
                throw new Exception(mensaje);
            #endif

        } // LanzarExcepciónYSuspender>


        internal static void Suspender() {

            #if DEBUG
                Debugger.Break();
            #endif

        } // Suspender>


        public static int ObtenerAleatorio(int mínimo, int máximo) {

            var nuevoAleatorio = new Random();
            return nuevoAleatorio.Next(mínimo, máximo);

        } // ObtenerAleatorio>


    } // Global>


} // Frugalia>