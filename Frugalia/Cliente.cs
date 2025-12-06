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
using static Frugalia.Global;


namespace Frugalia {


    internal class Cliente {


        private OpenAIClient ClienteGPT { get; }

        private object ClienteGemini { get; }

        private object ClienteClaude { get; }

        private Familia Familia { get; }

        private Func<string, Conversación, Opciones, Modelo, bool, (Respuesta, Tókenes)> FunciónObtenerRespuesta { get; }

        internal (Respuesta, Tókenes) ObtenerRespuesta(string instrucción, Conversación conversación, Opciones opciones, Modelo modelo, bool lote)
            => FunciónObtenerRespuesta(instrucción, conversación, opciones, modelo, lote);

        private Func<Archivador> FunciónObtenerArchivador { get; }

        internal Archivador ObtenerArchivador() => FunciónObtenerArchivador();


        internal Cliente(Familia familia, string claveAPI) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                ClienteGPT = new OpenAIClient(claveAPI);

                FunciónObtenerRespuesta = (instrucción, conversación, opciones, modelo, lote) => {

                    var nombreModelo = modelo.Nombre;
                    var respondedorInicial = ClienteGPT.GetOpenAIResponseClient(nombreModelo);
                    OpenAIResponse respuestaGPT;
                    if (!string.IsNullOrEmpty(instrucción)) {
                        respuestaGPT = (OpenAIResponse)respondedorInicial.CreateResponse(instrucción, opciones.OpcionesGPT);
                    } else if (conversación != null) {
                        respuestaGPT = (OpenAIResponse)respondedorInicial.CreateResponse(conversación.ConversaciónGPT, opciones.OpcionesGPT);
                    } else {
                        throw new InvalidOperationException("Debe haber al menos una instrucción o conversación.");
                    }

                    Tókenes tókenes;
                    if (respuestaGPT.Usage == null) {
                        tókenes = new Tókenes(nombreModelo, lote, "respuestaGPT.Usage es nulo.");
                    } else {
                        tókenes = new Tókenes(nombreModelo, lote, respuestaGPT.Usage.InputTokenCount, respuestaGPT.Usage.OutputTokenCount,
                            respuestaGPT.Usage.OutputTokenDetails?.ReasoningTokenCount, respuestaGPT.Usage.InputTokenDetails?.CachedTokenCount, 0, 0);
                    }
                        
                    return (new Respuesta(respuestaGPT), tókenes);

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