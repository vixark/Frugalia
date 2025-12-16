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

using OpenAI;
using OpenAI.Responses;
using System;
using System.Threading;
using static Frugalia.General;


namespace Frugalia {


    internal class Cliente {


        private OpenAIClient ClienteGPT { get; }

        private object ClienteGemini { get; }

        private object ClienteClaude { get; }

        private Familia Familia { get; }

        private Func<string, Conversación, Opciones, Modelo, bool, int, (Respuesta, Tókenes, Resultado)> FunciónObtenerRespuesta { get; }

        internal (Respuesta, Tókenes, Resultado) ObtenerRespuesta(string mensajeUsuario, Conversación conversación, Opciones opciones, Modelo modelo, bool lote, 
            int segundosLímite) => FunciónObtenerRespuesta(mensajeUsuario, conversación, opciones, modelo, lote, segundosLímite);

        private Func<Archivador> FunciónObtenerArchivador { get; }

        internal Archivador ObtenerArchivador() => FunciónObtenerArchivador();


        internal Cliente(Familia familia, string claveAPI) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                ClienteGPT = new OpenAIClient(claveAPI);

                FunciónObtenerRespuesta = (mensajeUsuario, conversación, opciones, modelo, lote, segundosLímite) => {

                    var respondedor = ClienteGPT.GetResponsesClient(modelo.Nombre);
                    ResponseResult respuestaGPT;
                    var resultado = Resultado.Respondido;

                    using (var cancelador = new CancellationTokenSource()) {

                        cancelador.CancelAfter(TimeSpan.FromSeconds(segundosLímite));

                        try {

                            opciones.OpcionesGPT.InputItems.Clear(); // Aunque deberían venir vacía porque no se está asignando este valor al crear el objeto OpcionesGPT, se limpia para asegurar que quede vacía..
                            if (!string.IsNullOrEmpty(mensajeUsuario)) {
                                opciones.OpcionesGPT.InputItems.Add(ResponseItem.CreateUserMessageItem(mensajeUsuario));
                            } else if (conversación != null) {
                                foreach (var item in conversación.ConversaciónGPT) opciones.OpcionesGPT.InputItems.Add(item);
                            } else {
                                throw new InvalidOperationException("Debe haber al menos un mensaje del usuario o conversación.");
                            }
                            respuestaGPT = respondedor.CreateResponse(opciones.OpcionesGPT, cancelador.Token);

                        } catch (OperationCanceledException) {
                            resultado = Resultado.TiempoSuperado;
                            return (new Respuesta(null), new Tókenes(), resultado);
                        }

                    }

                    Tókenes tókenes;
                    if (respuestaGPT.Usage == null) {
                        tókenes = new Tókenes(modelo, lote, "respuestaGPT.Usage es nulo.");
                    } else {
                        tókenes = new Tókenes(modelo, lote, respuestaGPT.Usage.InputTokenCount, respuestaGPT.Usage.OutputTokenCount,
                            respuestaGPT.Usage.OutputTokenDetails?.ReasoningTokenCount, respuestaGPT.Usage.InputTokenDetails?.CachedTokenCount, 0, 0);
                    }
                    
                    if (respuestaGPT?.IncompleteStatusDetails?.Reason.Value.ToString() == "max_output_tokens")
                        resultado = Resultado.MáximosTókenesAlcanzados;

                    return (new Respuesta(respuestaGPT), tókenes, resultado);

                };

                FunciónObtenerArchivador = () => new Archivador(ClienteGPT.GetOpenAIFileClient());

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
                throw new Exception($"No implementado cliente para el modelo {Familia}");
            }

        } // Cliente>


    } // Cliente>


} // Frugalia>