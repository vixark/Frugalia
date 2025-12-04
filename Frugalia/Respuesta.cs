using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using static Frugalia.Global;


namespace Frugalia {


    #pragma warning disable OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
    internal class Respuesta {


        internal OpenAIResponse RespuestaGPT { get; set; }

        internal object RespuestaGemini { get; set; }

        internal object RespuestaClaude { get; set; }

        private Familia Familia { get; }

        private Action<object> AcciónAsignarRespuesta { get; }

        public void AgregarMensajeUsuario(object respuesta) => AcciónAsignarRespuesta(respuesta);

        private Func<TratamientoNegritas, string> FunciónObtenerTextoRespuesta { get; }

        public string ObtenerTextoRespuesta(TratamientoNegritas tratamientoNegritas) => FunciónObtenerTextoRespuesta(tratamientoNegritas);

        private Func<List<ÍtemRespuesta>> FunciónObtenerÍtemsRespuesta { get; }

        public List<ÍtemRespuesta> ObtenerÍtemsRespuesta() => FunciónObtenerÍtemsRespuesta();


        private Respuesta(Familia familia) {

            Familia = familia;
            switch (Familia) {
            case Familia.GPT:

                AcciónAsignarRespuesta = respuesta => RespuestaGPT = (OpenAIResponse)respuesta;

                FunciónObtenerTextoRespuesta = (tratamientoNegritas) => {

                    var respuesta = RespuestaGPT.GetOutputText() ?? "";

                    if (respuesta.Contains("**")) {

                        switch (tratamientoNegritas) {
                        case TratamientoNegritas.Ninguno:
                            break;
                        case TratamientoNegritas.Eliminar:
                            respuesta = respuesta.Replace("**", "");
                            break;
                        case TratamientoNegritas.ConvertirEnHtml:
                            respuesta = Regex.Replace(respuesta, @"\*\*(.+?)\*\*", "<b>$1</b>"); // Reemplaza todos los **alguna cosa** por <b>alguna cosa</b>.
                            break;
                        default:
                            throw new Exception("Caso de tratamiento negritas no considerado.");
                        }

                    }

                    return respuesta;

                };

                FunciónObtenerÍtemsRespuesta = () => RespuestaGPT.OutputItems.Select(i => new ÍtemRespuesta(i)).ToList();

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


        public Respuesta(OpenAIResponse respuestaGPT) : this(Familia.GPT) => RespuestaGPT = respuestaGPT;


    } // Respuesta>
    #pragma warning restore OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.


} // Frugalia>