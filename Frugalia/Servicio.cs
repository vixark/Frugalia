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
using System.Text;
using System.Text.Json;
using static Frugalia.GlobalFrugalia;
using static Frugalia.General;


namespace Frugalia {


    public class Servicio {


        private Cliente Cliente { get; }

        private Modelo Modelo { get; }

        public Familia Familia { get; }

        public Razonamiento Razonamiento { get; }

        public Restricciones Restricciones { get; }

        public Verbosidad Verbosidad { get; }

        public CalidadAdaptable CalidadAdaptable { get; }

        public TratamientoNegritas TratamientoNegritas { get; }

        private string ClaveAPI { get; }

        private bool Lote { get; }

        private bool Iniciado { get; }

        private bool RellenarInstruccionesSistema { get; }

        private int SegundosLímitePorConsulta { get; }

        internal readonly decimal TasaDeCambioUsd; 

        public bool EsCalidadAdaptable => CalidadAdaptable != CalidadAdaptable.No;

        private string GrupoCaché { get; }


        public string Descripción => $"Modelo = {Familia} {Modelo}{(Lote ? " Lote" : "")}.{Environment.NewLine}" +
            $"Verbosidad = {Verbosidad}.{Environment.NewLine}" +
            $"Razonamiento = {Razonamiento}.{Environment.NewLine}" +
            $"Segundos límite por consulta = {SegundosLímitePorConsulta}.{Environment.NewLine}" +
            $"Calidad adaptable = {CalidadAdaptable}.{Environment.NewLine}" +
            $"Rellenar instrucciones del sistema = {(RellenarInstruccionesSistema ? "Sí" : "No")}.{Environment.NewLine}" +
            $"Tasa de cambio con USD = {TasaDeCambioUsd}.{Environment.NewLine}" +
            $"{Restricciones}{Environment.NewLine}";


        public static double ObtenerFactorCaché(Modelo modelo, string grupoCaché) => 
            !string.IsNullOrWhiteSpace(grupoCaché) ? (double)modelo.FactorÉxitoCachéConGrupoCaché : (double)modelo.FactorÉxitoCaché;


        public Servicio(string nombreModelo, bool lote, Razonamiento razonamiento, Verbosidad verbosidad, CalidadAdaptable calidadAdaptable, // A propósito se provee un constructor con varios parámetros no opcionales para forzar al usuario de la librería a manualmente omitir ciertas optimizaciones. El objetivo de la librería es generar ahorros, entonces por diseño se prefiere que el usuario omita estos ahorros manualmente.
            TratamientoNegritas tratamientoNegritas, string claveAPI, decimal tasaDeCambioUsd, string grupoCaché, out string error, out string advertencia, 
            out string información, bool rellenarInstruccionesSistema = true, Restricciones restricciones = null, int segundosLímitePorConsulta = 60) {

            información = "";
            var textoInformación = new StringBuilder(); // Se deja para futuro uso.

            error = null;
            advertencia = null;
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

            if (tasaDeCambioUsd <= 0) {
                error = "La tasa de cambio a dólares no puede ser 0 o menor.";
                Iniciado = false;
                return;
            }

            if (segundosLímitePorConsulta <= 0) {
                error = "Los segundos límite por consulta no pueden ser 0 o menores.";
                Iniciado = false;
                return;
            }

            if (!System.IO.File.Exists(@"D:\Proyectos\Frugalia\grupo-caché-frugalia-demostración-permitido.txt") 
                && grupoCaché != null && grupoCaché.StartsWith(GrupoCachéDemostración)) {
                GrupoCaché = "";
                advertencia = AdvertenciaGrupoCachéDemostración;
            } else {
                GrupoCaché = Normalizar(grupoCaché); // Se deben quitar tildes y símbolos porque las APIs pueden poner problema con ellos.
            }

            Restricciones = restricciones ?? new Restricciones(); // Si es nulo, se inicia con los valores por defecto.
            ClaveAPI = claveAPI;
            Modelo = (Modelo)modelo;
            Familia = Modelo.Familia;
            Razonamiento = razonamiento;
            Verbosidad = verbosidad;
            CalidadAdaptable = calidadAdaptable;
            TratamientoNegritas = tratamientoNegritas;
            Lote = lote;
            Cliente = new Cliente(Familia, ClaveAPI);
            Iniciado = true;
            RellenarInstruccionesSistema = rellenarInstruccionesSistema;
            información = textoInformación.ToString();
            TasaDeCambioUsd = tasaDeCambioUsd;
            SegundosLímitePorConsulta = segundosLímitePorConsulta;

        } // Servicio>


