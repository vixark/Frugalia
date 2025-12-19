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
using static Frugalia.General;


namespace Frugalia {



    public class Conversación {


        internal List<ResponseItem> ConversaciónGpt { get; }

        internal List<object> ConversaciónGemini { get; }

        internal List<object> ConversaciónClaude { get; }

        private Familia Familia { get; }

        private Action<string> AcciónAgregarMensajeUsuario { get; }

        public void AgregarMensajeUsuario(string mensajeUsuario) => AcciónAgregarMensajeUsuario(mensajeUsuario);

        private Func<string> FunciónObtenerTextoPrimerMensajeUsuario { get; }

        public string ObtenerTextoPrimerMensajeUsuario() => FunciónObtenerTextoPrimerMensajeUsuario();

        private Func<string> FunciónObtenerTextoÚltimoMensajeUsuario { get; }

        public string ObtenerTextoÚltimoMensajeUsuario() => FunciónObtenerTextoÚltimoMensajeUsuario();

        private Func<TipoMensaje, List<string>> FunciónObtenerMensajes { get; }

        public List<string> ObtenerTextosInstrucciones(TipoMensaje tipoInstrucción) => FunciónObtenerMensajes(tipoInstrucción);

        private Action<ÍtemRespuesta> AcciónAgregarÍtemRespuesta { get; }

        public void AgregarÍtemRespuesta(ÍtemRespuesta ítemRespuesta) => AcciónAgregarÍtemRespuesta(ítemRespuesta);


        public Conversación(Familia familia) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                ConversaciónGpt = new List<ResponseItem>();

                AcciónAgregarMensajeUsuario = mensajeUsuario => ConversaciónGpt.Add(ResponseItem.CreateUserMessageItem(mensajeUsuario));

                AcciónAgregarÍtemRespuesta = ítemRespuesta => ConversaciónGpt.Add(ítemRespuesta.ÍtemRespuestaGpt);

                FunciónObtenerTextoPrimerMensajeUsuario = () => {

                    var primerMensajeUsuario = ConversaciónGpt.OfType<MessageResponseItem>().FirstOrDefault(m => m.Role == MessageRole.User);
                    if (primerMensajeUsuario != null) {

                        var texto = primerMensajeUsuario.Content?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text));
                        return texto?.Text ?? "";

                    } else {
                        return "";
                    }

                };

                FunciónObtenerTextoÚltimoMensajeUsuario = () => {

                    var últimoMensajeUsuario = ConversaciónGpt.OfType<MessageResponseItem>().LastOrDefault(m => m.Role == MessageRole.User);
                    if (últimoMensajeUsuario != null) {

                        var texto = últimoMensajeUsuario.Content?.LastOrDefault(p => !string.IsNullOrEmpty(p.Text));
                        return texto?.Text ?? "";

                    } else {
                        return "";
                    }

                };

                FunciónObtenerMensajes = tipo => {

                    var mensajes = ConversaciónGpt.OfType<MessageResponseItem>();

                    IEnumerable<MessageResponseItem> filtrados;
                    switch (tipo) {
                    case TipoMensaje.Usuario:
                        filtrados = mensajes.Where(m => m.Role == MessageRole.User);
                        break;
                    case TipoMensaje.AsistenteIA:
                        filtrados = mensajes.Where(m => m.Role == MessageRole.Assistant);
                        break;
                    case TipoMensaje.Todos:
                    default:
                        filtrados = mensajes.Where(m => m.Role == MessageRole.User || m.Role == MessageRole.Assistant);
                        break;
                    }

                    var textos = new List<string>();
                    foreach (var mensaje in filtrados) {

                        if (mensaje.Content == null) continue;

                        foreach (var parte in mensaje.Content) {
                            var texto = parte?.Text;
                            if (!string.IsNullOrEmpty(texto)) textos.Add(texto);
                        }

                    }

                    return textos;

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


        public Conversación(List<ResponseItem> conversaciónGpt) : this(Familia.GPT) => ConversaciónGpt = conversaciónGpt;


        public double EstimarTókenesTotales() {

            var textos = ObtenerTextosInstrucciones(TipoMensaje.Todos);
            var totalCarácteres = 0;
            if (textos != null) {
                foreach (var t in textos) {
                    totalCarácteres += t?.Length ?? 0;
                }                    
            }

            return totalCarácteres / (double)CarácteresPorTokenTípicos;

        } // EstimarTókenesTotales>


        internal static string ObtenerTextoPrimeraInstrucción(Conversación conversación) => conversación?.ObtenerTextoPrimerMensajeUsuario() ?? "";


        internal static string ObtenerTextoÚltimaInstrucción(Conversación conversación) => conversación?.ObtenerTextoÚltimoMensajeUsuario() ?? "";


    } // Conversación>


} // Frugalia>