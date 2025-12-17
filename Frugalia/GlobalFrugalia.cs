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
        Ninguno, Bajo, Medio, Alto, MuyAlto, // Se configuran solo 5 niveles de razonamiento, en línea con GPT 5.2. El valor Ninguno se mapea al nivel más bajo disponible en otros modelos como 'minimal' en GPT 5 o su equivalentes en otras familias.
        NingunoOBajo, BajoOMedio, MedioOAlto, AltoOMuyAlto, // Adaptables de dos opciones: Los modelos de OpenAI ya adaptan internamente cuántos tókenes de razonamiento usan según la dificultad de la tarea, pero aquí se añade una capa extra de control para proteger costos. Por ejemplo, con NingunoOBajo se usa Ninguno (más barato) y sólo se sube a Bajo cuando la entrada es muy larga, de modo que el modelo tenga más margen de razonamiento cuando realmente lo necesita, sin arriesgarse a gastar de más en casos simples. Esto logra que si se estima que una tarea puede funcionar con Ninguno, se puede poner en NingunoOBajo para que la mayoría de las veces se ejecute con Ninguno y no hayan tókenes de razonamiento, y que cuando excepcionalmente ser requiera procesar textos más largos, que podrían requerir más razonamiento, se adapte a uno de los niveles de razonamiento superiores. Se usa la segunda opción si el texto de instrucción útil supera CarácteresLímiteInstrucciónParaSubirRazonamiento. 
        NingunoBajoOMedio, BajoMedioOAlto, MedioAltoOMuyAlto // Adaptables de tres opciones: Dan más granularidad al usuario de la librería para permitir que el modelo suba hasta dos niveles de razonamiento desde Ninguno hasta Medio o desde Bajo hasta Alto. Se usa la tercera opción si el texto de instrucción útil supera CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles. 
    }

    internal enum RazonamientoEfectivo { // La enumeración que se usa efectivamente en los modelos. Se separa de la otra para facilitar la depuración y evitar errores al estar escribiendo integraciones.
        Ninguno = 0, Bajo = 1, Medio = 2, Alto = 3, MuyAlto = 4 // No se deben cambiar los valores de la enumeración porque se usan para la restricción del razonamiento según el tamaño del modelo.
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
        MuyLarga,
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
        TiempoSuperado,
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


        internal const string UnEspacio = " ";

        internal const string LoHiceBien = "[lo-hice-bien]";

        internal const string UsaModeloMejor = "[usa-modelo-mejor]";

        internal const string UsaModeloMuchoMejor = "[usa-modelo-mucho-mejor]";

        internal const string Deshabilitado = "[deshabilitado]";

        internal const string Fin = ".[fin].";

        public static string AdvertenciaGrupoCachéDemostración = $"{DobleLínea}No uses el texto '{GrupoCachéDemostración}' en la variable GrupoCaché.{DobleLínea}¿Qué es GrupoCaché?{Environment.NewLine}Es el nombre bajo el cual se agrupan las consultas y se mejora el aprovechamiento de la caché cuando se repite la misma parte inicial de instrucciones del sistema + funciones + otras. No es una clave única de esa parte inicial ni un número único por consulta ni un identificador único del usuario.{DobleLínea}¿Por qué no debes dejarlo en '{GrupoCachéDemostración}'?{Environment.NewLine}Porque estarías usando el mismo valor que Frugalia usa para pruebas y se podría superar el umbral de consultas por minuto en el mismo grupo y con el mismo inicio que recomiendan los modelos, lo que puede degradar la caché y hacerte pagar más (para modelos GPT lee más en https://platform.openai.com/docs/guides/prompt-caching).{DobleLínea}Qué valor deberías usar:{Environment.NewLine}- Si tu aplicación tiene poco tráfico: usa un valor estable por aplicación y versión. Por ejemplo, frugalia-nombredetuapp.{Environment.NewLine}- Si tu aplicación tiene mucho tráfico: usa unos pocos valores diferentes para repartir la carga. Por ejemplo, puedes agrupar por consultas con instrucciones comunes: frugalia-nombredetuapp-identificarimágenes y otro con frugalia-nombredetuapp-chatservicioalcliente. También puedes agrupar por grupos de usuarios: frugalia-nombredetuapp-g07 asignando cada usuario a un grupo de forma permanente de tal manera que el mismo usuario siempre tenga asignado el mismo grupo. O con una combinación de ambos: frugalia-nombredetuapp-identificarimágenes-g04.{DobleLínea}Qué valores no deberías usar:{Environment.NewLine}- No generes un valor nuevo en cada consulta porque esto empeora el funcionamiento de la caché.{Environment.NewLine}- No uses valores asociados a un usuario en particular. Esto reduce la efectividad de la caché porque el servidor no podría compartir textos en caché entre diferentes usuarios.{DobleLínea}Para cambiarlo, modifica Frugalia.Demostración > Demostración.cs > GrupoCaché.{DobleLínea}Si no entiendes este texto (¡no te culpo!), cámbialo a texto vacío y se desactivará el agrupamiento de textos en caché y las consultas funcionarán, aunque con caché menos efectiva y costos ligeramente más altos. Sin embargo, te sugiero usar un valor adecuado según tu caso de uso para que la caché funcione óptimamente y se logre el máximo ahorro de dinero en tus consultas.";

        public const string GrupoCachéDemostración = "frugalia-demostración"; // No cambiar este valor.

        internal readonly static string InstrucciónAutoevaluación = "\n\nPrimero responde normalmente al usuario.\n\nDespués evalúa tu " +
            "propia respuesta y en la última línea escribe exactamente una de estas etiquetas, sola, sin dar explicaciones de tu elección:\n\n" +
            $"{LoHiceBien}\n{UsaModeloMejor}\n{UsaModeloMuchoMejor}\n\nUsa:\n{LoHiceBien}: Si tu respuesta fue buena, tiene " +
            $"sentido y es completa. Entendiste bien la consulta y es relativamente sencilla.\n{UsaModeloMejor}: Si tu respuesta no fue " +
            "buena en calidad, sentido o completitud, o si a la consulta le faltan detalles, contexto o no la entiendes bien.\n" +
            $"{UsaModeloMuchoMejor}: Si la consulta es muy compleja, requiere conocimiento experto o trata temas delicados."; // Alrededor de 650 carácteres.

        public const int CarácteresLímiteInstrucciónParaSubirRazonamiento = 750; // Aproximadamente 250 tókenes. Los límites de 750 y 2400 caracteres son a criterio. Se prefiere subir el razonamiento un poco antes (pagando algo más) para reducir errores, repreguntas y consultas repetidas (que valen más), que a la larga también consumen tókenes y empeoran la experiencia de usuario. Se encontró que cuando los textos son muy largos el agente se confunde y olvida cosas como preguntar un dato necesario para la función, al subir el nivel de razonamiento disminuye un poco este efecto. El valor de 750 es ligeramente superior a la longitud de InstrucciónAutoevaluación, por lo tanto al establecer CalidadAdaptable diferente de No y Razonamiento adaptable y sumarle la longitud de instrucción del sistema y de usuario, casi siempre subirá de razonamiento.

        public const int CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles = 2400; // Aproximadamente 800 tokenes.

        internal const int MáximosTókenesSalidaBaseVerbosidadBaja = 200;

        internal const int MáximosTókenesSalidaBaseVerbosidadMedia = 350;

        internal const int MáximosTókenesSalidaBaseVerbosidadAlta = 500;

        internal const int MáximosTókenesRazonamientoBaseNinguno = 0;

        internal const int MáximosTókenesRazonamientoBaseBajo = 1000;

        internal const int MáximosTókenesRazonamientoBaseMedio = 2500;

        internal const int MáximosTókenesRazonamientoBaseAlto = 5000;

        internal const int SinLímiteTókenes = int.MaxValue;

        internal const int CarácteresPorTokenTípicos = 3; // Aplica para mensajes del usuario, mensajes del asistente IA, archivos y funciones.

        internal const int CarácteresPorTokenInstrucciónSistema = 4; // La necesidad o no de rellenar la instrucción del sistema se decide usando valor promedio de 4 carácteres por tókenes y el relleno se hace con un exceso de tokenes (carácteresPorTokenMáximos) para asegurar que se generen suficientes carácteres para que con seguridad supere el límite para activar la caché (tókenesObjetivo). El valor de 4 carácteres por token se obtuvo de controlar eliminando los tókenes que consumía la función, así 340 tókenes (-73 función) = 267 tókenes para 1061 carácteres = 3.97 char/tk (para el primer mensaje de mensaje de usuario + instrucción del sistema sin rellenar). Para textos más normales que no sean instrucciones del sistema (que suelen tener frases cortas densas, referencias, datos, etc) suelen ser 3 carácteres por token. Pero como aquí se está intentando ajustar es instrucciones del sistema se trabaja con 4.

        internal const double CarácteresPorTokenRelleno = 5.5; // Se usa 5.5 como caso límite. Se asegura agregar suficientes carácteres para que supere los tókenes requeridos. Se hizo un experimento y se encontró esto: Sin relleno 340 tk y 1061 char: 3.12 char/tk, con relleno 955 tk y 4183 char: 4.38 char/ tk, solo el relleno: 615tk y 3122 char: 5.07 char/ tk. Este mismo experimento se repitió para el caso de usar solo el texto relleno sin lorems (solo para fines de calcular su cantidad de carácteres por token) y se encontró que es 4.75 char/tk. También se hizo el experimento únicamente con lorems (sin texto introductorio) y dio otra vez 5.07 char/tk, así que esto es algo inconsistente matemáticamente porque podrían haber cosas desconocidas de cómo el modelo calcula los tókenes, entonces para pecar por seguro, se usa 5.5 carácteres por token para el texto de relleno. Esto asegura que el relleno garantiza con cierto margen de seguridad que se active la caché. Se debe poner un valor superior porque hay incertidumbre de que tal vez el modelo cambié la forma de cálculo de tókenes y de pronto llegue a ser 5.3 o 5.2, y si así fuera y se hubiera puesto un valor muy ajustado como 5.1, no se activaría la caché y se gastaría innecesariamente en tókenes inutiles no en caché.

        internal const double FactorSeguridadTókenesEntradaMáximos = 0.75; // Para evitar sobrepasar el límite de tókenes de entrada del modelo, se usa un factor de seguridad del 75%. Esto es porque a veces la estimación de tókenes puede ser imprecisa y se corre el riesgo de exceder el límite permitido por el modelo, lo que causaría incremento de costos o errores en la solicitud. Al usar este factor, se garantiza que la cantidad estimada de tókenes de entrada esté por debajo del límite máximo, proporcionando un margen adicional para evitar errores. El 75% se obtiene de 3/4 que es un rango de carácteres tókenes típico promedio a típico máximo.

        public static readonly Dictionary<string, Modelo> Modelos = new Dictionary<string, Modelo>(StringComparer.OrdinalIgnoreCase) { // Para control de costos por el momento se deshabilita el modelo gpt-5-pro. Los que se quieran deshabilitar silenciosamente se les pone {Deshabilitado} en el nombre de modelos mejorados (no en la clave del diccionario) para que no saque excepción y lo ignore como si no existiera.
            { "gpt-5.2-pro", new Modelo("gpt-5.2-pro", Familia.GPT, 21, 21, 168, 168, null, null, null, null, 400000, 0.5m, 1, null, false,
                razonamientosEfectivosPermitidos: new List<RazonamientoEfectivo> { RazonamientoEfectivo.Medio, RazonamientoEfectivo.Alto,
                    RazonamientoEfectivo.MuyAlto }) }, // https://openai.com/api/pricing/. No tiene descuento para tókenes de entrada de caché y por lo tanto tampoco tiene límite de tókenes para activación automática de caché.
            { "gpt-5.2", new Modelo("gpt-5.2", Familia.GPT, 1.75m, 0.175m, 14, 14, null, null, null, 1024, 400000, 0.5m, 1, null, true,
                $"gpt-5.2-pro{Deshabilitado}", factorÉxitoCaché: 0.55, factorÉxitoCachéConGrupoCaché: 0.70) }, // Se hicieron 60 consultas que debían activar la caché y se encontró que sin grupo de caché falló 27 veces fallaba y con grupo de caché 18.5 veces fallaba. Igual experimento se hizo con los otros modelos GPT.
            { "gpt-5-pro", new Modelo("gpt-5-pro", Familia.GPT, 15, 15, 120, 120, null, null, null, null, 400000, 0.5m, 1, null, false,
                razonamientosEfectivosPermitidos: new List<RazonamientoEfectivo>() { RazonamientoEfectivo.Alto }) }, // https://openai.com/api/pricing/. No tiene descuento para tókenes de entrada de caché y por lo tanto tampoco tiene límite de tókenes para activación automática de caché.
            { "gpt-5.1", new Modelo("gpt-5.1", Familia.GPT, 1.25m, 0.125m, 10, 10, null, null, null, 1024, 400000, 0.5m, 1, null, true, // A Noviembre 2025 ChatGPT cobra igual los tókenes de salida de razonamiento que los de salida de no razonamiento.
                $"gpt-5.2-pro{Deshabilitado}", factorÉxitoCaché: 0.80, factorÉxitoCachéConGrupoCaché: 0.95,
                razonamientosEfectivosNoPermitidos: new List<RazonamientoEfectivo> { RazonamientoEfectivo.MuyAlto }) },
            { "gpt-5-mini", new Modelo("gpt-5-mini", Familia.GPT, 0.25m, 0.025m, 2, 2, null, null, null, 1024, 400000, 0.5m, 1, null, false,
                "gpt-5.2", $"gpt-5.2-pro{Deshabilitado}", factorÉxitoCaché: 0.25, factorÉxitoCachéConGrupoCaché: 0.25, // A diciembre 2025, la caché de este modelo no funcionaba bien.
                razonamientosEfectivosNoPermitidos: new List<RazonamientoEfectivo> { RazonamientoEfectivo.MuyAlto }) },
            { "gpt-5-nano", new Modelo("gpt-5-nano", Familia.GPT, 0.05m, 0.005m, 0.4m, 0.4m, null, null, null, 1024, 400000, 0.5m, 1, null, false,
                "gpt-5-mini", "gpt-5.2", $"gpt-5.2-pro{Deshabilitado}", factorÉxitoCaché: 0.75, factorÉxitoCachéConGrupoCaché: 0.75,
                razonamientosEfectivosNoPermitidos: new List<RazonamientoEfectivo> { RazonamientoEfectivo.MuyAlto }) },
            { "gpt-4.1", new Modelo("gpt-4.1", Familia.GPT, 2, 0.5m, 8, 8, null, null, null, 1024, 1047576, 0.5m, 1, null, true,
                "gpt-5.2", "gpt-5.2-pro", factorÉxitoCaché: 0.85, factorÉxitoCachéConGrupoCaché: 0.80, // A diciembre 2025, la caché con grupo caché de este modelo no funcionaba bien.
                razonamientosEfectivosPermitidos: new List<RazonamientoEfectivo> { RazonamientoEfectivo.Ninguno },
                verbosidadesPermitidas: new List<Verbosidad>()) }, // Modelo sin razonamiento y sin verbosidad. Se pasa la lista de las verbosidades permitidas vacía y la de los razonamientos efectivos solo con el elemento Ninguno.
            { "claude-opus-4-5", new Modelo("claude-opus-4-5", Familia.Claude, 5, 0.5m, 25, 25, 6.25m, 10, null, null, 200000, 0.5m, 0.5m, 0.5m, false) }, // https://claude.com/pricing#api.
            { "claude-sonnet-4-5", new Modelo("claude-sonnet-4-5", Familia.Claude, 3, 0.3m, 15, 15, 3.75m, 6, null, null, 200000, 0.5m, 0.5m, 0.5m, false,
                "claude-opus-4-5") },
            { "claude-sonnet+-4-5" , new Modelo("claude-sonnet+-4-5", Familia.Claude, 6, 0.6m, 22.5m, 22.5m, 7.5m, 12, null, null, 1000000, 0.5m, 0.5m, 0.5m, 
                false) }, // Modelo de contexto muy grande, útil para el procesamiento de textos muy grandes. 
            { "claude-haiku-4-5", new Modelo("claude-haiku-4-5", Familia.Claude, 1, 0.1m, 5, 5, 1.25m, 2, null, null, 200000, 0.5m, 0.5m, 0.5m, false,
                "claude-sonnet-4-5", "claude-opus-4-5") },
            { "gemini-3-pro-preview", new Modelo("gemini-3-pro-preview", Familia.Gemini, 2, 0.2m, 12, 12, null, null, 4.5m, null, 200000, 0.5m, 1, null, false) }, // https://ai.google.dev/gemini-api/docs/pricing.
            { "gemini-3-pro+-preview", new Modelo("gemini-3-pro+-preview", Familia.Gemini, 4, 0.4m, 18, 18, null, null, 4.5m, null, 1048576, 0.5m, 1, null, false) },
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
        /// Obtiene la longitud útil de la todas las instrucciones considerando el mensaje del usuario, la instrucción del sistema rellenada,
        /// el relleno de la instrucción del sistema (que no cuenta para la longitud útil), la longitud adicional y si es una consulta de calidad adaptable
        /// incluye la instrucción de autoevaluación.
        /// </summary>
        /// <param name="instrucciónSistemaRellena">Instrucción del sistema rellena.</param>
        /// <param name="rellenoInstrucciónSistema">Relleno del instruccion del sistema que no se tiene en cuenta para la longitud útil.</param>
        /// <param name="longitudAdicional">Longitud adicional que se le agrega a la longitud útil: longitud de la descripción de las funciones, 
        /// longitud de contenido de texto en archivos o longitud equivalente a complejidad visual de una imagen que requiera razonamiento.
        /// El caso de las imágenes sería adaptable según el tipo de imagen, pero por el momento no se implementa un análisis que lo calcule 
        /// (por ejemplo, una imagen de un solo color plano no agrega longitud equivalente a la instrucción útil, en cambio una imagen con una tabla de
        /// contenido nutricional si lo hace).</param>
        /// <param name="calidadAdaptable">Sí está en modo calidad adaptable, se agrega a la longitud útil la instrucción de autoevaluación.</param>
        /// <returns></returns>
        internal static int ObtenerLongitudInstrucciónÚtil(string mensajeUsuario, string instrucciónSistemaRellena, string rellenoInstrucciónSistema,
            double longitudAdicional, bool calidadAdaptable) // Se considera el texto de la instrucción de autoevaluación como parte de la longitud útil.
                => Math.Max((mensajeUsuario?.Length ?? 0) + (instrucciónSistemaRellena?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0)
                    + (calidadAdaptable ? InstrucciónAutoevaluación.Length : 0) + (int)Math.Round(longitudAdicional), 0);


        internal static double EstimarTókenesEntradaInstrucciones(string mensajeUsuario, string instrucciónSistemaRellena, string rellenoInstrucciónSistema)
            => (mensajeUsuario?.Length ?? 0) / CarácteresPorTokenTípicos
                + Math.Max((instrucciónSistemaRellena?.Length ?? 0) - (rellenoInstrucciónSistema?.Length ?? 0), 0) / CarácteresPorTokenInstrucciónSistema
                + (rellenoInstrucciónSistema?.Length ?? 0) / CarácteresPorTokenRelleno;


        internal static Razonamiento ObtenerRazonamientoMejorado(Modelo modelo, Razonamiento razonamiento, CalidadAdaptable calidadAdaptable,
            int nivelMejoramientoSugerido, ref StringBuilder información) {

            var nivelMejoramiento = ObtenerNivelMejoramientoRazonamientoEfectivo(calidadAdaptable, nivelMejoramientoSugerido);
            if (nivelMejoramiento == 0) return razonamiento;

            var tieneMuyAlto = modelo.RazonamientosEfectivosPermitidos.Contains(RazonamientoEfectivo.MuyAlto);
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
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.MuyAlto : Razonamiento.Alto;
                    break;
                case Razonamiento.NingunoOBajo:
                    razonamientoMejorado = Razonamiento.BajoOMedio;
                    break;
                case Razonamiento.BajoOMedio:
                    razonamientoMejorado = Razonamiento.MedioOAlto;
                    break;
                case Razonamiento.MedioOAlto:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.AltoOMuyAlto : Razonamiento.Alto;
                    break;
                case Razonamiento.NingunoBajoOMedio:
                    razonamientoMejorado = Razonamiento.BajoMedioOAlto;
                    break;
                case Razonamiento.BajoMedioOAlto:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.MedioAltoOMuyAlto : Razonamiento.MedioOAlto;
                    break;
                case Razonamiento.MuyAlto:
                    razonamientoMejorado = Razonamiento.MuyAlto; // No se puede subir más de MuyAlto.
                    break;
                case Razonamiento.AltoOMuyAlto:
                    razonamientoMejorado = Razonamiento.MuyAlto; // No hay valores con dos razonamientos mayores a AltoOMuyAlto.
                    break;
                case Razonamiento.MedioAltoOMuyAlto:
                    razonamientoMejorado = Razonamiento.AltoOMuyAlto; // No hay valores con tres razonamientos mayores a MedioAltoOMuyAlto, entonces se usa el de dos valores .
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
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.MuyAlto : Razonamiento.Alto;
                    break;
                case Razonamiento.Alto:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.MuyAlto : Razonamiento.Alto;
                    break;
                case Razonamiento.NingunoOBajo:
                    razonamientoMejorado = Razonamiento.MedioOAlto;
                    break;
                case Razonamiento.BajoOMedio:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.AltoOMuyAlto : Razonamiento.Alto;
                    break;
                case Razonamiento.MedioOAlto:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.MuyAlto : Razonamiento.Alto; // No hay valores con dos razonamientos dos niveles arriba de MedioOAlto, entonces solo se devuelve o MuyAlto o Alto si el modelo no soporta MuyAlto.
                    break;
                case Razonamiento.NingunoBajoOMedio:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.MedioAltoOMuyAlto : Razonamiento.MedioOAlto; // En el caso tieneMuyAlto = falso el salto de dos niveles lo lleva a MedioOAlto porque Ninguno pasa a Medio, Bajo a Alto y Medio al mismo Alto.
                    break;
                case Razonamiento.BajoMedioOAlto:
                    razonamientoMejorado = tieneMuyAlto ? Razonamiento.AltoOMuyAlto : Razonamiento.Alto; // En el caso tieneMuyAlto = falso el salto de dos niveles lo lleva directamente a Alto porque Bajo pasa a Alto.
                    break;
                case Razonamiento.MuyAlto:
                    razonamientoMejorado = Razonamiento.MuyAlto;
                    break;
                case Razonamiento.AltoOMuyAlto:
                    razonamientoMejorado = Razonamiento.MuyAlto;
                    break;
                case Razonamiento.MedioAltoOMuyAlto:
                    razonamientoMejorado = Razonamiento.MuyAlto;
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


        internal static string ALetras(this int número) => (número == 1 ? "un" : (número == 2 ? "dos" : "?"));


        internal static RazonamientoEfectivo ObtenerRazonamientoEfectivo(Razonamiento razonamiento, Modelo modelo, Restricciones restricciones,
            int longitudInstrucciónÚtil, out StringBuilder información) {

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
            case Razonamiento.MuyAlto:
                razonamientoEfectivo = RazonamientoEfectivo.MuyAlto;
                break;
            case Razonamiento.NingunoOBajo:
            case Razonamiento.BajoOMedio:
            case Razonamiento.MedioOAlto:
            case Razonamiento.NingunoBajoOMedio:
            case Razonamiento.BajoMedioOAlto:
            case Razonamiento.AltoOMuyAlto:
            case Razonamiento.MedioAltoOMuyAlto:
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

            } else if (razonamiento == Razonamiento.AltoOMuyAlto) {

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Alto;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.MuyAlto;
                }

            } else if (razonamiento == Razonamiento.MedioAltoOMuyAlto) {

                if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamiento) {
                    razonamientoEfectivo = RazonamientoEfectivo.Medio;
                } else if (longitudInstrucciónÚtil < CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles) {
                    razonamientoEfectivo = RazonamientoEfectivo.Alto;
                } else {
                    razonamientoEfectivo = RazonamientoEfectivo.MuyAlto;
                }

            }

            var tamaño = modelo.ObtenerTamaño();
            var aplicadaRestricción = false;

            void AplicarRestricción(ref RazonamientoEfectivo razonamientoEfectivo2, RestricciónRazonamiento restricciónRazonamiento, 
                RazonamientoEfectivo razonamientoEfectivoEvaluado, Tamaño tamaño2) {
                
                if (razonamientoEfectivo2 != razonamientoEfectivoEvaluado) return;
                if (restricciónRazonamiento == RestricciónRazonamiento.Ninguna) return;

                if (restricciónRazonamiento == RestricciónRazonamiento.ModelosPequeños) {

                    if (tamaño2 == Tamaño.MuyPequeño || tamaño2 == Tamaño.Pequeño) { 
                        aplicadaRestricción = true; 
                        razonamientoEfectivo2 = razonamientoEfectivoEvaluado - 1; // Baja un nivel.
                    } 

                } else if (restricciónRazonamiento == RestricciónRazonamiento.ModelosMuyPequeños) {

                    if (tamaño2 == Tamaño.MuyPequeño) { 
                        aplicadaRestricción = true; 
                        razonamientoEfectivo2 = razonamientoEfectivoEvaluado - 1; // Baja un nivel.
                    } 

                }

            } // AplicarRestricción>

            AplicarRestricción(ref razonamientoEfectivo, restricciones.RazonamientoMuyAlto, RazonamientoEfectivo.MuyAlto, tamaño);
            AplicarRestricción(ref razonamientoEfectivo, restricciones.RazonamientoAlto, RazonamientoEfectivo.Alto, tamaño);
            AplicarRestricción(ref razonamientoEfectivo, restricciones.RazonamientoMedio, RazonamientoEfectivo.Medio, tamaño);

            if (EsRazonamientoAdaptable(razonamiento)) información.AgregarLínea($"Razonamiento efectivo = {razonamientoEfectivo}.{(aplicadaRestricción ? " Aplicada restricción de razonamiento." : "")}");

            if (!modelo.RazonamientosEfectivosPermitidos.Contains(razonamientoEfectivo)) 
                throw new Exception($"El modelo {modelo} no permite razonamiento {razonamientoEfectivo}.");

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