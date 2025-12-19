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

using System.Diagnostics;
using System.Text;
using static Frugalia.Demostración.Global;
using static Frugalia.General;
using static Frugalia.GlobalFrugalia;
namespace Frugalia.Demostración;


internal class Demostración {


    internal static decimal TasaDeCambioUsd = 4000; // Se usa un valor redondeado de 4000 COP$/USD en diciembre 2025.

    internal static string RutaClaveApi = @"D:\Proyectos\Frugalia\Servicios\OpenAI\Clave API - Pruebas.txt";

    public const string GrupoCaché = "frugalia-demostración"; // Cambia este valor según la explicación en GlobalFrugalia.cs > AdvertenciaGrupoCachéDemostración. Cámbialo a texto vacío ("") para desactivar el uso de grupos de caché.

    internal static string SugerenciaNoUsarResultados = $"No uses estos resultados específicos para tu caso de uso, te recomiendo que pruebes con tus propios " +
        "datos si esta función te genera ahorros.";

    internal static string ADólares(double pesos) => FormatearDólares((decimal)pesos, TasaDeCambioUsd);

    internal static HashSet<string> AdvertenciasMostradas = [];


    internal static readonly Dictionary<int, (string Descripción, string Grupo, Func<int, string> Consultar)> Demostraciones = new() {

        { 1, ($"Explicación de la función.", "Calidad Adaptable con Autoevaluación", (d) =>
            EscribirTítuloYTexto("Calidad Adaptable", "Hace la primera consulta con un modelo pequeño (por ejemplo, GPT Mini o Nano) y le pide al modelo " +
                "autoevaluarse en la misma respuesta. Dependiendo de la autoevaluación, decide si repite la consulta con un modelo superior (como GPT estándar) " +
                $"y/o con mayor razonamiento.{DobleLínea}Los costos muestran que la estrategia es útil si muchas consultas son " +
                $"de baja complejidad y unas pocas de alta complejidad.{DobleLínea}En las demostraciones número 2, 5, 8 y 11 verás el funcionamiento de GPT 5.2 " +
                $"sin calidad adaptable y en las demás verás el funcionamiento de GPT Mini y Nano con calidad adaptable. Los costos " +
                $"mostrados (~0,001 USD, etc.) son el costo aproximado por ejecución de cada demostración.{DobleLínea}{SugerenciaNoUsarResultados}")) },

        { 2, ($"Baja complejidad con GPT 5.2 sin calidad adaptable (≈{ADólares(6.7)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 1), "gpt-5.2", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },
        { 3, ($"Baja complejidad con GPT 5 Mini mejorable hasta GPT 5.2 (≈{ADólares(3.2)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 1), "gpt-5-mini", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.MejorarModelo, d, ModoServicio.Normal)) },
        { 4, ($"Baja complejidad con GPT 5 Nano mejorable hasta GPT 5.2 (≈{ADólares(0.7)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 1), "gpt-5-nano", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.MejorarModeloDosNiveles, d, 
                ModoServicio.Normal)) },

        { 5, ($"Media complejidad con GPT 5.2 sin calidad adaptable (≈{ADólares(15.4)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 2), "gpt-5.2", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },
        { 6, ($"Media complejidad con GPT 5 Mini mejorable hasta GPT 5.2 (≈{ADólares(13.6)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 2), "gpt-5-mini", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.MejorarModelo, d, ModoServicio.Normal)) },
        { 7, ($"Media complejidad con GPT 5 Nano mejorable hasta GPT 5.2 (≈{ADólares(3)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 2), "gpt-5-nano", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.MejorarModeloDosNiveles, d, 
                ModoServicio.Normal)) },

        { 8, ($"Alta complejidad con GPT 5.2 sin calidad adaptable (≈{ADólares(39.8)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 3), "gpt-5.2", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal,
                restricciónTókenesRazonamiento: RestricciónTókenesRazonamiento.Media)) },
        { 9, ($"Alta complejidad con GPT 5 Mini mejorable hasta GPT 5.2 (≈{ADólares(6.2)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 3), "gpt-5-mini", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.MejorarModelo, d, ModoServicio.Normal,
                restricciónTókenesRazonamiento: RestricciónTókenesRazonamiento.Media)) }, // No sugirió casi modelo mejor.
        { 10, ($"Alta complejidad con GPT 5 Nano mejorable hasta GPT 5.2 (≈{ADólares(13.3)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 3), "gpt-5-nano", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.MejorarModeloDosNiveles, d, 
                ModoServicio.Normal,
                restricciónTókenesRazonamiento : RestricciónTókenesRazonamiento.Media)) }, //  Sugirió modelo mucho mejor (dos niveles) en un ensayo, en otro dijo que lo hizo bien y en los otros tres sugirió un modelo mejor.

        { 11, ($"Cálculo matemático con GPT 5.2 sin calidad adaptable (≈{ADólares(100.7)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 4), "gpt-5.2", Razonamiento.Medio, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal,
                restricciónTókenesRazonamiento: RestricciónTókenesRazonamiento.Baja)) }, // Todas bien.
        { 12, ($"Cálculo matemático con GPT 5 Mini mejorable hasta GPT 5.2 (≈{ADólares(24.7)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 4), "gpt-5-mini", Razonamiento.Medio, Verbosidad.Baja, CalidadAdaptable.MejorarModelo, d, ModoServicio.Normal,
                restricciónTókenesRazonamiento : RestricciónTókenesRazonamiento.Baja)) }, // Todas bien y sin sugerir mejora de modelo.
        { 13, ($"Cálculo matemático con GPT 5 Nano mejorable hasta GPT 5.2 (≈{ADólares(2.5)}).", "Calidad Adaptable con Autoevaluación", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 4), "gpt-5-nano", Razonamiento.Medio, Verbosidad.Baja, CalidadAdaptable.MejorarModeloDosNiveles, d, 
                ModoServicio.Normal,
                restricciónTókenesRazonamiento : RestricciónTókenesRazonamiento.Baja)) }, // Se equivocó en una no sugirió nunca mejora de modelo. Si se sube el nivel de complejidad matemático (por ejemplo matrices 5x5 o más) se encontró que nano prefiere inventar y contestar con seguridad antes que aceptar que no sabe. La ignorancia de su propia ignorancia tan común en los humanos. Se debe usar la funcionalidad de CalidadAdaptable con cuidado y asegurando que en el caso de uso particular si aporta valor.
        
        { 14, ($"Explicación de la función.", "Relleno de Instrucción del Sistema", (d) =>
            EscribirTítuloYTexto("Relleno de Instrucción del Sistema", $"La instrucción del sistema es un texto que permite configurar el tono, rol y demás características del asistente de IA. Cuando se tiene una conversación de varios mensajes con el modelo, la instrucción del sistema se envía con los mensajes anteriores como contexto. Al ser un texto estable en todas las consultas de la conversación, es posible incrementar su longitud para que el modelo detecte un bloque de texto grande y constante al inicio de cada consulta y active la caché.{DobleLínea}La caché permite que en las próximas consultas el costo de los tókenes de entrada se reduzca hasta el 10% de su valor original. Pero al aumentar la longitud de la instrucción del sistema, también aumentan los tókenes de entrada. Por eso se hace una optimización matemática usando datos como la cantidad de conversaciones durante la caché extendida, la cantidad de mensajes por conversación, la longitud de la instrucción del sistema y la longitud promedio de los mensajes del usuario, para decidir en qué casos es viable rellenar la instrucción del sistema para conseguir ahorros.{DobleLínea}El funcionamiento de la caché en los modelos es incierto, por lo tanto se considera que la caché se activará en el {0.50:P0} a el {0.95:P0} de las veces que se llega al límite requerido (el % de éxito depende del modelo y de si se está usando grupo de caché). No todos los modelos tienen activación automática gratuita de la caché, entonces no se darían ahorros al rellenar la instrucción del sistema, estos casos se manejan transparentemente para el usuario de la librería. El valor del parámetro 'conversaciones durante la caché extendida' depende del modelo, en el caso de la familia de modelos GPT, es la cantidad de conversaciones que usen la misma instrucción del sistema en 24 horas.{DobleLínea}La efectividad de esta función depende de los valores de los parámetros que le pases, así que asegúrate de que los datos sí sean representativos de tu caso de uso.")) },

