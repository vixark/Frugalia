using OpenAI.Responses;
using System;
using System.Text.Json;
using static Frugalia.Global;



namespace Frugalia {



    #pragma warning disable OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
    public class ÍtemRespuesta {


        internal ResponseItem ÍtemRespuestaGPT { get; set; }

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

                FunciónEsSolicitudFunción = () => ÍtemRespuestaGPT is FunctionCallResponseItem;

                FunciónObtenerJsonArgumentosFunción = () => JsonDocument.Parse(((FunctionCallResponseItem)ÍtemRespuestaGPT).FunctionArguments);

                FunciónObtenerNombreFunción = () => ((FunctionCallResponseItem)ÍtemRespuestaGPT).FunctionName;

                FunciónCrearÍtemRespuestaFunción = (resultadoFunción)
                    => new ÍtemRespuesta(ResponseItem.CreateFunctionCallOutputItem(((FunctionCallResponseItem)ÍtemRespuestaGPT).CallId, resultadoFunción));

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
                throw new Exception($"No implementado ítem respuesta para el modelo {Familia}");
            }

        } // ÍtemRespuesta>


        public ÍtemRespuesta(ResponseItem ítemRespuestaGPT) : this(Familia.GPT) => ÍtemRespuestaGPT = ítemRespuestaGPT;


    } // ÍtemRespuesta>
    #pragma warning restore OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.



} // Frugalia>