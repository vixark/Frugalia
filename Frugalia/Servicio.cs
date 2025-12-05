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

using System;
using System.Collections.Generic;
using System.Text.Json;
using static Frugalia.Global;


namespace Frugalia {


    public class Servicio {


        private readonly Cliente Cliente;

        protected string NombreModelo { get; }

        public Familia Familia { get; }

        public Razonamiento Razonamiento { get; }

        public RestricciónRazonamiento RestricciónRazonamientoAlto { get; }

        public RestricciónRazonamiento RestricciónRazonamientoMedio { get; }

        public Verbosidad Verbosidad { get; }

        public CalidadAdaptable ModoCalidadAdaptable { get; }

        public TratamientoNegritas TratamientoNegritas { get; }

        protected string ClaveAPI { get; }

        protected bool Lote { get; }

        internal bool Iniciado { get; }


        public Servicio(string nombreModelo, bool lote, Razonamiento razonamiento, Verbosidad verbosidad, CalidadAdaptable modoCalidadAdaptable,
            RestricciónRazonamiento restricciónRazonamientoAlto, TratamientoNegritas tratamientoNegritas, string claveAPI, out string error,
            RestricciónRazonamiento restricciónRazonamientoMedio = RestricciónRazonamiento.Ninguna) { // A propósito solo se provee un constructor con muchos parámetros para forzar al usuario de la librería a manualmente omitir ciertas optimizaciones. El objetivo de la librería es generar ahorros, entonces por diseño se prefiere que el usuario omita estos ahorros manualmente.

            error = null;
            var modelo = Modelo.ObtenerModeloNulable(nombreModelo);
            if (modelo == null) {
                error = $"El modelo '{nombreModelo}' no es válido.";
                Iniciado = false;
                return;
            }

            if (string.IsNullOrWhiteSpace(claveAPI)) {
                error = "La clave de la API no puede ser nula ni vacía.";
                Iniciado = false;
                return;
            }

            ClaveAPI = claveAPI;
            NombreModelo = nombreModelo;
            Familia = ((Modelo)modelo).Familia;
            Razonamiento = razonamiento;
            Verbosidad = verbosidad;
            ModoCalidadAdaptable = modoCalidadAdaptable;
            TratamientoNegritas = tratamientoNegritas;
            Lote = lote;
            RestricciónRazonamientoAlto = restricciónRazonamientoAlto;
            RestricciónRazonamientoMedio = restricciónRazonamientoMedio;
            Cliente = new Cliente(Familia, ClaveAPI);
            Iniciado = true;

        } // ServicioIA>


        private static string ObtenerTextoPrimeraInstrucción(Conversación conversación) => conversación?.ObtenerTextoPrimeraInstrucción() ?? "";


        private static string ObtenerTextoÚltimaInstrucción(Conversación conversación) => conversación?.ObtenerTextoÚltimaInstrucción() ?? "";


