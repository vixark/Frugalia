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
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;


namespace Frugalia {


    public enum Razonamiento {
        Ninguno, Bajo, Medio, Alto, // Se configuran solo 4 niveles de razonamiento, en línea con GPT 5.1. El valor Ninguno se mapea al nivel más bajo disponible en otros modelos como 'minimal' en GPT 5 o su equivalentes en otras familias.
        NingunoOMayor, BajoOMayor, MedioOMayor, // Adaptables: Los modelos de OpenAI ya adaptan internamente cuántos tókenes de razonamiento usan según la dificultad de la tarea, pero aquí se añade una capa extra de control para proteger costos. Por ejemplo, con NingunoOMayor se usa Ninguno (más barato) y sólo se sube a Bajo o Medio cuando la entrada es muy larga, de modo que el modelo tenga más margen de razonamiento cuando realmente lo necesita, sin arriesgarse a gastar de más en casos simples. Esto logra que si se estima que una tarea puede funcionar con Ninguno, se puede poner en NingunoOMayor para que la mayoría de las veces se ejecute con Ninguno y no hayan tókenes de razonamiento, y que cuando excepcionalmente ser requiera procesar textos más largos, que podrían requerir más razonamiento, se adapte a uno de los niveles de razonamiento superiores.
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
        AsistenteAI // Solo mensajes del asistente IA.
    } // TipoMensaje>


    public static class Global {


        private static readonly Random AleatorioCompartido = new Random();

        public static string DobleLínea = $"{Environment.NewLine}{Environment.NewLine}";

        internal const string LoHiceBien = "[lo-hice-bien]";

        internal const string UsaModeloMejor = "[usa-modelo-mejor]";

        internal const string UsaModeloMuchoMejor = "[usa-modelo-mucho-mejor]";

        internal const string Deshabilitado = "[deshabilitado]";

        internal const string Fin = ".[fin].";

        internal const int SinLímiteTókenes = int.MaxValue;

        internal const double FactorÉxitoCaché = 0.8; // Si OpenAI funcionara bien no debería pasar que no se active la caché en el segundo mensaje de una conversación que tiene instrucciones de sistema rellenadas desde el primer mensaje, pero sí pasa. En algunos casos OpenAI simplemente ignora la caché por razones desconocidas. Se hizo un experimento inflando más las instrucciones de sistema y queda demostrado que no es cuestión del tamaño de tokenes de la función ni que se cambie ni nada, simplemente a veces no lo coje, con 1294 tókenes hay más que suficiente para garantizar que toda la instruccion sistema es superior a 1024 . 1294 - 10 (o lo que sea de la instrucción de usuario) - 73 de la función  = 1211 1: ENC = 1294 EC = 0 SNR = 103 SR = 0 2: ENC = 1402 EC = 0 SNR = 222 SR = 0 OpenAI describe el prompt caching como una optimización de “best effort”, no determinista como un contrato fuerte tipo: “si el prefijo coincide, 100 % te voy a servir desde caché”. Así que básicamente dicen, si no funciona, no me culpen. Lo mejor entonces es asumir un % de éxito que se incorporá en la fórmula para solo engordar las instrucciones de sistema que considerando ese porcentaje de éxito de uso de la caché logren ahorros. 0.8 es un valor que se tira al aire basado en un pequeño experimento limitado: se ejecutó 10 veces una conversación de 6-7 mensajes y se obtuvo una tasa de fallo de 13%, es decir un factor de éxito de 0.87, sin embargo debido a que hay incertidumbre con este número y a que hay una ligera demesmejoría en el comportamiento del agente cuando se rellenan las instrucciones del sistema, se prefiere dejar en 0.8. Se usa el mismo valor para las otras familias de modelos porque no se conoce aún su funcionamiento.

        internal const int CarácteresPorTokenConversaciónTípicos = 3; // Aplica para mensajes tanto del usuario como del asistente IA.

        internal const int CarácteresPorTokenInstrucciónSistemaTípicos = 4; // La necesidad o no de rellenar la instrucción de sistema se decide usando valor promedio de 4 carácteres por tókenes y la rellenada aw hace con un exceso de tokenes (carácteresPorTokenMáximos) para asegurar que se generen suficientes carácteres para que con seguridad supere el límite para activar la caché (tókenesObjetivo). El valor de 4 carácteres por token se obtuvo de controlar eliminando los tókenes que consumía la función, así 340 tókenes (-73 función) = 267 tókenes para 1061 carácteres = 3.97 char/tk (para el primer mensaje de mensaje de usuario + instrucción de sistema sin rellenar). Para textos más normales que no sean instrucciones de sistema (que suelen tener frases cortas densas, referencias, datos, etc) suelen ser 3 carácteres por token. Pero como aquí se está intentando ajustar es instrucciones de sistema se trabaja con 4.

