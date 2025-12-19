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
using OpenAI.Files;
using OpenAI.Responses;
using System;
using System.ClientModel.Primitives;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
//using static Frugalia.General;


namespace Frugalia {


    public class Lote {


        internal string Id { get; set; }

        internal string ConsultaId { get; set; }

        internal DateTime Creación { get; set; }

        internal DateTime Expiración { get; set; }

        internal EstadoLote Estado { get; set; }

        internal List<string> ArchivosIds { get; set; }

        internal string RespuestaId { get; set; }

        internal string Errores { get; set; }

        internal string ArchivoErroresId { get; set; }

        internal DateTime Finalización { get; set; }

        internal Tókenes Tókenes { get; set; }


        public override string ToString() 
            => $"Id = {Id ?? "null"} ConsultaId = {ConsultaId ?? "null"}  Estado = {Estado}  Creación = {Creación:O}  Expiración = {Expiración:O}";


        public static string ConsultarLote(Familia familia, string loteId, string claveApi, out string error, out Dictionary<string, Tókenes> tókenes, 
            out StringBuilder información, out Resultado resultado, string consultaId = null) {

            resultado = Resultado.Abortado;
            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            error = null;
            Lote lote;

            switch (familia) {
            case Familia.GPT:

                var clienteLote = new BatchClient(claveApi);
                var opcionesSolicitud = new RequestOptions();
                var respuestaLote = clienteLote.GetBatch(loteId, opcionesSolicitud);
                lote = ObtenerLoteGpt(JsonDocument.Parse(respuestaLote.GetRawResponse().Content), consultaId);

                if (lote.Estado == EstadoLote.Completado) {

                    var clienteArchivo = new OpenAIFileClient(claveApi);
                    if (!string.IsNullOrWhiteSpace(lote.RespuestaId)) {

                        var respuestaBinaria = clienteArchivo.DownloadFile(lote.RespuestaId);
                        var respuestaJson = respuestaBinaria.ToString();
                        var líneaRespuesta = consultaId == null ? ObtenerFilaÚnica(respuestaJson) : BuscarLíneaPorConsultaIdGpt(respuestaJson, consultaId); // Permite que en los casos que se hace una consulta por lote, no sea necesario proporcionar el Id de la consulta.
                        if (líneaRespuesta != null && líneaRespuesta.HasValue) {
                            voy aqui
                        }

                    }

                }

                break;

            case Familia.Claude:
            case Familia.Gemini:
            case Familia.DeepSeek:
            case Familia.Mistral:
            case Familia.Llama:
            case Familia.Qwen:
            case Familia.GLM:
            default:
                throw new Exception($"ConsultarLote() no está implementado para la familia {familia}.");
            }

            switch (lote.Estado) {
            case EstadoLote.Falló:
                resultado = Resultado.ErrorLote;
                error = lote.Errores;
                break;
            case EstadoLote.Validando:
            case EstadoLote.EnProgreso:
            case EstadoLote.Finalizando:
                resultado = Resultado.ProcesandoLote;
                break;
            case EstadoLote.Completado:
                resultado = Resultado.Respondido;
                break;
            case EstadoLote.Expiró:
                resultado = Resultado.LoteExpirado;
                break;
            case EstadoLote.Cancelando:
            case EstadoLote.Cancelado:
                resultado = Resultado.LoteCancelado;
                break;
            default:
                throw new Exception("Estado de lote desconocido.");
            }

            return lote.Estado.ToString();

        } // ConsultarLote>


        private static JsonElement? ObtenerFilaÚnica(string json) {

            using (var sr = new StringReader(json)) {

                string líneaUnica = sr.ReadLine();
                if (líneaUnica == null || sr.ReadLine() != null) 
                    throw new Exception("Se esperaba que el archivo de salida del lote contuviera exactamente una línea.");

                return JsonDocument.Parse(líneaUnica).RootElement;  // Clonar como en BuscarLineaPorConsultaIdGpt()>

            }

        } // ObtenerFilaÚnica>


        private static JsonElement? BuscarLíneaPorConsultaIdGpt(string json, string consultaId) {

            using (var sr = new StringReader(json)) {

                string línea;
                while ((línea = sr.ReadLine()) != null) {
                    
                    if (string.IsNullOrWhiteSpace(línea)) continue;
                    using (var documento = JsonDocument.Parse(línea)) {
                        var raíz = documento.RootElement;
                        if (raíz.TryGetProperty("custom_id", out var cid) && cid.GetString() == consultaId) 
                            return JsonDocument.Parse(raíz.GetRawText()).RootElement; // Clonar porque doc se va a Dispose

                    }

                }
                return null;

            }

        } // BuscarLíneaPorConsultaIdGpt>


