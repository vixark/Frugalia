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

        public void EscribirInstrucciónSistema(string instrucciónSistema) => AcciónEscribirInstrucciónSistema(instrucciónSistema);

        private Action<Razonamiento, RestricciónRazonamiento, RestricciónRazonamiento, string, int> AcciónEscribirOpcionesRazonamiento { get; }

        public void EscribirOpcionesRazonamiento(Razonamiento razonamiento, RestricciónRazonamiento restricciónRazonamientoAlto,
            RestricciónRazonamiento restricciónRazonamientoMedio, string nombreModelo, int largoInstrucciónÚtil)
                => AcciónEscribirOpcionesRazonamiento(razonamiento, restricciónRazonamientoAlto, restricciónRazonamientoMedio, nombreModelo, largoInstrucciónÚtil);

        private Func<string> FunciónObtenerInstrucciónSistema { get; }

        public string ObtenerInstrucciónSistema() => FunciónObtenerInstrucciónSistema();


        public Opciones(Familia familia, string instrucciónSistema, string nombreModelo, Razonamiento razonamiento,
            RestricciónRazonamiento restricciónRazonamientoAlto, RestricciónRazonamiento restricciónRazonamientoMedio, int largoInstrucciónÚtil,
            int máximosTókenesSalida, Verbosidad verbosidad, bool buscarEnInternet, List<Función> funciones) {

            Familia = familia;
            switch (Familia) {
            case Familia.GPT:

                OpcionesGPT = new ResponseCreationOptions();

                AcciónEscribirInstrucciónSistema = instrucciónSistema2 => OpcionesGPT.Instructions = instrucciónSistema2 ?? "";

                AcciónEscribirOpcionesRazonamiento = (razonamiento2, rRazonamientoAlto2, rRazonamientoMedio2, nombreModelo2, largoInstrucciónÚtil2) => {

                    var razonamientoEfectivo = ObtenerRazonamientoEfectivo(razonamiento2, rRazonamientoAlto2, rRazonamientoMedio2,
                        nombreModelo2, largoInstrucciónÚtil2);

                    if (string.IsNullOrWhiteSpace(nombreModelo2)) throw new ArgumentException("nombreModelo2 no puede ser nulo ni vacío.", nameof(nombreModelo2));
                    var nombreModeloMinúsculas = nombreModelo2.ToLowerInvariant();

                    if (razonamientoEfectivo != Razonamiento.Alto && nombreModeloMinúsculas == "gpt-5-pro")
                        throw new InvalidOperationException("gpt-5-pro no permite nivel de razonamiento diferente de alto.");

                    var modelosSinRazonamiento = new List<string> { "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o", "gpt-4o-mini" };
                    if (modelosSinRazonamiento.Contains(nombreModeloMinúsculas)) {

                        if (razonamientoEfectivo != Razonamiento.Ninguno) // No se debe agregar ReasoningOptions incluso con none en estos modelos porque saca error. Si el usuario correctamente especificó Razonamiento = Ninguno para este modelo, se deja pasar.
                            throw new NotSupportedException($"El modelo {nombreModeloMinúsculas} no soporta razonamiento. Solo se puede usar con Razonamiento = Ninguno.");

                    } else {

                        var modelosConRazonamientoMinimal = new List<string> { "gpt-5-nano", "gpt-5-mini", "gpt-5" }; // Se hace la lista de los viejos porque se espera que los nuevos mantengan none.                          
                        var textoRazonamiento = "";
                        switch (razonamientoEfectivo) {
                        case Razonamiento.Ninguno:
                            textoRazonamiento = modelosConRazonamientoMinimal.Contains(nombreModeloMinúsculas) ? "minimal" : "none";
                            break;
                        case Razonamiento.Bajo:
                            textoRazonamiento = "low";
                            break;
                        case Razonamiento.Medio:
                            textoRazonamiento = "medium";
                            break;
                        case Razonamiento.Alto:
                            textoRazonamiento = "high";
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(razonamiento2), $"Razonamiento {razonamientoEfectivo} no considerado.");
                        }

                        OpcionesGPT.ReasoningOptions =
                            new ResponseReasoningOptions { ReasoningEffortLevel = new ResponseReasoningEffortLevel(textoRazonamiento) };

                    }

                };

                EscribirInstrucciónSistema(instrucciónSistema);

                EscribirOpcionesRazonamiento(razonamiento, restricciónRazonamientoAlto, restricciónRazonamientoMedio, nombreModelo, largoInstrucciónÚtil);

                OpcionesGPT.MaxOutputTokenCount = máximosTókenesSalida;
                var modelosSinVerbosidad = new List<string> { "gpt-4.1", "gpt-4.1-mini", "gpt-4.1-nano", "gpt-4o", "gpt-4o-mini" }; // Se hace la lista de los viejos porque se espera que los nuevos mantengan verbosidad configurable.
                if (modelosSinVerbosidad.Contains(nombreModelo.ToLowerInvariant())) {
                    if (verbosidad != Verbosidad.Media) throw new Exception($"El modelo {nombreModelo} no soporta configuración de la verbosidad.");
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
                    OpcionesGPT.Patch.Set(Encoding.UTF8.GetBytes("$.text.verbosity"), textoVerbosidad); // Sí funciona. En unas pruebas se obtuvo con media 393 carácteres promedio. 249 caractéres promedio con baja y 657 carácteres promedio con alta. Este parche con Patch.Set() es temporal mientras la API de OpenAI para .Net no incluya esta opción de manera estructurada.                    

                }

                OpcionesGPT.Patch.Set(Encoding.UTF8.GetBytes("$.prompt_cache_retention"), "24h"); // No he probado si esto funciona. Este parche con Patch.Set() es temporal mientras la API de OpenAI para .Net no incluya esta opción de manera estructurada.

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


    } // Opciones>


} // Frugalia>