        /// <summary>
        // Calcula automáticamente la nueva longitud objetivo de la instrucción del sistema para aprovechar el ahorro en el costo de lecturas tókenes de caché
        // en OpenAI y similares y devuelve un texto de relleno para la instrucción del sistema. La idea es que si la instrucción del sistema se reutiliza muchas
        // veces, conviene que su longitud compartida supere el umbral aproximado de cacheo (1024 tókenes para OpenAI). Esta función devuelve un relleno para la
        // instrucción del sistema que activaría la caché en llamadas posteriores y de esta manera obtener un costo reducido de tókenes de entrada de caché.
        // Ver fórmula en comentario interno en la función.
        /// </summary>
        /// <param name="conversacionesDuranteCachéExtendida">
        /// Cantidad de conversaciones completas o solicitudes aisladas al modelo. Se debe considerar las llamadas dentro de una conversación como una unidad 
        /// completa porque en los casos de conversaciones, se usa el valor de instruccionesPorConversación para decidir si rellena o no la instrucción del sistema.
        /// </param>
        /// <param name="instrucciónSistema">
        /// Instrucción del sistema original sin relleno.
        /// </param>
        /// <param name="consultasPorConversación">
        /// Cantidad de consultas en el contexto de una conversación. Por ejemplo, cuando se usa ConsultasConFunciones como un agente de servicio 
        /// al cliente. Si la cantidad de mensajes esperada es muy alta, puede darse que no sea necesario rellenar las instrucciones del sistema porque es posible 
        /// que se active la caché sin necesidad de esto (debido al historial de mensajes que gastan tókenes de entrada), y en cambio si se rellenara en un 
        /// entorno en el que de todas maneras se iba activar la caché, solo traería gasto innecesario de tókenes de entrada de caché aunque baratos, suman.
        /// </param>
        /// <param name="tókenesPromedioMensajeUsuario">
        /// Tókenes promedio de los usuarios, incluyendo el primer mensaje.
        /// </param>
        /// <param name="tókenesPromedioMensajeIA">
        /// Las respuestas del modelo también se incluyen en los tókenes de entrada para siguientes consultas en la misma conversación, entonces deben ser tenidos
        /// en cuenta para el cálculo total de los tókenes de entrada de la conversación completa.
        /// </param>
        /// <param name="tókenesFuncionesYArchivos">
        /// Tókenes de archivos y funciones.
        /// </param>
        /// <returns></returns>
        internal static string ObtenerRellenoInstrucciónSistema(bool rellenarInstruccionesSistema, ref string rellenoInstrucciónSistema, 
            int conversacionesDuranteCachéExtendida, string instrucciónSistema, double tókenesFunciones, double tókenesArchivos, Modelo modelo, 
            decimal tasaDeCambioUsd, string grupoCaché, ref StringBuilder información, int consultasPorConversación = 1, Conversación conversación = null, 
            double? tókenesPromedioMensajeUsuario = null, double? tókenesPromedioRespuestaIA = null) {

            /* Cálculos de longitud límite para que salga más barato dadas K llamadas estimadas en las próximas horas

            K: Número de consultas totales reusando las instrucciones del sistema, funciones y archivos. 
            Si está en modo conversacional, es conversacionesDuranteCachéExtendida * consultasPorConversación.
            S: Tókenes iniciales de instrucciones del sistema más tókenes de funciones y archivos.
            C: Costo por token.
            fE: El factor de éxito de uso de la caché. Si es 0.8, significa que de las K - 1 llamadas que deberían usar la caché, solo (K - 1) * 0,8 
                de verdad resultan usando la caché.
            fD: El factor de descuento al usar la caché. Por ejemplo, es 0.1 para gpt-5.1.
            sObj: Tókenes objetivo de S, en el caso de OpenAI es 1025 para superar el umbral de 1024. 
            En el caso de conversaciones se optimiza el valor según la cantidad de consultas en la conversación.

            Valor Original = C * S * K
            Valor Ajustando Instrucciones Sistema = 1 * C * 1025 + (K - 1) * (fE * fD + (1 - fE) * 1) * C * 1025
                = 1025C + 1025C(K - 1)(1 - (1 - fD)fE)

            Limite se alcanza cuando:
            Valor Aj. = Valor Orig.
            1025 + 1025 * (K - 1) * (1 - (1 - fD) * fE) = SK
            SLim = (1025 / K) * (1 + (K - 1) * (1 - (1 - fD) * fE))
            Y de manera general:
            SLim = (SObj / K) * (1 + (K - 1) * (1 - (1 - fD) * fE))

            Con fE = 1 se obtiene:
            K = 1 -> SLim = SObj = 1025
            K = 2 -> SLim = 563,5
            K = 3 -> SLim = 409,8
            K = 5 -> SLim = 286,9
            K = 10 -> SLim = 194,75 
            K = 35 -> SLim = 128.86
            K = 50 -> SLim = 120,94
            K = inf -> SLim = 102,5

            Caso límite: En un uso intensivo y recursivo de las instrucciones del sistema en entornos de producción donde se espera que fácilmente se superen 
            50 llamadas en menos de un día, vale la pena aumentar la instrucción del sistema si no es posible resumirla por debajo de 120 tókenes que es 
            aproximadamente 600 carácteres en español (pecando por exceso 5 caracteres por token) y elevarlo 1025 tókenes que serían pecando por exceso
            5120 carácteres para asegurarse que active la caché.
             
            */

            if (rellenoInstrucciónSistema == UnEspacio) return UnEspacio; // El espacio tiene un significado especial en el contexto de esta función. Si rellenoInstrucciónSistema es un espacio en blanco, significa que ya se calculó en una iteración anterior y que no es necesario volver a calcularla.

            if (instrucciónSistema == null) instrucciónSistema = "";

            if (!rellenarInstruccionesSistema) {
                rellenoInstrucciónSistema = UnEspacio;
                información.AgregarLínea("Se omitió rellenar las instrucciones del sistema porque rellenarInstruccionesSistema = false.");
                return rellenoInstrucciónSistema;
            }

            if (modelo.LímiteTókenesActivaciónCachéAutomática == null) {
                rellenoInstrucciónSistema = UnEspacio;
                información.AgregarLínea($"Se omitió rellenar las instrucciones del sistema porque {modelo} no tiene activación automática gratuita de la caché.");
                return rellenoInstrucciónSistema; // En los modelos que no soportan la activación automática de la caché, rellenar la instrucción del sistema no genera ningún ahorro.
            }

            if (rellenoInstrucciónSistema == null) rellenoInstrucciónSistema = "";

            if (!modelo.UsaCachéExtendida && conversacionesDuranteCachéExtendida > 1) {
                conversacionesDuranteCachéExtendida = 1; // En el caso de modelos que solo guarden la caché por unos cuantos minutos (que se espera que sean los razonables para esperar otra respuesta del usuario en un contexto de una conversación) no se puede realizar ninguna optimización considerando múltiples conversaciones que se puedan dar en el transcurso del día y que reusen las instrucciones del sistema. Entonces solo se considera la optimización que tiene en cuenta la cantidad de mensajes en una conversación (consultasPorConversación) porque se espera que estos si puedan reusar la caché.
                información.AgregarLíneaSiNoEstá($"Se forzó conversacionesDuranteCachéExtendida a 1 porque {modelo} no soporta caché extendida.");
            }

            if (conversacionesDuranteCachéExtendida <= 0)
                throw new ArgumentOutOfRangeException(nameof(conversacionesDuranteCachéExtendida), "conversacionesDuranteCachéExtendida debe ser mayor a 0.");
            if (consultasPorConversación <= 0)
                throw new ArgumentOutOfRangeException(nameof(consultasPorConversación), "consultasPorConversación debe ser mayor a 0.");
            if (tókenesFunciones < 0)
                throw new ArgumentOutOfRangeException(nameof(tókenesFunciones), "tókenesFunciones no puede ser negativo.");
            if (tókenesArchivos < 0)
                throw new ArgumentOutOfRangeException(nameof(tókenesArchivos), "tókenesArchivos no puede ser negativo.");
            var tókenesInstrucciónSistema = instrucciónSistema.Length / (double)CarácteresPorTokenInstrucciónSistema;

            if (modelo.LímiteTókenesActivaciónCachéAutomática <= 0) throw new Exception("El límite de tókenes no puede ser 0 o negativo.");
            if (!string.IsNullOrEmpty(rellenoInstrucciónSistema)) { // Si ya se pasa el relleno de la instrucción del sistema de una íteración anterior, se usa esta sin realizar ningún cálculo. No se usa IsNullOrWhiteSpace porque el espacio significa que ya ejecutó esta función y no se requirió relleno.
                return rellenoInstrucciónSistema;
            } else {
                rellenoInstrucciónSistema = "";
            }

            var s = tókenesInstrucciónSistema + tókenesArchivos + tókenesFunciones;
            var fE = ObtenerFactorCaché(modelo, grupoCaché);
            var fD = Modelo.ObtenerFactorDescuentoCaché(modelo);
            var sObj = (int)modelo.LímiteTókenesActivaciónCachéAutomática + 1.0; // Para GPT a partir de 1024 se activa la caché de tókenes de entrada https://platform.openai.com/docs/guides/prompt-caching.
            bool rellenarS;

            if (fD >= 0.9) { // Se le pone este control para el caso de modelos que no tienen descuento suficiente para tókenes de entrada en caché. Se ignora el relleno cuando el descuento es menor o igual al 10% porque se considera que ahorros de esa magnitud no justifican la potencial disminución de la calidad de las respuestas debido a la inclusión del texto de relleno de las instrucciones del sistema.
                rellenoInstrucciónSistema = UnEspacio;
                información.AgregarLínea("Se omitió rellenar las instrucciones del sistema porque el descuento de caché es menor o igual al 10%.");
                return rellenoInstrucciónSistema;
            }

            if (conversación != null) { // En el uso típico de la conversación (como se hace en la función ObtenerConFunción()), lo usual es incluir las respuestas anteriores del modelo. Entonces se debe verificar en que casos la caché se activa sola y es económicamente más óptima que rellenar las instrucciones del sistema desde el principio. No se rellena en la mitad de la conversación porque eso implica un recálculo de la caché y la pérdida de todo el bloque de información repetida que puede ser guardable en caché, la decisión es o rellenarlo al principio o no hacerlo. En estos casos no se rellena las instrucciones del sistema.

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
 
                Considerando que R1=R2=R3=...=R y que M2=M3=M4=...=M, es decir que las respuestas del modelo son de la misma longitud y que los mensajes del usuario son de la misma longitud después del primero.
 
                Total Tókenes = (S + M1) + (S + M1 + M + R) + (S + M1 + 2M + 2R) + (S + M1 + 3M + 3R) ... + (S + M1 + (N-1) * M + (N - 1) * R)
 
                Total Tókenes = N * (S + M1) + (1 + 2 + 3 .... + (N - 1)) * (M + R)
                 
                */

                if (tókenesPromedioMensajeUsuario == null || tókenesPromedioMensajeUsuario <= 0)
                    throw new ArgumentOutOfRangeException(nameof(tókenesPromedioMensajeUsuario), "tókenesPromedioMensajeUsuario debe no nulo y mayor a 0.");
                if (tókenesPromedioRespuestaIA == null || tókenesPromedioRespuestaIA <= 0)
                    throw new ArgumentOutOfRangeException(nameof(tókenesPromedioRespuestaIA), "tókenesPromedioRespuestaIA debe ser no nulo y mayor a 0.");

                (decimal Costo, bool SeActivóCaché) obtenerCostoTókenes(double t, double tAnteriores, bool forzarCachéSRellena, double sRellena) {

                    var tNuevos = t - tAnteriores;
                    var tEquivalentesNoCaché = 0.0; // Suma los tókenes completos de no caché y con los tókenes de caché multiplicados por el factorDescuentoCaché.
                    var seActivóCaché = false;
                    if (tAnteriores >= sObj || forzarCachéSRellena) { // Se usa caché porque se espera que en la consulta anterior se haya activado y se pueda usar en la consulta actual. El efecto del factor de éxito de la cache se incluye en al calcular los tEquivalentesNoCaché.           

                        var tCandidatosCaché = forzarCachéSRellena ? sRellena : tAnteriores; // Cuando se esta forzando la caché con la SRellena es porque viene de una conversación anterior que activó la caché con la SRellena. Esta línea permite que la caché se active correctamente en el primer mensaje de las siguientes conversaciones.
                        var tPosiblementeEnCaché = 128 * (int)Math.Floor(tCandidatosCaché / 128.0); // En la caché se guardan tókenes en múltiplos de 128.
                        tEquivalentesNoCaché += tPosiblementeEnCaché * ((1 - fE) * 1 + fE * fD); // Los equivalentes son los completos que se cobren por fallo de la caché y los parciales (con factorDescuentoCaché) cuando la caché se active correctamente.
                        tEquivalentesNoCaché += tCandidatosCaché - tPosiblementeEnCaché; // Los tókenes restantes se cobran a precio completo.
                        seActivóCaché = true;

                    } else {
                        tEquivalentesNoCaché += tAnteriores; // Cómo no se ha activado la caché, se cobran los tókenes anteriores a precio completo de no cáché.
                    }
                    tEquivalentesNoCaché += tNuevos; // Los tókenes nuevos siempre se cobran a precio completo de no caché.

                    return (Tókenes.CalcularCostoMonedaLocalTókenes((int)Math.Round(tEquivalentesNoCaché), modelo.PrecioEntradaNoCaché, tasaDeCambioUsd), 
                        seActivóCaché);

                } // obtenerCostoTókenes>

                var n = consultasPorConversación + (tókenesFunciones > 0 ? 1 : 0); // Para efectos de la simulación de costos teórica, se agrega un mensaje adicional cuando se tienen funciones. Este mensaje adicional equivale a la consulta doble que se hace al modelo cuando se contesta con el resultado de una función.
                var c = conversacionesDuranteCachéExtendida;
                var m1 = (conversación.ObtenerTextoPrimerMensajeUsuario()?.Length ?? 0) / CarácteresPorTokenTípicos;
                var m = n == 1 ? m1 : Math.Max(((double)tókenesPromedioMensajeUsuario * n - m1) / (n - 1), 1); // m es el valor promedio de los tókenes de los mensajes de usuario después del primero, por lo tanto en esta línea se calcula usando el promedio de todos quitando el efecto del primer mensaje. Si recibe un valor irreal para tókenesPromedioMensajeUsuario, se acota a 1 la cantidad de tókenes por mensajes del usuario.
                var r = (double)tókenesPromedioRespuestaIA;
                double obtenerTókenes(int númeroMensaje, double sRellena) => númeroMensaje <= 0 ? 0 : sRellena + m1 + (númeroMensaje - 1) * (m + r); // Cantidad de tókenes de entrada gastados en la consulta numeroMensaje y guardables en caché en la consulta numeroMensaje - 1.
                var cantidadRellenosProbar = Math.Max(0, (int)Math.Ceiling((sObj - s) / 128.0));
                var menorTotalCostoTókenesConRelleno = decimal.MaxValue;
                var rellenoDelMenorCosto = 0.0;
                información.AgregarLínea($"Tókenes estimados: Sistema + Funciones + Archivos = {s:N0}   1er Mensaje Usuario = {m1:N0}   " +
                    $"Usuario = {m:N0} / mensaje   IA = {r:N0} / mensaje   % Éxito Caché = {ObtenerFactorCaché(modelo, grupoCaché):0%}");
                
                for (int iR = 0; iR <= cantidadRellenosProbar; iR++) { // Itera entre diferentes posibilidades de relleno usando escalas de 128 tókenes.

                    var sRellenaIt = iR * 128 + s;
                    var totalCostoConRelleno = 0.0M;
                    var cachéActivada = false; // Esta no sirve para recordar la activación de la caché para el primer mensaje 
                    var númeroConsultaActivaciónCaché = -1;
                    var cachéActivadaConSoloSRellena = false; // Cuando la SRellena supera el límite para la activación de caché, si se tiene una sola conversación, esta caché es usada en el segundo mensaje. Pero si se tienen varias conversaciones, esta caché puede ser usada en el primer mensaje de todas las siguientes conversaciones que se hagan en en el intervalo de tiempo de la caché extendida.

                    for (int iC = 1; iC <= c; iC++) { // Itera entre las conversaciones.

                        if (iC == 1 && sRellenaIt > sObj) {
                            información.AgregarLínea($"Con un relleno de {iR * 128:N0} tókenes se cruza el umbral para forzar la caché en todas las consultas.");
                            cachéActivadaConSoloSRellena = true; // Si la s rellena es suficiente para activar la caché para el segundo mensaje de la conversacion, también lo es para activarla para el primer mensaje de las siguientes conversaciones.
                        }
                             
                        for (int iM = 1; iM <= n; iM++) { // Itera entre los mensajes de la conversación.

                            var tConRelleno = obtenerTókenes(iM, sRellenaIt);
                            var tAnterioresConRelleno = obtenerTókenes(iM - 1, sRellenaIt);
                            (var costo, var iCachéActivada) = 
                                obtenerCostoTókenes(tConRelleno, tAnterioresConRelleno, iC != 1 && iM == 1 ? cachéActivadaConSoloSRellena : false, sRellenaIt); // Solo si se está en el mensaje 1 es necesario forzar el uso de la caché por solo SRellena. En los siguientes mensajes de esa conversación la caché estara activada normalmente por SRellena + M1 + R1 + ....
                            if (iCachéActivada) {
                                if (númeroConsultaActivaciónCaché == -1) númeroConsultaActivaciónCaché = iM; // En el primer mensaje que se active en la primera conversación. Si se va activar la caché, se debe activar en la primera conversación porque de lo contrario no se activaría en ninguna.
                                cachéActivada = true;
                            }
                            totalCostoConRelleno += costo;

                        }

                    }

                    var textoMenorEncontrado = "";
                    if (totalCostoConRelleno < menorTotalCostoTókenesConRelleno) {
                        menorTotalCostoTókenesConRelleno = totalCostoConRelleno;
                        rellenoDelMenorCosto = iR * 128;
                        textoMenorEncontrado = " Nuevo mínimo";
                    }

                    información.AgregarLínea($"Relleno = {iR * 128,4:N0}  C = {c}  M = {consultasPorConversación}  " +
                        $"Caché = {(cachéActivada ? $"Sí, en M{númeroConsultaActivaciónCaché}" : "No       ")}  " +
                        $"Costo Aprox. Entrada = {FormatearMoneda(totalCostoConRelleno)}{textoMenorEncontrado}");

                }

                if (rellenoDelMenorCosto == 0) {
                    rellenarS = false; // La opción más económica es no rellenar la instrucción del sistema.
                } else {
                    rellenarS = true;
                    sObj = rellenoDelMenorCosto + s;
                }

            } else { // conversación == null.

                var k = conversacionesDuranteCachéExtendida; // En el caso que no hay conversación se usa la fórmula teórica directa para decidir si se debe rellenar la instrucción del sistema.
                var sLímite = (sObj / k) * (1 + (k - 1) * (1 - (1 - fD) * fE)); // Umbral mínimo teórico de a partir del cual conviene rellenar hasta tókenesObjetivo para activar la caché: se iguala el costo sin relleno con el costo usando caché, que mezcla tókenes a precio completo y con descuento según la probabilidad de acierto de caché), se despeja S esta expresión y marca el punto en el que el ahorro esperado por caché compensa el relleno inicial. Ver desarrollo completo al inicio de la función.
                rellenarS = s > sLímite; // Siempre evalúa rellenar cuando no se está en una conversación.
                información.AgregarLínea($"Mínimos tókenes de la instrucción del sistema para que se obtengan ahorros al llenarla = {sLímite:N0}.");

            }

            if (rellenarS && s < sObj) {

                var inicioRelleno = $"{Fin} A continuación se incluye un texto genérico de ejemplo que no forma parte " + // No quitar la parte {fin} porque es útil para insertar otras instrucciones antes del texto relleno.
                    "de la lógica de negocio. No debes usar su contenido para responderle al usuario. ";

                var lorem =
                    "Texto genérico: Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. " +
                    "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat. " +
                    "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur. " +
                    "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum. ";

                var tFaltantes = sObj - s;
                var cFaltantes = tFaltantes * CarácteresPorTokenRelleno;

                if (inicioRelleno.Length > cFaltantes) {
                    rellenoInstrucciónSistema = inicioRelleno; // En estos casos se puede pasar un poco del largo requerido, pero no es grave. Se prefiere que este mensaje esté siempre completo para no confundir al modelo.
                } else {

                    var cLoremFaltantes = (int)Math.Ceiling(cFaltantes - inicioRelleno.Length);
                    var loremsEnteros = (int)Math.Floor(cLoremFaltantes / ((double)lorem.Length));
                    var lorems = new StringBuilder();
                    for (int i = 0; i < loremsEnteros; i++) {
                        lorems.Append(lorem);
                    }
                    lorems.Append(lorem.Substring(0, cLoremFaltantes - loremsEnteros * lorem.Length));
                    rellenoInstrucciónSistema = inicioRelleno + lorems.ToString();

                }

            }

            if (rellenoInstrucciónSistema.Length > 0) {
                información.AgregarLínea($"Instrucción del sistema rellenada con {rellenoInstrucciónSistema.Length / CarácteresPorTokenRelleno:N0} " +
                    $"tókenes para optimizar costos.");
            } else {
                información.AgregarLínea($"No se rellenó la instrucción del sistema porque resultaban costos mayores con cualquier relleno.");
            }
                
            if (rellenoInstrucciónSistema == "") rellenoInstrucciónSistema = UnEspacio; // El espacio en blanco tiene un significado especial en el contexto de esta función. Significa que la función ya se evaluó la primera vez y que no se encontró rentable rellenar la instrucción del sistema. Esto permite ejecutar las funciones Consulta() en un ciclo, usando la misma variable rellenoInstrucciónSistema para pasar como ref y de esta manera se evita la ejecución de esta función múltiples veces. El espacio adicional que se le agrega a la instrucción del sistema, no afecta en nada el funcionamiento general.
            return rellenoInstrucciónSistema;

        } // ObtenerRellenoInstrucciónSistema>