        /// <summary>
        // Calcula automáticamente la nueva longitud objetivo de la instrucción de sistema para aprovechar el ahorro en el costo de lecturas tókenes de caché
        // en OpenAI y similares. La idea es que si la instrucción de sistema se reutiliza muchas veces, conviene que su longitud compartida supere el umbral
        // aproximado de cacheo (1024 tókenes para OpenAI). Esta función devuelve una nueva instrucción de sistema más larga que activaría la caché en llamadas
        // posteriores y de esta manera obtener un costo reducido de tókenes de entrada de caché. Ver fórmula en comentario interno en la función.
        /// </summary>
        /// <param name="conversacionesEnPocasHoras">
        /// Cantidad de conversaciones completas o solicitudes aisladas al modelo. Se debe considerar las llamadas dentro de una conversación como una unidad 
        /// completa porque en los casos de conversaciones, se usa el valor de instruccionesPorConversación para decidir si rellena o no la instrucción del
        /// sistema.
        /// </param>
        /// <param name="instrucciónSistema"></param>
        /// <param name="instruccionesEnConversación">
        /// Cantidad de instrucciones del usuario en el contexto de una conversación. Por ejemplo, cuando se usa ConsultasConFunciones como un agente de servicio 
        /// al cliente. Si la cantidad de mensajes esperada es muy alta, puede darse que no sea necesario rellenar las instrucciones de sistema porque es posible 
        /// que se active la caché sin necesidad de esto (debido al historial de mensajes que gastan tókenes de entrada), y en cambio si se rellenara en un 
        /// entorno en el que de todas maneras se iba activar la caché, solo traería gasto innecesario de tókenes de entrada de caché aunque baratos, suman.
        /// </param>
        /// <param name="proporciónPrimerInstrucciónVsSiguientes">
        /// Puede suceder que la primera instrucción de una conversación sea de mayor longitud que los demás. Este factor permite tener en cuenta este efecto para 
        /// calcular el largo total de instrucciones del usuario en la conversación.
        /// </param>
        /// <param name="proporciónRespuestasVsInstrucciones">
        /// Las respuestas del modelo también se incluyen en los tókenes de entrada para siguientes consultas en la misma conversación, entonces deben ser tenidos
        /// en cuenta para el cálculo total de los tókenes de entrada de la conversación completa.
        /// </param>
        /// <returns></returns>
        internal string ObtenerRellenoInstrucciónSistema(int conversacionesEnPocasHoras, string instrucciónSistema, ref string rellenoInstrucciónSistema,
            Conversación conversación, int instruccionesPorConversación, double proporciónPrimerInstrucciónVsSiguientes,
            double proporciónRespuestasVsInstrucciones) {

            /* Cálculos de longitud límite para que salga más barato dadas K llamadas estimadas en las próximas horas
            
            K: Número de consultas totales reusando las instrucciones de sistema.
            S: Tókenes iniciales de instrucciones de sistema.
            C: Costo por token.
            fE: El factor de éxito de uso de la caché. Si es 0.8, significa que de las K - 1 llamadas que deberían usar la caché, solo (K - 1) * 0,8 
                de verdad resultan usando la caché.
            fD: El factor de descuento al usar la caché. Por ejemplo, es 0.1 para gpt-5.1.

            Valor Original = C * S * K
            Valor Ajustando Instrucciones Sistema = 1 * C * 1025 + (K - 1) * (fE * fD + (1 - fE) * 1) * C * 1025
                = 1025C + 1025C(K - 1)(1 - (1 - fD)fE)

            Limite se alcanza cuando:
            Valor Aj. = Valor Orig.
            1025 + 1025 * (K - 1) * (1 - (1 - fD) * fE) = SK
            S = (1025/K) * (1 + (K - 1) * (1 - (1 - fD) * fE))

            (con fE = 1) se obtiene:
            K = 2 -> S = 563,5
            K = 3 -> S = 409,8
            K = 5 -> S = 286,9
            K = 10 -> S = 194,75 
            K = 35 -> S = 128.86
            K = 50 -> S = 120,94
            K = inf -> S = 102,5

            Caso límite: En un uso intensivo y recursivo de las instrucciones de sistema en entornos de producción donde se espera que fácilmente se superen 
            50 llamadas en menos de un día, vale la pena aumentar la instrucción de sistema si no es posible resumirla por debajo de 120 tókenes que es 
            aproximadamente 600 carácteres en español (pecando por exceso 5 caracteres por token) y elevarlo 1025 tókenes que serían pecando por exceso
            5120 carácteres para asegurarse que active la caché.
             
            */

            if (instrucciónSistema == null) instrucciónSistema = "";
            if (rellenoInstrucciónSistema == null) rellenoInstrucciónSistema = "";
            if (instruccionesPorConversación <= 0)
                throw new ArgumentOutOfRangeException(nameof(instruccionesPorConversación), "instruccionesPorConversación debe ser mayor a 0.");
            if (proporciónPrimerInstrucciónVsSiguientes <= 0)
                throw new ArgumentOutOfRangeException(nameof(proporciónPrimerInstrucciónVsSiguientes), "proporciónPrimerInstrucciónVsSiguientes debe ser mayor a 0.");
            if (proporciónRespuestasVsInstrucciones < 0)
                throw new ArgumentOutOfRangeException(nameof(proporciónRespuestasVsInstrucciones), "proporciónRespuestasVsInstrucciones no puede ser negativo.");

            var modelo = Modelo.ObtenerModelo(NombreModelo);
            if (modelo.LímiteTókenesActivaciónCachéAutomática == null) return "";
            if (modelo.LímiteTókenesActivaciónCachéAutomática <= 0) throw new Exception("El límite de tókenes no puede ser 0 o negativo.");
            if (!string.IsNullOrWhiteSpace(rellenoInstrucciónSistema)) { // Si ya se pasa el relleno de la instrucción de sistema de una íteración anterior, se usa esta sin realizar ningún cálculo.
                return rellenoInstrucciónSistema;
            } else {
                rellenoInstrucciónSistema = "";
            }

            if (string.IsNullOrWhiteSpace(instrucciónSistema)) return ""; // Si no hay instrucción de sistema nunca va a compensar agregar 'algo' porque no hay nada que optimizar.
            if (conversacionesEnPocasHoras <= 1) return ""; // Para 1 ejecución nunca justifica incrementar la instrucción de sistema.
            if (instruccionesPorConversación <= 0) return ""; // No se permite que no existan instrucciones por conversación.
            var factorDescuentoCaché = Modelo.ObtenerFactorDescuentoCaché(NombreModelo);
            if (factorDescuentoCaché > 0.9) return ""; // Se le pone este control para el caso de modelos que no tienen descuento para tókenes de entrada en caché.

            var factorÉxitoCaché = 0.8; // Si OpenAI funcionara bien no debería pasar que no se active la caché en el segundo mensaje de una conversación que tiene instrucciones de sistema rellenadas desde el primer mensaje, pero sí pasa. En algunos casos OpenAI simplemente ignora la caché por razones desconocidas. Se hizo un experimento inflando más las instrucciones de sistema y queda demostrado que no es cuestión del tamaño de tokenes de la función ni que se cambie ni nada, simplemente a veces no lo coje, con 1294 tókenes hay más que suficiente para garantizar que toda la instruccion sistema es superior a 1024 . 1294 - 10 (o lo que sea de la instrucción de usuario) - 73 de la función  = 1211 1: ENC = 1294 EC = 0 SNR = 103 SR = 0 2: ENC = 1402 EC = 0 SNR = 222 SR = 0 OpenAI describe el prompt caching como una optimización de “best effort”, no determinista como un contrato fuerte tipo: “si el prefijo coincide, 100 % te voy a servir desde caché”. Así que básicamente dicen, si no funciona, no me culpen. Lo mejor entonces es asumir un % de éxito que se incorporá en la fórmula para solo engordar las instrucciones de sistema que considerando ese porcentaje de éxito de uso de la caché logren ahorros. 0.8 es un valor que se tira al aire basado en un pequeño experimento limitado: se ejecutó 10 veces una conversación de 6-7 mensajes y se obtuvo una tasa de fallo de 13%, es decir un factor de éxito de 0.87, sin embargo debido a que hay incertidumbre con este número y a que hay una ligera demesmejoría en el comportamiento del agente cuando se rellenan las instrucciones del sistema, se prefiere dejar en 0.8. Se usa el mismo valor para las otras familias de modelos porque no se conoce aún su funcionamiento.
            var charPorTokenTípicosConversación = 3;
            var charPorTokenTípicosInstrucciónSistema = 4; // La necesidad o no de rellenar la instrucción de sistema se decide usando valor promedio de 4 carácteres por tókenes y la rellenada aw hace con un exceso de tokenes (carácteresPorTokenMáximos) para asegurar que se generen suficientes carácteres para que con seguridad supere el límite para activar la caché (tókenesObjetivo). El valor de 4 carácteres por token se obtuvo de controlar eliminando los tókenes que consumía la función, así 340 tókenes (-73 función) = 267 tókenes para 1061 carácteres = 3.97 char/tk (para el primer mensaje de mensaje de usuario + instrucción de sistema sin rellenar). Para textos más normales que no sean instrucciones de sistema (que suelen tener frases cortas densas, referencias, datos, etc) suelen ser 3 carácteres por token. Pero como aquí se está intentando ajustar es instrucciones de sistema se trabaja con 4.
            var charPorTokenRelleno = 5.5; // Se usa 5.5 como caso límite. Se asegura agregar suficientes carácteres para que supere los tókenes requeridos. Se hizo un experimento y se encontró esto: Sin relleno 340 tk y 1061 char: 3.12 char/tk, con relleno 955 tk y 4183 char: 4.38 char/ tk, solo el relleno: 615tk y 3122 char: 5.07 char/ tk. Este mismo experimento se repitió para el caso de usar solo el texto relleno sin lorems (solo para fines de calcular su cantidad de carácteres por token) y se encontró que es 4.75 char/tk. También se hizo el experimento únicamente con lorems (sin texto introductorio) y dio otra vez 5.07 char/tk, así que esto es algo inconsistente matemáticamente porque podrían haber cosas desconocidas de cómo el modelo calcula los tókenes, entonces para pecar por seguro, se usa 5.5 carácteres por token para el texto de relleno. Esto asegura que el relleno garantiza con cierto margen de seguridad que se active la caché. Se debe poner un valor superior porque hay incertidumbre de que tal vez el modelo cambié la forma de cálculo de tókenes y de pronto llegue a ser 5.3 o 5.2, y si así fuera y se hubiera puesto un valor muy ajustado como 5.1, no se activaría la caché y se gastaría innecesariamente en tókenes inutiles no en caché.

            var consultasEnPocasHoras = conversacionesEnPocasHoras * instruccionesPorConversación;
            var tókenesObjetivo = (int)modelo.LímiteTókenesActivaciónCachéAutomática + 1; // A partir de 1024 se activa la caché de tókenes de entrada https://platform.openai.com/docs/guides/prompt-caching para GPT.
            var evaluarRellenarInstruccionesSistema = true;

            if (conversación != null) { // En el uso típico de la conversación (como se hace en la función ObtenerConFunción()), lo usual es incluir las respuestas anteriores del modelo. Entonces se debe verificar en que casos la caché se activa sola y es económicamente más óptima que rellenar las instrucciones de sistema desde el principio. No se rellena en la mitad de la conversación porque eso implica un recálculo de la caché y la pérdida de todo el bloque de información repetida que puede ser guardable en caché, la decisión es o rellenarlo al principio o no hacerlo. En estos casos no se rellena las instrucciones del sistema.

                /* Fórmula de cantidad de tókenes de entrada en el mensaje N. También se deriva una fórmula para el total de mensajes de entrada gastados hasta el mensaje N, pero esta fórmula aunque elegante matemáticamente no es útil porque no sirve para calcular el costo incluyendo el efecto de la caché.
                        
                N: cantidad de mensajes 

                S: 100 tokenes sistema
                M1: 50 tokenes mensaje 1
                R1: 100 tokenes respuesta 1
                T1: S + M1 (tókenes de entrada de mensaje 1).
                ***
                E2: S + M1 + R1
                M2: 25 tokenes mensaje conversación 2
                R2: 100 tokenes respuesta 2
                T2: E2 + M2 = S + M1 + M2 + R1
                ***
                E3: S + M1 + M2 + R1 + R2
                M3: 25 tokenes mensaje conversación 3
                R3: 100 tokenes respuesta 3 = R
                T3: E3 + M3 = S + M1 + M2 + R1 + R2 + M3
   
                Tókenes Entrada de Mensaje N: TN = S + SumaMHastaN + SumaRHastaN-1 = S + M1 + (N - 1) * M + (N - 1) * R = S + M1 + (N - 1) * (M + R) 
  
                Total Tókenes hasta N = (S + SumaMHasta1) + (S + SumaMHasta2 + SumaRHasta1) + (S + SumaMHasta3 + SumaRHasta2) + .... (S + SumaMHastaN + SumaRHastaN-1)
 
                Considerando que R1=R2=R3=...=R y que M2=M3=M4=...=M, es decir que las respuestas del modelo son del mismo largo y que los mensajes del usuario son del mismo largo después del primero.
 
                Total Tókenes = (S + M1) + (S + M1 + M + R) + (S + M1 + 2M + 2R) + (S + M1 + 3M + 3R) ... + (S + M1 + (N-1) * M + (N - 1) * R)
 
                Total Tókenes = N * (S + M1) + (1 + 2 + 3 .... + (N - 1)) * (M + R)
                 
                */

                var m1 = ObtenerTextoPrimeraInstrucción(conversación);
                if (!string.IsNullOrWhiteSpace(m1)) {

                    var largoM1 = m1.Length;
                    var largoM = m1.Length / proporciónPrimerInstrucciónVsSiguientes;
                    var largoR = largoM * proporciónRespuestasVsInstrucciones;
                    var tókenesS = (int)Math.Round(instrucciónSistema.Length / (double)charPorTokenTípicosInstrucciónSistema);
                    double obtenerTókenes(int n, int tókenesObjetivoSRellena) { // Cantidad de tókenes de entrada gastados en el mensaje n y guardables en caché en el mensaje n - 1.

                        if (n < 1) return 0;
                        var largoSRellena = tókenesS < tókenesObjetivoSRellena ? tókenesObjetivoSRellena : tókenesS;
                        return largoSRellena + largoM1 / (double)charPorTokenTípicosConversación + (n - 1) * (largoM + largoR) / charPorTokenTípicosConversación;

                    } // obtenerTókenes>

                    double obtenerCostoTókenes(double tókenes, double tókenesAnteriores) {

                        var tókenesNuevos = tókenes - tókenesAnteriores;
                        var costoTókenes = 0.0;
                        if (tókenesAnteriores >= tókenesObjetivo) { // Entonces se usa caché.             

                            var tókenesPosiblementeEnCaché = 128 * (int)Math.Floor(tókenesAnteriores / 128.0); // En la caché se guardan tókenes en múltiplos de 128.
                            costoTókenes += tókenesPosiblementeEnCaché * ((1 - factorÉxitoCaché) * 1 + factorÉxitoCaché * factorDescuentoCaché);
                            costoTókenes += tókenesAnteriores - tókenesPosiblementeEnCaché;

                        } else {
                            costoTókenes += tókenesAnteriores;
                        }
                        costoTókenes += tókenesNuevos;
                        return costoTókenes;

                    } // obtenerCostoTókenes>

                    var cantidadRellenosProbar = Math.Max(0, (int)Math.Ceiling((tókenesObjetivo - tókenesS) / 128.0));
                    var menorTotalCostoTókenesConRelleno = double.MaxValue;
                    var rellenoDelMenorCosto = 0;

                    for (int r = 0; r <= cantidadRellenosProbar; r++) {

                        var tókenesObjetivoInstrucciónSistema = r * 128 + tókenesS;
                        var totalCostoTókenesConRelleno = 0.0;

                        for (int n = 1; n <= instruccionesPorConversación; n++) {

                            var tókenesConRelleno = obtenerTókenes(n, tókenesObjetivoInstrucciónSistema);
                            var tókenesAnterioresConRelleno = obtenerTókenes(n - 1, tókenesObjetivoInstrucciónSistema);
                            totalCostoTókenesConRelleno += obtenerCostoTókenes(tókenesConRelleno, tókenesAnterioresConRelleno);

                        }

                        if (totalCostoTókenesConRelleno < menorTotalCostoTókenesConRelleno) {
                            menorTotalCostoTókenesConRelleno = totalCostoTókenesConRelleno;
                            rellenoDelMenorCosto = r * 128;
                        }

                    }

                    if (rellenoDelMenorCosto == 0) {
                        evaluarRellenarInstruccionesSistema = false;
                    } else {
                        tókenesObjetivo = rellenoDelMenorCosto + tókenesS;
                    }

                } // primeraInstrucción != null>

            } // conversación != null>

            var tókenesMínimoParaRellenar
                = (tókenesObjetivo / (double)consultasEnPocasHoras) * (1 + (consultasEnPocasHoras - 1) * (1 - (1 - factorDescuentoCaché) * factorÉxitoCaché));
            var largoMínimoParaRellenar = tókenesMínimoParaRellenar * charPorTokenTípicosInstrucciónSistema;
            var tókenesActuales = instrucciónSistema.Length / charPorTokenTípicosInstrucciónSistema;
            var carácteresObjetivo = (int)Math.Ceiling(instrucciónSistema.Length + (tókenesObjetivo - tókenesActuales) * charPorTokenRelleno);

            if (evaluarRellenarInstruccionesSistema && instrucciónSistema.Length > largoMínimoParaRellenar && instrucciónSistema.Length < carácteresObjetivo) {

                var inicioRelleno = $"{Fin} A continuación se incluye un texto genérico de ejemplo que no forma parte " + // No quitar la parte {fin} porque es útil para insertar otras instrucciones antes del texto relleno.
                    "de la lógica de negocio. No debes usar su contenido para responder al usuario. ";

                var lorem =
                    "Texto genérico: Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                    "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
                    "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
                    "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. ";

                var carácteresFaltantes = carácteresObjetivo - instrucciónSistema.Length;
                if (inicioRelleno.Length > carácteresFaltantes) {

                    Suspender(); // Verificar cuando suceda.
                    rellenoInstrucciónSistema = inicioRelleno; // En estos casos se puede pasar un poco, pero no es grave. Se prefiere que este mensaje esté siempre completo para no confundir al modelo.

                } else {

                    var carácteresFaltantesConLorem = carácteresObjetivo - instrucciónSistema.Length - inicioRelleno.Length;
                    var vecesEnterasLorem = (int)Math.Floor(carácteresFaltantesConLorem / ((double)lorem.Length));
                    var lorems = "";
                    for (int i = 0; i < vecesEnterasLorem; i++) {
                        lorems += lorem;
                    }
                    lorems += lorem.Substring(0, carácteresFaltantesConLorem - vecesEnterasLorem * lorem.Length);
                    rellenoInstrucciónSistema = inicioRelleno + lorems;

                }

            } // else: rellenoInstrucciónSistema permanece en "".

            return rellenoInstrucciónSistema;

        } // ObtenerRellenoInstrucciónSistema>


