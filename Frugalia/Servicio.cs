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
using System.IO;
using System.Text;
using System.Text.Json;
using static Frugalia.GlobalFrugalia;


namespace Frugalia {


    public class Servicio {


        private Cliente Cliente { get; }

        private Modelo Modelo { get; }

        public Familia Familia { get; }

        public Razonamiento Razonamiento { get; }

        public RestricciónRazonamiento RestricciónRazonamientoAlto { get; }

        public RestricciónRazonamiento RestricciónRazonamientoMedio { get; }

        public RestricciónTókenesSalida RestricciónTókenesSalida { get; }

        public RestricciónTókenesRazonamiento RestricciónTókenesRazonamiento { get; }

        public Verbosidad Verbosidad { get; }

        public CalidadAdaptable CalidadAdaptable { get; }

        public TratamientoNegritas TratamientoNegritas { get; }

        private string ClaveAPI { get; }

        private bool Lote { get; }

        private bool Iniciado { get; }

        public string Descripción => $"Modelo: {Modelo}. Lote: {Lote}. Razonamiento: {Razonamiento}. Verbosidad: {Verbosidad}. " +
            $"Calidad adaptable: {CalidadAdaptable}. Restricción razonamiento alto: {RestricciónRazonamientoAlto}. " +
            $"Restricción tókenes salida: {RestricciónTókenesSalida}. Restricción tókenes razonamiento: {RestricciónTókenesRazonamiento}";


