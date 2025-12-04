using OpenAI.Files;
using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using static Frugalia.Global;


namespace Frugalia {


    #pragma warning disable OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
    internal class Archivador {


        internal OpenAIFileClient ArchivadorGPT { get; set; }

        internal object ArchivadorGemini { get; set; }

        internal object ArchivadorClaude { get; set; }

        private readonly List<string> ArchivosIds = new List<string>();

        private Familia Familia { get; }

        private Action AcciónEliminarArchivos { get; }

        public void EliminarArchivos() => AcciónEliminarArchivos();

        private Func<List<string>, string, TipoArchivo, (Conversación Conversación, string Error)> FunciónObtenerConversaciónConArchivos { get; }

        public (Conversación Conversación, string Error) ObtenerConversaciónConArchivos(List<string> rutasArchivos, string instrucción, TipoArchivo tipoArchivo)
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

                        if (string.IsNullOrWhiteSpace(rutaArchivo) || !File.Exists(rutaArchivo)) return (null, $"No se encontró la ruta de archivo {rutaArchivo}.");

                        var archivo = (OpenAIFile)ArchivadorGPT.UploadFile(rutaArchivo, FileUploadPurpose.UserData);
                        ArchivosIds.Add(archivo.Id);
                        if (tipoArchivo == TipoArchivo.Pdf) {
                            instruccionesYArchivos.Add(ResponseContentPart.CreateInputFilePart(archivo.Id));
                        } else if (tipoArchivo == TipoArchivo.Imagen) {
                            instruccionesYArchivos.Add(ResponseContentPart.CreateInputImagePart(archivo.Id));
                        } else {
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


    } // Archivador>
    #pragma warning restore OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.


} // Frugalia>