        internal Opciones ObtenerOpciones(string instrucciónSistema, bool buscarEnInternet, int largoInstrucciónÚtil, List<Función> funciones) {

            int máximosTókenesSalida; // Un control interno de seguridad para que el modelo no se vaya a enloquecer en algún momento y devuelva miles de tókenes de salida, generando altos costos.

            if (Verbosidad == Verbosidad.Baja) {
                máximosTókenesSalida = 200;
            } else if (Verbosidad == Verbosidad.Media) {
                máximosTókenesSalida = 350;
            } else if (Verbosidad == Verbosidad.Alta) {
                máximosTókenesSalida = 500;
            } else {
                throw new Exception($"Valor de verbosidad no considerado: {Verbosidad}.");
            }

            return new Opciones(Familia, instrucciónSistema, NombreModelo, Razonamiento, RestricciónRazonamientoAlto, RestricciónRazonamientoMedio,
                    largoInstrucciónÚtil, máximosTókenesSalida, Verbosidad, buscarEnInternet, funciones);

        } // ObtenerOpciones>


        internal Respuesta ObtenerRespuesta(string instrucción, Conversación conversación, Opciones opciones, string nombreModelo,
            ref Dictionary<string, Tókenes> tókenes) {

            if (!string.IsNullOrEmpty(instrucción) && conversación != null) throw new Exception("Debe haber instrucción o conversación, pero no ambas.");

            var (respuesta, tókenesUsadosEnConsulta) = Cliente.ObtenerRespuesta(instrucción, conversación, opciones, nombreModelo, Lote);
            tókenes = tókenes.AgregarSumando(tókenesUsadosEnConsulta); // Se asigna a si mismo para que funcione cuando viene nulo.

            return respuesta;

        } // ObtenerRespuesta>


