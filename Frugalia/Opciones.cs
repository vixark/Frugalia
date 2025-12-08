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

using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Frugalia.Global;


namespace Frugalia {


    internal class Opciones {


        internal ResponseCreationOptions OpcionesGPT { get; }

        internal object OpcionesGemini { get; }

        internal object OpcionesClaude { get; }

        private Familia Familia { get; }

        private Action<string> AcciónEscribirInstrucciónSistema { get; }

        internal void EscribirInstrucciónSistema(string instrucciónSistema) => AcciónEscribirInstrucciónSistema(instrucciónSistema);

        private Action<int> AcciónEscribirMáximosTókenesSalida { get; }

        private Func<RazonamientoEfectivo, RestricciónRazonamiento, RestricciónRazonamiento, Modelo, int, StringBuilder> 
            FunciónEscribirOpcionesRazonamientoYObtenerInformación { get; }


        /// <summary>
        /// Por facilidad y evitar introducir errores en el uso de las funciones escribir opciones y escribir máximos tókenes de salida y razonamiento,
        /// ambas se escriben en la misma función. Esto es necesario porque la funcíón que establece los tókenes máximos necesita el razonamiento efectivo 
        /// que se calcula al escribir las opciones de consulta, por lo tanto están altamente acopladas y es mejor llamarlas siempre juntas. Si se implementara un
        /// diseño de escritura independiente, el desarrollador de la librería tendría que recordar llamarlas una después de la otra para no introducir estados
        /// inconsistentes.
        /// </summary>
        /// <param name="razonamiento"></param>
        /// <param name="restricciónRazonamientoAlto"></param>
        /// <param name="restricciónRazonamientoMedio"></param>
        /// <param name="modelo"></param>
        /// <param name="largoInstrucciónÚtil"></param>
        /// <param name="restricciónTókenesSalida"></param>
        /// <param name="restricciónTókenesRazonamiento"></param>
        /// <param name="verbosidad"></param>
        /// <param name="información"></param>
        /// <exception cref="Exception"></exception>
        internal void EscribirOpcionesRazonamientoYLímitesTókenes(Razonamiento razonamiento, RestricciónRazonamiento restricciónRazonamientoAlto,
            RestricciónRazonamiento restricciónRazonamientoMedio, Modelo modelo, int largoInstrucciónÚtil, RestricciónTókenesSalida restricciónTókenesSalida,
            RestricciónTókenesRazonamiento restricciónTókenesRazonamiento, Verbosidad verbosidad, ref StringBuilder información) {

            var razonamientoEfectivo = ObtenerRazonamientoEfectivo(razonamiento, restricciónRazonamientoAlto, restricciónRazonamientoMedio, modelo,
                largoInstrucciónÚtil, out StringBuilder informaciónRazonamientoEfectivo);
            if (!informaciónRazonamientoEfectivo.EsNuloOVacío()) información.AgregarLíneas(informaciónRazonamientoEfectivo);

            var informaciónOpcionesRazonamiento = FunciónEscribirOpcionesRazonamientoYObtenerInformación(razonamientoEfectivo, restricciónRazonamientoAlto, 
                restricciónRazonamientoMedio, modelo, largoInstrucciónÚtil);
            if (!informaciónOpcionesRazonamiento.EsNuloOVacío()) información.AgregarLíneas(informaciónOpcionesRazonamiento);

            var máximosTókenesSalidaYRazonamiento
                = ObtenerMáximosTókenesSalidaYRazonamiento(restricciónTókenesSalida, restricciónTókenesRazonamiento, verbosidad, razonamientoEfectivo);

            if (máximosTókenesSalidaYRazonamiento <= 0) throw new Exception("No se permiten valores negativos o cero para máximosTókenesSalida.");
            if (máximosTókenesSalidaYRazonamiento != SinLímiteTókenes) AcciónEscribirMáximosTókenesSalida(máximosTókenesSalidaYRazonamiento);

        } // EscribirOpcionesRazonamientoYLímitesTókenes>


        private Func<string> FunciónObtenerInstrucciónSistema { get; }

        internal string ObtenerInstrucciónSistema() => FunciónObtenerInstrucciónSistema();


