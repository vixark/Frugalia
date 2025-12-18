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
using System.Text.Json;
using static Frugalia.General;


namespace Frugalia {


    public class ÍtemRespuesta {


        internal ResponseItem ÍtemRespuestaGpt { get; set; }

        internal object ÍtemRespuestaGemini { get; set; }

        internal object ÍtemRespuestaClaude { get; set; }

        private Familia Familia { get; }

        private Func<bool> FunciónEsSolicitudFunción { get; }

        public bool EsSolicitudFunción() => FunciónEsSolicitudFunción();


        private Func<JsonDocument> FunciónObtenerJsonArgumentosFunción { get; }

        public JsonDocument ObtenerJsonArgumentosFunción() {

            if (EsSolicitudFunción()) {
                return FunciónObtenerJsonArgumentosFunción();
            } else {
                throw new Exception("No se pueden obtener argumentos json de un ítem respuesta que no es solicitud de función.");
            }

        } // ObtenerJsonArgumentosFunción>


        private Func<string> FunciónObtenerNombreFunción { get; }

        public string ObtenerNombreFunción() {

            if (EsSolicitudFunción()) {
                return FunciónObtenerNombreFunción();
            } else {
                throw new Exception("No se pueden obtener el nombre de la función de un ítem respuesta que no es solicitud de función.");
            }

        } // ObtenerNombreFunción>


        private Func<string, ÍtemRespuesta> FunciónCrearÍtemRespuestaFunción { get; }

        public ÍtemRespuesta CrearÍtemRespuestaFunción(string resultadoFunción) {

            if (EsSolicitudFunción()) {
                return FunciónCrearÍtemRespuestaFunción(resultadoFunción);
            } else {
                throw new Exception("No se pueden obtener crear un ítem respuesta de función a partir de un ítem respuesta que no es solicitud de función.");
            }

        } // CrearÍtemRespuestaFunción>


        private ÍtemRespuesta(Familia familia) {

            Familia = familia;
            switch (Familia) {
            case Familia.GPT:

                FunciónEsSolicitudFunción = () => ÍtemRespuestaGpt is FunctionCallResponseItem;

                FunciónObtenerJsonArgumentosFunción = () => JsonDocument.Parse(((FunctionCallResponseItem)ÍtemRespuestaGpt).FunctionArguments);

                FunciónObtenerNombreFunción = () => ((FunctionCallResponseItem)ÍtemRespuestaGpt).FunctionName;

                FunciónCrearÍtemRespuestaFunción = (resultadoFunción)
                    => new ÍtemRespuesta(ResponseItem.CreateFunctionCallOutputItem(((FunctionCallResponseItem)ÍtemRespuestaGpt).CallId, resultadoFunción));

                break;

            case Familia.Claude:
                Suspender(); // Implementación pendiente.
                break;
            case Familia.Gemini:
                Suspender(); // Implementación pendiente.
                break;
            case Familia.Mistral:
            case Familia.Llama:
            case Familia.DeepSeek:
            case Familia.Qwen:
            case Familia.GLM:
            default:
                throw new Exception($"No implementado ítem respuesta para el modelo {Familia}");
            }

        } // ÍtemRespuesta>


        public ÍtemRespuesta(ResponseItem ítemRespuestaGpt) : this(Familia.GPT) => ÍtemRespuestaGpt = ítemRespuestaGpt;


    } // ÍtemRespuesta>
    

} // Frugalia>