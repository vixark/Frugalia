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
using System.IO;
using System.Text;
using System.Text.Json;
using static Frugalia.General;


namespace Frugalia {


    public enum Razonamiento {
        Ninguno, Bajo, Medio, Alto, // Se configuran solo 4 niveles de razonamiento, en línea con GPT 5.1. El valor Ninguno se mapea al nivel más bajo disponible en otros modelos como 'minimal' en GPT 5 o su equivalentes en otras familias.
        NingunoOBajo, BajoOMedio, MedioOAlto, // Adaptables de dos opciones: Los modelos de OpenAI ya adaptan internamente cuántos tókenes de razonamiento usan según la dificultad de la tarea, pero aquí se añade una capa extra de control para proteger costos. Por ejemplo, con NingunoOBajo se usa Ninguno (más barato) y sólo se sube a Bajo cuando la entrada es muy larga, de modo que el modelo tenga más margen de razonamiento cuando realmente lo necesita, sin arriesgarse a gastar de más en casos simples. Esto logra que si se estima que una tarea puede funcionar con Ninguno, se puede poner en NingunoOBajo para que la mayoría de las veces se ejecute con Ninguno y no hayan tókenes de razonamiento, y que cuando excepcionalmente ser requiera procesar textos más largos, que podrían requerir más razonamiento, se adapte a uno de los niveles de razonamiento superiores. Se usa la segunda opción si el texto de instrucción supera CarácteresLímiteInstrucciónParaSubirRazonamiento. 
        NingunoBajoOMedio, BajoMedioOAlto // Adaptables de tres opciones: Dan más granularidad al usuario de la librería para permitir que el modelo suba hasta dos niveles de razonamiento desde Ninguno hasta Medio o desde Bajo hasta Alto. Se usa la tercera opción si el texto de instrucción supera CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles. 
    }

    internal enum RazonamientoEfectivo { // La enumeración que se usa efectivamente en los modelos. Se separa de la otra para facilitar la depuración y evitar errores al estar escribiendo integraciones.
        Ninguno, Bajo, Medio, Alto
    }

    public enum Verbosidad { Baja, Media, Alta }

    public enum CalidadAdaptable {  // Si se usa un modo de calidad adaptable, el modelo contestará con alguno de estos textos LoHiceBien, UsaModeloMejor o UsaModeloMuchoMejor. Si se tiene el modo MejorarModelo se realizará nuevamente la consulta usando un modelo inmediatamente superior al actual (si es posible). Si se tiene el modo MejorarModeloYRazonamiento se mejora tanto el modelo como el razonamiento (si es posible). Si se eligen las opciones que incluyen mejoras de dos niveles, cuando el modelo responde UsaModeloMuchoMejor se realizan dos mejoras del modelo o dos mejoras al nivel de razonamiento según corresponda. Se prefiere mantener una configuración unificada porque solo se soportarán dos niveles de mejoramiento y la mayoría de los usuarios de la libreria usarían solo el salto de un nivel entonces están contenidos todos casos y los casos de más uso son simples y entendibles: Ninguna, MejorarModelo, MejorarRazonamiento y MejorarModeloYRazonamiento.
        No, 
        MejorarModelo, 
        MejorarRazonamiento,
        MejorarModeloYRazonamiento,
        MejorarModeloDosNiveles,
        MejorarRazonamientoDosNiveles,
        MejorarModeloYRazonamientoDosNiveles,
        MejorarModeloUnNivelYRazonamientoDosNiveles,
        MejorarModeloDosNivelesYRazonamientoUnNivel
    };

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

    public enum Longitud {
        Larga,
        Media,
        Corta
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

    public enum Resultado {
        Respondido,
        Abortado,        
        MáximosTókenesAlcanzados,
        MáximasIteracionesConFunción,
        SinAutoevaluación,
        OtroError
    }

    public enum RestricciónTókenesSalida {
        Alta,
        Media,
        Baja,
        Ninguna // No se debería activar porque libera al modelo de gastar lo que él quiera.
    }

