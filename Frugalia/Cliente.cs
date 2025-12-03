using OpenAI;
using OpenAI.Responses;
using System;
using static Frugalia.Global;



namespace Frugalia {



    #pragma warning disable OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
    internal class Cliente {


        internal OpenAIClient ClienteGPT { get; }

        internal object ClienteGemini { get; }

        internal object ClienteClaude { get; }

        private Familia Familia { get; }

        private Func<string, Conversación, Opciones, string, bool, (Respuesta, Tókenes)> FunciónObtenerRespuesta { get; }

        public (Respuesta, Tókenes) ObtenerRespuesta(string instrucción, Conversación conversación, Opciones opciones, string nombreModelo, bool lote)
            => FunciónObtenerRespuesta(instrucción, conversación, opciones, nombreModelo, lote);

        private Func<Archivador> FunciónObtenerArchivador { get; }

        public Archivador ObtenerArchivador() => FunciónObtenerArchivador();


        public Cliente(Familia familia, string claveAPI) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                ClienteGPT = new OpenAIClient(claveAPI);

                FunciónObtenerRespuesta = (instrucción, conversación, opciones, nombreModelo, lote) => {

                    var respondedorInicial = ClienteGPT.GetOpenAIResponseClient(nombreModelo);
                    OpenAIResponse respuestaGPT;
                    if (!string.IsNullOrEmpty(instrucción)) {
                        respuestaGPT = (OpenAIResponse)respondedorInicial.CreateResponse(instrucción, opciones.OpcionesGPT);
                    } else if (conversación != null) {
                        respuestaGPT = (OpenAIResponse)respondedorInicial.CreateResponse(conversación.ConversaciónGPT, opciones.OpcionesGPT);
                    } else {
                        throw new InvalidOperationException("Debe haber al menos una instrucción o conversación.");
                    }

                    var tókenes = new Tókenes(nombreModelo, lote, respuestaGPT.Usage.InputTokenCount, respuestaGPT.Usage.OutputTokenCount,
                        respuestaGPT.Usage.OutputTokenDetails?.ReasoningTokenCount, respuestaGPT.Usage.InputTokenDetails?.CachedTokenCount, 0, 0);

                    return (new Respuesta(respuestaGPT), tókenes);

                };

                FunciónObtenerArchivador = () => new Archivador(ClienteGPT.GetOpenAIFileClient());

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


    } // Cliente>
    #pragma warning restore OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.



} // Frugalia>