        /// <summary>
        /// 
        /// </summary>
        /// <param name="instrucción"></param>
        /// <param name="conversación"></param>
        /// <param name="instrucciónSistema"></param>
        /// <param name="rellenoInstrucciónSistema"></param>
        /// <param name="buscarEnInternet"></param>
        /// <param name="funciones"></param>
        /// <param name="respuestaTextoLimpio">Usar siempre este texto para responderle al usuario porque este texto elimina algunas marcas de texto auxiliares 
        /// que se le dicen al modelo que ponga en su respuesta y que pueden quedar en el GetOutputText(). Por ejemplo, [lo-hice-bien].
        /// al modelo</param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        internal Respuesta Responder(string instrucción, Conversación conversación, string instrucciónSistema, string rellenoInstrucciónSistema,
            bool buscarEnInternet, List<Función> funciones, out string respuestaTextoLimpio, ref Dictionary<string, Tókenes> tókenes) {

            if (!string.IsNullOrEmpty(instrucción) && conversación != null)
                throw new Exception("No se permite pasar a la funcion Responder() instrucciones individuales no nulas y a la vez pasar conversación no nula.");
            if (string.IsNullOrEmpty(instrucción) && conversación == null)
                throw new Exception("No se permite pasar a la funcion Responder() instrucciones individuales nulas y a la vez pasar conversación nula.");

            if (tókenes == null) tókenes = new Dictionary<string, Tókenes>();
            var últimaInstruccion = instrucción;
            if (string.IsNullOrEmpty(instrucción)) últimaInstruccion = ObtenerTextoÚltimaInstrucción(conversación);
            var largoInstrucciónÚtil = ObtenerLargoInstrucciónÚtil(últimaInstruccion, instrucciónSistema, rellenoInstrucciónSistema);
            Respuesta respuesta;
            var modoCalidadAdaptable
                = ModoCalidadAdaptable == CalidadAdaptable.MejorarModelo || ModoCalidadAdaptable == CalidadAdaptable.MejorarModeloYRazonamiento;

            var opciones = ObtenerOpciones(instrucciónSistema, buscarEnInternet, largoInstrucciónÚtil, funciones);
            if (modoCalidadAdaptable) {

                var instruccionesOriginales = opciones.ObtenerInstrucciónSistema();
                opciones.EscribirInstrucciónSistema(instruccionesOriginales.Replace(Fin, ".\n\nPrimero responde normalmente al usuario.\n\nDespués evalúa tu " +
                    "propia respuesta y en la última línea escribe exactamente una de estas etiquetas, sola, sin dar explicaciones de tu elección:\n\n" +
                    $"{LoHiceBien}\n{MedioRecomendado}\n{GrandeRecomendado}\n\nUsa:\n{LoHiceBien}: Si tu respuesta fue buena, tiene " +
                    $"sentido y es completa. Entendiste bien la consulta y es relativamente sencilla.\n{MedioRecomendado}: Si tu respuesta no fue " +
                    "buena en calidad, sentido o completitud, o si a la consulta le faltan detalles, contexto o no la entiendes bien.\n" +
                    $"{GrandeRecomendado}: Si la consulta es muy compleja, requiere conocimiento experto o trata temas delicados{Fin}"));

                var respuestaInicial = ObtenerRespuesta(instrucción, conversación, opciones, NombreModelo, ref tókenes);
                var textoRespuesta = respuestaInicial.ObtenerTextoRespuesta(TratamientoNegritas);

                int nivelesMejoramiento;
                if (textoRespuesta.Contains(LoHiceBien)) {
                    nivelesMejoramiento = 0;
                } else if (textoRespuesta.Contains(MedioRecomendado)) {
                    nivelesMejoramiento = 1;
                } else if (textoRespuesta.Contains(GrandeRecomendado)) {
                    nivelesMejoramiento = 2;
                } else {
                    Suspender(); // Verificar cuando pase. El modelo no contestó con la etiqueta correcta.
                    throw new Exception("La respuesta del modelo no incluyó la etiqueta de autoevaluación esperada.");
                }

                if (nivelesMejoramiento == 0) {
                    respuesta = respuestaInicial;
                } else {

                    var nombreModeloMejorado = Modelo.ObtenerModeloMejorado(NombreModelo, nivelesMejoramiento);
                    if (string.IsNullOrEmpty(nombreModeloMejorado)) {
                        respuesta = respuestaInicial; // No hay modelos disponibles por encima del usado inicialmente.
                    } else {

                        opciones.EscribirInstrucciónSistema(instruccionesOriginales); // Con el nuevo modelo usa las instrucciones originales, sin la instrucción de autoevaluacion porque solo se autoevaluará una vez.
                        var razonamientoAUsar = Razonamiento;
                        if (ModoCalidadAdaptable == CalidadAdaptable.MejorarModeloYRazonamiento)
                            razonamientoAUsar = ObtenerRazonamientoMejorado(razonamientoAUsar, nivelesMejoramiento);
                        opciones.EscribirOpcionesRazonamiento(razonamientoAUsar, RestricciónRazonamientoAlto, RestricciónRazonamientoMedio, nombreModeloMejorado,
                            largoInstrucciónÚtil);
                        respuesta = ObtenerRespuesta(instrucción, conversación, opciones, nombreModeloMejorado, ref tókenes);

                    }

                }

            } else {
                respuesta = ObtenerRespuesta(instrucción, conversación, opciones, NombreModelo, ref tókenes);
            }

            respuestaTextoLimpio = respuesta.ObtenerTextoRespuesta(TratamientoNegritas);
            respuestaTextoLimpio = modoCalidadAdaptable 
                ? respuestaTextoLimpio.Replace(LoHiceBien, "").Replace(MedioRecomendado, "").Replace(GrandeRecomendado, "") : respuestaTextoLimpio; // También se limpia el texto MedioRecomendado y el GrandeRecomendado porque podría darse el caso en el que el modelo sugiera un modelo superior, pero no hayan modelos superiores disponbiles.
            return respuesta;

        } // Responder>