        internal const double CarácteresPorTokenRellenoMáximos = 5.5; // Se usa 5.5 como caso límite. Se asegura agregar suficientes carácteres para que supere los tókenes requeridos. Se hizo un experimento y se encontró esto: Sin relleno 340 tk y 1061 char: 3.12 char/tk, con relleno 955 tk y 4183 char: 4.38 char/ tk, solo el relleno: 615tk y 3122 char: 5.07 char/ tk. Este mismo experimento se repitió para el caso de usar solo el texto relleno sin lorems (solo para fines de calcular su cantidad de carácteres por token) y se encontró que es 4.75 char/tk. También se hizo el experimento únicamente con lorems (sin texto introductorio) y dio otra vez 5.07 char/tk, así que esto es algo inconsistente matemáticamente porque podrían haber cosas desconocidas de cómo el modelo calcula los tókenes, entonces para pecar por seguro, se usa 5.5 carácteres por token para el texto de relleno. Esto asegura que el relleno garantiza con cierto margen de seguridad que se active la caché. Se debe poner un valor superior porque hay incertidumbre de que tal vez el modelo cambié la forma de cálculo de tókenes y de pronto llegue a ser 5.3 o 5.2, y si así fuera y se hubiera puesto un valor muy ajustado como 5.1, no se activaría la caché y se gastaría innecesariamente en tókenes inutiles no en caché.

        internal const double FactorSeguridadTókenesEntradaMáximos = 0.9; // Para evitar sobrepasar el límite de tókenes de entrada del modelo, se usa un factor de seguridad del 90%. Esto es porque a veces la estimación de tókenes puede ser imprecisa y se corre el riesgo de exceder el límite permitido por el modelo, lo que causaría incremento de costos o errores en la solicitud. Al usar este factor, se garantiza que la cantidad estimada de tókenes de entrada esté por debajo del límite máximo, proporcionando un margen adicional para evitar errores. 


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


        public static string LeerClave(string rutaArchivo, out string error) {

            error = null;
            if (string.IsNullOrEmpty(rutaArchivo) || !File.Exists(rutaArchivo)) { error = "No se encontró el archivo con la clave de la API."; return null; }

            var contenido = File.ReadAllText(rutaArchivo).Trim();
            if (string.IsNullOrWhiteSpace(contenido)) { error = "El archivo de la API key está vacío."; return null; }

            return contenido;

        } // LeerClave>


        internal static int ObtenerLargoInstrucciónÚtil(string instrucción, string instrucciónSistemaRellena, string rellenoInstrucciónSistema)
            => Math.Max((instrucción?.Length ?? 0) + (instrucciónSistemaRellena?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0), 0);


        internal static double EstimarTókenesEntradaInstrucciones(string instrucción, string instrucciónSistemaRellena, string rellenoInstrucciónSistema)
            => (instrucción?.Length ?? 0) / CarácteresPorTokenConversaciónTípicos
                + Math.Max((instrucciónSistemaRellena?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0), 0) / CarácteresPorTokenInstrucciónSistemaTípicos
                + (rellenoInstrucciónSistema?.Length ?? 0) / CarácteresPorTokenRellenoMáximos;


        public static string Reemplazar(this string texto, string valorAnterior, string nuevoValor, StringComparison comparisonType) {

            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(valorAnterior)) return texto;

            if (comparisonType == StringComparison.Ordinal) return texto.Replace(valorAnterior, nuevoValor);

            int idx = 0;
            int largoAnterior = valorAnterior.Length;
            var respuesta = "";

            while (idx < texto.Length) {

                int found = texto.IndexOf(valorAnterior, idx, comparisonType);
                if (found < 0) {
                    respuesta += texto.Substring(idx);
                    break;
                }
                respuesta += texto.Substring(idx, found - idx) + nuevoValor;
                idx = found + largoAnterior;

            }