    public enum RestricciónTókenesRazonamiento {
        Alta,
        Media,
        Baja,
        Ninguna // No se debería activar porque libera al modelo de gastar lo que él quiera.
    }

    public enum TratamientoNegritas {
        Ninguno, // Mantiene el formato de negritas que devuelve el modelo. Por ejemplo en el caso de ChatGPT mantiene los dobles asteriscos: **negrita**.
        Eliminar,
        ConvertirEnHtml
    }

    public enum TipoMensaje {
        Todos, // Mensajes tanto del asistente IA como del usuario. 
        Usuario,
        AsistenteIA // Solo mensajes del asistente IA.
    } // TipoMensaje>

   
    public static class GlobalFrugalia { // Funciones y constantes auxiliares de lógica de negocio que solo aplican en esta librería. Se diferencia de General que contiene funciones que se podrían copiar y pegar en otros proyectos.
  

        internal const string LoHiceBien = "[lo-hice-bien]";

        internal const string UsaModeloMejor = "[usa-modelo-mejor]";

        internal const string UsaModeloMuchoMejor = "[usa-modelo-mucho-mejor]";

        internal const string Deshabilitado = "[deshabilitado]";

        internal const string Fin = ".[fin].";

        internal readonly static string InstrucciónAutoevaluación = "\n\nPrimero responde normalmente al usuario.\n\nDespués evalúa tu " +
            "propia respuesta y en la última línea escribe exactamente una de estas etiquetas, sola, sin dar explicaciones de tu elección:\n\n" +
            $"{LoHiceBien}\n{UsaModeloMejor}\n{UsaModeloMuchoMejor}\n\nUsa:\n{LoHiceBien}: Si tu respuesta fue buena, tiene " +
            $"sentido y es completa. Entendiste bien la consulta y es relativamente sencilla.\n{UsaModeloMejor}: Si tu respuesta no fue " +
            "buena en calidad, sentido o completitud, o si a la consulta le faltan detalles, contexto o no la entiendes bien.\n" +
            $"{UsaModeloMuchoMejor}: Si la consulta es muy compleja, requiere conocimiento experto o trata temas delicados."; // Alrededor de 650 carácteres.

        internal const int CarácteresLímiteInstrucciónParaSubirRazonamiento = 750; // Aproximadamente 250 tókenes. Los límites de 750 y 2400 caracteres son a criterio. Se prefiere subir el razonamiento un poco antes (pagando algo más) para reducir errores, repreguntas y consultas repetidas (que valen más), que a la larga también consumen tókenes y empeoran la experiencia de usuario. Se encontró que cuando los textos son muy largos el agente se confunde y olvida cosas como preguntar un dato necesario para la función, al subir el nivel de razonamiento disminuye un poco este efecto. El valor de 750 es ligeramente superior a la longitud de InstrucciónAutoevaluación, por lo tanto al establecer CalidadAdaptable diferente de No y Razonamiento adaptable y sumarle la longitud de instrucción del sistema y de usuario, casi siempre subirá de razonamiento.
        
        internal const int CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles = 2400; // Aproximadamente 800 tokenes.

        internal const int MáximosTókenesSalidaBaseVerbosidadBaja = 200;

        internal const int MáximosTókenesSalidaBaseVerbosidadMedia = 350;

        internal const int MáximosTókenesSalidaBaseVerbosidadAlta = 500;

        internal const int MáximosTókenesRazonamientoBaseNinguno = 0;

        internal const int MáximosTókenesRazonamientoBaseBajo = 1000;

        internal const int MáximosTókenesRazonamientoBaseMedio = 2500;

        internal const int MáximosTókenesRazonamientoBaseAlto = 5000;

        internal const int SinLímiteTókenes = int.MaxValue;