        /// <summary>
        /// Convierte CreateResponseOptions una línea json para agregar al archivo jsonl de lote.
        /// </summary>
        /// <param name="metadatos">Datos adicionales que se quieran guardar en el servidor de OpenAI que son recuperables al obtener 
        /// la respuesta del lote.</param>
        internal static string ObtenerLíneaJsonGpt(CreateResponseOptions opciones, string nombreModelo, string idConsulta, 
            Dictionary<string, string> metadatos = null) {

            var binarioOpciones = ModelReaderWriter.Write(opciones);
            var jsonOpciones = JsonNode.Parse(binarioOpciones.ToString()).AsObject();
           
            jsonOpciones["model"] = nombreModelo;
 
            if (metadatos != null && metadatos.Count > 0) {

                var metadatosJson = new JsonObject();
                foreach (var kv in metadatos) {
                    metadatosJson[kv.Key] = kv.Value == null ? null : JsonValue.Create(kv.Value);
                }
                jsonOpciones["metadata"] = metadatosJson;

            }

            var line = new JsonObject {
                ["custom_id"] = idConsulta,
                ["method"] = "POST",
                ["url"] = "/v1/responses",
                ["body"] = jsonOpciones
            };

            return line.ToJsonString(new JsonSerializerOptions() { WriteIndented = false });

        } // ObtenerLíneaJsonGpt>


        internal static Lote ObtenerLoteGpt(JsonDocument jsonRespuesta, string consultaId) {

            if (jsonRespuesta == null) throw new ArgumentNullException(nameof(jsonRespuesta));
            var raíz = jsonRespuesta.RootElement;
            var nombreModelo = Regex.Replace(ObtenerTexto(raíz, "model"), @"-\d{4}-\d{2}-\d{2}$", "") ;
            var modelo = Modelo.ObtenerModelo(nombreModelo) 
                ?? throw new Exception($"No se esperaba que el modelo del archivo del lote {nombreModelo} no estuviera con un modelo soportado.");
            return new Lote {
                RespuestaId = ObtenerTexto(raíz, "output_file_id"),
                Errores = ObtenerTexto(raíz, "errors"),
                ArchivoErroresId = ObtenerTexto(raíz, "error_file_id"),
                Id = ObtenerTexto(raíz, "id"),
                ConsultaId = consultaId,
                Creación = ObtenerFecha(ObtenerLong(raíz, "created_at")),
                Expiración = ObtenerFecha(ObtenerLong(raíz, "expires_at")),
                Finalización = ObtenerFecha(ObtenerLong(raíz, "completed_at")),
                Tókenes = ExtraerTókenesGpt(raíz, modelo),
                Estado = MapearEstadoGpt(ObtenerTexto(raíz, "status")),
                ArchivosIds = ObtenerArchivosIdsDesdeMetadatosGpt(raíz),
            };

        } // ObtenerLoteGpt>


        internal static EstadoLote MapearEstadoGpt(string estado) {
            
            switch ((estado ?? "").Trim().ToLowerInvariant()) {
            case "validating": return EstadoLote.Validando;
            case "failed": return EstadoLote.Falló;
            case "in_progress": return EstadoLote.EnProgreso;
            case "finalizing": return EstadoLote.Finalizando;
            case "completed": return EstadoLote.Completado;
            case "expired": return EstadoLote.Expiró;
            case "cancelling": return EstadoLote.Cancelando;
            case "cancelled": return EstadoLote.Cancelado;
            default:
                throw new Exception($"Estado no reconocido {estado}");
            }

        } // MapearEstadoGpt>


        internal static Tókenes ExtraerTókenesGpt(JsonElement elementoJson, Modelo modelo) {

            int? entradaTotal = null;
            int? salidaTotal = null;
            int? salidaRazonamiento = null;
            int? entradaCaché = null;

            if (elementoJson.TryGetProperty("usage", out var usoTókenes) && usoTókenes.ValueKind == JsonValueKind.Object) {

                if (usoTókenes.TryGetProperty("input_tokens", out var it) && it.ValueKind == JsonValueKind.Number) entradaTotal = it.GetInt32();

                if (usoTókenes.TryGetProperty("output_tokens", out var ot) && ot.ValueKind == JsonValueKind.Number) salidaTotal = ot.GetInt32();

                if (usoTókenes.TryGetProperty("input_tokens_details", out var itd) && itd.ValueKind == JsonValueKind.Object
                    && itd.TryGetProperty("cached_tokens", out var ct) && ct.ValueKind == JsonValueKind.Number) entradaCaché = ct.GetInt32();

                if (usoTókenes.TryGetProperty("output_tokens_details", out var otd) && otd.ValueKind == JsonValueKind.Object
                    && otd.TryGetProperty("reasoning_tokens", out var rt) && rt.ValueKind == JsonValueKind.Number) salidaRazonamiento = rt.GetInt32();

            }

            return new Tókenes(modelo, ModoServicio.Lote, entradaTotal, salidaTotal, salidaRazonamiento, entradaCaché, null, null);

        } // ExtraerTókenesGpt>


        internal static List<string> ObtenerArchivosIdsDesdeMetadatosGpt(JsonElement raiz) {

            if (!raiz.TryGetProperty("metadata", out JsonElement meta) || meta.ValueKind != JsonValueKind.Object)
                return new List<string>();

            var archivosIds = new List<string>();

            foreach (var prop in meta.EnumerateObject()) {

                if (!prop.Name.StartsWith("archivo-", StringComparison.OrdinalIgnoreCase)) continue;

                if (prop.Value.ValueKind != JsonValueKind.String) continue;

                var id = prop.Value.GetString();
                if (!string.IsNullOrWhiteSpace(id)) archivosIds.Add(id.Trim());

            }

            return archivosIds;

        } // ObtenerArchivosIdsDesdeMetadatosGpt>


    } // Lote>


} // Frugalia>