        /// <summary>
        /// Consulta simple, buscando o no en internet.
        /// </summary>
        /// <param name="instrucciónSistema">Rol, tono, formato respuesta, reglas generales, límites, comportamiento del agente, etc.</param>
        /// <param name="instrucción">Instrucción del usuario específica.</param>
        /// <param name="error"></param>
        /// <returns></returns>
        public string Consulta(int consultasEnPocasHoras, string instrucciónSistema, ref string rellenoInstrucciónSistema, string instrucción,
            out string error, out Dictionary<string, Tókenes> tókenes, bool buscarEnInternet = false) {

            tókenes = new Dictionary<string, Tókenes>();
            if (!Iniciado) {
                error = "No se ha iniciado correctamente el servicio.";
                return null;
            }

            if (consultasEnPocasHoras <= 0) {
                error = "consultasEnPocasHoras debe ser mayor a 0.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(instrucción)) {
                error = "La instrucción del usuario no puede ser vacía.";
                return null;
            }

            if (instrucciónSistema == null) instrucciónSistema = "";

            try {

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(consultasEnPocasHoras, instrucciónSistema, ref rellenoInstrucciónSistema, null, 1, 1, 1);

                var razonamientoEfectivo = ObtenerRazonamientoEfectivo(Razonamiento, RestricciónRazonamientoAlto, RestricciónRazonamientoMedio, NombreModelo,
                    ObtenerLargoInstrucciónÚtil(instrucción, instrucciónSistema, rellenoInstrucciónSistema));
                if (buscarEnInternet && (razonamientoEfectivo == Razonamiento.Ninguno)) { // Buscar en internet no se permite hacer con Razonamiento = Ninguno.
                    error = "No se puede ejecutar una búsqueda en internet con Razonamiento = Ninguno.";
                    return null;
                }

                Responder(instrucción, conversación: null, instrucciónSistema, rellenoInstrucciónSistema, buscarEnInternet, funciones: null,
                    out string respuestaTextoLimpio, ref tókenes);
                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                return null;

            }

        } // Consulta>