        public const double FactorÉxitoCaché = 0.8; // Si OpenAI funcionara bien no debería pasar que no se active la caché en el segundo mensaje de una conversación que tiene instrucciones del sistema rellenadas desde el primer mensaje, pero sí pasa. En algunos casos OpenAI simplemente ignora la caché por razones desconocidas. Se hizo un experimento inflando más las instrucciones del sistema y queda demostrado que no es cuestión del tamaño de tokenes de la función ni que se cambie ni nada, simplemente a veces no lo coje, con 1294 tókenes hay más que suficiente para garantizar que toda la instruccion sistema es superior a 1024 . 1294 - 10 (o lo que sea de la instrucción de usuario) - 73 de la función  = 1211 1: ENC = 1294 EC = 0 SNR = 103 SR = 0 2: ENC = 1402 EC = 0 SNR = 222 SR = 0 OpenAI describe el prompt caching como una optimización de “best effort”, no determinista como un contrato fuerte tipo: “si el prefijo coincide, 100 % te voy a servir desde caché”. Así que básicamente dicen, si no funciona, no me culpen. Lo mejor entonces es asumir un % de éxito que se incorporá en la fórmula para solo engordar las instrucciones del sistema que considerando ese porcentaje de éxito de uso de la caché logren ahorros. 0.8 es un valor que se tira al aire basado en un pequeño experimento limitado: se ejecutó 10 veces una conversación de 6-7 mensajes y se obtuvo una tasa de fallo de 13%, es decir un factor de éxito de 0.87, sin embargo debido a que hay incertidumbre con este número y a que hay una ligera demesmejoría en el comportamiento del agente cuando se rellenan las instrucciones del sistema, se prefiere dejar en 0.8. Se usa el mismo valor para las otras familias de modelos porque no se conoce aún su funcionamiento.

        internal const int CarácteresPorTokenTípicos = 3; // Aplica para mensajes del usuario, mensajes del asistente IA, archivos y funciones.

        internal const int CarácteresPorTokenInstrucciónSistemaTípicos = 4; // La necesidad o no de rellenar la instrucción del sistema se decide usando valor promedio de 4 carácteres por tókenes y el relleno se hace con un exceso de tokenes (carácteresPorTokenMáximos) para asegurar que se generen suficientes carácteres para que con seguridad supere el límite para activar la caché (tókenesObjetivo). El valor de 4 carácteres por token se obtuvo de controlar eliminando los tókenes que consumía la función, así 340 tókenes (-73 función) = 267 tókenes para 1061 carácteres = 3.97 char/tk (para el primer mensaje de mensaje de usuario + instrucción del sistema sin rellenar). Para textos más normales que no sean instrucciones del sistema (que suelen tener frases cortas densas, referencias, datos, etc) suelen ser 3 carácteres por token. Pero como aquí se está intentando ajustar es instrucciones del sistema se trabaja con 4.

        internal const double CarácteresPorTokenRellenoMáximos = 5.5; // Se usa 5.5 como caso límite. Se asegura agregar suficientes carácteres para que supere los tókenes requeridos. Se hizo un experimento y se encontró esto: Sin relleno 340 tk y 1061 char: 3.12 char/tk, con relleno 955 tk y 4183 char: 4.38 char/ tk, solo el relleno: 615tk y 3122 char: 5.07 char/ tk. Este mismo experimento se repitió para el caso de usar solo el texto relleno sin lorems (solo para fines de calcular su cantidad de carácteres por token) y se encontró que es 4.75 char/tk. También se hizo el experimento únicamente con lorems (sin texto introductorio) y dio otra vez 5.07 char/tk, así que esto es algo inconsistente matemáticamente porque podrían haber cosas desconocidas de cómo el modelo calcula los tókenes, entonces para pecar por seguro, se usa 5.5 carácteres por token para el texto de relleno. Esto asegura que el relleno garantiza con cierto margen de seguridad que se active la caché. Se debe poner un valor superior porque hay incertidumbre de que tal vez el modelo cambié la forma de cálculo de tókenes y de pronto llegue a ser 5.3 o 5.2, y si así fuera y se hubiera puesto un valor muy ajustado como 5.1, no se activaría la caché y se gastaría innecesariamente en tókenes inutiles no en caché.