        internal Opciones(Familia familia, string instrucciónSistema, Modelo modelo, Razonamiento razonamiento, RestricciónRazonamiento restricciónRazonamientoAlto, RestricciónRazonamiento restricciónRazonamientoMedio, 
            RestricciónTókenesSalida restricciónTókenesSalida, RestricciónTókenesRazonamiento restricciónTókenesRazonamiento, int largoInstrucciónÚtil,
            Verbosidad verbosidad, bool buscarEnInternet, List<Función> funciones, ref StringBuilder información) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                OpcionesGPT = new ResponseCreationOptions();

                AcciónEscribirInstrucciónSistema = instrucciónSistema2 => OpcionesGPT.Instructions = instrucciónSistema2 ?? "";

                FunciónEscribirOpcionesRazonamientoYObtenerInformación = 
                    (razonamientoEfectivo2, rRazonamientoAlto2, rRazonamientoMedio2, modelo2, largoInstrucciónÚtil2) => {

                    var información2 = new StringBuilder(); // No se ha escrito aún, pero se deja por si es necesario llenarlo más adelante.

                    var nombreModeloMinúsculas = modelo2.Nombre.ToLowerInvariant();

                    if (razonamientoEfectivo2 != RazonamientoEfectivo.Alto && nombreModeloMinúsculas == "gpt-5-pro")
                        throw new InvalidOperationException("gpt-5-pro no permite nivel de razonamiento diferente de alto.");

                    var modelosSinRazonamiento = new List<string> { "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o", "gpt-4o-mini" };
                    if (modelosSinRazonamiento.Contains(nombreModeloMinúsculas)) {

                        if (razonamientoEfectivo2 != RazonamientoEfectivo.Ninguno) // No se debe agregar ReasoningOptions incluso con none en estos modelos porque saca error. Si el usuario correctamente especificó Razonamiento = Ninguno para este modelo, se deja pasar.
                            throw new NotSupportedException($"El modelo {nombreModeloMinúsculas} no soporta razonamiento. Solo se puede usar con " +
                                $"Razonamiento = Ninguno.");

                    } else {

                        var modelosConRazonamientoMinimal = new List<string> { "gpt-5-nano", "gpt-5-mini", "gpt-5" }; // Se hace la lista de los viejos porque se espera que los nuevos mantengan none.                          
                        var textoRazonamiento = "";
                        switch (razonamientoEfectivo2) {
                        case RazonamientoEfectivo.Ninguno:
                            textoRazonamiento = modelosConRazonamientoMinimal.Contains(nombreModeloMinúsculas) ? "minimal" : "none";
                            break;
                        case RazonamientoEfectivo.Bajo:
                            textoRazonamiento = "low";
                            break;
                        case RazonamientoEfectivo.Medio:
                            textoRazonamiento = "medium";
                            break;
                        case RazonamientoEfectivo.Alto:
                            textoRazonamiento = "high";
                            break;
                        default:
                            throw new Exception($"Razonamiento {razonamientoEfectivo2} no considerado.");
                        }

                        OpcionesGPT.ReasoningOptions =
                            new ResponseReasoningOptions { ReasoningEffortLevel = new ResponseReasoningEffortLevel(textoRazonamiento) };

                    }

                    return información2;

                };
    
                AcciónEscribirMáximosTókenesSalida = máximos => OpcionesGPT.MaxOutputTokenCount = máximos;

                EscribirInstrucciónSistema(instrucciónSistema);

                EscribirOpcionesRazonamientoYLímitesTókenes(razonamiento, restricciónRazonamientoAlto, restricciónRazonamientoMedio, modelo, largoInstrucciónÚtil, 
                    restricciónTókenesSalida, restricciónTókenesRazonamiento, verbosidad, ref información);
   