        public Servicio(string nombreModelo, bool lote, Razonamiento razonamiento, Verbosidad verbosidad, CalidadAdaptable calidadAdaptable, // A propósito se provee un constructor con varios parámetros no opcionales para forzar al usuario de la librería a manualmente omitir ciertas optimizaciones. El objetivo de la librería es generar ahorros, entonces por diseño se prefiere que el usuario omita estos ahorros manualmente.
            TratamientoNegritas tratamientoNegritas, string claveAPI, out string error,
            RestricciónRazonamiento restricciónRazonamientoAlto = RestricciónRazonamiento.ModelosMuyPequeños, // Se ha encontrado con GPT que los modelos muy pequeños con alto razonamiento no funcionan muy bien porque terminan gastando muchos tókenes de razonamiento para cubrir sus limitaciones, reduciendo la ventaja económica de usar este modelo muy pequeño en primer lugar.
            RestricciónRazonamiento restricciónRazonamientoMedio = RestricciónRazonamiento.Ninguna, // No se ha realizados pruebas suficientes para sugerir un valor predeterminado para este parámetro.
            RestricciónTókenesSalida restricciónTókenesSalida = RestricciónTókenesSalida.Alta, // Predeterminada se establecen estas restricciones altas, el usuario de la librería podría relajarlas para evitar respuestas incompletas si en su caso de uso está sucediendo frecuentemente pero teniendo en cuenta que se incrementan los costos.
            RestricciónTókenesRazonamiento restricciónTókenesRazonamiento = RestricciónTókenesRazonamiento.Alta) {

            error = null;
            var modelo = Modelo.ObtenerModelo(nombreModelo);
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
            Modelo = (Modelo)modelo;
            Familia = Modelo.Familia;
            Razonamiento = razonamiento;
            Verbosidad = verbosidad;
            RestricciónTókenesSalida = restricciónTókenesSalida;
            RestricciónTókenesRazonamiento = restricciónTókenesRazonamiento;
            CalidadAdaptable = calidadAdaptable;
            TratamientoNegritas = tratamientoNegritas;
            Lote = lote;
            RestricciónRazonamientoAlto = restricciónRazonamientoAlto;
            RestricciónRazonamientoMedio = restricciónRazonamientoMedio;
            Cliente = new Cliente(Familia, ClaveAPI);
            Iniciado = true;

        } // ServicioIA>


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
        /// <param name="tókenesAdicionales">
        /// Tókenes de archivos y funciones
        /// </param>
        /// <returns></returns>
        internal string ObtenerRellenoInstrucciónSistema(int conversacionesEnPocasHoras, string instrucciónSistema, ref string rellenoInstrucciónSistema,
            Conversación conversación, int instruccionesPorConversación, double proporciónPrimerInstrucciónVsSiguientes, double proporciónRespuestasVsInstrucciones, 
            double tókenesAdicionales) {

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

            if (Modelo.LímiteTókenesActivaciónCachéAutomática == null) return "";
            if (Modelo.LímiteTókenesActivaciónCachéAutomática <= 0) throw new Exception("El límite de tókenes no puede ser 0 o negativo.");
            if (!string.IsNullOrWhiteSpace(rellenoInstrucciónSistema)) { // Si ya se pasa el relleno de la instrucción de sistema de una íteración anterior, se usa esta sin realizar ningún cálculo.
                return rellenoInstrucciónSistema;
            } else {
                rellenoInstrucciónSistema = "";
            }

            if (string.IsNullOrWhiteSpace(instrucciónSistema)) return ""; // Si no hay instrucción de sistema nunca va a compensar agregar 'algo' porque no hay nada que optimizar.
            if (conversacionesEnPocasHoras <= 1) return ""; // Para 1 ejecución nunca justifica incrementar la instrucción de sistema.
            if (instruccionesPorConversación <= 0) return ""; // No se permite que no existan instrucciones por conversación.
            var factorDescuentoCaché = Modelo.ObtenerFactorDescuentoCaché(Modelo);
            if (factorDescuentoCaché > 0.9) return ""; // Se le pone este control para el caso de modelos que no tienen descuento para tókenes de entrada en caché.

            var consultasEnPocasHoras = conversacionesEnPocasHoras * instruccionesPorConversación;
            var tókenesObjetivo = (int)Modelo.LímiteTókenesActivaciónCachéAutomática + 1 - tókenesAdicionales; // A partir de 1024 se activa la caché de tókenes de entrada https://platform.openai.com/docs/guides/prompt-caching para GPT.
            if (tókenesObjetivo <= 1) return ""; // No es necesario realizar relleno porque con los tókenes adicionales ya se alcanzó el límite de tókenes para la activación de la caché.
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

                var m1 = Conversación.ObtenerTextoPrimeraInstrucción(conversación);
                if (!string.IsNullOrWhiteSpace(m1)) {

                    var largoM1 = m1.Length;
                    var largoM = m1.Length / proporciónPrimerInstrucciónVsSiguientes;
                    var largoR = largoM * proporciónRespuestasVsInstrucciones;
                    var tókenesS = (int)Math.Round(instrucciónSistema.Length / (double)CarácteresPorTokenInstrucciónSistemaTípicos);
                    double obtenerTókenes(int n, int tókenesObjetivoSRellena) { // Cantidad de tókenes de entrada gastados en el mensaje n y guardables en caché en el mensaje n - 1.

                        if (n < 1) return 0;
                        var largoSRellena = tókenesS < tókenesObjetivoSRellena ? tókenesObjetivoSRellena : tókenesS;
                        return largoSRellena + largoM1 / (double)CarácteresPorTokenConversaciónTípicos 
                            + (n - 1) * (largoM + largoR) / CarácteresPorTokenConversaciónTípicos;

                    } // obtenerTókenes>

                    double obtenerCostoTókenes(double tókenes, double tókenesAnteriores) {

                        var tókenesNuevos = tókenes - tókenesAnteriores;
                        var costoTókenes = 0.0;
                        if (tókenesAnteriores >= tókenesObjetivo) { // Entonces se usa caché.             

                            var tókenesPosiblementeEnCaché = 128 * (int)Math.Floor(tókenesAnteriores / 128.0); // En la caché se guardan tókenes en múltiplos de 128.
                            costoTókenes += tókenesPosiblementeEnCaché * ((1 - FactorÉxitoCaché) * 1 + FactorÉxitoCaché * factorDescuentoCaché);
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
                = (tókenesObjetivo / (double)consultasEnPocasHoras) * (1 + (consultasEnPocasHoras - 1) * (1 - (1 - factorDescuentoCaché) * FactorÉxitoCaché));
            var largoMínimoParaRellenar = tókenesMínimoParaRellenar * CarácteresPorTokenInstrucciónSistemaTípicos;
            var tókenesActuales = instrucciónSistema.Length / CarácteresPorTokenInstrucciónSistemaTípicos;
            var carácteresObjetivo = (int)Math.Ceiling(instrucciónSistema.Length + (tókenesObjetivo - tókenesActuales) * CarácteresPorTokenRellenoMáximos);

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


        internal Opciones ObtenerOpciones(string instrucciónSistema, bool buscarEnInternet, int largoInstrucciónÚtil, List<Función> funciones,
            ref StringBuilder información) => new Opciones(Familia, instrucciónSistema, Modelo, Razonamiento, RestricciónRazonamientoAlto, 
                RestricciónRazonamientoMedio, RestricciónTókenesSalida, RestricciónTókenesRazonamiento, largoInstrucciónÚtil, Verbosidad, buscarEnInternet, 
                funciones, ref información);


        private Respuesta ObtenerRespuesta(string instrucción, Conversación conversación, Opciones opciones, Modelo modelo, 
            ref Dictionary<string, Tókenes> tókenes, out Resultado resultado) {

            if (!string.IsNullOrEmpty(instrucción) && conversación != null) throw new Exception("Debe haber instrucción o conversación, pero no ambas.");

            var (respuesta, tókenesUsadosEnConsulta, resultado2) = Cliente.ObtenerRespuesta(instrucción, conversación, opciones, modelo, Lote);
            resultado = resultado2;
            AgregarSumandoPosibleNulo(ref tókenes, tókenesUsadosEnConsulta);

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
        private Respuesta Responder(string instrucción, Conversación conversación, string instrucciónSistema, string rellenoInstrucciónSistema,
            bool buscarEnInternet, List<Función> funciones, out string respuestaTextoLimpio, ref Dictionary<string, Tókenes> tókenes, 
            ref StringBuilder información, out Resultado resultado) {

            resultado = Resultado.Respondido;

            if (!string.IsNullOrEmpty(instrucción) && conversación != null)
                throw new Exception("No se permite pasar a la funcion Responder() instrucciones individuales no nulas y a la vez pasar conversación no nula.");
            if (string.IsNullOrEmpty(instrucción) && conversación == null)
                throw new Exception("No se permite pasar a la funcion Responder() instrucciones individuales nulas y a la vez pasar conversación nula.");

            if (tókenes == null) tókenes = new Dictionary<string, Tókenes>();
            var últimaInstruccion = instrucción;
            if (conversación != null) últimaInstruccion = Conversación.ObtenerTextoÚltimaInstrucción(conversación);
            var largoInstrucciónÚtil = ObtenerLargoInstrucciónÚtil(últimaInstruccion, instrucciónSistema, rellenoInstrucciónSistema);
            Respuesta respuesta;
            var esCalidadAdaptable = CalidadAdaptable != CalidadAdaptable.No; 

            var opciones = ObtenerOpciones(instrucciónSistema, buscarEnInternet, largoInstrucciónÚtil, funciones, ref información);
            if (esCalidadAdaptable) {

                var instrucciónAplicable = string.IsNullOrEmpty(instrucción) ? últimaInstruccion : instrucción;
                if (instrucciónAplicable.Contains(UsaModeloMuchoMejor) || instrucciónAplicable.Contains(UsaModeloMejor) || instrucciónAplicable.Contains(LoHiceBien))
                    instrucciónAplicable = instrucciónAplicable.Replace(UsaModeloMuchoMejor, " ").Replace(UsaModeloMejor, " ").Replace(LoHiceBien, " ");

                if (instrucciónAplicable.IndexOf(UsaModeloMuchoMejor, StringComparison.OrdinalIgnoreCase) >= 0 
                    || instrucciónAplicable.IndexOf(UsaModeloMejor, StringComparison.OrdinalIgnoreCase) >= 0
                    || instrucciónAplicable.IndexOf(LoHiceBien, StringComparison.OrdinalIgnoreCase) >= 0) { // Para evitar que algún usuario escriba las etiquetas especiales en su mensaje y haga que el modelo repita esas etiquetas forzando el uso de un modelo más costoso sin ser necesario. Esto se podría manejar también a nivel de la instrucción de sistema si los usuarios se pusieran más creativos con formas de forzar a que el modelo conteste con esas etiquetas específicas.

                    información.AgregarLínea("El usuario escribió palabras protegidas.");

                    instrucciónAplicable = instrucciónAplicable.Reemplazar(UsaModeloMuchoMejor, " ", StringComparison.OrdinalIgnoreCase)
                        .Reemplazar(UsaModeloMejor, " ", StringComparison.OrdinalIgnoreCase).Reemplazar(LoHiceBien, " ", StringComparison.OrdinalIgnoreCase);

                    if (conversación != null) {
                        throw new Exception("El usuario ha escrito palabras protegidas."); // No se controla del todo este caso porque implicaría recrear el objeto Conversación o sobreescribirlo para agregar la instrucción del usuario limpia sin las palabras clave y se prefiere no agregar esa complejidad en el momento.
                    } else {
                        instrucción = instrucciónAplicable; // En el caso que no hay conversación es más fácil hacer la limpieza de la instrucción del usuario.
                    }

                }

                var instruccionesOriginales = opciones.ObtenerInstrucciónSistema();
                var instrucciónAutoevaluación = "\n\nPrimero responde normalmente al usuario.\n\nDespués evalúa tu " +
                    "propia respuesta y en la última línea escribe exactamente una de estas etiquetas, sola, sin dar explicaciones de tu elección:\n\n" +
                    $"{LoHiceBien}\n{UsaModeloMejor}\n{UsaModeloMuchoMejor}\n\nUsa:\n{LoHiceBien}: Si tu respuesta fue buena, tiene " +
                    $"sentido y es completa. Entendiste bien la consulta y es relativamente sencilla.\n{UsaModeloMejor}: Si tu respuesta no fue " +
                    "buena en calidad, sentido o completitud, o si a la consulta le faltan detalles, contexto o no la entiendes bien.\n" +
                    $"{UsaModeloMuchoMejor}: Si la consulta es muy compleja, requiere conocimiento experto o trata temas delicados.";
                var nuevasInstruccionesSistema = instruccionesOriginales.Contains(Fin) ? instruccionesOriginales.Replace(Fin, $"{instrucciónAutoevaluación}{Fin}")
                    : $"{instruccionesOriginales}.{instrucciónAutoevaluación}";
                opciones.EscribirInstrucciónSistema(nuevasInstruccionesSistema);

                var restricciónTókenesSalida = RestricciónTókenesSalida; // Hace copia para no reemplazar la propiedad original. Solo se reduce cuando inicia en Alta o Media.

                reintentarMenosRestrictiva:
                var respuestaInicial = ObtenerRespuesta(instrucción, conversación, opciones, Modelo, ref tókenes, out Resultado resultadoInicial);
                var textoRespuesta = respuestaInicial.ObtenerTextoRespuesta(TratamientoNegritas);

                int nivelMejoramientoSugerido;
                if (textoRespuesta.Contains(LoHiceBien)) {
                    información.AgregarLínea($"El modelo se autoevaluó con {LoHiceBien}.");
                    nivelMejoramientoSugerido = 0;
                } else if (textoRespuesta.Contains(UsaModeloMejor)) {
                    información.AgregarLínea($"El modelo sugirió {UsaModeloMejor}.");
                    nivelMejoramientoSugerido = 1;
                } else if (textoRespuesta.Contains(UsaModeloMuchoMejor)) {
                    información.AgregarLínea($"El modelo sugirió {UsaModeloMuchoMejor}.");
                    nivelMejoramientoSugerido = 2;
                } else {

                    if (resultadoInicial == Resultado.MáximosTókenesAlcanzados) { // Aquí puede entrar por traer el texto vacío o truncado. El vacío sucede cuando se llega al máximo de tókenes de salida solo con razonamiento y aún no había empezado a generar la respuesta de texto. Pueden suceder los dos casos de con OpenAI. Por lo tanto la etiqueta de autoevaluación puede estar truncada o no estar. El intento de solución más inmediato es reintantar la consulta con menos restricción en la cantidad de máximos tókenes de salida.

                        var informaciónMáximosTókenesAlcanzados = $"El modelo {Modelo} no pudo terminar la respuesta en los tókenes máximos " +
                            $"permitidos con restricción {restricciónTókenesSalida}";

                        if (Modelo.ObtenerTamaño() == Tamaño.Pequeño || Modelo.ObtenerTamaño() == Tamaño.MuyPequeño) {
                                                      
                            var modificadaRestricción = false;
                            switch (restricciónTókenesSalida) { // Debido a que normalmente el con el modo CalidadAdaptable se hace consulta inicialmente con un modelo pequeño o muy pequeño, se acepta unas consultas adicionales reduciendo la restricción a los tókenes de salida, hasta máximo nivel bajo. Esto significa que se llegaría aceptar hasta 3 veces más tókenes de salida de lo normal, que considerando las 3 respuestas, serían 1 + 2 + 3 = 6 veces más tókenes de lo normal. Lo cual puede ser aceptable en términos de costos en modelos pequeños y muy pequeños.
                            case RestricciónTókenesSalida.Alta:
                                
                                restricciónTókenesSalida = RestricciónTókenesSalida.Media;
                                modificadaRestricción = true;
                                break;

                            case RestricciónTókenesSalida.Media:

                                restricciónTókenesSalida = RestricciónTókenesSalida.Baja;
                                modificadaRestricción = true;
                                break;

                            case RestricciónTókenesSalida.Baja:
                            case RestricciónTókenesSalida.Ninguna:                                
                                break;
                            default:
                                throw new NotImplementedException();
                            }

                            if (modificadaRestricción) {

                                información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Se intentó nuevamente con una restricción menor.");
                                opciones.EscribirOpcionesRazonamientoYLímitesTókenes(Razonamiento, RestricciónRazonamientoAlto, RestricciónRazonamientoMedio, 
                                    Modelo, largoInstrucciónÚtil, restricciónTókenesSalida, RestricciónTókenesRazonamiento, Verbosidad, ref información);
                                goto reintentarMenosRestrictiva;

                            } else {
                                información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Cómo ya estaba muy baja la restricción, no se reintentó.");
                                nivelMejoramientoSugerido = 0;
                            }

                        } else {
                            información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Cómo el modelo no era muy pequeño o pequeño, no se reintentó.");
                            nivelMejoramientoSugerido = 0; // Para modelos medios o grandes, puede no justificar el costo extra de la llamada adicionales para intentar corregir este problema usando una menor restricción de máximos tókenes de salida.
                        }
                                               
                    } else { // El modelo olvidó agregar la etiqueta de autoevaluación. No tiene problemas de máximos tókenes de salida alcanzados.

                        resultado = Resultado.SinAutoevaluación; // El modelo no contestó con la etiqueta correcta. No debería pasar mucho. Se devuelve el estado apropiado al usuario librería para que pueda llevar un registro de cuándo sucede esto y tomar las acciones necesarias. Al usuario del programa se le responde normalmente sin realizar ningún mejoramiento del modelo. Lo peor que puede pasa es que el usuario a veces obtenga resultados no tan buenos con un modelo más pequeño, es similar a lo que sucede cuando el modelo es ignorantemente confidente y se califica [lo-hice-bien] sin haberlo hecho bien.
                        nivelMejoramientoSugerido = 0;
                        información.AgregarLínea("La respuesta del modelo no incluyó la etiqueta de autoevaluación esperada.");

                    }

                } // textoRespuesta.Contains(Etiqueta)>

                if (nivelMejoramientoSugerido == 0) {

                    respuesta = respuestaInicial;
                    if (resultado != Resultado.SinAutoevaluación) resultado = resultadoInicial;
                    información.AgregarLínea($"No se realizó mejoramiento por encima de {Modelo}.");

                } else {

                    var modeloMejoradoNulable = Modelo.ObtenerModeloMejorado(Modelo, CalidadAdaptable, nivelMejoramientoSugerido, ref información);
                    if (modeloMejoradoNulable == null) {
                        respuesta = respuestaInicial; // No hay modelos disponibles por encima del usado inicialmente.
                        resultado = resultadoInicial;
                    } else {

                        var modeloMejorado = (Modelo)modeloMejoradoNulable;
                        var razonamientoMejorado = ObtenerRazonamientoMejorado(Razonamiento, CalidadAdaptable, nivelMejoramientoSugerido, ref información);

                        if (Modelo.Nombre == modeloMejorado.Nombre && razonamientoMejorado == Razonamiento) {
                            respuesta = respuestaInicial;
                            resultado = resultadoInicial;
                            información.AgregarLínea($"No se repitió la consulta mejorada sugerida por el modelo porque se retringió el mejoramiento del " +
                                $"modelo y/o el razonamiento.");
                        } else { 

                            opciones.EscribirInstrucciónSistema(instruccionesOriginales); // Con el nuevo modelo usa las instrucciones originales, sin la instrucción de autoevaluacion porque solo se autoevaluará una vez.

                            opciones.EscribirOpcionesRazonamientoYLímitesTókenes(razonamientoMejorado, RestricciónRazonamientoAlto, RestricciónRazonamientoMedio,
                                modeloMejorado, largoInstrucciónÚtil, RestricciónTókenesSalida, RestricciónTókenesRazonamiento, Verbosidad, ref información);

                            respuesta = ObtenerRespuesta(instrucción, conversación, opciones, modeloMejorado, ref tókenes, out resultado);

                            información.AgregarLínea($"Se repitió consulta con {modeloMejorado}.");

                            if (resultado == Resultado.MáximosTókenesAlcanzados)
                                información.AgregarLínea($"Se alcanzó la cantidad máxima de tókenes en la consulta mejorada. Se recomienda aumentar los tókenes " +
                                    $"máximos de razonamiento o la verbosidad si en tu caso de uso estás encontrando frecuentemente esta situación."); // Se prefiere no reintentar con una restricción menor en máximos tókenes de salida en estos casos porque al ser un modelo mejorado no solo es más caro si no que también puede generar más tókenes de razonamiento. Entonces repetir de manera automática la consulta con menos restricción de tókenes podría ser costoso para el usuario de la librería.

                        }

                    }

                }

            } else {
                respuesta = ObtenerRespuesta(instrucción, conversación, opciones, Modelo, ref tókenes, out resultado);
            }

            respuestaTextoLimpio = respuesta.ObtenerTextoRespuesta(TratamientoNegritas);
            respuestaTextoLimpio = esCalidadAdaptable
                ? respuestaTextoLimpio.Replace(LoHiceBien, "").Replace(UsaModeloMejor, "").Replace(UsaModeloMuchoMejor, "") : respuestaTextoLimpio; // También se limpia el texto MedioRecomendado y el GrandeRecomendado porque podría darse el caso en el que el modelo sugiera un modelo superior, pero no hayan modelos superiores disponbiles.
            respuestaTextoLimpio = respuestaTextoLimpio.TrimEnd();
            if (resultado == Resultado.MáximosTókenesAlcanzados) 
                respuestaTextoLimpio = "No pude completar la respuesta porque se alcanzó el límite de texto. " +
                    "Intenta hacer una consulta más sencilla, concreta o separarla en varias preguntas."; // Este mensaje no sigue el tono especificado por el usuario de la libreria en instrucciones sistema, pero se acepta por ser algo que no debería suceder mucho. Se prefiere responder al usuario del programa con un tono diferente esperado que no responderle nada.
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
            out string error, out Dictionary<string, Tókenes> tókenes, out StringBuilder información, out Resultado resultado, bool buscarEnInternet = false) {

            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            resultado = Resultado.Abortado;
            if (!Iniciado) { error = "No se ha iniciado correctamente el servicio."; return null; }
            if (consultasEnPocasHoras <= 0) { error = "consultasEnPocasHoras debe ser mayor a 0."; return null; }
            if (string.IsNullOrWhiteSpace(instrucción)) { error = "La instrucción del usuario no puede ser vacía."; return null; }

            if (instrucciónSistema == null) instrucciónSistema = "";

            try {

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(consultasEnPocasHoras, instrucciónSistema, ref rellenoInstrucciónSistema, conversación: null,
                    instruccionesPorConversación: 1, proporciónPrimerInstrucciónVsSiguientes: 1, proporciónRespuestasVsInstrucciones: 1, tókenesAdicionales: 0);

                if (EstimarTókenesEntradaInstrucciones(instrucción, instrucciónSistema, rellenoInstrucciónSistema) > Modelo.TókenesEntradaLímiteSeguro) {
                    error = $"Se supera el límite de tókenes de entrada permitidos ({Modelo.TókenesEntradaLímiteSeguro}) para el modelo {Modelo}. " +
                        "Reduce el tamaño de la instrucción de sistema o la instrucción del usuario, o usa un modelo con límite mayor.";
                    return null;
                }

                var razonamientoEfectivo = ObtenerRazonamientoEfectivo(Razonamiento, RestricciónRazonamientoAlto, RestricciónRazonamientoMedio, Modelo,
                    ObtenerLargoInstrucciónÚtil(instrucción, instrucciónSistema, rellenoInstrucciónSistema), out _); // No se agrega a la información el resultado de esta función porque esta función se vuelve a llamar internamente en Responder().
                if (buscarEnInternet && (razonamientoEfectivo == RazonamientoEfectivo.Ninguno)) { // Buscar en internet no se permite hacer con Razonamiento = Ninguno.
                    error = "No se puede ejecutar una búsqueda en internet con Razonamiento = Ninguno.";
                    return null;
                }

                Responder(instrucción, conversación: null, instrucciónSistema, rellenoInstrucciónSistema, buscarEnInternet, funciones: null,
                    out string respuestaTextoLimpio, ref tókenes, ref información, out resultado);
                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                resultado = Resultado.OtroError;
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
            List<string> rutasArchivos, out string error, out Dictionary<string, Tókenes> tókenes, TipoArchivo tipoArchivo, out StringBuilder información, 
            out Resultado resultado) {

            resultado = Resultado.Abortado;
            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            if (!Iniciado) { error = "No se ha iniciado correctamente el servicio."; return null; }
            if (consultasEnPocasHoras <= 0) { error = "consultasEnPocasHoras debe ser mayor a 0."; return null; }
            if (string.IsNullOrWhiteSpace(instrucción)) { error = "La instrucción del usuario no puede ser vacía."; return null; }

            if (instrucciónSistema == null) instrucciónSistema = "";

            Archivador archivador = null;
            try {

                var tókenesEstimadosArchivos = Archivador.EstimarTókenesEntradaArchivos(rutasArchivos);

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(consultasEnPocasHoras, instrucciónSistema, ref rellenoInstrucciónSistema, 
                    conversación: null, instruccionesPorConversación: 1, proporciónPrimerInstrucciónVsSiguientes: 1, proporciónRespuestasVsInstrucciones: 1, 
                    tókenesAdicionales: tókenesEstimadosArchivos);

                if (rutasArchivos == null || rutasArchivos.Count == 0) { error = "La lista rutasArchivos está vacía."; return null; }

                if (tókenesEstimadosArchivos 
                    + EstimarTókenesEntradaInstrucciones(instrucción, instrucciónSistema, rellenoInstrucciónSistema) > Modelo.TókenesEntradaLímiteSeguro) {
                    error = $"Se supera el límite de tókenes de entrada permitidos ({Modelo.TókenesEntradaLímiteSeguro}) para el modelo {Modelo}. " +
                        "Reduce el tamaño de la instrucción de sistema, la instrucción del usuario o los archivos adjuntos, o usa un modelo con un límite mayor.";
                    return null;
                }

                archivador = Cliente.ObtenerArchivador();
                var conversaciónConArchivosYError = archivador.ObtenerConversaciónConArchivos(rutasArchivos, instrucción, tipoArchivo);
                if (!string.IsNullOrEmpty(conversaciónConArchivosYError.Error)) { error = conversaciónConArchivosYError.Error; return null; }

                var respuesta = Responder(instrucción: null, conversaciónConArchivosYError.Conversación, instrucciónSistema, rellenoInstrucciónSistema,
                    buscarEnInternet: false, funciones: null, out string respuestaTextoLimpio, ref tókenes, ref información, out resultado);
                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                resultado = Resultado.OtroError;
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
            double proporciónPrimerInstrucciónVsSiguientes, double proporciónRespuestasVsInstrucciones, out bool funciónEjecutada, out StringBuilder información, 
            out Resultado resultado) {

            resultado = Resultado.Abortado;
            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            funciónEjecutada = false;
            if (!Iniciado) { error = "No se ha iniciado correctamente el servicio."; return null; }
            if (conversacionesEnPocasHoras <= 0) { error = "conversacionesEnPocasHoras debe ser mayor a 0."; return null; }
            if (instruccionesPorConversación <= 0) { error = "instruccionesPorConversación debe ser mayor a 0."; return null; }
            if (proporciónPrimerInstrucciónVsSiguientes <= 0) { error = "proporciónPrimerInstrucciónVsSiguientes debe ser mayor a 0."; return null; }
            if (proporciónRespuestasVsInstrucciones < 0) { error = "proporciónRespuestasVsInstrucciones no puede ser negativo."; return null; }
            if (conversación == null) { error = "No se permite pasar objeto conversación en nulo. Se debe reusar la conversación."; return null; }

            if (instrucciónSistema == null) instrucciónSistema = "";

            var máximasConsultas = 5; // Límite de iteraciones entre llamadas a la función y a la IA para evitar que se quede en un ciclo infinito.
            var consultas = 0;

            try {

                var tókenesEstimadosFunciones = Función.EstimarTókenes(funciones);
                instrucciónSistema += ObtenerRellenoInstrucciónSistema(conversacionesEnPocasHoras, instrucciónSistema, ref rellenoInstrucciónSistema,
                    conversación, instruccionesPorConversación, proporciónPrimerInstrucciónVsSiguientes, proporciónRespuestasVsInstrucciones, 
                    tókenesAdicionales: tókenesEstimadosFunciones);

                if (conversación.EstimarTókenesTotales() + tókenesEstimadosFunciones // Las funciones se incluyen en el objeto Opciones que se envía en cada llamada al modelo, y no se repiten por cada mensaje del usuario. El modelo recibe la definición de funciones una sola vez en el contexto de la consulta, así que su costo en tókenes solo se cuenta una vez por petición. Leer más en https://platform.openai.com/docs/guides/function-calling.
                    + EstimarTókenesEntradaInstrucciones("", instrucciónSistema, rellenoInstrucciónSistema) > Modelo.TókenesEntradaLímiteSeguro) {
                    error = $"Se supera el límite de tókenes de entrada permitidos ({Modelo.TókenesEntradaLímiteSeguro}) para el modelo {Modelo}. " +
                        "Reduce el tamaño de la instrucción de sistema, la instrucción del usuario o las funciones, o usa un modelo con un límite mayor.";
                    return null;
                }

                Respuesta respuesta;
                string respuestaTextoLimpio;
                var funciónEjecutadaÚltimaConsulta = false;

                do { // Se sigue llamando a la API mientras esta siga solicitando ejecutar funciones y termina cuando el modelo ya no pida más; lo habitual es que el modelo se ejecute dos veces: primero identifica que necesita una función y luego usa el resultado para responder al usuario. Podrían existir casos en los que intente varias funciones en cola, así que se permite repetirse las veces necesarias dentro de un límite razonable.

                    consultas++;
                    if (consultas > máximasConsultas) {
                        resultado = Resultado.MáximasIteracionesConFunción;
                        error = $"Se llegó al máximo de {máximasConsultas} consultas en la función ConsultaConFunciones() y no se pudo llegar a una solución.";
                        return null;
                    }
                    respuesta = Responder(instrucción: null, conversación, instrucciónSistema, rellenoInstrucciónSistema, buscarEnInternet: false,
                        funciones, out respuestaTextoLimpio, ref tókenes, ref información, out resultado); // No es necesario procesar el objeto resultado porque al estar en un ciclo do...while, se reintentará varias veces y si no logra un resultado satisfactorio, sale por haber llegado al límite de iteraciones.
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

                                var resultadoFunción = Función.ObtenerResultado(funciones, ítemRespuesta.ObtenerNombreFunción(),
                                    out (string ParámetroConError, string Descripción) errorFunción, ExtraerParámetros(json));
                                var hayErrorFunción = !string.IsNullOrEmpty(errorFunción.Descripción); // El valor predeterminado de la tupla errorFunción es (null, null), pero el usuario de la librería también podría devolver ("", ""), entonces se evalúa la existencia de error ante ambas opciones: "" y null.

                                if (hayErrorFunción) {

                                    var errorJson = JsonSerializer.Serialize(new { error = errorFunción.Descripción, field = errorFunción.ParámetroConError });
                                    conversación.AgregarÍtemRespuesta(ítemRespuesta.CrearÍtemRespuestaFunción(errorJson));
                                    funciónEjecutadaÚltimaConsulta = true;
                                    continue;

                                } else {

                                    conversación.AgregarÍtemRespuesta(ítemRespuesta.CrearÍtemRespuestaFunción(resultadoFunción));
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
                resultado = Resultado.OtroError;
                return null;

            }

        } // Consulta>


    } // Servicio>


} // Frugalia>