        /// <summary>
        /// Con archivos.
        /// </summary>
        /// <param name="consultasEnPocasHoras"></param>
        /// <param name="instrucciónSistema"></param>
        /// <param name="rellenoInstrucciónSistema"></param>
        /// <param name="instrucción"></param>
        /// <param name="rutasArchivos"></param>
        /// <param name="error"></param>
        /// <param name="tókenes"></param>
        /// <param name="tipoArchivo"></param>
        /// <returns></returns>
        public string Consulta(int consultasEnPocasHoras, string instrucciónSistema, ref string rellenoInstrucciónSistema, string instrucción,
            List<string> rutasArchivos, out string error, out Dictionary<string, Tókenes> tókenes, TipoArchivo tipoArchivo) {

            tókenes = new Dictionary<string, Tókenes>();
            if (!Iniciado) {
                error = "No se ha iniciado correctamente el servicio.";
                return null;
            }

            if (consultasEnPocasHoras <= 0) {
                error = "consultasEnPocasHoras debe ser mayor a 0.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(instrucción)) {
                error = "La instrucción del usuario no puede ser vacía.";
                return null;
            }

            if (instrucciónSistema == null) instrucciónSistema = "";

            Archivador archivador = null;
            try {

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(consultasEnPocasHoras, instrucciónSistema, ref rellenoInstrucciónSistema, null, 1, 1, 1);

                if (rutasArchivos == null || rutasArchivos.Count == 0) { error = "La lista rutasArchivos está vacía."; return null; }

                archivador = Cliente.ObtenerArchivador();
                var conversaciónConArchivosYError = archivador.ObtenerConversaciónConArchivos(rutasArchivos, instrucción, tipoArchivo);
                if (!string.IsNullOrEmpty(conversaciónConArchivosYError.Error)) {
                    error = conversaciónConArchivosYError.Error;
                    return null;
                }

                var respuesta = Responder(instrucción: null, conversaciónConArchivosYError.Conversación, instrucciónSistema, rellenoInstrucciónSistema,
                    buscarEnInternet: false, funciones: null, out string respuestaTextoLimpio, ref tókenes);
                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                return null;

            } finally {
                archivador?.EliminarArchivos();
            }

        } // Consulta>