        internal const double FactorSeguridadTókenesEntradaMáximos = 0.75; // Para evitar sobrepasar el límite de tókenes de entrada del modelo, se usa un factor de seguridad del 75%. Esto es porque a veces la estimación de tókenes puede ser imprecisa y se corre el riesgo de exceder el límite permitido por el modelo, lo que causaría incremento de costos o errores en la solicitud. Al usar este factor, se garantiza que la cantidad estimada de tókenes de entrada esté por debajo del límite máximo, proporcionando un margen adicional para evitar errores. El 75% se obtiene de 3/4 que es un rango de carácteres tókenes típico promedio a típico máximo.


        internal static readonly Dictionary<string, Modelo> Modelos = new Dictionary<string, Modelo>(StringComparer.OrdinalIgnoreCase) { // Para control de costos por el momento se deshabilita el modelo gpt-5-pro. Los que se quieran deshabilitar silenciosamente se les pone {Deshabilitado} en el nombre de modelos mejorados (no en la clave del diccionario) para que no saque excepción y lo ignore como si no existiera.
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


        public static string LeerClave(string rutaArchivo, out string error) {

            error = null;
            if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo)) { 
                error = $"No se encontró el archivo con la clave de la API en {rutaArchivo}. Crea un archivo .txt en esa ruta conteniendo solo la clave API " +
                    $"en la primera línea o modifica la ruta para que apunte al archivo que contiene la clave API."; 
                return null; 
            }

            var contenido = File.ReadAllText(rutaArchivo).Trim();
            if (string.IsNullOrWhiteSpace(contenido)) { error = "El archivo de la API key está vacío."; return null; }

            return contenido;

        } // LeerClave>


        /// <summary>
        /// Obtiene la longitud útil de la instrucción considerando la instrucción del usuario, la instrucción del sistema rellenada,
        /// el relleno de la instrucción del sistema (que no cuenta para la longitud útil), la longitud adicional y si es una consulta de calidad adaptable.
        /// </summary>
        /// <param name="instrucción">Instrucción del usuario.</param>
        /// <param name="instrucciónSistemaRellena">Instrucción del sistema rellena.</param>
        /// <param name="rellenoInstrucciónSistema">Relleno del instruccion del sistema que no se tiene en cuenta para la longitud útil.</param>
        /// <param name="longitudAdicional">Longitud adicional que se le agrega a la longitud útil: longitud de la descripción de las funciones, 
        /// longitud de contenido de texto en archivos o longitud equivalente a complejidad visual de una imagen que requiera razonamiento.
        /// El caso de las imágenes sería adaptable según el tipo de imagen, pero por el momento no se implementa un análisis que lo calcule 
        /// (por ejemplo, una imagen de un solo color plano no agrega longitud equivalente a la instrucción útil, en cambio una imagen con una tabla de
        /// contenido nutricional si lo hace).</param>
        /// <param name="calidadAdaptable">Sí está en modo calidad adaptable, se agrega a la longitud útil la instrucción de autoevaluación.</param>
        /// <returns></returns>
        internal static int ObtenerLongitudInstrucciónÚtil(string instrucción, string instrucciónSistemaRellena, string rellenoInstrucciónSistema, 
            double longitudAdicional, bool calidadAdaptable) // Se considera el texto de la instrucción de autoevaluación como parte de la longitud útil.
                => Math.Max((instrucción?.Length ?? 0) + (instrucciónSistemaRellena?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0)
                    + (calidadAdaptable ? InstrucciónAutoevaluación.Length : 0) + (int)Math.Round(longitudAdicional), 0);


        internal static double EstimarTókenesEntradaInstrucciones(string instrucción, string instrucciónSistemaRellena, string rellenoInstrucciónSistema)
            => (instrucción?.Length ?? 0) / CarácteresPorTokenTípicos
                + Math.Max((instrucciónSistemaRellena?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0), 0) / CarácteresPorTokenInstrucciónSistemaTípicos
                + (rellenoInstrucciónSistema?.Length ?? 0) / CarácteresPorTokenRellenoMáximos;