        internal static Opciones ObtenerOpciones(string instrucciónSistema, bool buscarEnInternet, int longitudInstrucciónÚtil, Familia familia, Modelo modelo,
            Razonamiento razonamiento, Verbosidad verbosidad, string grupoCaché, Restricciones restricciones, 
            List<Función> funciones, ref StringBuilder información)
                => new Opciones(familia, instrucciónSistema, modelo, razonamiento, restricciones, longitudInstrucciónÚtil, verbosidad, grupoCaché, 
                    buscarEnInternet, funciones, ref información);


        private static Respuesta ObtenerRespuesta(string mensajeUsuario, Conversación conversación, Opciones opciones, Modelo modelo, Cliente cliente, bool lote,
            ref Dictionary<string, Tókenes> tókenes, int segundosLímite, out Resultado resultado) {

            if (!string.IsNullOrEmpty(mensajeUsuario) && conversación != null) throw new Exception("Debe haber mensajeUsuario o conversación, pero no ambas.");

            var (respuesta, tókenesUsadosEnConsulta, resultado2) = cliente.ObtenerRespuesta(mensajeUsuario, conversación, opciones, modelo, lote, segundosLímite);
            resultado = resultado2;
            AgregarSumandoPosibleNulo(ref tókenes, tókenesUsadosEnConsulta);

            return respuesta;

        } // ObtenerRespuesta>