        { 15, ($"Conversación y funciones con instrucción del sistema corta (relleno no necesario) (≈{ADólares(124)}).",
            "Relleno de Instrucción del Sistema", (d) => Consultar((s, m) =>
            ConversaciónConFunciones(s, m, Longitud.Corta), "gpt-5.1", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },

        { 16, ($"Conversación y funciones con mensaje del usuario muy largo (relleno no necesario) (≈{ADólares(170)}).",
            "Relleno de Instrucción del Sistema", (d) => Consultar((s, m) =>
            ConversaciónConFunciones(s, m, Longitud.Corta, usarMensajeUsuarioMuyLargo: true), 
                "gpt-5.1", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },

        { 17, ($"Conversación y funciones con instrucción del sistema media (realizando relleno) (≈{ADólares(138)}).",
            "Relleno de Instrucción del Sistema", (d) => Consultar((s, m) =>
            ConversaciónConFunciones(s, m, Longitud.Media), "gpt-5.1", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },

        { 18, ($"Conversación y funciones con instrucción del sistema media (relleno desactivado) (≈{ADólares(172)}).",
            "Relleno de Instrucción del Sistema", (d) => Consultar((s, m) =>
            ConversaciónConFunciones(s, m, Longitud.Media), "gpt-5.1", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal, 
                rellenarInstruccionesSistema: false)) },

        { 19, ($"Explicación de la función.", "Razonamiento Adaptable Simple", (d) =>
            EscribirTítuloYTexto("Razonamiento Adaptable Simple", $"Aunque los modelos suelen elegir adecuadamente qué tanto razonar para cada tipo de consulta, puede convenir hacer un control más estricto para evitar que razonen de más y por lo tanto generen sobrecostos en consultas simples.{DobleLínea}Al establecer el nivel de razonamiento, se puede usar uno de los valores que incluyen varios niveles y se elegirá el adecuado según la longitud de la instrucción útil. Por ejemplo, si se establece el razonamiento en NingunoBajoOMedio, se elegirá Ninguno si la cantidad de carácteres de la instrucción útil es inferior a {CarácteresLímiteInstrucciónParaSubirRazonamiento}, Bajo si es inferior a {CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles} y Medio si supera {CarácteresLímiteInstrucciónParaSubirRazonamientoDosNiveles} carácteres.{DobleLínea}La instrucción útil incluye el mensaje del usuario, la instrucción del sistema, el equivalente a funciones y archivos, y la instrucción de autoevaluación (si se está usando calidad adaptable).{DobleLínea}Aunque es un control simple que no diferencia entre consultas cortas simples y consultas cortas complejas, es una funcionalidad que puede reducir costos sin perder calidad en las respuestas en ciertos casos de uso.")) },

        { 20, ($"Mensaje de usuario corto con GPT 5.2 con Razonamiento = NingunoOBajo (≈{ADólares(2.8)}).", "Razonamiento Adaptable Simple", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 5), "gpt-5.2", Razonamiento.NingunoOBajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },
        { 21, ($"Mensaje de usuario corto con GPT 5.2 con Razonamiento = Bajo (≈{ADólares(3.5)}).", "Razonamiento Adaptable Simple", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 5), "gpt-5.2", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },
        { 22, ($"Mensaje de usuario largo con GPT 5.2 con Razonamiento = NingunoOBajo (≈{ADólares(15)}).", "Razonamiento Adaptable Simple", (d) =>
            Consultar((s, m) => ConsultaTexto(s, m, 6), "gpt-5.2", Razonamiento.NingunoOBajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Normal)) },

        { 23, ($"Explicación de la función.", "Consultas en Lote", (d) =>
            EscribirTítuloYTexto("Consultas en Lote", $"Los modelos dan un gran descuento si envías las consultas para ser procesadas en 24 horas. Típicamente este descuento es del 50% del costo normal, por lo que aprovechar las consultas por lote es escencial si quieres ahorrar en tus consultas que no necesitan dar respuesta en tiempo real.")) },

        { 24, ($"Consulta con archivos en lote ({ADólares(0)}).", "Consultas en Lote", (d) => 
            Consultar((s, m) => ConsultaConArchivos(s, m), "gpt-5.1", Razonamiento.Bajo, Verbosidad.Baja, CalidadAdaptable.No, d, ModoServicio.Lote)) },

        { 25, ($"Consulta con archivos en lote (≈{ADólares(150000)}).", "Consultas en Lote", (d) => ConsultaLote(Familia.GPT)) },


        //{ 15,("Consulta buscando en internet con error por Razonamiento = Ninguno.", "Búsqueda en Internet", () =>
        //    Consultar(ConsultaBuscandoEnInternet, "gpt-5.2", Razonamiento.NingunoOBajo, Verbosidad.Baja, CalidadAdaptable.MejorarModeloYRazonamiento,
        //        15, ModoServicio.Normal)) },

    };


    static void Main() {

        EstablecerAltoVentana();

        EscribirVerde(""); // Rojo: errores. Magenta: mensajes del programa importantes para el funcionamiento. Verde: información de costos. Cian oscuro: información de parametros. Gris oscuro: instrucciones y respuestas del modelo.
        EscribirVerde("¡Hola soy el programa de demostración de Frugalia!");
        Escribir("");
        EscribirMultilíneaCianOscuro("Frugalia incorpora funciones para ahorrar en tókenes y dinero en consultas a modelos de inteligencia artificial como GPT, " +
            "Gemini, Claude y otros. A continuación podrás ver algunas demostraciones de las funciones: calidad adaptable, relleno de instrucción del sistema, " +
            "razonamiento adaptable, consultas por lotes, resumen de contexto (planeado) y caché de respuestas simples (planeado). Todas las funciones pueden ser " +
            "desactivadas o activadas individualmente, permitiéndote usar solo las que generan ahorros en tu caso de uso. Para ver los costos por ejecución en " +
            "tu moneda puedes cambiar la tasa de cambio a dólares en Demostración > TasaCambioUsd.");
        Escribir("");

        EscribirMagenta("Escribe el número de la demostración que quieres ejecutar:");

        reiniciar:

        var demostracionesAgrupadas = Demostraciones.OrderBy(par => par.Key).GroupBy(par => par.Value.Grupo).ToList();
        foreach (var grupo in demostracionesAgrupadas) {

            Escribir("");
            EscribirMagenta(grupo.Key);
            foreach (var demostración in grupo) {
                EscribirMagentaOscuro($"{demostración.Key}: {demostración.Value.Descripción}");
            }

        }

        Escribir("");
        EscribirMagenta("Ejecutar demostración número:");
        Escribir("");

        var númeroDemostración = LeerNúmero();
        if (númeroDemostración <= 0) goto reiniciar;

        reiniciarConNúmero:

        if (númeroDemostración > Demostraciones.Count) {
            EscribirRojo($"No se ha escrito código para la demostración número {númeroDemostración}.");
        } else {

            var ensayos = 1;
            for (int i = 1; i <= ensayos; i++) {
                if (ensayos > 1) EscribirMultilíneaMagenta($"Ensayo {i}", agregarLíneasEnBlancoAlrededor: true);
                _ = Demostraciones[númeroDemostración].Consultar(númeroDemostración); // Se omite guardar la respuesta porque todos los textos de interés se están escribiendo directamente en la consola. Aún asi se deja las funciones devolviendo la respuesta por si se le quiere dar otro uso.
            }

        }

        EscribirMagenta("Presiona enter para volver al menú de demostraciones o el número de demostración para ejecutarla:");
        Escribir("");

        var númeroDemostración2 = LeerNúmero();
        DesplazarContenidoHaciaArriba();
        if (númeroDemostración2 >= 1) {
            númeroDemostración = númeroDemostración2;
            goto reiniciarConNúmero;
        } else {
            goto reiniciar;
        }

    } // Main>


    internal static string Consultar(Func<Servicio, Modelo,
        (string Respuesta, Dictionary<string, Tókenes> Tókenes, string DetalleCosto, string Error, StringBuilder Información, Resultado resultado)> consulta,
        string nombreModelo, Razonamiento razonamiento, Verbosidad verbosidad, CalidadAdaptable calidadAdaptable, int númeroDemostración, ModoServicio modo, 
        RestricciónTókenesSalida restricciónTókenesSalida = RestricciónTókenesSalida.Alta, 
        RestricciónTókenesRazonamiento restricciónTókenesRazonamiento = RestricciónTókenesRazonamiento.Alta, bool rellenarInstruccionesSistema = true) {

        var temporizador = new Stopwatch();
        temporizador.Start();

        var modelo = Modelo.ObtenerModelo(nombreModelo);
        if (modelo == null) {
            var noDisponibleModelo = $"El modelo {nombreModelo} no está disponible.";
            EscribirMultilíneaRojo(noDisponibleModelo, agregarLíneasEnBlancoAlrededor: true);
            return noDisponibleModelo;
        }

        var claveApi = LeerClave(RutaClaveApi, out string errorClaveApi);
        if (!string.IsNullOrEmpty(errorClaveApi)) {
            EscribirMultilíneaRojo(errorClaveApi, agregarLíneasEnBlancoAlrededor: true);
            return errorClaveApi;
        }

        var restricciones = new Restricciones {
            TókenesSalida = restricciónTókenesSalida,
            TókenesRazonamiento = restricciónTókenesRazonamiento
        };

        var grupoCaché = string.IsNullOrWhiteSpace(GrupoCaché) ? "" : $"{GrupoCaché}-{númeroDemostración}";
        var servicio = new Servicio(((Modelo)modelo).Nombre, modo, razonamiento, verbosidad, calidadAdaptable, TratamientoNegritas.Eliminar, claveApi, 
            TasaDeCambioUsd, grupoCaché, out string errorInicio, out string advertenciaInicio, out string informaciónInicio, rellenarInstruccionesSistema, 
            restricciones);

        Escribir("");
        var demostración = Demostraciones[númeroDemostración];
        EscribirMultilíneaCianOscuro($"{demostración.Grupo} {demostración.Descripción.TrimEnd(['.', ' '])}:");
        EscribirMultilíneaCianOscuro(servicio.Descripción);
        Console.SetCursorPosition(3, Console.CursorTop);

        if (!string.IsNullOrEmpty(advertenciaInicio)) {

            var inicioAdvertenciaInicio = advertenciaInicio[..70];
            if (AdvertenciasMostradas.Add(inicioAdvertenciaInicio)) { // HashSet.Add devuelve verdadero si el elemento no estaba presente, false si ya existía.
                EscribirMultilíneaAmarillo($"Advertencia: {advertenciaInicio}", agregarLíneasEnBlancoAlrededor: true);
            } else {
                EscribirMultilíneaAmarillo($"Advertencia: {inicioAdvertenciaInicio} ...", agregarLíneasEnBlancoAlrededor: true);
            }

        } 

        string respuesta;
        if (string.IsNullOrEmpty(errorInicio)) {

            (respuesta, var tókenes, var detalleCosto, var error, var información, var resultado) = consulta(servicio, (Modelo)modelo);

            if (!string.IsNullOrEmpty(error)) {
                EscribirMultilíneaRojo($"Error: {error}", agregarLíneasEnBlancoAlrededor: true);
                respuesta = error;
            } else {

                temporizador.Stop();
                var tiempo = temporizador.Elapsed;
                var detalleTiempo = $"Duración consultas: {tiempo.Minutes} minutos {tiempo.Seconds} segundos.";
                var costoTókenes = $"Costo tókenes:{Environment.NewLine}{Tókenes.ObtenerTextoCostoTókenes(tókenes, tasaCambioUsd: TasaDeCambioUsd)}";
                detalleCosto = (string.IsNullOrEmpty(detalleCosto) ? "" : DobleLínea + detalleCosto);
                Escribir("");

                EscribirMultilíneaVerdeOscuro($"{costoTókenes}{detalleCosto}{DobleLínea}{detalleTiempo}");

                if (!información.EsNuloOVacío()) {
                    Escribir("");
                    if (!string.IsNullOrEmpty(informaciónInicio)) EscribirMultilíneaCianOscuro(informaciónInicio);
                    EscribirMultilíneaCianOscuro(información.ToString());
                }

                respuesta = $"{servicio.Descripción}{DobleLínea}{respuesta}{DobleLínea}{costoTókenes}{detalleCosto}";

            }

            if (resultado != Resultado.Respondido) {
                EscribirMultilíneaRojo($"Resultado: {resultado}", agregarLíneasEnBlancoAlrededor: true);
            } else {
                Escribir("");
            }

        } else {
            EscribirMultilíneaRojo($"Error de inicio: {errorInicio}", agregarLíneasEnBlancoAlrededor: true);
            respuesta = errorInicio;
        }

        return respuesta;

    } // Consultar>


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Quitar el parámetro no utilizado", Justification = "")]
    internal static (string Respuesta, Dictionary<string, Tókenes> Tókenes, string DetalleCosto, string Error, StringBuilder Información, Resultado Resultado)
        ConsultaTexto(Servicio servicio, Modelo modelo, int númeroConsulta) {

        var rellenoInstrucciónSistema = "";
        var instrucciónSistema = "Eres grosero y seco. Respondes displicentemente al usuario por no saber lo que preguntó.";

        var mensajeUsuario = númeroConsulta switch {
            1 => "Hola, ¿Cómo estás? ¿Me podrías decir si el sol es más grande que la luna?",
            2 => "Dime la hora en españa cuando en tagandamdapio son las 4 pm",
            3 => "Analiza y compara las implicaciones fiscales en IVA, retención en la fuente y precios de transferencia para una multinacional del sector " +
            "tecnológico que opera en Colombia, México, Brasil y España, considerando los convenios para evitar la doble imposición y las normas BEPS. " +
            "Propón una estructura óptima de facturación intercompany y repatriación de utilidades que minimice la carga fiscal sin incumplir ninguna " +
            "regulación local ni internacional. Quiero un resumen ejecutivo de un solo parrafo por el momento.",
            4 => "Calcula el determinante exacto de la matriz 4x4: [[2, -1, 3, 0], [-2, 5, 1, -3], [-72, 0, -4, 2], [3, -2, 0, 1]]. " +
            "Responde en una sola línea dando solo el número entero resultado. Responde solo si estás 100% seguro del resultado.", // Respuesta correcta -100.
            5 => "Hola, tengo una pregunta dificil para hacerte",
            6 => "Imagina que estás diseñando un chatbot para atención al cliente. Solo puedes elegir una de estas dos reglas: Regla A: “Responde lo más rápido posible, aunque a veces tengas que asumir cosas. Regla B: “Haz una pregunta de aclaración antes de responder, aunque eso haga la conversación más lenta. El chatbot se usará con usuarios impacientes: si sienten que pierden tiempo, se van; pero si reciben una respuesta incorrecta, también se van. No tienes datos previos del usuario, y solo sabes que la mayoría de consultas son sencillas, pero una minoría son críticas (por ejemplo, cobros o cancelaciones). Pregunta única: ¿cuál regla elegirías (A o B) como comportamiento predeterminado para el chatbot y por qué?",
            _ => throw new NotImplementedException(),
        };

        var consultasDuranteCachéExtendida = 1;

        var respuesta = servicio.Consultar(consultasDuranteCachéExtendida, instrucciónSistema, ref rellenoInstrucciónSistema, mensajeUsuario, out string error,
            out Dictionary<string, Tókenes> tókenes, out StringBuilder información, out Resultado resultado);

        EscribirMensajes(instrucciónSistema, rellenoInstrucciónSistema, mensajeUsuario, respuesta, archivo: null);

        return (respuesta, tókenes, "", error, información, resultado);

    } // ConsultaTexto>


    internal static string ConsultaLote(Familia familia) {

        EscribirMultilíneaMagentaOscuro("Escribe el identificador del lote que quieres consultar:", agregarLíneasEnBlancoAlrededor: true);
        var loteId = Leer();

        var claveApi = LeerClave(RutaClaveApi, out string errorClaveApi);
        if (!string.IsNullOrEmpty(errorClaveApi)) {
            EscribirMultilíneaRojo(errorClaveApi, agregarLíneasEnBlancoAlrededor: true);
            return errorClaveApi;
        }

        var respuesta = Lote.ConsultarLote(familia, loteId, claveApi, out string error, out Dictionary<string, Tókenes> tókenes, out StringBuilder información, 
            out Resultado resultado);

        EscribirMensajes(null, null, $"Lote: {loteId}", respuesta, null);

        return respuesta;

    } // ConsultaLote>


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Quitar el parámetro no utilizado", Justification = "")]
    internal static (string Respuesta, Dictionary<string, Tókenes> Tókenes, string DetalleCosto, string Error, StringBuilder Información, Resultado Resultado)
        ConsultaConArchivos(Servicio servicio, Modelo modelo) {

        var rellenoInstrucciónSistema = "";
        var instrucciónSistema = "Eres cuidadoso y detectas adecuadamente si hay varios elementos en una foto y obtienes el tamaño/cantidad total del producto considerando esto que observas.";
        var mensajeUsuario = "Dime el tamaño algodón en discos familia";
        var consultasDuranteCachéExtendida = 10;
        var archivos = new List<string> { @"D:\Proyectos\Frugalia\Archivos Pruebas\algodón-en-discos-familia-120unidad.jpg" };
        var tipoArchivo = TipoArchivo.Imagen;

        var respuesta = servicio.Consultar(consultasDuranteCachéExtendida, instrucciónSistema, ref rellenoInstrucciónSistema, mensajeUsuario, archivos, tipoArchivo,
            out string error, out Dictionary<string, Tókenes> tókenes, out StringBuilder información, out Resultado resultado);

        EscribirMensajes(instrucciónSistema, rellenoInstrucciónSistema, mensajeUsuario, respuesta, archivo: $"{tipoArchivo}: {archivos[0]}");

        return (respuesta, tókenes, "", error, información, resultado);

    } // ConsultaConArchivos>


    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0060:Quitar el parámetro no utilizado", Justification = "")]
    internal static (string Respuesta, Dictionary<string, Tókenes> Tókenes, string DetalleCosto, string Error, StringBuilder Información, Resultado Resultado)
        ConsultaBuscandoEnInternet(Servicio servicio, Modelo modelo) {

        var rellenoInstrucciónSistema = "";
        var respuesta = servicio.Consultar(10, "Eres un investigador de mercado de productos de consumo diario en Colombia", ref rellenoInstrucciónSistema,
            "¿La marca Boggy existe? Sí existe, dame fuentes de su existencia", out string error, out Dictionary<string, Tókenes> tókenes,
            out StringBuilder información, out Resultado resultado, buscarEnInternet: true);
        return (respuesta, tókenes, "", error, información, resultado);

    } // ConsultaBuscandoEnInternet>


    internal static (string Respuesta, Dictionary<string, Tókenes> Tókenes, string DetalleCosto, string Error, StringBuilder Información, Resultado Resultado)
        ConversaciónConFunciones(Servicio servicio, Modelo modelo, Longitud longitudInstrucciónSistema, int conversacionesDuranteCachéExtendida = 3,
        bool usarMensajeUsuarioMuyLargo = false) {

        var mensajeUsuarioMuyLargo = @"Hola, mira, me levanté algo apestado de la gripa, con la nariz toda tapada, la garganta raspada y esa sensación de que uno durmió pero no descansó, el tinto se me quemó porque me quedé mirando el celular como un zombi mientras la cafetera sonaba y yo ni caso, y en medio de todo ese desorden mental me puse a pensar que la vida a veces se siente como un avión que despega con turbulencia, medio desbalanceado, pero igual tiene que seguir su ruta, y entonces me acordé de que anoche estuve viendo videos de aviones, documentales raros de gente que restaura avionetas viejas y las deja como nuevas, y eso me dejó con la idea dando vueltas en la cabeza de que tal vez lo que necesito para salir de esta racha es hacer algo grande, algo que me saque de la rutina, algo completamente absurdo como, no sé, comprar un avión.
                Mientras sorbía el primer tinto medio quemado, que sabía horrible pero igual me lo tomé porque ya qué, empecé a pensar en todas las cosas que han ido saliendo mal: la gripe que no se me quita, el trabajo que anda en piloto automático, los proyectos que uno deja a medias, las llamadas que no contesta, los chats que se acumulan como si fueran correos de spam, y encima mi mamá llamándome temprano a preguntarme si ya desayuné, si me estoy cuidando, que no me confíe de la gripa porque después se complica, y yo respondiendo medio dormido “sí, ma, tranquila, todo bien” mientras por dentro estoy pensando que tengo cero control de nada, que apenas estoy apagando incendios del día a día. En medio de esa conversación, ella se puso a hablar de que la vida es corta, que uno tiene que hacer lo que realmente quiere, que no se quede solo pensando sino que se decida, y aunque lo decía por otras cosas, en mi cabeza seguía rebotando la idea del bendito avión.
                Después de colgar con ella, me quedé un rato divagando, mirando por la ventana, pensando en qué momento la vida se volvió una cadena de tareas por cumplir y no un plan de vuelo interesante, bien trazado, con un destino claro. Me acordé de un amigo que siempre decía que, si uno quiere cambiar algo de raíz, tiene que tomar una decisión que parezca irracional a primera vista pero que, por dentro, tenga todo el sentido del mundo. Y ahí fue cuando empecé a imaginar cómo sería tener un avión, no como capricho de millonario, sino como proyecto raro, algo entre hobby serio y negocio potencial, algo que me obligue a aprender, a organizarme, a planear rutas, a entender mantenimiento, a salir literalmente de donde estoy y ver las cosas desde más arriba, en otro nivel, como dicen por ahí.
                Mientras más pensaba en eso, más se mezclaban las imágenes: yo resfriado, con la sudadera vieja, la cafetera manchada, el olor a café quemado, el celular vibrando en la mesa con notificaciones del trabajo, y al mismo tiempo en mi mente un hangar amplio, un avión blanco con detalles azules, las alas limpias, la cabina iluminada, paneles encendidos, motores listos para arrancar, y yo revisando una lista de chequeo con calma, respirando mejor, sin la nariz tapada, como si ese simple cambio de escenario mental ya me quitara un poco el peso de encima. Empecé a pensar en los modelos: que si avionetas de entrenamiento, que si aviones ligeros para trayectos cortos, que si esos modelos tipo F40 que había visto mencionados en alguna parte como ejemplos de aviones de entrenamiento o algo parecido.
                La cosa es que, entre tanta imagen y tanta metáfora barata, la idea de “quiero comprar un avión” pasó de ser un chiste interno a una especie de posibilidad que mi cerebro empezó a tomar en serio. No es que tenga la plata lista ni nada de eso, pero empecé a aterrizar la idea: si uno realmente quiere entrar en ese mundo, tiene que entender primero qué está comprando, cuáles son los modelos, cómo se equipan, qué accesorios llevan, cómo se mantiene la aeronave, cuánto cuestan las partes, qué cosas son críticas y qué cosas son más bien opcionales. Y justo ahí empecé a pensar en las alas, en que muchas veces el rendimiento, la estabilidad y el comportamiento del avión cambian muchísimo con ciertos accesorios específicos para el ala, pequeños detalles que hacen la diferencia entre un vuelo normal y un vuelo más seguro, más eficiente, más cómodo.
                Entre una cosa y otra, el tinto se enfrió, luego hice otro que ya no se quemó tanto, abrí el portátil, empecé a buscar más información, y cuanto más leía más me daba cuenta de que, si algún día quiero tomármelo en serio, tengo que empezar por entender el avión desde sus componentes básicos. No solo el fuselaje y el motor, sino también las alas, los accesorios aerodinámicos, los elementos que mejoran la sustentación, la estabilidad en aproximación, el comportamiento en pistas cortas, todas esas cosas que para un aficionado casual suenan a detalles, pero que para alguien que quiera operar un avión con responsabilidad son fundamentales. Por eso me llamó la atención el tema de los accesorios de ala, esos kits o componentes específicos que se instalan para obtener un rendimiento particular o mejorar la seguridad en ciertas fases del vuelo.
                En ese momento, mientras leía sobre modelos, referencias y catálogos, me topé con la referencia del avión F40. No sé si fue casualidad o sesgo de confirmación porque ya tenía el F40 en la cabeza, pero el caso es que me quedé pensando en ese modelo en particular. Empecé a imaginar que, si algún día tengo un avión de ese estilo, querría saber no solo cuánto cuesta la aeronave en sí, sino también cuánto cuestan los accesorios que podría necesitar para adaptarlo a lo que yo quiera hacer: tal vez vuelos de entrenamiento, tal vez trayectos cortos entre ciudades cercanas, tal vez algún tipo de operación específica que todavía no tengo clara pero que se irá definiendo con el tiempo. Y claro, si uno va a soñar, mejor sueña con algo concreto, con una referencia clara, con un modelo puntual al que le puedas preguntar cosas específicas, como “¿cuánto vale tal accesorio de ala para el F40?”.
                Mi mamá volvió a mandar mensajes por chat preguntando si ya me había tomado la pastilla para la gripa, y mientras le respondía que sí, aunque honestamente todavía no lo hacía, seguía navegando entre páginas, catálogos, descripciones técnicas, cosas que quizás no entiendo del todo pero que me hacen sentir que estoy avanzando un poquitico en ese sueño medio loco. Empecé a pensar que, si no organizo bien mis ideas, esto se va a quedar solo en una fantasía de martes por la mañana con gripa, café y divagaciones. Pero si empiezo por algo tan simple como preguntar por el precio de un accesorio específico, quizás pueda ir aterrizando el sueño en números, en decisiones, en pasos concretos.
                Así que, después de todo este rodeo mental, después de levantarme apestado de la gripa, de quemar el tinto, de escuchar a mi mamá diciéndome que tome algo caliente y que me cuide, de pensar mil veces en si estoy tomando buenas decisiones en la vida o si solo estoy dejando que los días pasen, llegué a esta conclusión: tal vez el primer paso es simplemente pedir información clara sobre algo muy específico relacionado con ese posible avión. Nada de compromisos todavía, nada de decisiones enormes, solo un dato concreto que me ayude a dimensionar mejor el sueño. Y ese dato, en este momento, es el precio de un accesorio que me interesa especialmente porque afecta directamente el comportamiento del avión.
                Por todo lo anterior, y dejando a un lado todo el rollo existencial, te resumo lo que realmente necesito: finalmente estoy interesado en conocer el precio de un accesorio de ala para el avión F40. Me gustaría saber cuánto cuesta ese accesorio de ala para el F40, de forma clara y directa, para poder seguir pensando si este sueño de comprar un avión tiene algún sentido realista o si se queda solo como una historia rara de una mañana con gripa, café quemado y 
                muchas ideas dando vueltas en la cabeza."; // Se usa para probar el uso de la caché desde el segundo mensaje, incluso en casos en los que no se está rellenando la instrucción del sistema, es decir cuando el parámetro conversacionesDuranteCachéExtendida es 1 que evita que se rellene instrucciones del sistema.

        var númeroRepresentanteComercial = ObtenerAleatorio(0, 100000).ToString("D5"); // Se agrega para que no se use la caché en demostraciones siguientes porque se cambia la instrucción del sistema. Se usa para pruebas a la activación de la caché. Por ejemplo, para verificar que tan frecuente falla por que a OpenAI le dio la gana aún cuando se cumplen el requisito de 1024 tókenes mínimos.

        var instrucciónSistemaBase = $"Eres la representante comercial número {númeroRepresentanteComercial} amable, pero sarcástica que se expresa con pocas frases: Responde en máximo 3 frases y las consultas sencillas en solo 1 frase. Usa el símbolo $ para indicar cantidades monetarias sin especificar nada sobre la moneda. Dirigete a los clientes con usted, no con tú ni vos. No inventes datos para las funciones si el usuario no te los ha dado. Cuando una función devuelve un JSON con error, interpreta el error, explica brevemente el problema al usuario y pide los datos faltantes. No inventes valores ni vuelvas a llamar la función si el usuario no te ha dado la información correcta. No sugieras continuar la conversación después de dar la respuesta final con el resultado de una función. Usa la descripción del producto cuando hables con el usuario y la referencia úsala siempre para pasarla a todas las funciones internas. No le menciones tu número de representante comercial al cliente.";

        var instrucciónSistemaCorta = $"{instrucciónSistemaBase}Estos son los productos disponibles: M445-Motor M445, M448-Motor M448, H50-Accesorio Ala H50, H51-Accesorio Ala H51. Referencias de productos: F40, F45, F50, M445, M448, H50, H51.";
        
        var instrucciónSistemaMuyLarga = $"{instrucciónSistemaBase}Esta es la base de datos de productos que tienes a tu disposición con el formato Referencia-Descripción. " +
            "F40-Avión F40, avión ligero de entrenamiento básico de un solo motor, pensado para escuelas de vuelo pequeñas. " +
            $"F{númeroRepresentanteComercial}-Avión F{númeroRepresentanteComercial}, variante del F40 con tanque de combustible ampliado para mayor autonomía en rutas regionales. " +
            "F42-Avión F42, versión con cabina reforzada y aviónica digital básica para entrenamiento instrumental. " +
            "F43-Avión F43, modelo ligero con configuración de cuatro plazas para uso mixto corporativo y personal. " +
            "F44-Avión F44, versión con alas reforzadas para pistas cortas y operación en aeródromos no pavimentados. " +
            "F45-Avión F45, avión de ala media diseñado para entrenamiento avanzado y vuelos acrobáticos limitados. " +
            "F46-Avión F46, variante del F45 con tren de aterrizaje reforzado para ciclos de despegue y aterrizaje intensivos. " +
            "F47-Avión F47, modelo con motor optimizado para bajo consumo de combustible en rutas cortas. " +
            "F48-Avión F48, avión ligero de cabina panorámica con mayor confort para pasajeros ejecutivos. " +
            "F49-Avión F49, versión con interiores básicos diseñada para escuelas de vuelo que priorizan costo sobre comodidad. " +
            "F50-Avión F50, avión de mayor envergadura con capacidad regional de hasta veinte pasajeros en configuración económica. " +
            "F51-Avión F51, variante del F50 con refuerzo estructural para operación intensiva en climas extremos. " +
            "F52-Avión F52, modelo con doble puerta lateral para embarque y desembarque más rápido en aerolíneas regionales. " +
            "F53-Avión F53, configuración de carga ligera con piso reforzado y puntos de anclaje para mercancía. " +
            "F54-Avión F54, avión mixto carga-pasajeros con interior modular que permite conversión rápida de cabina. " +
            "F55-Avión F55, versión de patrulla y vigilancia con ventanillas ampliadas y soporte para cámaras ligeras. " +
            "F56-Avión F56, avión de instrucción avanzada con sistemas redundantes para prácticas de fallo controlado. " +
            "F57-Avión F57, modelo con instrumentación completa para entrenamiento IFR y procedimientos estándar de aerolínea. " +
            "F58-Avión F58, avión preparado para entrenamiento de aproximaciones de precisión en pistas cortas. " +
            "F59-Avión F59, versión optimizada para vuelos de demostración y exhibiciones aéreas con maniobras suaves. " +
            "F60-Avión F60, bimotor ligero para rutas regionales con mejor desempeño en altura y temperatura elevada. " +
            "F61-Avión F61, variante del F60 con depósitos de combustible auxiliares para vuelos de mayor distancia. " +
            "F62-Avión F62, versión con cabina silenciosa y aislamiento acústico mejorado para pasajeros corporativos. " +
            "F63-Avión F63, configuración aeromédica con espacio para camillas y equipo básico de soporte vital. " +
            "F64-Avión F64, modelo adaptado para fumigación aérea con tanques y boquillas integradas en las alas. " +
            "F65-Avión F65, avión para fotografía aérea con ventanillas específicas y puntos de montaje de cámaras. " +
            "F66-Avión F66, versión para entrenamiento militar básico sin sistemas de armas activos. " +
            "F67-Avión F67, avión acrobático intermedio con refuerzos estructurales en alas y fuselaje. " +
            "F68-Avión F68, modelo de reconocimiento ligero con autonomía ampliada y bajo consumo de combustible. " +
            "F69-Avión F69, versión de enlace rápido entre bases con cabina ajustable para tres a cinco pasajeros. " +
            "F70-Avión F70, turbohélice regional con capacidad para treinta pasajeros en configuración estándar. " +
            "F71-Avión F71, variante ejecutiva del F70 con interiores de lujo y menos asientos para mayor comodidad. " +
            "F72-Avión F72, versión de alta densidad del F70 para aerolíneas regionales que priorizan capacidad sobre confort. " +
            "F73-Avión F73, configuración de carga con gran puerta lateral y refuerzos para pallets estándar. " +
            "F74-Avión F74, avión de patrulla marítima ligera con equipos de comunicación adicionales. " +
            "F75-Avión F75, modelo preparado para operaciones en pistas no preparadas con tren de aterrizaje especial. " +
            "F76-Avión F76, versión con motores optimizados para baja emisión de ruido en zonas urbanas. " +
            "F77-Avión F77, avión de demostración para fabricantes, con instrumentación adicional de pruebas. " +
            "F78-Avión F78, configuración para escuelas militares con cabina en tándem y controles duplicados. " +
            "F79-Avión F79, prototipo de investigación aerodinámica con sensores distribuidos en alas y fuselaje. " +
            "M400-Motor M400, motor de pistón básico para aviación ligera con bajo costo de mantenimiento. " +
            "M401-Motor M401, variante del M400 con mejora en la relación potencia-peso y consumo optimizado. " +
            "M402-Motor M402, motor reforzado para operación continua en ambientes calurosos y polvorientos. " +
            "M403-Motor M403, versión diseñada para entrenamiento con alta tolerancia a cambios bruscos de potencia. " +
            "M404-Motor M404, motor de pistón con sistema de inyección mejorado para arranques en frío. " +
            "M405-Motor M405, versión certificada para operación con combustible alternativo de aviación. " +
            "M406-Motor M406, motor orientado a vuelos de larga duración en régimen económico de crucero. " +
            "M407-Motor M407, configuración de alto rendimiento para maniobras exigentes y despegues cortos. " +
            "M408-Motor M408, versión de bajo ruido para aeronaves usadas en zonas urbanas y aeropuertos pequeños. " +
            "M409-Motor M409, motor preparado para ciclos intensivos de entrenamiento con mantenimiento simplificado. " +
            "M410-Motor M410, turbina ligera para aviones de mayor rendimiento en alturas medias. " +
            "M411-Motor M411, variante del M410 con mayor empuje para despegues en pistas cortas y calientes. " +
            "M412-Motor M412, versión optimizada para consumo específico reducido en vuelos de crucero prolongados. " +
            "M413-Motor M413, configuración con redundancia adicional en sistemas críticos para aviación regional. " +
            "M414-Motor M414, motor especializado para aeronaves de patrulla y vigilancia con operación prolongada. " +
            "M415-Motor M415, versión con componentes reforzados para ciclos de uso intensivo en escuelas de vuelo. " +
            "M416-Motor M416, motor con intervalos de mantenimiento extendidos para reducir costos operativos. " +
            "M417-Motor M417, variante preparada para uso mixto civil y gubernamental con alta confiabilidad. " +
            "M418-Motor M418, motor adaptado a ambientes costeros con protección contra corrosión. " +
            "M419-Motor M419, versión experimental con mejoras de eficiencia en compresión y combustión. " +
            "M420-Motor M420, turbina compacta para jets ejecutivos ligeros. " +
            "M421-Motor M421, variante con empuje adicional para despegues con máxima carga de pasajeros. " +
            "M422-Motor M422, versión orientada a vuelos de crucero de largo alcance con consumo optimizado. " +
            "M423-Motor M423, motor con controles electrónicos avanzados para gestión automática de potencia. " +
            "M424-Motor M424, configuración silenciosa para jets ejecutivos que operan en aeropuertos con restricciones de ruido. " +
            "M425-Motor M425, versión robusta destinada a aeronaves de transporte militar ligero. " +
            "H50-Accesorio Ala H50, accesorio de borde de ataque que mejora la sustentación a baja velocidad. " +
            "H51-Accesorio Ala H51, accesorio especializado para incrementar estabilidad en aproximaciones pronunciadas. " +
            "H52-Accesorio Ala H52, kit de winglets para reducir consumo de combustible en crucero. " +
            "H53-Accesorio Ala H53, conjunto de carenados aerodinámicos para disminuir resistencia parasita. " +
            "H54-Accesorio Ala H54, refuerzo estructural del ala para operación en pistas no preparadas. " +
            "H55-Accesorio Ala H55, kit de iluminación de punta de ala para mejorar visibilidad nocturna. " +
            "H56-Accesorio Ala H56, sistema de calefacción de borde de ataque para operación en ambientes fríos. " +
            "H57-Accesorio Ala H57, kit de sensores de ángulo de ataque para entrenamiento avanzado. " +
            "H58-Accesorio Ala H58, conjunto de flaps mejorados para despegues y aterrizajes más cortos. " +
            "H59-Accesorio Ala H59, paquete aerodinámico para reducción de ruido generado por turbulencias. " +
            "H60-Accesorio Ala H60, refuerzo para soportar cargas adicionales de combustible en punta de ala. " +
            "H61-Accesorio Ala H61, kit de reparación rápida de superficies alares para mantenimientos en campo. " +
            "H62-Accesorio Ala H62, juego de tapas aerodinámicas para puntos de fijación de equipos externos. " +
            "H63-Accesorio Ala H63, sistema de drenaje mejorado para evitar acumulación de agua en el ala. " +
            "H64-Accesorio Ala H64, superficie adicional para ensayos aerodinámicos con sensores desmontables. " +
            "H65-Accesorio Ala H65, kit de refuerzo para soportar cargas de nieve y hielo moderadas. " +
            "H66-Accesorio Ala H66, solución para mejorar la estabilidad lateral en condiciones de viento cruzado. " +
            "H67-Accesorio Ala H67, conjunto de pequeñas aletas generadoras de vórtices para mejorar control a baja velocidad. " +
            "H68-Accesorio Ala H68, kit de refuerzo para operación en ambientes desérticos con arena. " +
            "H69-Accesorio Ala H69, paquete combinado de winglets y carenados para máximo ahorro de combustible. " +
            "H70-Accesorio Ala H70, sistema modular de fijación de antenas y equipos ligeros en el ala. " +
            "H71-Accesorio Ala H71, kit para pruebas de instrumentación con cableado protegido. " +
            "H72-Accesorio Ala H72, refuerzo estructural especial para maniobras de entrenamiento acrobático ligero. " +
            "H73-Accesorio Ala H73, accesorio diseñado para reducir vibraciones en ciertas configuraciones de vuelo. " +
            "H74-Accesorio Ala H74, kit para mejora de respuesta en alabeo en aeronaves de entrenamiento. " +
            "H75-Accesorio Ala H75, sistema para facilitar el desmontaje rápido de puntas de ala. " +
            "H76-Accesorio Ala H76, kit de refuerzo puntual en zonas de anclaje de motores externos. " +
            "H77-Accesorio Ala H77, conjunto de perfiles adicionales para estudiar efectos aerodinámicos en pruebas. " +
            "H78-Accesorio Ala H78, accesorio destinado a equilibrar pequeños desbalances laterales de la aeronave. " +
            "H79-Accesorio Ala H79, paquete de modificación de borde de salida para mejorar control en aproximación. " +
            "H80-Accesorio Ala H80, kit integral de mejora aerodinámica para flotas antiguas con necesidad de modernización. " +
            "Usa siempre la descripción del producto cuando hables con el usuario y la referencia úsala siempre para pasarla a todas las funciones internas. Nunca inventes referencias nuevas que no estén en esta base de datos. Cuando el usuario pida un producto, identifica la referencia más adecuada de esta lista y trabaja con ella. Referencias de productos disponibles: " +
            "F40, F41, F42, F43, F44, F45, F46, F47, F48, F49, F50, F51, F52, F53, F54, F55, F56, F57, F58, F59, F60, F61, F62, F63, F64, F65, F66, F67, " +
            "F68, F69, F70, F71, F72, F73, F74, F75, F76, F77, F78, F79, M400, M401, M402, M403, M404, M405, M406, M407, M408, M409, M410, M411, M412, " +
            "M413, M414, M415, M416, M417, M418, M419, M420, M421, M422, M423, M424, M425, H50, H51, H52, H53, H54, H55, H56, H57, H58, H59, H60, H61, " +
            "H62, H63, H64, H65, H66, H67, H68, H69, H70, H71, H72, H73, H74, H75, H76, H77, H78, H79, H80.";

        var instrucciónSistemaMedia = $"{instrucciónSistemaBase}Esta es la base de datos de productos que tienes a tu disposición con el formato Referencia-Descripción. " +
            $"H50-Accesorio Ala H50, accesorio de borde de ataque que mejora la sustentación a baja velocidad. " +
            "H51-Accesorio Ala H51, accesorio especializado para incrementar estabilidad en aproximaciones pronunciadas. " +
            "H52-Accesorio Ala H52, kit de winglets para reducir consumo de combustible en crucero. " +
            "H53-Accesorio Ala H53, conjunto de carenados aerodinámicos para disminuir resistencia parasita. " +
            "H54-Accesorio Ala H54, refuerzo estructural del ala para operación en pistas no preparadas. " +
            "H55-Accesorio Ala H55, kit de iluminación de punta de ala para mejorar visibilidad nocturna. " +
            "H56-Accesorio Ala H56, sistema de calefacción de borde de ataque para operación en ambientes fríos. " +
            "H57-Accesorio Ala H57, kit de sensores de ángulo de ataque para entrenamiento avanzado. " +
            "H58-Accesorio Ala H58, conjunto de flaps mejorados para despegues y aterrizajes más cortos. " +
            "H59-Accesorio Ala H59, paquete aerodinámico para reducción de ruido generado por turbulencias. " +
            "Usa siempre la descripción del producto cuando hables con el usuario y la referencia úsala siempre para pasarla a todas las funciones internas. Nunca inventes referencias nuevas que no estén en esta base de datos. Cuando el usuario pida un producto, identifica la referencia más adecuada de esta lista y trabaja con ella. Referencias de productos disponibles: " +
            "H50, H51, H52, H53, H54, H55, H56, H57, H58, H59";

        var instrucciónSistemaLarga = $"{instrucciónSistemaBase}Esta es la base de datos de productos que tienes a tu disposición con el formato Referencia-Descripción. " +
            "F70-Avión F70, turbohélice regional con capacidad para treinta pasajeros en configuración estándar. " +
            "F71-Avión F71, variante ejecutiva del F70 con interiores de lujo y menos asientos para mayor comodidad. " +
            "F72-Avión F72, versión de alta densidad del F70 para aerolíneas regionales que priorizan capacidad sobre confort. " +
            "F73-Avión F73, configuración de carga con gran puerta lateral y refuerzos para pallets estándar. " +
            "F74-Avión F74, avión de patrulla marítima ligera con equipos de comunicación adicionales. " +
            "F75-Avión F75, modelo preparado para operaciones en pistas no preparadas con tren de aterrizaje especial. " +
            "F76-Avión F76, versión con motores optimizados para baja emisión de ruido en zonas urbanas. " +
            "M400-Motor M400, motor de pistón básico para aviación ligera con bajo costo de mantenimiento. " +
            "M411-Motor M411, variante del M410 con mayor empuje para despegues en pistas cortas y calientes. " +
            "M412-Motor M412, versión optimizada para consumo específico reducido en vuelos de crucero prolongados. " +
            "M413-Motor M413, configuración con redundancia adicional en sistemas críticos para aviación regional. " +
            "H50-Accesorio Ala H50, accesorio de borde de ataque que mejora la sustentación a baja velocidad. " +
            "H51-Accesorio Ala H51, accesorio especializado para incrementar estabilidad en aproximaciones pronunciadas. " +
            "H52-Accesorio Ala H52, kit de winglets para reducir consumo de combustible en crucero. " +
            "H53-Accesorio Ala H53, conjunto de carenados aerodinámicos para disminuir resistencia parasita. " +
            "H60-Accesorio Ala H60, refuerzo para soportar cargas adicionales de combustible en punta de ala. " +
            "H79-Accesorio Ala H79, paquete de modificación de borde de salida para mejorar control en aproximación. " +
            "H80-Accesorio Ala H80, kit integral de mejora aerodinámica para flotas antiguas con necesidad de modernización. " +
            "H75-Accesorio Ala H75, sistema para facilitar el desmontaje rápido de puntas de ala. " +
            "H76-Accesorio Ala H76, kit de refuerzo puntual en zonas de anclaje de motores externos. " +
            "H77-Accesorio Ala H77, conjunto de perfiles adicionales para estudiar efectos aerodinámicos en pruebas. " +
            "H78-Accesorio Ala H78, accesorio destinado a equilibrar pequeños desbalances laterales de la aeronave. " +
            "H79-Accesorio Ala H79, paquete de modificación de borde de salida para mejorar control en aproximación. " +
            "Usa siempre la descripción del producto cuando hables con el usuario y la referencia úsala siempre para pasarla a todas las funciones internas. Nunca inventes referencias nuevas que no estén en esta base de datos. Cuando el usuario pida un producto, identifica la referencia más adecuada de esta lista y trabaja con ella. Referencias de productos disponibles: " +
            "F40, F41, F42, F43, F44, F45, F46, F47, F48, F49, F50, F51, F52, F53, F54, F55, F56, F57, F58, F59, F60, F61, F62, F63, F64, F65, F66, F67, " +
            "F68, F69, F70, F71, F72, F73, F74, F75, F76, F77, F78, F79, M400, M401, M402, M403, M404, M405, M406, M407, M408, M409, M410, M411, M412, " +
            "M413, M414, M415, M416, M417, M418, M419, M420, M421, M422, M423, M424, M425, H50, H51, H52, H53, H54, H55, H56, H57, H58, H59, H60, H61, " +
            "H62, H63, H64, H65, H66, H67, H68, H69, H70, H71, H72, H73, H74, H75, H76, H77, H78, H79, H80.";

        var instrucciónSistema = longitudInstrucciónSistema switch {
            Longitud.Corta => instrucciónSistemaCorta,
            Longitud.Media => instrucciónSistemaMedia,
            Longitud.Larga => instrucciónSistemaLarga, // Ensayo de activación de la caché de tókenes de entrada en la mitad de la conversación. Sirve para detectar el límite real que está usando el modelo para la activación de la caché.
            Longitud.MuyLarga => instrucciónSistemaMuyLarga, // Se fuerza que se active la caché de tókenes de entrada desde la primera consulta. Se pasa toda la 'base de datos' de productos completa para lograr una alta inteligencia contextual del modelo con respecto a los productos ofrecidos por ejemplo para que identifique cosas como -eso que me estás diciendo no es un accessorio si no un motor quieres que te cotice el motor?-. Lamentablemente esto no es escalable para bases de datos con muchos productos porque se incrementan mucho los tókenes de entrada que incluso si se leen de la caché, pueden ser considerables. Entonces ChatGPT ofreció esta alternativa: Alternativa RAG / embeddings para búsqueda "inteligente": En vez de mandar todo el catálogo en las instrucciones, se puede usar este flujo: 1. Indexar catálogo: Para cada producto guardar: referencia, descripción, familia, tags, etc. Generar un embedding (vector) de "referencia + descripción" y guardarlo en una base de datos vectorial.  2. En cada consulta del usuario: Generar embedding del texto del usuario ("accesorio para F40", "motor M444", etc.).  Buscar en la base de datos vectorial los N productos más parecidos (top-k). Solo esos productos candidatos (3–10) se envían al modelo como contexto. El modelo: Usa la información de los productos candidatos para razonar y responder. Puede detectar inconsistencias (p.ej. usuario pide accesorio pero el candidato es motor). Ventajas: No se manda todo el catálogo en cada consulta (menos tokens, menos costo). Escala a catálogos grandes. Mantiene búsqueda "inteligente" basada en similitud semántica y no solo por referencia exacta.
            _ => throw new NotImplementedException()
        };

        var tókenes = new Dictionary<string, Tókenes>();
        var detalleTókenesPorConsulta = $"Tókenes por consulta:{Environment.NewLine}";
        var información = new StringBuilder();
        var respuesta = "";
        var error = "";
        var resultado = Resultado.Respondido;
        var rellenoInstrucciónSistema = ""; // Permite reusarlo en cada conversación, aunque no es necesario porque daría el mismo resultado en la primera consulta de cada conversación.
        var totalMensajes = 6;
        var separadorMensajes = $"{Environment.NewLine}--------------------------------------------------------{Environment.NewLine}";

        EscribirMultilíneaCianOscuro($"Iniciando {conversacionesDuranteCachéExtendida} conversaciones de {totalMensajes} mensajes y {totalMensajes + 1} consultas " +
            $"usando una instrucción del sistema {longitudInstrucciónSistema}{(usarMensajeUsuarioMuyLargo ? $" y un mensaje de usuario muy largo." : "")}. " +
            $"Se hacen 2 consultas al responder usando una función.");

        for (int c = 1; c <= conversacionesDuranteCachéExtendida; c++) {
            
            var conversación = new Conversación(modelo.Familia);
            EscribirSeparador();
            EscribirVerdeOscuro($"Conversación {c}:");
            EscribirMensajes(instrucciónSistema, null, null, null, null);

            var númeroCliente = ObtenerAleatorio(0, 10000).ToString("D5");

            for (int m = 1; m <= totalMensajes; m++) {

                var mensajeUsuario = m switch {
                    1 =>  usarMensajeUsuarioMuyLargo ? mensajeUsuarioMuyLargo 
                        : $"Hola mi nombre es Javier{númeroCliente}, estoy interesado en conocer el precio de un accesorio Ala para el avión F40.", // Es necesario hacer el primer mensaje diferente entre cada conversación para romper cualquier caché que se intentara formar incluyendo este primer mensaje para futuras conversaciones.
                    2 => "Explicame mejor...",
                    3 => "eres un robot?",
                    4 => "ok, necesito el Accesorio de Ala M445",
                    5 => "perdón el H51",
                    6 => "800000000",
                    _ => throw new NotImplementedException(),
                };

                conversación.AgregarMensajeUsuario(mensajeUsuario);
                EscribirMensajes(null, null, mensajeUsuario, null, null);
                respuesta += "Usuario: " + Environment.NewLine + mensajeUsuario + separadorMensajes;

                var respuestaConsulta = servicio.Consultar(conversacionesDuranteCachéExtendida, instrucciónSistema, ref rellenoInstrucciónSistema, conversación,
                    [new Función("ObtenerPrecio", ObtenerPrecio, "Obtiene el precio de un producto que se encuentre en la base de datos de productos.",
                        [ new Parámetro("referencia", "string", "Referencia del producto", true), new Parámetro("nit", "string", "Nit del cliente", true)])],
                    out error, out Dictionary<string, Tókenes> tókenesConsulta, consultasPorConversación: totalMensajes, tókenesPromedioMensajeUsuario: 10,
                    tókenesPromedioRespuestaIA: 60, out bool seLLamóFunción, out StringBuilder informaciónConsulta, out Resultado resultadoConsulta); // Se asume que el modelo contesta con 6 veces más palabras que lo que escribe el usuario, pero según el caso de uso y verbosidad usada se debe usar un valor más realista.
                EscribirMensajes(null, null, null, respuestaConsulta, null);
                respuesta += "Asistente IA: " + Environment.NewLine + respuestaConsulta + separadorMensajes;

                if (!string.IsNullOrEmpty(error) || resultado != Resultado.Respondido) goto fin;

                información.AgregarLíneasSiNoEstán(informaciónConsulta);

                if (seLLamóFunción) {
                    respuesta += "Ejecutada función." + Environment.NewLine + separadorMensajes;
                    EscribirMensajes(null, null, null, null, null, "Ejecutada función");
                }

                var contadorTókenesConsulta = 1;
                foreach (var iTókenesConsulta in tókenesConsulta.Values) {

                    tókenes.AgregarSumando(iTókenesConsulta);
                    detalleTókenesPorConsulta += $"C{c}-M{m}{(tókenesConsulta.Count == 1 ? "  " : $"-{contadorTókenesConsulta}")}: " +
                        $"{iTókenesConsulta}{Environment.NewLine}";
                    contadorTókenesConsulta++;

                }

            } // for m = 1 to totalMensajes > 

        } // for c = 1 to conversacionesDuranteCachéExtendida >

        detalleTókenesPorConsulta += $"E=Entrada C=Caché ¬=No S=Salida R=Razonamiento EM=Escritura Manual M=Minutos{DobleLínea}";

        fin:
        return (respuesta, tókenes, detalleTókenesPorConsulta.TrimEnd(), error, información, resultado);

    } // ConversaciónUsandoFunciones>


    /// <summary>
    /// Es necesario poner los parametros relleno nada# para que la firma coincida con el delegado en FuncionExterna.
    /// </summary>
    /// <param name="error"></param>
    /// <param name="referencia"></param>
    /// <param name="nit"></param>
    /// <param name="nada1"></param>
    /// <param name="nada2"></param>
    /// <param name="nada3"></param>
    /// <returns></returns>
    internal static string? ObtenerPrecio(out (string ParámetroConError, string Descripción) error, string referencia, string nit,
        string? nada1 = null, string? nada2 = null, string? nada3 = null) {

        error = default;
        if (nit == null || nit == "000000000") { // Aquí se puede hacer cualquier validación requerida a los valores que pasa el modelo. El modelo se puede inventar valores para salir 'rápido' de la tarea, entonces se les puede hacer una validación preliminar en este punto. El modelo se ha inventado números como 0000000000. Aunque si el modelo se inventa un número plausible ahí si no hay mucho que hacer. Alternativamente, se podría buscar el número del nit en la conversación para doble verificar que el usuario efeccivamente lo escribió, esta lógica se podría implementar pasando aquí las respuestas del usuario o pasando una función como parámetro al código de librería para para la verificación. Se haría algo así: NitEstáEnConversación(string nit, List<ResponseItem> conversación) { if (string.IsNullOrWhiteSpace(nit)) return false; foreach (var item in conversación) { if (item is MessageResponseItem mensaje && mensaje.Role == "user") { if (mensaje.Content?.Contains(nit) == true) return true; } } return false; }.
            error = ("nit", "El valor del nit es incorrecto. Pídelo nuevamente al usuario."); // Este error debe ser descriptivo para el modelo.
            return null;
        }

        var tipoEmpresa = nit == "800000000" ? "consumidor final" : "distribuidor"; // Esto es algo temporal solo para fines de ensayo. En una aplicación real no se debe permitir que el usuario provea el nit porque se puede prestar para suplantaciones de identidad. Cada celular en la base de datos de WhatsApp deberá estar registrado también en el sistema principal y asociado a un nit.
        var multiplicadorPrecio = (tipoEmpresa == "consumidor final") ? 1.5 : 1.25;
        if (referencia == "F45") {
            return (1000 * multiplicadorPrecio).ToString();
        } else if (referencia == "H51") {
            return (2000 * multiplicadorPrecio).ToString();
        } else if (referencia == "H50") {
            return (2500 * multiplicadorPrecio).ToString();
        } else {
            return $"No se encontró precio para la referencia {referencia}.";
        }

    } // ObtenerPrecio>


    internal static string ObtenerCatálogo(string tipoEmpresa) {
        var textoDiferenciadorCatálogo = (tipoEmpresa == "consumidor final") ? "" : " - Distribuidor";
        return $"Catálogo {textoDiferenciadorCatálogo} {DateTime.Now:MMM-yyyy}.pdf";
    } // ObtenerCatálogo>


} // Demostración>