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
using static Frugalia.Global;


namespace Frugalia {



    public class Conversación {


        internal List<ResponseItem> ConversaciónGPT { get; }

        internal List<object> ConversaciónGemini { get; }

        internal List<object> ConversaciónClaude { get; }

        private Familia Familia { get; }

        private Action<string> AcciónAgregarInstrucción { get; }

        public void AgregarInstrucción(string instrucción) => AcciónAgregarInstrucción(instrucción);

        private Func<string> FunciónObtenerTextoPrimeraInstrucción { get; }

        public string ObtenerTextoPrimeraInstrucción() => FunciónObtenerTextoPrimeraInstrucción();

        private Func<string> FunciónObtenerTextoÚltimaInstrucción { get; }

        public string ObtenerTextoÚltimaInstrucción() => FunciónObtenerTextoÚltimaInstrucción();

        private Func<TipoMensaje, List<string>> FunciónObtenerTextosInstrucciones { get; }

        public List<string> ObtenerTextosInstrucciones(TipoMensaje tipoInstrucción) => FunciónObtenerTextosInstrucciones(tipoInstrucción);

        private Action<ÍtemRespuesta> AcciónAgregarÍtemRespuesta { get; }

        public void AgregarÍtemRespuesta(ÍtemRespuesta ítemRespuesta) => AcciónAgregarÍtemRespuesta(ítemRespuesta);


        public Conversación(Familia familia) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                ConversaciónGPT = new List<ResponseItem>();

                AcciónAgregarInstrucción = instrucción => ConversaciónGPT.Add(ResponseItem.CreateUserMessageItem(instrucción));

                AcciónAgregarÍtemRespuesta = ítemRespuesta => ConversaciónGPT.Add(ítemRespuesta.ÍtemRespuestaGPT);

                FunciónObtenerTextoPrimeraInstrucción = () => {

                    var primeraInstrucción = ConversaciónGPT.OfType<MessageResponseItem>().FirstOrDefault(m => m.Role == MessageRole.User);
                    if (primeraInstrucción != null) {

                        var texto = primeraInstrucción.Content?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text));
                        return texto?.Text ?? "";

                    } else {
                        return "";
                    }

                };

                FunciónObtenerTextoÚltimaInstrucción = () => {

                    var últimaInstrucción = ConversaciónGPT.OfType<MessageResponseItem>().LastOrDefault(m => m.Role == MessageRole.User);
                    if (últimaInstrucción != null) {

                        var texto = últimaInstrucción.Content?.LastOrDefault(p => !string.IsNullOrEmpty(p.Text));
                        return texto?.Text ?? "";

                    } else {
                        return "";
                    }

                };

                FunciónObtenerTextosInstrucciones = tipo => {

                    var mensajes = ConversaciónGPT.OfType<MessageResponseItem>();

                    IEnumerable<MessageResponseItem> filtrados;
                    switch (tipo) {
                    case TipoMensaje.Usuario:
                        filtrados = mensajes.Where(m => m.Role == MessageRole.User);
                        break;
                    case TipoMensaje.AsistenteAI:
                        filtrados = mensajes.Where(m => m.Role == MessageRole.Assistant);
                        break;
                    case TipoMensaje.Todos:
                    default:
                        filtrados = mensajes.Where(m => m.Role == MessageRole.User || m.Role == MessageRole.Assistant);
                        break;
                    }

                    var textosInstrucciones = new List<string>();
                    foreach (var mensaje in filtrados) {

                        if (mensaje.Content == null) continue;

                        foreach (var parte in mensaje.Content) {
                            var texto = parte?.Text;
                            if (!string.IsNullOrEmpty(texto)) textosInstrucciones.Add(texto);
                        }

                    }
                    return textosInstrucciones;

                };

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
                throw new Exception($"No implementada conversación para el modelo {Familia}");
            }

        } // Conversación>


        public Conversación(List<ResponseItem> conversaciónGPT) : this(Familia.GPT) => ConversaciónGPT = conversaciónGPT;


        public double EstimarTókenesTotales() {

            var textos = ObtenerTextosInstrucciones(TipoMensaje.Todos);
            var totalCarácteres = 0;
            if (textos != null) {
                foreach (var t in textos) {
                    totalCarácteres += t?.Length ?? 0;
                }                    
            }
            return totalCarácteres / (double)CarácteresPorTokenConversaciónTípicos;

        } // EstimarTókenesTotales>


    } // Conversación>


} // Frugalia>