        /// <summary>
        /// 
        /// </summary>
        /// <param name="mensajeUsuario"></param>
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
        private static Respuesta ObtenerRespuesta(string mensajeUsuario, Conversación conversación, string instrucciónSistema, string rellenoInstrucciónSistema,
            bool buscarEnInternet, List<Función> funciones, double tókenesAdicionalesInstrucciónÚtil, Cliente cliente, Familia familia, Modelo modelo, bool lote, 
            Razonamiento razonamiento, Verbosidad verbosidad, string grupoCaché, bool esCalidadAdaptable, Restricciones restricciones, 
            CalidadAdaptable calidadAdaptable, TratamientoNegritas tratamientoNegritas, int segundosLímite, out string respuestaTextoLimpio, 
            ref Dictionary<string, Tókenes> tókenes, ref StringBuilder información, out Resultado resultado) {

            resultado = Resultado.Respondido;

            if (!string.IsNullOrEmpty(mensajeUsuario) && conversación != null)
                throw new Exception("No se permite pasar a la funcion ObtenerRespuesta() instrucciones individuales no nulas y a la vez pasar conversación no nula.");
            if (string.IsNullOrEmpty(mensajeUsuario) && conversación == null)
                throw new Exception("No se permite pasar a la funcion ObtenerRespuesta() instrucciones individuales nulas y a la vez pasar conversación nula.");

            if (tókenes == null) tókenes = new Dictionary<string, Tókenes>();
            var últimaInstruccion = mensajeUsuario;
            if (conversación != null) últimaInstruccion = Conversación.ObtenerTextoÚltimaInstrucción(conversación);

            Respuesta respuesta;
            var longitudInstrucciónÚtil = ObtenerLongitudInstrucciónÚtil(últimaInstruccion, instrucciónSistema, rellenoInstrucciónSistema,
                tókenesAdicionalesInstrucciónÚtil * CarácteresPorTokenTípicos, esCalidadAdaptable);
            if (EsRazonamientoAdaptable(razonamiento)) información.AgregarLínea($"Longitud instrucción útil = {longitudInstrucciónÚtil} carácteres.");

            var opciones = ObtenerOpciones(instrucciónSistema, buscarEnInternet, longitudInstrucciónÚtil, familia, modelo, razonamiento, verbosidad, grupoCaché,
                restricciones, funciones, ref información);

            if (esCalidadAdaptable) {

                var mensajeUsuarioAplicable = string.IsNullOrEmpty(mensajeUsuario) ? últimaInstruccion : mensajeUsuario;
                if (mensajeUsuarioAplicable.IndexOf(UsaModeloMuchoMejor, StringComparison.OrdinalIgnoreCase) >= 0
                    || mensajeUsuarioAplicable.IndexOf(UsaModeloMejor, StringComparison.OrdinalIgnoreCase) >= 0
                    || mensajeUsuarioAplicable.IndexOf(LoHiceBien, StringComparison.OrdinalIgnoreCase) >= 0) { // Para evitar que algún usuario escriba las etiquetas especiales en su mensaje y haga que el modelo repita esas etiquetas forzando el uso de un modelo más costoso sin ser necesario. Esto se podría manejar también a nivel de la instrucción del sistema si los usuarios se pusieran más creativos con formas de forzar a que el modelo conteste con esas etiquetas específicas.

                    información.AgregarLínea("El usuario escribió palabras protegidas.");

                    mensajeUsuarioAplicable = mensajeUsuarioAplicable.Reemplazar(UsaModeloMuchoMejor, UnEspacio, StringComparison.OrdinalIgnoreCase)
                        .Reemplazar(UsaModeloMejor, UnEspacio, StringComparison.OrdinalIgnoreCase).Reemplazar(LoHiceBien, UnEspacio, StringComparison.OrdinalIgnoreCase);

                    if (conversación != null) {
                        throw new Exception("El usuario escribió palabras protegidas."); // No se controla del todo este caso porque implicaría recrear el objeto Conversación o sobreescribirlo para agregar el mensaje del usuario limpio sin las palabras clave y se prefiere no agregar esa complejidad en el momento.
                    } else {
                        mensajeUsuario = mensajeUsuarioAplicable; // En el caso que no hay conversación es más fácil hacer la limpieza del mensaje del usuario.
                    }

                }

                var instruccionesOriginales = opciones.ObtenerInstrucciónSistema();
                var nuevasInstruccionesSistema = instruccionesOriginales.Contains(Fin) ? instruccionesOriginales.Replace(Fin, $"{InstrucciónAutoevaluación}{Fin}")
                    : $"{instruccionesOriginales}.{InstrucciónAutoevaluación}";
                opciones.EscribirInstrucciónSistema(nuevasInstruccionesSistema);

                var restricciónTókenesSalidaCopia = restricciones.TókenesSalida; // Hace copia para no reemplazar la propiedad original. Solo se reduce cuando inicia en Alta o Media.
                var restricciónTókenesRazonamientoCopia = restricciones.TókenesRazonamiento;
                var reintentoMenosRestrictivoRealizado = false;

                reintentarMenosRestrictiva:
                var respuestaInicial = ObtenerRespuesta(mensajeUsuario, conversación, opciones, modelo, cliente, lote, ref tókenes, segundosLímite,
                    out Resultado resultadoInicial);
                var textoRespuesta = respuestaInicial.ObtenerTextoRespuesta(tratamientoNegritas);

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

                        var informaciónMáximosTókenesAlcanzados = $"No se terminó la respuesta en los tókenes máximos";

                        if (modelo.ObtenerTamaño() == Tamaño.Pequeño || modelo.ObtenerTamaño() == Tamaño.MuyPequeño) {

                            var modificadaRestricción = false;
                            switch (restricciónTókenesSalidaCopia) { // Debido a que normalmente el con el modo CalidadAdaptable se hace consulta inicialmente con un modelo pequeño o muy pequeño, se acepta unas consultas adicionales reduciendo la restricción a los tókenes de salida, hasta máximo nivel bajo. Esto significa que se llegaría aceptar hasta 3 veces más tókenes de salida de lo normal, que considerando las 3 respuestas, serían 1 + 2 + 3 = 6 veces más tókenes de lo normal. Lo cual puede ser aceptable en términos de costos en modelos pequeños y muy pequeños.
                            case RestricciónTókenesSalida.Alta:

                                restricciónTókenesSalidaCopia = RestricciónTókenesSalida.Media;
                                modificadaRestricción = true;
                                break;

                            case RestricciónTókenesSalida.Media:

                                restricciónTókenesSalidaCopia = RestricciónTókenesSalida.Baja;
                                modificadaRestricción = true;
                                break;

                            case RestricciónTókenesSalida.Baja:
                            case RestricciónTókenesSalida.Ninguna:
                                break;
                            default:
                                throw new NotImplementedException();
                            }

                            switch (restricciónTókenesRazonamientoCopia) { // Debido a que normalmente el con el modo CalidadAdaptable se hace consulta inicialmente con un modelo pequeño o muy pequeño, se acepta unas consultas adicionales reduciendo la restricción a los tókenes de salida, hasta máximo nivel bajo. Esto significa que se llegaría aceptar hasta 3 veces más tókenes de salida de lo normal, que considerando las 3 respuestas, serían 1 + 2 + 3 = 6 veces más tókenes de lo normal. Lo cual puede ser aceptable en términos de costos en modelos pequeños y muy pequeños.
                            case RestricciónTókenesRazonamiento.Alta:

                                restricciónTókenesRazonamientoCopia = RestricciónTókenesRazonamiento.Media;
                                modificadaRestricción = true;
                                break;

                            case RestricciónTókenesRazonamiento.Media:

                                restricciónTókenesRazonamientoCopia = RestricciónTókenesRazonamiento.Baja;
                                modificadaRestricción = true;
                                break;

                            case RestricciónTókenesRazonamiento.Baja:
                            case RestricciónTókenesRazonamiento.Ninguna:
                                break;
                            default:
                                throw new NotImplementedException();
                            }

                            if (modificadaRestricción) {

                                if (!reintentoMenosRestrictivoRealizado) { // Se asegura que se hace solo un intento de reducción de restricción de tókenes de salida. Más de un intento, se puede tardar mucho la consulta y escalar el costo de una manera que no debería ser automática.

                                    información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Se reintentó con menos restricciones.");

                                    opciones.EscribirOpcionesRazonamientoYLímitesTókenes(razonamiento, modelo,
                                        new Restricciones(restricciones.RazonamientoMuyAlto, restricciones.RazonamientoAlto,
                                        restricciones.RazonamientoMedio, restricciónTókenesSalidaCopia, restricciónTókenesRazonamientoCopia),
                                        longitudInstrucciónÚtil, verbosidad, ref información);

                                    reintentoMenosRestrictivoRealizado = true;
                                    goto reintentarMenosRestrictiva;

                                } else {
                                    información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Como solo se permite un reintento, no se reintentó.");
                                    nivelMejoramientoSugerido = 0;
                                }

                            } else {
                                información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Cómo ya estaba muy baja la restricción, no se reintentó.");
                                nivelMejoramientoSugerido = 0;
                            }

                        } else {
                            información.AgregarLínea($"{informaciónMáximosTókenesAlcanzados}. Cómo el modelo no era muy pequeño o pequeño, no se reintentó.");
                            nivelMejoramientoSugerido = 0; // Para modelos medios o grandes, puede no justificar el costo extra de la llamada adicionales para intentar corregir este problema usando una menor restricción de máximos tókenes de salida.
                        }

                    } else if (resultadoInicial == Resultado.TiempoSuperado) {

                        nivelMejoramientoSugerido = 0; // No es necesario asignar el resultado porque se asigna en la rama de nivelMejoramientoSugerido = 0.
                        información.AgregarLínea("Se alcanzó el tiempo límite para la consulta.");

                    } else { // El modelo olvidó agregar la etiqueta de autoevaluación. No tiene problemas de máximos tókenes de salida alcanzados.

                        resultado = Resultado.SinAutoevaluación; // El modelo no contestó con la etiqueta correcta. No debería pasar mucho. Se devuelve el estado apropiado al usuario librería para que pueda llevar un registro de cuándo sucede esto y tomar las acciones necesarias. Al usuario del programa se le responde normalmente sin realizar ningún mejoramiento del modelo. Lo peor que puede pasa es que el usuario a veces obtenga resultados no tan buenos con un modelo más pequeño, es similar a lo que sucede cuando el modelo es ignorantemente confidente y se califica [lo-hice-bien] sin haberlo hecho bien.
                        nivelMejoramientoSugerido = 0;
                        información.AgregarLínea("La respuesta del modelo no incluyó la etiqueta de autoevaluación esperada.");

                    }

                } // textoRespuesta.Contains(Etiqueta)>

