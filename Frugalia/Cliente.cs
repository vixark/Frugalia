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
using OpenAI.Batch;
using OpenAI.Files;
using OpenAI.Responses;
using System;
using System.ClientModel;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using static Frugalia.General;


namespace Frugalia {


    internal class Cliente {


        private OpenAIClient ClienteGpt { get; }

        private object ClienteGemini { get; }

        private object ClienteClaude { get; }

        private Familia Familia { get; }

        private string ClaveAPI { get; }

        private DelegadoObtenerRespuesta FunciónObtenerRespuesta { get; }

        private delegate (Respuesta, Tókenes, Resultado) DelegadoObtenerRespuesta(string mensajeUsuario, Conversación conversación, Opciones opciones, 
            Modelo modelo, ModoServicio modo, int segundosLímite, List<string> archivosIds, ref StringBuilder información);

        internal (Respuesta, Tókenes, Resultado) ObtenerRespuesta(string mensajeUsuario, Conversación conversación, Opciones opciones, Modelo modelo, 
            ModoServicio modo, int segundosLímite, List<string> archivosIds, ref StringBuilder información) 
                => FunciónObtenerRespuesta(mensajeUsuario, conversación, opciones, modelo, modo, segundosLímite, archivosIds, ref información);
           
        private Func<Archivador> FunciónObtenerArchivador { get; }

        internal Archivador ObtenerArchivador() => FunciónObtenerArchivador();


        internal Cliente(Familia familia, string claveAPI) {

            Familia = familia;
            ClaveAPI = claveAPI;

            switch (Familia) {
            case Familia.GPT:

                ClienteGpt = new OpenAIClient(claveAPI);
                FunciónObtenerRespuesta = ObtenerRespuestaGpt;
                FunciónObtenerArchivador = () => new Archivador(ClienteGpt.GetOpenAIFileClient());
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


        private (Respuesta, Tókenes, Resultado) ObtenerRespuestaGpt(string mensajeUsuario, Conversación conversación, Opciones opciones, Modelo modelo,
            ModoServicio modo, int segundosLímite, List<string> archivosIds, ref StringBuilder información) {

            var respondedor = ClienteGpt.GetResponsesClient(modelo.Nombre);
            ResponseResult respuestaGpt = null;
            Lote lote = null;
            var resultado = Resultado.Respondido;

            using (var cancelador = new CancellationTokenSource()) {

                cancelador.CancelAfter(TimeSpan.FromSeconds(segundosLímite));

                try {

                    opciones.OpcionesGpt.InputItems.Clear(); // Aunque deberían venir vacía porque no se está asignando este valor al crear el objeto OpcionesGpt, se limpia para asegurar que quede vacía..
                    if (!string.IsNullOrEmpty(mensajeUsuario)) {
                        opciones.OpcionesGpt.InputItems.Add(ResponseItem.CreateUserMessageItem(mensajeUsuario));
                    } else if (conversación != null) {
                        foreach (var item in conversación.ConversaciónGpt) opciones.OpcionesGpt.InputItems.Add(item);
                    } else {
                        throw new InvalidOperationException("Debe haber al menos un mensaje del usuario o conversación.");
                    }

                    if (modo == ModoServicio.Lote) {

                        var json = "";
                        var consultaId = Guid.NewGuid().ToString("N");
                        var líneaJson = Lote.ObtenerLíneaJsonGpt(opciones.OpcionesGpt, modelo.Nombre, consultaId, Archivador.ObtenerDiccionario(archivosIds)); // Se pasan los archivos procesados en los metadatos para recuperarlos cuando se obtenga el resultado de la consulta y eliminarlos.
                        json = AgregarLineaJson(json, líneaJson);

                        var archivador = ObtenerArchivador();
                        var bytes = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false).GetBytes(json);
                        OpenAIFile archivoLote;
                        using (var flujoMemoria = new MemoryStream(bytes)) {
                            archivoLote = (OpenAIFile)archivador.ArchivadorGpt.UploadFile(flujoMemoria, "lote.jsonl", FileUploadPurpose.Batch);
                        }

                        var contenidoLote = BinaryContent.Create(BinaryData.FromObjectAsJson(
                            new { input_file_id = archivoLote.Id, endpoint = "/v1/responses", completion_window = "24h" }));
                        var clienteLote = new BatchClient(ClaveAPI);
                        var operaciónLote = clienteLote.CreateBatch(contenidoLote, waitUntilCompleted: false);
                        var respuesta = operaciónLote.GetRawResponse().Content;
                        var jsonRespuesta = JsonDocument.Parse(respuesta);
                        lote = Lote.ObtenerLoteGpt(jsonRespuesta, consultaId);
                        if (lote.Estado != EstadoLote.Validando) {
                            resultado = Resultado.OtroError;
                            información.AgregarLínea($"Se encontró un estado inesperado en al procesar el lote: {lote.Estado}.");
                        }

                    } else {
                        respuestaGpt = respondedor.CreateResponse(opciones.OpcionesGpt, cancelador.Token);
                    }

                } catch (OperationCanceledException) {
                    resultado = Resultado.TiempoSuperado;
                    return (new Respuesta(respuestaGpt: null), new Tókenes(), resultado);
                }

            }

            var tókenes = new Tókenes();

            if (modo == ModoServicio.Lote) {
                return (new Respuesta(lote, Familia), tókenes, resultado);
            } else {

                if (respuestaGpt.Usage == null) {
                    tókenes = new Tókenes(modelo, modo, "respuestaGpt.Usage es nulo.");
                } else {
                    tókenes = new Tókenes(modelo, modo, respuestaGpt.Usage.InputTokenCount, respuestaGpt.Usage.OutputTokenCount,
                        respuestaGpt.Usage.OutputTokenDetails?.ReasoningTokenCount, respuestaGpt.Usage.InputTokenDetails?.CachedTokenCount, 0, 0);
                }

                if (respuestaGpt?.IncompleteStatusDetails?.Reason.Value.ToString() == "max_output_tokens")
                    resultado = Resultado.MáximosTókenesAlcanzados;
                return (new Respuesta(respuestaGpt), tókenes, resultado);

            }

        } // ObtenerRespuestaGpt>


    } // Cliente>


} // Frugalia>