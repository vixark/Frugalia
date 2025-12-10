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

using OpenAI.Files;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using static Frugalia.General;
using static Frugalia.GlobalFrugalia;


namespace Frugalia {


    internal class Archivador {


        internal OpenAIFileClient ArchivadorGPT { get; set; }

        internal object ArchivadorGemini { get; set; }

        internal object ArchivadorClaude { get; set; }

        private readonly List<string> ArchivosIds = new List<string>();

        private Familia Familia { get; }

        private Action AcciónEliminarArchivos { get; }

        internal void EliminarArchivos() => AcciónEliminarArchivos();

        private void EliminarArchivosSubidosParcialmente() => AcciónEliminarArchivos();

        private Func<List<string>, string, TipoArchivo, (Conversación Conversación, string Error)> FunciónObtenerConversaciónConArchivos { get; }

        internal (Conversación Conversación, string Error) ObtenerConversaciónConArchivos(List<string> rutasArchivos, string instrucción, TipoArchivo tipoArchivo)
            => FunciónObtenerConversaciónConArchivos(rutasArchivos, instrucción, tipoArchivo);


        private Archivador(Familia familia) {

            Familia = familia;
            switch (Familia) {
            case Familia.GPT:

                AcciónEliminarArchivos = () => {

                    foreach (var archivoId in ArchivosIds) {
                        ArchivadorGPT.DeleteFile(archivoId);
                    }
                    ArchivosIds.Clear();

                };

                FunciónObtenerConversaciónConArchivos = (rutasArchivos, instrucción, tipoArchivo) => {

                    if (rutasArchivos == null || rutasArchivos.Count == 0) return (null, "Debe enviarse al menos un archivo.");
                    if (string.IsNullOrWhiteSpace(instrucción)) return (null, "La instrucción asociada a los archivos no puede estar vacía.");

                    string error = null;

                    var instruccionesYArchivos = new List<ResponseContentPart>();
                    foreach (var rutaArchivo in rutasArchivos) {

                        if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo)) {
                            EliminarArchivosSubidosParcialmente();
                            return (null, $"No se encontró la ruta de archivo {rutaArchivo}.");
                        }
                            
                        var archivo = (OpenAIFile)ArchivadorGPT.UploadFile(rutaArchivo, FileUploadPurpose.UserData);
                        ArchivosIds.Add(archivo.Id);
                        if (tipoArchivo == TipoArchivo.Pdf) {
                            instruccionesYArchivos.Add(ResponseContentPart.CreateInputFilePart(archivo.Id));
                        } else if (tipoArchivo == TipoArchivo.Imagen) {
                            instruccionesYArchivos.Add(ResponseContentPart.CreateInputImagePart(archivo.Id));
                        } else {
                            EliminarArchivosSubidosParcialmente();
                            error = "Tipo archivo no soportado";
                            return (null, "Tipo archivo no soportado.");
                        }

                    }
                    instruccionesYArchivos.Add(ResponseContentPart.CreateInputTextPart(instrucción));

                    var conversaciónConArchivosGPT = new List<ResponseItem>() { ResponseItem.CreateUserMessageItem(instruccionesYArchivos) };

                    return (new Conversación(conversaciónConArchivosGPT), error);

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
                throw new Exception($"No implementado archivador para el modelo {Familia}");
            }

        } // Archivador>


        public Archivador(OpenAIFileClient archivadorGPT) : this(Familia.GPT) => ArchivadorGPT = archivadorGPT;


        internal static double EstimarTókenes(List<string> rutasArchivos) {

            var tókenesEstimados = 0.0;

            if (rutasArchivos != null) {

                foreach (var rutaArchivo in rutasArchivos) {

                    if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo)) continue;

                    var tamañoArchivoBytes = new FileInfo(rutaArchivo).Length;
                    tókenesEstimados += tamañoArchivoBytes / CarácteresPorTokenTípicos; // Conversión aproximada de bytes a tókenes asumiendo 1 byte por carácter.

                }

            }

            return tókenesEstimados;

        } // EstimarTókenes>


    } // Archivador>


} // Frugalia>