                if (nivelMejoramientoSugerido == 0) {

                    respuesta = respuestaInicial;
                    if (resultado != Resultado.SinAutoevaluación) resultado = resultadoInicial;
                    información.AgregarLínea($"No se realizó mejoramiento por encima del modelo {modelo} y de razonamiento {razonamiento}.");

                } else {

                    var modeloMejoradoNulable = Modelo.ObtenerModeloMejorado(modelo, calidadAdaptable, nivelMejoramientoSugerido, ref información);
                    if (modeloMejoradoNulable == null) {
                        respuesta = respuestaInicial; // No hay modelos disponibles por encima del usado inicialmente.
                        resultado = resultadoInicial;
                    } else {

                        var modeloMejorado = (Modelo)modeloMejoradoNulable;
                        var razonamientoMejorado = ObtenerRazonamientoMejorado(modeloMejorado, razonamiento, calidadAdaptable, nivelMejoramientoSugerido, ref información);

                        if (modelo.Nombre == modeloMejorado.Nombre && razonamientoMejorado == razonamiento) {
                            respuesta = respuestaInicial;
                            resultado = resultadoInicial;
                            información.AgregarLínea($"No se repitió la consulta mejorada porque se retringió el mejoramiento.");
                        } else {

                            información.AgregarLínea($"Se repitió consulta con {modeloMejorado} y razonamiento {razonamientoMejorado}.");

                            opciones.EscribirInstrucciónSistema(instruccionesOriginales); // Con el nuevo modelo usa las instrucciones originales, sin la instrucción de autoevaluacion porque solo se autoevaluará una vez.

                            opciones.EscribirOpcionesRazonamientoYLímitesTókenes(razonamientoMejorado, modeloMejorado, restricciones, longitudInstrucciónÚtil, 
                                verbosidad, ref información);

                            respuesta = ObtenerRespuesta(mensajeUsuario, conversación, opciones, modeloMejorado, cliente, lote, ref tókenes, segundosLímite, 
                                out resultado);

                            if (resultado == Resultado.MáximosTókenesAlcanzados)
                                información.AgregarLínea($"Se alcanzó la cantidad máxima de tókenes en la consulta mejorada. Se recomienda aumentar los tókenes " +
                                    $"máximos de razonamiento o la verbosidad si en tu caso de uso estás encontrando frecuentemente esta situación."); // Se prefiere no reintentar con una restricción menor en máximos tókenes de salida en estos casos porque al ser un modelo mejorado no solo es más caro si no que también puede generar más tókenes de razonamiento. Entonces repetir de manera automática la consulta con menos restricción de tókenes podría ser costoso para el usuario de la librería.

                        }

                    }

                }

            } else {
                respuesta = ObtenerRespuesta(mensajeUsuario, conversación, opciones, modelo, cliente, lote, ref tókenes, segundosLímite, out resultado);
            }