                var modelosSinVerbosidad = new List<string> { "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o", "gpt-4o-mini" }; // Se enumeran los modelos antiguos porque se espera que los nuevos mantengan la verbosidad configurable.
                if (modelosSinVerbosidad.Contains(modelo.Nombre.ToLowerInvariant())) {
                    if (verbosidad != Verbosidad.Media) throw new Exception($"El modelo {modelo} no soporta configuración de la verbosidad.");
                } else {

                    var textoVerbosidad = ""; // https://platform.openai.com/docs/guides/latest-model#verbosity.
                    switch (verbosidad) {
                    case Verbosidad.Baja:
                        textoVerbosidad = "low";
                        break;
                    case Verbosidad.Media:
                        textoVerbosidad = "medium";
                        break;
                    case Verbosidad.Alta:
                        textoVerbosidad = "high";
                        break;
                    default:
                        throw new Exception($"Verbosidad {verbosidad} no considerada.");
                    }
                    OpcionesGPT.Patch.Set(Encoding.UTF8.GetBytes("$.text.verbosity"), textoVerbosidad); // Sí funciona. En unas pruebas se obtuvieron en promedio 393 caracteres; 249 con verbosidad baja y 657 con verbosidad alta. Este parche con Patch.Set() es temporal mientras la API de OpenAI para .NET incluye esta opción de forma estructurada.

                }

                var modelosSinCachéConfigurableA24Horas = new List<string> {
                    "gpt-5-nano", "gpt-5-mini", "gpt-5", "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o", "gpt-4o-mini" };
                if (!modelosSinCachéConfigurableA24Horas.Contains(modelo.Nombre.ToLowerInvariant()))
                    OpcionesGPT.Patch.Set(Encoding.UTF8.GetBytes("$.prompt_cache_retention"), "24h"); // No he comprobado aún si esto funciona. Este parche con Patch.Set() es temporal mientras la API de OpenAI para .NET no incluya esta opción de manera estructurada.

                if (buscarEnInternet) OpcionesGPT.Tools.Add(ResponseTool.CreateWebSearchTool());

                if (funciones != null) {

                    foreach (var función in funciones) {

                        var propiedades = función.Parámetros.ToDictionary(p => p.Nombre, p => (object)new { type = p.Tipo, description = p.Descripción });
                        var requeridos = función.Parámetros.Where(p => p.Requerido).Select(p => p.Nombre).ToArray();
                        var esquema = new { type = "object", properties = propiedades, required = requeridos, additionalProperties = false };
                        var parametros = BinaryData.FromObjectAsJson(esquema);
                        OpcionesGPT.Tools.Add(ResponseTool.CreateFunctionTool(función.Nombre, parametros, strictModeEnabled: true, función.Descripción));

                    }

                }

                FunciónObtenerInstrucciónSistema = () => OpcionesGPT.Instructions;

                break;