        internal static Razonamiento ObtenerRazonamientoMejorado(Razonamiento razonamiento, CalidadAdaptable calidadAdaptable, int nivelMejoramientoSugerido, 
            ref StringBuilder información) {

            var nivelMejoramiento = ObtenerNivelMejoramientoRazonamientoEfectivo(calidadAdaptable, nivelMejoramientoSugerido);
            if (nivelMejoramiento == 0) return razonamiento;
            
            Razonamiento razonamientoMejorado;

            if (nivelMejoramiento == 1) {

                switch (razonamiento) {
                case Razonamiento.Ninguno:
                    razonamientoMejorado = Razonamiento.Bajo;
                    break;
                case Razonamiento.Bajo:
                    razonamientoMejorado = Razonamiento.Medio;
                    break;
                case Razonamiento.Medio:
                    razonamientoMejorado = Razonamiento.Alto;
                    break;
                case Razonamiento.Alto:
                    razonamientoMejorado = Razonamiento.Alto;  // Permanece igual.
                    break;
                case Razonamiento.NingunoOBajo:
                    razonamientoMejorado = Razonamiento.BajoOMedio;
                    break;
                case Razonamiento.BajoOMedio:
                    razonamientoMejorado = Razonamiento.MedioOAlto;
                    break;
                case Razonamiento.MedioOAlto:
                    razonamientoMejorado = Razonamiento.Alto; // No hay adaptable con Alto. Va directo a Alto.
                    break;
                case Razonamiento.NingunoBajoOMedio:
                    razonamientoMejorado = Razonamiento.BajoMedioOAlto;
                    break;
                case Razonamiento.BajoMedioOAlto:
                    razonamientoMejorado = Razonamiento.MedioOAlto; // No hay valor de 3 opciones, entonces pasa a MedioOAlto.
                    break;
                default:
                    throw new Exception("Valor de razonamiento incorrecto.");
                }


            } else { // nivelesMejoramiento == 2.

                switch (razonamiento) {
                case Razonamiento.Ninguno:
                    razonamientoMejorado = Razonamiento.Medio;
                    break;
                case Razonamiento.Bajo:
                    razonamientoMejorado = Razonamiento.Alto;
                    break;
                case Razonamiento.Medio:
                    razonamientoMejorado = Razonamiento.Alto; // No se puede subir dos niveles entonces se queda en Alto.
                    break;
                case Razonamiento.Alto:
                    razonamientoMejorado = Razonamiento.Alto; // Permanece igual.
                    break;
                case Razonamiento.NingunoOBajo:
                    razonamientoMejorado = Razonamiento.MedioOAlto;
                    break;
                case Razonamiento.BajoOMedio:
                    razonamientoMejorado = Razonamiento.Alto; // No se puede subir dos niveles entonces se queda en Alto.
                    break;
                case Razonamiento.MedioOAlto:
                    razonamientoMejorado = Razonamiento.Alto; // No hay adaptable con Alto. Va directo a Alto.
                    break;
                case Razonamiento.NingunoBajoOMedio:
                    razonamientoMejorado = Razonamiento.MedioOAlto; // El salto de dos niveles lo lleva a MedioOAlto porque Ninguno pasa a Medio, Bajo a Alto y Medio al mismo Alto.
                    break;
                case Razonamiento.BajoMedioOAlto:
                    razonamientoMejorado = Razonamiento.Alto; // El salto de dos niveles lo lleva directamente a Alto porque Bajo pasa a Alto.
                    break;
                default:
                    throw new Exception("Valor de razonamiento incorrecto.");
                }

            }

            if (razonamiento != razonamientoMejorado) {
                información.AgregarLínea($"Se mejoró el razonamiento {nivelMejoramiento.ALetras()} nivel{(nivelMejoramiento > 1 ? "es" : "")} de {razonamiento} a " +
                    $"{razonamientoMejorado}.");
            } else {
                información.AgregarLínea($"No se mejoró el razonamiento {razonamiento}.");
            }

            return razonamientoMejorado;

        } // ObtenerRazonamientoMejorado>


