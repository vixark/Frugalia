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

using OpenAI.Batch;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Frugalia.General;


namespace Frugalia {


    internal class Respuesta {


        internal ResponseResult RespuestaGpt { get; set; }
        
        internal Lote Lote { get; set; }

        internal object RespuestaGemini { get; set; }

        internal object RespuestaLoteGemini { get; set; }

        internal object RespuestaClaude { get; set; }

        internal object RespuestaLoteClaude { get; set; }

        private Familia Familia { get; }

        private Func<TratamientoNegritas, string> FunciónObtenerTextoRespuesta { get; }

        internal string ObtenerTextoRespuesta(TratamientoNegritas tratamientoNegritas) => FunciónObtenerTextoRespuesta(tratamientoNegritas);

        private Func<List<ÍtemRespuesta>> FunciónObtenerÍtemsRespuesta { get; }

        internal List<ÍtemRespuesta> ObtenerÍtemsRespuesta() => FunciónObtenerÍtemsRespuesta();


        private Respuesta(Familia familia) {

            Familia = familia;
            switch (Familia) {
            case Familia.GPT:

                FunciónObtenerTextoRespuesta = (tratamientoNegritas) => {

                    if (Lote != null && RespuestaGpt == null) return $"{Lote}";

                    var respuesta = RespuestaGpt?.GetOutputText() ?? "";

                    if (respuesta.Contains("**")) {

                        switch (tratamientoNegritas) {
                        case TratamientoNegritas.Ninguno:
                            break;
                        case TratamientoNegritas.Eliminar:
                            respuesta = respuesta.Replace("**", "");
                            break;
                        case TratamientoNegritas.ConvertirEnHtml:
                            respuesta = Regex.Replace(respuesta, @"\*\*(.+?)\*\*", "<b>$1</b>", RegexOptions.Singleline); // Reemplaza cualquier secuencia **texto** por <b>texto</b>. El RegexOptions.SingleLine, permite que hayan textos en negritas en múltiples líneas y se conviertan adecuadamente a negritas en múltiples líneas. 
                            break;
                        default:
                            throw new Exception("Caso de tratamiento negritas no considerado.");
                        }

                    }

                    return respuesta;

                };

                FunciónObtenerÍtemsRespuesta = () => RespuestaGpt?.OutputItems == null ? new List<ÍtemRespuesta>() 
                    : RespuestaGpt.OutputItems.Select(i => new ÍtemRespuesta(i)).ToList();

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
                throw new Exception($"No implementada respuesta para el modelo {Familia}");
            }

        } // Respuesta>


        internal Respuesta(ResponseResult respuestaGpt) : this(Familia.GPT) => RespuestaGpt = respuestaGpt;


        internal Respuesta(Lote lote, Familia familia) : this(familia) => Lote = lote;


    } // Respuesta>


} // Frugalia>