        /// <summary>
        /// Con funciones.
        /// </summary>
        /// <param name="conversacionesEnPocasHoras"></param>
        /// <param name="instrucciónSistema"></param>
        /// <param name="rellenoInstrucciónSistema"></param>
        /// <param name="conversación"></param>
        /// <param name="funciones"></param>
        /// <param name="error"></param>
        /// <param name="tókenes"></param>
        /// <param name="funciónEjecutada"></param>
        /// <returns></returns>
        public string Consulta(int conversacionesEnPocasHoras, string instrucciónSistema, ref string rellenoInstrucciónSistema, Conversación conversación,
            List<Función> funciones, out string error, out Dictionary<string, Tókenes> tókenes, int instruccionesPorConversación, 
            double proporciónPrimerInstrucciónVsSiguientes, double proporciónRespuestasVsInstrucciones, out bool funciónEjecutada) {

            tókenes = new Dictionary<string, Tókenes>();
            funciónEjecutada = false;
            if (!Iniciado) {
                error = "No se ha iniciado correctamente el servicio.";
                return null;
            }

            if (conversacionesEnPocasHoras <= 0) {
                error = "conversacionesEnPocasHoras debe ser mayor a 0.";
                return null;
            }

            if (instruccionesPorConversación <= 0) {
                error = "instruccionesPorConversación debe ser mayor a 0.";
                return null;
            }

            if (proporciónPrimerInstrucciónVsSiguientes <= 0) {
                error = "proporciónPrimerInstrucciónVsSiguientes debe ser mayor a 0.";
                return null;
            }

            if (proporciónRespuestasVsInstrucciones < 0) {
                error = "proporciónRespuestasVsInstrucciones no puede ser negativo.";
                return null;
            }

            if (conversación == null) {
                error = "No se permite pasar objeto conversación en nulo. ConsultaConFunciones() debe reusar la conversación para su correcto funcionamiento.";
                return null;
            }

            if (instrucciónSistema == null) instrucciónSistema = "";

            var máximasConsultas = 5; // Límite de iteraciones entre llamadas a la función y a la IA para evitar que se quede en un ciclo infinito.
            var consultas = 0;

            try {

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(conversacionesEnPocasHoras, instrucciónSistema, ref rellenoInstrucciónSistema,
                    conversación, instruccionesPorConversación, proporciónPrimerInstrucciónVsSiguientes, proporciónRespuestasVsInstrucciones);
            
                Respuesta respuesta;
                string respuestaTextoLimpio;
                var funciónEjecutadaÚltimaConsulta = false;

                do { // Se sigue llamando a la API mientras esta siga solicitando ejecutar funciones y termina cuando el modelo ya no pida más; lo habitual es que el modelo se ejecute dos veces: primero identifica que necesita una función y luego usa el resultado para responder al usuario. Podrían existir casos en los que intente varias funciones en cola, así que se permite repetirse las veces necesarias dentro de un límite razonable.

                    consultas++;
                    if (consultas > máximasConsultas) {
                        error = $"Se llegó al máximo de {máximasConsultas} consultas en la función ConsultaConFunciones() y no se pudo llegar a una solución.";
                        return null;
                    }
                    respuesta = Responder(instrucción: null, conversación, instrucciónSistema, rellenoInstrucciónSistema, buscarEnInternet: false,
                        funciones, out respuestaTextoLimpio, ref tókenes);
                    funciónEjecutadaÚltimaConsulta = false;

                    foreach (var ítemRespuesta in respuesta.ObtenerÍtemsRespuesta()) {

                        conversación.AgregarÍtemRespuesta(ítemRespuesta);

                        if (ítemRespuesta.EsSolicitudFunción()) {

                            JsonDocument json;
                            try { // El modelo podría contestar incorrectamente con un json mal formado.
                                json = ítemRespuesta.ObtenerJsonArgumentosFunción();
                            } catch (JsonException) {

                                var errorJson = JsonSerializer.Serialize(new {
                                    error = "Error en JSON: no se construyó correctamente el archivo y falló su lectura. Revísalo e inténtalo nuevamente.",
                                });
                                conversación.AgregarÍtemRespuesta(ítemRespuesta.CrearÍtemRespuestaFunción(errorJson));
                                funciónEjecutadaÚltimaConsulta = true;
                                continue;

                            }

                            using (json) {

                                var resultado = Función.ObtenerResultado(funciones, ítemRespuesta.ObtenerNombreFunción(),
                                    out (string ParámetroConError, string Descripción) errorFunción, ExtraerParámetros(json));
                                var hayErrorFunción = !string.IsNullOrEmpty(errorFunción.Descripción); // El valor predeterminado de la tupla errorFunción es (null, null), pero el usuario de la librería también podría devolver ("", ""), entonces se evalúa la existencia de error ante ambas opciones: "" y null.

                                if (hayErrorFunción) {

                                    var errorJson = JsonSerializer.Serialize(new { error = errorFunción.Descripción, field = errorFunción.ParámetroConError });
                                    conversación.AgregarÍtemRespuesta(ítemRespuesta.CrearÍtemRespuestaFunción(errorJson));
                                    funciónEjecutadaÚltimaConsulta = true;
                                    continue;

                                } else {

                                    conversación.AgregarÍtemRespuesta(ítemRespuesta.CrearÍtemRespuestaFunción(resultado));
                                    funciónEjecutadaÚltimaConsulta = true;
                                    funciónEjecutada = true;

                                } // Validación valores argumentos correcta >

                            } // using (json) >

                        } // ítemRespuesta is FunctionCallResponseItem >

                    } // foreach ítemRespuesta >

                } while (funciónEjecutadaÚltimaConsulta);

                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                return null;

            }

        } // Consulta>


    } // Servicio>


} // Frugalia>