            respuestaTextoLimpio = respuesta.ObtenerTextoRespuesta(tratamientoNegritas);
            respuestaTextoLimpio = esCalidadAdaptable
                ? respuestaTextoLimpio.Replace(LoHiceBien, "").Replace(UsaModeloMejor, "").Replace(UsaModeloMuchoMejor, "") : respuestaTextoLimpio; // También se limpia el texto MedioRecomendado y el GrandeRecomendado porque podría darse el caso en el que el modelo sugiera un modelo superior, pero no hayan modelos superiores disponbiles.
            respuestaTextoLimpio = respuestaTextoLimpio.TrimEnd();

            var reintentaConsultaMásSimple = "Intenta hacer una consulta más sencilla, concreta o separarla en varias preguntas.";
            if (resultado == Resultado.MáximosTókenesAlcanzados) 
                respuestaTextoLimpio = $"No pude completar la respuesta porque se alcanzó el límite de texto o de capacidad de razonamiento. " +
                    $"{reintentaConsultaMásSimple}"; // Este mensaje no sigue el tono especificado por el usuario de la libreria en instrucciones sistema, pero se acepta por ser algo que no debería suceder mucho. Se prefiere responder al usuario del programa con un tono diferente esperado que no responderle nada.
            
            if (resultado == Resultado.TiempoSuperado)
                respuestaTextoLimpio = "No pude completar la respuesta porque se alcanzó el límite de tiempo. " +
                    $"Intenta más tarde porque los servidores pueden estar teniendo problemas. O si prefieres, {reintentaConsultaMásSimple.ToLower()}"; // Este mensaje no sigue el tono especificado por el usuario de la libreria en instrucciones sistema, pero se acepta por ser algo que no debería suceder mucho. Se prefiere responder al usuario del programa con un tono diferente esperado que no responderle nada.

