using OpenAI.Responses;
using System;
using System.Collections.Generic;
using System.Linq;
using static Frugalia.Global;


namespace Frugalia {


    #pragma warning disable OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.
    public class Conversación {


        internal List<ResponseItem> ConversaciónGPT { get; }

        internal List<object> ConversaciónGemini { get; }

        internal List<object> ConversaciónClaude { get; }

        private Familia Familia { get; }

        private Action<string> AcciónAgregarMensajeUsuario { get; }

        public void AgregarMensajeUsuario(string mensaje) => AcciónAgregarMensajeUsuario(mensaje);

        private Func<string> FunciónObtenerTextoPrimeraInstrucción { get; }

        public string ObtenerTextoPrimeraInstrucción() => FunciónObtenerTextoPrimeraInstrucción();

        private Func<string> FunciónObtenerTextoÚltimaInstrucción { get; }

        public string ObtenerTextoÚltimaInstrucción() => FunciónObtenerTextoÚltimaInstrucción();

        private Action<ÍtemRespuesta> AcciónAgregarÍtemRespuesta { get; }

        public void AgregarÍtemRespuesta(ÍtemRespuesta ítemRespuesta) => AcciónAgregarÍtemRespuesta(ítemRespuesta);


        public Conversación(Familia familia) {

            Familia = familia;

            switch (Familia) {
            case Familia.GPT:

                ConversaciónGPT = new List<ResponseItem>();

                AcciónAgregarMensajeUsuario = mensaje => ConversaciónGPT.Add(ResponseItem.CreateUserMessageItem(mensaje));

                AcciónAgregarÍtemRespuesta = ítemRespuesta => ConversaciónGPT.Add(ítemRespuesta.ÍtemRespuestaGPT);

                FunciónObtenerTextoPrimeraInstrucción = () => {

                    var primeraInstrucción = ConversaciónGPT.OfType<MessageResponseItem>().FirstOrDefault();
                    if (primeraInstrucción != null) {

                        var texto = primeraInstrucción.Content?.FirstOrDefault(p => !string.IsNullOrEmpty(p.Text));
                        return texto?.Text ?? "";

                    } else {
                        return "";
                    }

                };

                FunciónObtenerTextoÚltimaInstrucción = () => {

                    var últimaInstrucción = ConversaciónGPT.OfType<MessageResponseItem>().LastOrDefault();
                    if (últimaInstrucción != null) {

                        var texto = últimaInstrucción.Content?.LastOrDefault(p => !string.IsNullOrEmpty(p.Text));
                        return texto?.Text ?? "";

                    } else {
                        return "";
                    }

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
                throw new Exception($"No implementada conversación para el modelo {Familia}");
            }

        } // Conversación>


        public Conversación(List<ResponseItem> conversaciónGPT) : this(Familia.GPT) => ConversaciónGPT = conversaciónGPT;


    } // Conversación>
    #pragma warning restore OPENAI001 // Este tipo se incluye solo con fines de evaluación y está sujeto a cambios o a que se elimine en próximas actualizaciones. Suprima este diagnóstico para continuar.


} // Frugalia>