            return respuesta;

        } // Reemplazar>


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
                case Razonamiento.NingunoOMayor:
                    razonamientoMejorado = Razonamiento.BajoOMayor;
                    break;
                case Razonamiento.BajoOMayor:
                    razonamientoMejorado = Razonamiento.MedioOMayor;
                    break;
                case Razonamiento.MedioOMayor:
                    razonamientoMejorado = Razonamiento.Alto; // No hay adaptable con Alto. Va directo a Alto.
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
                case Razonamiento.NingunoOMayor:
                    razonamientoMejorado = Razonamiento.MedioOMayor;
                    break;
                case Razonamiento.BajoOMayor:
                    razonamientoMejorado = Razonamiento.Alto; // No se puede subir dos niveles entonces se queda en Alto.
                    break;
                case Razonamiento.MedioOMayor:
                    razonamientoMejorado = Razonamiento.Alto; // No hay adaptable con Alto. Va directo a Alto.
                    break;
                default:
                    throw new Exception("Valor de razonamiento incorrecto.");
                }

            }

            if (razonamiento != razonamientoMejorado) {
                información.AgregarLínea($"Se mejoró el razonamiento {nivelMejoramiento} nivel{(nivelMejoramiento > 1 ? "es" : "")} de " +
                    $"{razonamiento} a {razonamientoMejorado}.");
            } else {
                información.AgregarLínea($"No se mejoró el razonamiento {razonamiento}.");
            }

            return razonamientoMejorado;

        } // ObtenerRazonamientoMejorado>


        internal static RazonamientoEfectivo ObtenerRazonamientoEfectivo(Razonamiento razonamiento, RestricciónRazonamiento restricciónRazonamientoAlto,
            RestricciónRazonamiento restricciónRazonamientoMedio, Modelo modelo, int largoInstrucciónÚtil, out StringBuilder información) {

            var largoLímite1 = 750; // Aproximadamente 250 tókenes. Los límites de 750 y 2400 caracteres son a criterio. Se prefiere subir el razonamiento un poco antes (pagando algo más) para reducir errores, repreguntas y consultas repetidas (que valen más), que a la larga también consumen tókenes y empeoran la experiencia de usuario. Se encontró que cuando los textos son muy largos el agente se confunde y olvida cosas como preguntar un dato necesario para la función, al subir el nivel de razonamiento disminuye un poco este efecto.
            var largoLímite2 = 2400; // Aproximadamente 800 tokenes.

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
            case Razonamiento.NingunoOMayor:
            case Razonamiento.BajoOMayor:
            case Razonamiento.MedioOMayor:
                razonamientoEfectivo = RazonamientoEfectivo.Ninguno; // Se establece solo para que el compilador no se queje, pero se asegura que este cambiará en el código siguiente.
                break;
            default:
                throw new Exception("Valor de razonamiento no considerado.");
            }

            if (razonamiento == Razonamiento.NingunoOMayor) {

                if (largoInstrucciónÚtil < largoLímite1) {
                    razonamientoEfectivo = RazonamientoEfectivo.Ninguno;
                } else if (largoInstrucciónÚtil < largoLímite2) {
                    razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                }

            } else if (razonamiento == Razonamiento.BajoOMayor) {

                if (largoInstrucciónÚtil < largoLímite1) {
                    razonamientoEfectivo = RazonamientoEfectivo.Bajo;
                } else if (largoInstrucciónÚtil < largoLímite2) {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Alto;
                }

            } else if (razonamiento == Razonamiento.MedioOMayor) {

                if (largoInstrucciónÚtil < largoLímite1) {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.Alto;
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

            if (razonamiento.ToString() != razonamientoEfectivo.ToString())
                información.AgregarLínea($"Razonamiento efectivo: {razonamientoEfectivo}, Razonamiento Original: {razonamiento}, " +
                    $"Largo instrucción útil: {largoInstrucciónÚtil}{(aplicadaRestricción ? ", Aplicada restricción de razonamiento" : "")}.");

            return razonamientoEfectivo;

        } // ObtenerRazonamientoEfectivo>


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


        /// <summary>
        /// Agrega una nueva línea a un texto guardado en un StringBuilder.
        /// Si el texto es nulo, lanza excepción.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevaLínea"></param>
        public static void AgregarLínea(this StringBuilder texto, string nuevaLínea) {

            if (texto == null) throw new ArgumentNullException(nameof(texto), "El método AgregarLínea() no permite nulos. Usa AgregarLíneaPosibleNulo().");
            texto.AppendLine(nuevaLínea);            

        } // AgregarLínea>


        /// <summary>
        /// Agrega varias nuevas líneas a un texto guardado en un StringBuilder.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevasLíneas"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AgregarLíneas(this StringBuilder texto, StringBuilder nuevasLíneas) {

            if (texto == null) throw new ArgumentNullException(nameof(texto), "El método AgregarLíneas() no permite nulos. Usa primero AgregarLíneaPosibleNulo().");
            if (nuevasLíneas != null) { // No agrega si nuevaLíneas es nulo.
                if (texto.Length > 0) texto.AgregarLínea(""); // Si ya hay algo escrito, separa con una línea en blanco.
                texto.Append(nuevasLíneas);
            }
               
        } // AgregarLíneas>


        /// <summary>
        /// Indica si el StringBuilder es nulo o está vacío (Length == 0).
        /// Equivalente a string.IsNullOrEmpty pero para StringBuilder.
        /// </summary>
        /// <param name="texto">Instancia a evaluar (puede ser nula).</param>
        /// <returns>true si es nulo o está vacío; false en caso contrario.</returns>
        public static bool EsNuloOVacío(this StringBuilder texto) => texto == null || texto.Length == 0;


        /// <summary>
        /// Agrega una nueva línea a un texto guardado en un StringBuilder.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevaLínea"></param>
        /// <returns></returns>
        public static void AgregarLíneaPosibleNulo(ref StringBuilder texto, string nuevaLínea) { // Es necesaria esta función auxiliar porque C# 7.3 no permite 'ref this StringBuilder texto' para objetos que no son struct.

            if (texto == null) texto = new StringBuilder();
            texto.AgregarLínea(nuevaLínea);

        } // AgregarLíneaPosibleNulo>


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

            lock (AleatorioCompartido) {
                return AleatorioCompartido.Next(mínimo, máximo);
            }

        } // ObtenerAleatorio>


    } // Global>


} // Frugalia>