            return respuesta;

        } // ObtenerRespuesta>


        /// <summary>
        /// Consulta simple, buscando o no en internet.
        /// </summary>
        /// <param name="instrucciónSistema">Rol, tono, formato respuesta, reglas generales, límites, comportamiento del agente, etc.</param>
        /// <param name="error"></param>
        /// <param name="rellenoInstrucciónSistema">Si se pasa un espacio " ", se omitirá la evaluación del relleno de instrucción del sistema.</param>
        /// <returns></returns>
        public string Consultar(int consultasDuranteCachéExtendida, string instrucciónSistema, ref string rellenoInstrucciónSistema, string mensajeUsuario,
            out string error, out Dictionary<string, Tókenes> tókenes, out StringBuilder información, out Resultado resultado, bool buscarEnInternet = false) {

            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            resultado = Resultado.Abortado;
            if (!Iniciado) { error = "No se ha iniciado correctamente el servicio."; return null; }
            if (consultasDuranteCachéExtendida <= 0) { error = "consultasDuranteCachéExtendida debe ser mayor a 0."; return null; }
            if (string.IsNullOrWhiteSpace(mensajeUsuario)) { error = "El mensaje del usuario no puede ser vacío."; return null; }

            if (instrucciónSistema == null) instrucciónSistema = "";

            try {

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(RellenarInstruccionesSistema, ref rellenoInstrucciónSistema,
                    consultasDuranteCachéExtendida, instrucciónSistema, tókenesFunciones: 0, tókenesArchivos: 0, Modelo, TasaDeCambioUsd, GrupoCaché, 
                    ref información);

                if (EstimarTókenesEntradaInstrucciones(mensajeUsuario, instrucciónSistema, rellenoInstrucciónSistema) > Modelo.TókenesEntradaLímiteSeguro) {
                    error = $"Se supera el límite de tókenes de entrada permitidos ({Modelo.TókenesEntradaLímiteSeguro}) para el modelo {Modelo}. " +
                        "Reduce el tamaño de la instrucción del sistema o el mensaje del usuario, o usa un modelo con límite mayor.";
                    return null;
                }

                var razonamientoEfectivo = ObtenerRazonamientoEfectivo(Razonamiento, Modelo, Restricciones, 
                    ObtenerLongitudInstrucciónÚtil(mensajeUsuario, instrucciónSistema, rellenoInstrucciónSistema, longitudAdicional: 0,
                    CalidadAdaptable != CalidadAdaptable.No), out _); // No se agrega a la información el resultado de esta función porque esta función se vuelve a llamar internamente en Responder().
                
                if (buscarEnInternet && (razonamientoEfectivo == RazonamientoEfectivo.Ninguno)) { // Buscar en internet no se permite hacer con Razonamiento = Ninguno.
                    error = "No se puede ejecutar una búsqueda en internet con Razonamiento = Ninguno.";
                    return null;
                }

                ObtenerRespuesta(mensajeUsuario, conversación: null, instrucciónSistema, rellenoInstrucciónSistema, buscarEnInternet, funciones: null,
                    tókenesAdicionalesInstrucciónÚtil: 0, Cliente, Familia, Modelo, Lote, Razonamiento, Verbosidad, GrupoCaché, EsCalidadAdaptable, Restricciones, 
                    CalidadAdaptable, TratamientoNegritas, SegundosLímitePorConsulta, out string respuestaTextoLimpio, ref tókenes, ref información, out resultado);

                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                resultado = Resultado.OtroError;
                return null;

            }

        } // Consultar>


        /// <summary>
        /// Con archivos.
        /// </summary>
        /// <param name="conversacionesDuranteCachéExtendida"></param>
        /// <param name="instrucciónSistema"></param>
        /// <param name="rellenoInstrucciónSistema">Si se pasa un espacio " ", se omitirá la evaluación del relleno de instrucción del sistema.</param>
        /// <param name="mensajeUsuario"></param>
        /// <param name="rutasArchivos"></param>
        /// <param name="error"></param>
        /// <param name="tókenes"></param>
        /// <param name="tipoArchivo"></param>
        /// <returns></returns>
        public string Consultar(int conversacionesDuranteCachéExtendida, string instrucciónSistema, ref string rellenoInstrucciónSistema, string mensajeUsuario,
            List<string> rutasArchivos, out string error, out Dictionary<string, Tókenes> tókenes, TipoArchivo tipoArchivo, out StringBuilder información,
            out Resultado resultado) {

            resultado = Resultado.Abortado;
            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            if (!Iniciado) { error = "No se ha iniciado correctamente el servicio."; return null; }
            if (conversacionesDuranteCachéExtendida <= 0) { error = "conversacionesDuranteCachéExtendida debe ser mayor a 0."; return null; }
            if (string.IsNullOrWhiteSpace(mensajeUsuario)) { error = "El mensaje del usuario no puede ser vacía."; return null; }

            if (instrucciónSistema == null) instrucciónSistema = "";

            Archivador archivador = null;
            try {

                var tókenesArchivos = Archivador.EstimarTókenes(rutasArchivos);

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(RellenarInstruccionesSistema, ref rellenoInstrucciónSistema, 
                    conversacionesDuranteCachéExtendida, instrucciónSistema, tókenesFunciones: 0, tókenesArchivos, Modelo, TasaDeCambioUsd, GrupoCaché, 
                    ref información);

                if (rutasArchivos == null || rutasArchivos.Count == 0) { error = "La lista rutasArchivos está vacía."; return null; }

                if (tókenesArchivos
                    + EstimarTókenesEntradaInstrucciones(mensajeUsuario, instrucciónSistema, rellenoInstrucciónSistema) > Modelo.TókenesEntradaLímiteSeguro) {
                    error = $"Se supera el límite de tókenes de entrada permitidos ({Modelo.TókenesEntradaLímiteSeguro}) para el modelo {Modelo}. " +
                        "Reduce el tamaño de la instrucción del sistema, el mensaje del usuario o los archivos adjuntos, o usa un modelo con un límite mayor.";
                    return null;
                }

                archivador = Cliente.ObtenerArchivador();
                var conversaciónConArchivosYError = archivador.ObtenerConversaciónConArchivos(rutasArchivos, mensajeUsuario, tipoArchivo);
                if (!string.IsNullOrEmpty(conversaciónConArchivosYError.Error)) { error = conversaciónConArchivosYError.Error; return null; }

                var respuesta = ObtenerRespuesta(mensajeUsuario: null, conversaciónConArchivosYError.Conversación, instrucciónSistema, rellenoInstrucciónSistema,
                    buscarEnInternet: false, funciones: null, tipoArchivo != TipoArchivo.Imagen ? tókenesArchivos : 0, Cliente, Familia, Modelo, Lote, // Si los archivos son imágenes, por el momento no se suman los tókenes del archivo a los tókenes de entrada adicionales porque el caso de imágenes muy simples (por ejemplo, un imagen de un solo color) no implicarían una alta carga de razonamiento adicional requerido, en cambio imágenes más complejas como fotos de tablas nutricionales sí implicarían una alta carga de razonamiento adicional. Por lo tanto, derivar los tókenes adicionales de entrada de los tókenes del tamaño del archivo de la imagen no sería muy apropiado. Si se quisiera ser más estricto, se debería preanalizar la imagen para determinar que tanta carga de razonamiento extra puede requerir.
                    Razonamiento, Verbosidad, GrupoCaché, EsCalidadAdaptable, Restricciones, CalidadAdaptable, TratamientoNegritas, SegundosLímitePorConsulta, 
                    out string respuestaTextoLimpio, ref tókenes, ref información, out resultado);

                error = null;
                return respuestaTextoLimpio;

            } catch (Exception ex) {

                error = ex.Message;
                resultado = Resultado.OtroError;
                return null;

            } finally {
                archivador?.EliminarArchivos();
            }

        } // Consultar>


        /// <summary>
        /// Ejecuta una consulta en el contexto de una conversación reutilizable, habilitando llamadas a funciones.
        /// El modelo puede solicitar ejecutar una o varias funciones, y tras cada ejecución se retroalimenta la conversación
        /// con el resultado para que el modelo continúe hasta producir la respuesta final al usuario o hasta alcanzar
        /// el límite de iteraciones permitido.
        /// </summary>
        /// <param name="conversacionesDuranteCachéExtendida">
        /// Estimación de cuántas conversaciones completas se reutilizarán durante la ventana de caché extendida del modelo.
        /// Se usa para decidir si conviene rellenar las instrucciones del sistema para activar la caché de lectura y reducir costos.
        /// </param>
        /// <param name="instrucciónSistema">
        /// Instrucciones del sistema (rol, tono, reglas y límites) que guían el comportamiento del asistente durante toda la consulta.
        /// Pueden ser rellenadas automáticamente para activar caché según configuración y estimación de uso.
        /// </param>
        /// <param name="rellenoInstrucciónSistema">
        /// Texto de relleno agregado a las instrucciones del sistema para superar el umbral de activación de caché de entrada del modelo.
        /// Se pasa por referencia para conservar y reutilizar el mismo relleno en llamadas subsecuentes, evitando recalcularlo. 
        /// Si se pasa un espacio " ", se omitirá la evaluación del relleno de instrucción del sistema. 
        /// </param>
        /// <param name="conversación">
        /// Conversación reutilizable que contiene el historial de mensajes del usuario y del asistente IA.
        /// No debe ser nula; se usará para agregar respuestas y resultados de funciones durante el ciclo de consulta.
        /// </param>
        /// <param name="funciones">
        /// Lista de funciones disponibles para que el modelo solicite su ejecución cuando lo estime necesario.
        /// Cada función define su nombre, parámetros y lógica de ejecución.
        /// </param>
        /// <param name="error">
        /// Devuelve la descripción del error si la consulta falla (por ejemplo, por superar límites de tókenes de entrada,
        /// argumentos de función inválidos o alcanzar el máximo de iteraciones). Nulo si no hubo errores.
        /// </param>
        /// <param name="tókenes">
        /// Devuelve el detalle de tókenes consumidos, separando cada iteración del ciclo de funciones
        /// (se agrega un sufijo con el número de consulta). Útil para auditoría de costos.
        /// </param>
        /// <param name="consultasPorConversación">
        /// Estimación de la cantidad de consultas que se darán dentro de una conversación típica.
        /// Se usa para el cálculo del relleno de instrucciones del sistema y su conveniencia económica.
        /// </param>
        /// <param name="funciónEjecutada">
        /// Indica si, durante la consulta, se llegó a ejecutar al menos una función. True si se ejecutó alguna; false en caso contrario.
        /// </param>
        /// <param name="información">
        /// Devuelve un registro informativo de decisiones internas (relleno de sistema, cambios de restricciones,
        /// sugerencias del modelo, selección de modelo y razonamiento, advertencias y otros eventos relevantes).
        /// </param>
        /// <param name="resultado">
        /// Devuelve el estado final de la consulta: Respondido, Abortado, MáximosTókenesAlcanzados, MáximasIteracionesConFunción,
        /// SinAutoevaluación o OtroError, según corresponda.
        /// </param>
        /// <param name="usarModeloYRazonamientoMínimosEnRespuestaFunción">Cuando el modelo solicita ejecutar una función, luego de obtener el resultado de la 
        /// función, se vuelve a llamar al modelo para que genere la respuesta final al usuario. En esta segunda llamada, por lo general no es necesario usar 
        /// un modelo grande ni un razonamiento alto, ya que el trabajo pesado lo hizo la primera llamada al modelo para decidir qué función ejecutar y cómo 
        /// ejecutarla. Por lo tanto, si este parámetro es verdadero, se fuerza a que en la segunda llamada se use el modelo más pequeño disponible y razonamiento 
        /// ninguno. Esto podría reducir costos y tiempos de respuesta. Si se desea que en la segunda llamada se use el mismo modelo y razonamiento 
        /// que en la primera llamada, se debe pasar falso en este parámetro. Aunque en el papel suena a buena idea, la utilidad de pasar verdadero en este 
        /// parámetro dependerá del caso de uso. Sí la conversación ya traía caché activada, al pasar a otro modelo esa caché ya no será accesible para el nuevo
        /// modelo, por lo tanto todos los tókenes del contexto de la conversación se cobrarán a precio de no caché. Esto, dependiendo de la tabla de precios y 
        /// del modelo de partida, podría hacer que resulte ser más cara la respuesta final con el modelo más pequeño y con un deterioro en la calidad de la
        /// respuesta.</param>
        /// <returns>
        /// El texto de respuesta final para el usuario, limpio de marcas auxiliares internas. Devuelve null si ocurre un error.
        /// </returns>
        public string Consultar(int conversacionesDuranteCachéExtendida, string instrucciónSistema, ref string rellenoInstrucciónSistema, Conversación conversación,
            List<Función> funciones, out string error, out Dictionary<string, Tókenes> tókenes, int consultasPorConversación,
            double tókenesPromedioMensajeUsuario, double tókenesPromedioRespuestaIA, out bool funciónEjecutada, out StringBuilder información, 
            out Resultado resultado, bool usarModeloYRazonamientoMínimosEnRespuestaFunción = false) {

            resultado = Resultado.Abortado;
            información = new StringBuilder();
            tókenes = new Dictionary<string, Tókenes>();
            funciónEjecutada = false;
            if (!Iniciado) { error = "No se ha iniciado correctamente el servicio."; return null; }
            if (conversacionesDuranteCachéExtendida <= 0) { error = "conversacionesDuranteCachéExtendida debe ser mayor a 0."; return null; }
            if (consultasPorConversación <= 0) { error = "consultasPorConversación debe ser mayor a 0."; return null; }
            if (tókenesPromedioMensajeUsuario <= 0) { error = "tókenesPromedioMensajeUsuario debe ser mayor a 0."; return null; }
            if (tókenesPromedioRespuestaIA <= 0) { error = "tókenesPromedioRespuestaIA debe ser mayor a 0."; return null; }
            if (conversación == null) { error = "No se permite pasar objeto conversación en nulo. Se debe reusar la conversación."; return null; }

            if (instrucciónSistema == null) instrucciónSistema = "";

            var máximasConsultasFunciones = 5; // Límite de iteraciones entre llamadas a la función y a la IA para evitar que se quede en un ciclo infinito.
            var consultas = 0;

            try {

                var tókenesFunciones = Función.EstimarTókenes(funciones);

                instrucciónSistema += ObtenerRellenoInstrucciónSistema(RellenarInstruccionesSistema, ref rellenoInstrucciónSistema, 
                    conversacionesDuranteCachéExtendida, instrucciónSistema, tókenesFunciones, tókenesArchivos: 0, Modelo, TasaDeCambioUsd, GrupoCaché,
                    ref información, consultasPorConversación, conversación, tókenesPromedioMensajeUsuario, tókenesPromedioRespuestaIA);

                if (conversación.EstimarTókenesTotales() + tókenesFunciones // Las funciones se incluyen en el objeto Opciones que se envía en cada llamada al modelo, y no se repiten por cada mensaje del usuario. El modelo recibe la definición de funciones una sola vez en el contexto de la consulta, así que su costo en tókenes solo se cuenta una vez por petición. Leer más en https://platform.openai.com/docs/guides/function-calling.
                    + EstimarTókenesEntradaInstrucciones("", instrucciónSistema, rellenoInstrucciónSistema) > Modelo.TókenesEntradaLímiteSeguro) {
                    error = $"Se supera el límite de tókenes de entrada permitidos ({Modelo.TókenesEntradaLímiteSeguro}) para el modelo {Modelo}. " +
                        "Reduce el tamaño de la instrucción del sistema, el mensaje del usuario o las funciones, o usa un modelo con un límite mayor.";
                    return null;
                }

                Respuesta respuesta;
                string respuestaTextoLimpio;
                var funciónEjecutadaÚltimaConsulta = false;

                do { // Se sigue llamando a la API mientras esta siga solicitando ejecutar funciones y termina cuando el modelo ya no pida más; lo habitual es que el modelo se ejecute dos veces: primero identifica que necesita una función y luego usa el resultado para responder al usuario. Podrían existir casos en los que intente varias funciones en cola, así que se permite repetirse las veces necesarias dentro de un límite razonable.

                    consultas++;
                    if (consultas > máximasConsultasFunciones) {
                        resultado = Resultado.MáximasIteracionesConFunción;
                        error = $"Se llegó al máximo de {máximasConsultasFunciones} consultas en la función ConsultaConFunciones() y no se pudo llegar a una solución.";
                        return null;
                    }

                    var tókenesConsulta = new Dictionary<string, Tókenes>();
                    var modeloAUsar = Modelo;
                    var razonamientoAUsar = Razonamiento;
                    if (usarModeloYRazonamientoMínimosEnRespuestaFunción && funciónEjecutadaÚltimaConsulta) { // Una vez se tiene la respuesta requerida por el usuario, componer el mensaje de respuesta por lo general no requiere alto razonamiento ni modelos grandes.
                        modeloAUsar = ObtenerModeloMásPequeño(Modelo.Familia);
                        razonamientoAUsar = Razonamiento.Ninguno;
                    }

                    respuesta = ObtenerRespuesta(mensajeUsuario: null, conversación, instrucciónSistema, rellenoInstrucciónSistema, buscarEnInternet: false,
                        funciones, tókenesFunciones, Cliente, Familia, modeloAUsar, Lote, razonamientoAUsar, Verbosidad, GrupoCaché, EsCalidadAdaptable, 
                        Restricciones, CalidadAdaptable, TratamientoNegritas, SegundosLímitePorConsulta, out respuestaTextoLimpio, ref tókenesConsulta, 
                        ref información, out resultado); // No es necesario procesar el objeto resultado porque al estar en un ciclo do...while, se reintentará varias veces y si no logra un resultado satisfactorio, sale por haber llegado al límite de iteraciones.
                    
                    funciónEjecutadaÚltimaConsulta = false;

                    foreach (var cv in tókenesConsulta) {
                        tókenes.Add($"{cv.Key}-{consultas}", cv.Value); // Se separan los tókenes por cada consulta de la función para poder hacer una supervisión más granular de los tókenes gastados en este ciclo Do...While. 
                    }

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

        } // Consultar>


    } // Servicio>


} // Frugalia>