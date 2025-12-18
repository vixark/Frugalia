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
using System.Text.Json;
using System.Text.Json.Nodes;
using static Frugalia.General;


namespace Frugalia {


    internal class Lote {


        internal string Id { get; set; }

        internal string ConsultaId { get; set; }

        internal DateTime Creación { get; set; }

        internal DateTime Expiración { get; set; }

        internal EstadoLote Estado { get; set; }

        internal List<string> ArchivosIds { get; set; }


        public override string ToString() 
            => $"Id = {Id ?? "null"} ConsultaId = {ConsultaId ?? "null"}  Estado = {Estado}  Creación = {Creación:O}  Expiración = {Expiración:O}";

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
            var raízJson = jsonRespuesta.RootElement;
            return new Lote {
                Id = ObtenerTexto(raízJson, "id"),
                ConsultaId = consultaId,
                Creación = ObtenerFecha(ObtenerLong(raízJson, "created_at")),
                Expiración = ObtenerFecha(ObtenerLong(raízJson, "expires_at")),
                Estado = MapearEstadoGpt(ObtenerTexto(raízJson, "status")),
                ArchivosIds = Archivador.ObtenerArchivosIdsDesdeMetadatos(raízJson),
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


    } // Lote>


} // Frugalia>