            case Familia.Claude:
                Suspender(); // Pendiente implementar.
                break;
            case Familia.Gemini:
                Suspender(); // Pendiente implementar.
                break;
            case Familia.Mistral:
            case Familia.Llama:
            case Familia.DeepSeek:
            case Familia.Qwen:
            case Familia.GLM:
            default:
                throw new Exception($"No implementado el objeto opciones para el modelo {Familia}.");
            }

        } // Opciones>


        /// <summary>
        /// Un control interno de seguridad para que el modelo no se vaya a enloquecer y cobre decenas de miles de tókenes de salida y/o razonamiento, 
        /// generando altos costos. Con los valores de multiplicadores: 1, 2 y 3, máximosTókenesSalidaBase: 200, 350 y 500, y 
        /// máximosTókenesRazonamientoBase: 0, 300, 1000 y 3000, el valor máximo a pagar por consulta en un modelo medio cómo gpt-5.1 sería de 
        /// aproximadamente 0.1 USD. Al eliminar una restricción efectivamente se liberan ambas porque los modelos solo permiten restringir la suma de las dos. Para mantener la consistencia y claridad sobre este comportamiento,
        /// se lanza excepción si se elimina la restricción de razonamiento y no se libera la restricción de salida, y viceversa.
        /// </summary>
        /// <param name="restricciónTókenesSalida"></param>
        /// <param name="verbosidad"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal static int ObtenerMáximosTókenesSalidaYRazonamiento(RestricciónTókenesSalida restricciónTókenesSalida,
            RestricciónTókenesRazonamiento restricciónTókenesRazonamiento, Verbosidad verbosidad, RazonamientoEfectivo razonamientoEfectivo) {

            if ((restricciónTókenesRazonamiento == RestricciónTókenesRazonamiento.Ninguna && restricciónTókenesSalida != RestricciónTókenesSalida.Ninguna) 
                || restricciónTókenesRazonamiento != RestricciónTókenesRazonamiento.Ninguna && restricciónTókenesSalida == RestricciónTókenesSalida.Ninguna)
                throw new Exception("No se permite que RestricciónTókenesRazonamiento == Ninguna y RestricciónTókenesSalida != Ninguna, o viceversa. " +
                    "Los modelos solo permiten establecer el límite de tókenes a la suma de los dos, por lo tanto la eliminación de la restricción en " +
                    "una debe estar acompañada por la restricción de la eliminación de la restricción en la otra para hacer explícito esta liberación de " +
                    "costos completa en el código que usa la librería.");

            int multiplicadorMáximosTókenesSalida;
            switch (restricciónTókenesSalida) {
            case RestricciónTókenesSalida.Alta:
                multiplicadorMáximosTókenesSalida = 1;
                break;
            case RestricciónTókenesSalida.Media:
                multiplicadorMáximosTókenesSalida = 2;
                break;
            case RestricciónTókenesSalida.Baja:
                multiplicadorMáximosTókenesSalida = 3;
                break;
            case RestricciónTókenesSalida.Ninguna:
                return SinLímiteTókenes; // Al liberar salida también se libera razonamiento porque los modelos no aceptan límites independientes.
            default:
                throw new Exception($"Valor de RestricciónTókenesSalida no considerado: {restricciónTókenesSalida}");
            }

            int máximosTókenesSalida; // Un control interno de seguridad para que el modelo no se vaya a enloquecer en algún momento y devuelva miles de tókenes de salida, generando altos costos.
            if (verbosidad == Verbosidad.Baja) {
                máximosTókenesSalida = 200 * multiplicadorMáximosTókenesSalida;
            } else if (verbosidad == Verbosidad.Media) {
                máximosTókenesSalida = 350 * multiplicadorMáximosTókenesSalida;
            } else if (verbosidad == Verbosidad.Alta) {
                máximosTókenesSalida = 500 * multiplicadorMáximosTókenesSalida;
            } else {
                throw new Exception($"Valor de verbosidad no considerado: {verbosidad}.");
            }

            int multiplicadorMáximosTókenesRazonamiento;
            switch (restricciónTókenesRazonamiento) {
            case RestricciónTókenesRazonamiento.Alta:
                multiplicadorMáximosTókenesRazonamiento = 1;
                break;
            case RestricciónTókenesRazonamiento.Media:
                multiplicadorMáximosTókenesRazonamiento = 2;
                break;
            case RestricciónTókenesRazonamiento.Baja:
                multiplicadorMáximosTókenesRazonamiento = 3;
                break;
            case RestricciónTókenesRazonamiento.Ninguna:
                return SinLímiteTókenes; // Al liberar razonamiento también se libera salida porque los modelos no aceptan límites independientes.
            default:
                throw new Exception($"Valor de RestricciónTókenesRazonamiento no considerado: {restricciónTókenesRazonamiento}");
            }

            int máximosTókenesRazonamiento;
            switch (razonamientoEfectivo) {
            case RazonamientoEfectivo.Ninguno:
                máximosTókenesRazonamiento = 0 * multiplicadorMáximosTókenesRazonamiento;
                break;
            case RazonamientoEfectivo.Bajo:
                máximosTókenesRazonamiento = 300 * multiplicadorMáximosTókenesRazonamiento;
                break;
            case RazonamientoEfectivo.Medio:
                máximosTókenesRazonamiento = 1000 * multiplicadorMáximosTókenesRazonamiento;
                break;
            case RazonamientoEfectivo.Alto:
                máximosTókenesRazonamiento = 3000 * multiplicadorMáximosTókenesRazonamiento;
                break;
            default:
                throw new Exception("Valor de razonamiento no considerado.");
            }

            return máximosTókenesSalida + máximosTókenesRazonamiento;

        } // ObtenerMáximosTókenesSalidaYRazonamiento>


    } // Opciones>


} // Frugalia>