        /// <summary>
        /// Obtiene el modelo de mínima potencia dentro de la misma familia del modelo de referencia.
        /// La mínima potencia se define como Tamaño.MuyPequeño (modelo con tres niveles superiores definidos).
        /// Si hay múltiples candidatos del mismo tamaño, se elige el de menor PrecioSalidaNoRazonamiento.
        /// Si no existe Tamaño.MuyPequeño, se intenta con Pequeño, luego Medio y finalmente Grande.
        /// </summary>
        /// <param name="modeloReferencia">Modelo de referencia para determinar la familia.</param>
        /// <returns>Modelo de mínima potencia de la misma familia; null si no hay modelos en esa familia.</returns>
        internal static Modelo ObtenerModeloMásPequeño(Familia familia) {

            Modelo? másPequeño = null;
            var tamañoMásPequeño = Tamaño.Grande;

            foreach (var cv in Modelos) {

                var modelo = cv.Value;
                if (modelo.Familia != familia) continue;

                var tamaño = modelo.ObtenerTamaño();
                
                var esTamañoMásPequeño = // Orden de preferencia de tamaños: MuyPequeño < Pequeño < Medio < Grande.
                    (tamaño == Tamaño.MuyPequeño && (másPequeño == null || tamañoMásPequeño != Tamaño.MuyPequeño)) ||
                    (tamaño == Tamaño.Pequeño && (másPequeño == null || (tamañoMásPequeño != Tamaño.MuyPequeño && tamañoMásPequeño != Tamaño.Pequeño))) ||
                    (tamaño == Tamaño.Medio && (másPequeño == null || (tamañoMásPequeño == Tamaño.Grande))) ||
                    (tamaño == Tamaño.Grande && másPequeño == null);

                if (esTamañoMásPequeño) {
                    másPequeño = modelo;
                    tamañoMásPequeño = tamaño;
                } else if (másPequeño != null && tamaño == tamañoMásPequeño) { // Ya existe un tamaño mejor que tiene el mismo tamaño que el recién encontrado.
                   
                    if (modelo.PrecioSalidaNoRazonamiento < másPequeño.Value.PrecioSalidaNoRazonamiento) { // Desempate por precio de salida no razonamiento. Se elije el de más barato precio salida no razonamiento como el candidato a ser menos potente.
                        másPequeño = modelo;
                        tamañoMásPequeño = tamaño;
                    }

                }

            }

            if (másPequeño == null) {
                throw new Exception($"No se esperaba no encontrar el modelo más pequeño de la familia {familia}."); // Si no se encuentra es un problema de la construcción del diccionario Modelos. 
            } else {
                return (Modelo)másPequeño;
            }                

        } // ObtenerModeloMásPequeño>


        internal static string ALetras(this int número) => (número  == 1 ? "un" : (número == 2 ? "dos" : "?"));


        internal static RazonamientoEfectivo ObtenerRazonamientoEfectivo(Razonamiento razonamiento, RestricciónRazonamiento restricciónRazonamientoAlto,
            RestricciónRazonamiento restricciónRazonamientoMedio, Modelo modelo, int longitudInstrucciónÚtil, out StringBuilder información) {

            información = new StringBuilder();

            RazonamientoEfectivo razonamientoEfectivo;

            switch (razonamiento) { // Inicia los valores actuales por defecto. 
            case Razonamiento.Ninguno:
                razonamientoEfectivo = RazonamientoEfectivo.Ninguno;
                break;
            case Razonamiento.Bajo:
                razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                break;
            case Razonamiento.Medio:
                razonamientoEfectivo = RazonamientoEfectivo.Medio;
                break;
            case Razonamiento.Alto:
                razonamientoEfectivo = RazonamientoEfectivo.Alto;
                break;
            case Razonamiento.NingunoOBajo:
            case Razonamiento.BajoOMedio:
            case Razonamiento.MedioOAlto:
            case Razonamiento.NingunoBajoOMedio:
            case Razonamiento.BajoMedioOAlto:
                razonamientoEfectivo = RazonamientoEfectivo.Ninguno; // Se establece solo para que el compilador no se queje, pero se asegura que este cambiará en el código siguiente.
                break;
            default:
                throw new Exception("Valor de razonamiento no considerado.");
            }

            if (razonamiento == Razonamiento.NingunoBajoOMedio) {

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Ninguno;
                } else if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles) {
                    razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                }

            } else if (razonamiento == Razonamiento.BajoMedioOAlto) {

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                } else if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles) {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Alto;
                }

            } else if (razonamiento == Razonamiento.MedioOAlto) {

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Alto;
                }

            } else if (razonamiento == Razonamiento.NingunoOBajo) { // Solo admite una mejora de razonamiento.

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Ninguno;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                }

            } else if (razonamiento == Razonamiento.BajoOMedio) { // Solo admite una mejora de razonamiento.

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                }

            }

            var tamaño = modelo.ObtenerTamaño();
            var aplicadaRestricción = false;

            if (razonamientoEfectivo == RazonamientoEfectivo.Alto && restricciónRazonamientoAlto != RestricciónRazonamiento.Ninguna) {

                if (restricciónRazonamientoAlto == RestricciónRazonamiento.ModelosPequeños) {
                    if (tamaño == Tamaño.MuyPequeño || tamaño == Tamaño.Pequeño) { aplicadaRestricción = true; razonamientoEfectivo = RazonamientoEfectivo.Medio; }
                } else if (restricciónRazonamientoAlto == RestricciónRazonamiento.ModelosMuyPequeños) {
                    if (tamaño == Tamaño.MuyPequeño) { aplicadaRestricción = true; razonamientoEfectivo = RazonamientoEfectivo.Medio; }
                }

            }

            if (razonamientoEfectivo == RazonamientoEfectivo.Medio && restricciónRazonamientoMedio != RestricciónRazonamiento.Ninguna) { // Se debe poner después de la revisión de razonamiento Alto porque es posible que tenga doble restricción, entonces el anterior código lo pasa a razonamiento medio y el siguiente verifica si se debe pasar a razonamiento bajo.

                if (restricciónRazonamientoMedio == RestricciónRazonamiento.ModelosPequeños) {
                    if (tamaño == Tamaño.MuyPequeño || tamaño == Tamaño.Pequeño) { aplicadaRestricción = true; razonamientoEfectivo = RazonamientoEfectivo.Bajo; }
                } else if (restricciónRazonamientoMedio == RestricciónRazonamiento.ModelosMuyPequeños) {
                    if (tamaño == Tamaño.MuyPequeño) { aplicadaRestricción = true; razonamientoEfectivo = RazonamientoEfectivo.Bajo; }
                }

            }

            if (EsRazonamientoAdaptable(razonamiento)) información.AgregarLínea($"Razonamiento efectivo = {razonamientoEfectivo}.{(aplicadaRestricción ? " Aplicada restricción de razonamiento." : "")}");

            return razonamientoEfectivo;

        } // ObtenerRazonamientoEfectivo>


        public static bool EsRazonamientoAdaptable(Razonamiento razonamiento)
            => razonamiento != Razonamiento.Ninguno && razonamiento != Razonamiento.Bajo && razonamiento != Razonamiento.Medio && razonamiento != Razonamiento.Alto;


        public static int ObtenerNivelMejoramientoModeloMáximo(this CalidadAdaptable calidad) {

            switch (calidad) {
            case CalidadAdaptable.No:
                return 0;
            case CalidadAdaptable.MejorarModelo:
                return 1;
            case CalidadAdaptable.MejorarRazonamiento:
                return 0;
            case CalidadAdaptable.MejorarModeloYRazonamiento:
                return 1;
            case CalidadAdaptable.MejorarModeloDosNiveles:
                return 2;
            case CalidadAdaptable.MejorarRazonamientoDosNiveles:
                return 0;
            case CalidadAdaptable.MejorarModeloYRazonamientoDosNiveles:
                return 2;
            case CalidadAdaptable.MejorarModeloUnNivelYRazonamientoDosNiveles:
                return 1;
            case CalidadAdaptable.MejorarModeloDosNivelesYRazonamientoUnNivel:
                return 2;
            default:
                throw new ArgumentOutOfRangeException(nameof(calidad), calidad, null);
            }

        } // ObtenerNivelMejoramientoModeloMáximo>


        public static int ObtenerNivelMejoramientoRazonamientoMáximo(this CalidadAdaptable calidad) {

            switch (calidad) {
            case CalidadAdaptable.No:
                return 0;
            case CalidadAdaptable.MejorarModelo:
                return 0;
            case CalidadAdaptable.MejorarRazonamiento:
                return 1;
            case CalidadAdaptable.MejorarModeloYRazonamiento:
                return 1;
            case CalidadAdaptable.MejorarModeloDosNiveles:
                return 0;
            case CalidadAdaptable.MejorarRazonamientoDosNiveles:
                return 2;
            case CalidadAdaptable.MejorarModeloYRazonamientoDosNiveles:
                return 2;
            case CalidadAdaptable.MejorarModeloUnNivelYRazonamientoDosNiveles:
                return 2;
            case CalidadAdaptable.MejorarModeloDosNivelesYRazonamientoUnNivel:
                return 1;
            default:
                throw new ArgumentOutOfRangeException(nameof(calidad), calidad, null);
            }

        } // ObtenerNivelMejoramientoRazonamientoMáximo>


        public static int ObtenerNivelMejoramientoRazonamientoEfectivo(CalidadAdaptable calidad, int nivelMejoramientoSugerido) 
            => Math.Min(calidad.ObtenerNivelMejoramientoRazonamientoMáximo(), nivelMejoramientoSugerido);


        public static int ObtenerNivelMejoramientoModeloEfectivo(CalidadAdaptable calidad, int nivelMejoramientoSugerido)
            => Math.Min(calidad.ObtenerNivelMejoramientoModeloMáximo(), nivelMejoramientoSugerido);


        /// <summary>
        /// Agrega un objeto Tókenes al diccionario. Si ya existe una entrada con la misma clave, suma los tókenes.
        /// Si el diccionario es nulo, lanza excepción.
        /// </summary>
        /// <param name="diccionario"></param>
        /// <param name="tókenes"></param>
        public static void AgregarSumando(this Dictionary<string, Tókenes> diccionario, Tókenes tókenes) {

            if (diccionario == null) 
                throw new ArgumentNullException(nameof(diccionario), "El método AgregarSumando() no permite nulos. Usa AgregarSumandoPosibleNulo().");
            var clave = tókenes.Clave;
            if (diccionario.ContainsKey(clave)) {
                diccionario[clave] += tókenes;
            } else {
                diccionario.Add(clave, tókenes);
            }

        } // AgregarSumando>


        /// <summary>
        /// Agrega un objeto Tókenes al diccionario. Si ya existe una entrada con la misma clave, suma los tókenes. 
        /// </summary>
        /// <param name="diccionario"></param>
        /// <param name="tókenes"></param>
        public static void AgregarSumandoPosibleNulo(ref Dictionary<string, Tókenes> diccionario, Tókenes tókenes) { // Es necesaria esta función auxiliar porque C# 7.3 no permite 'ref this Dictionary<string, Tókenes> diccionario' para objetos que no son struct.

            if (diccionario == null) diccionario = new Dictionary<string, Tókenes>();
            diccionario.AgregarSumando(tókenes);

        } // AgregarSumandoPosibleNulo>


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


    } // GlobalFrugalia>


} // Frugalia>