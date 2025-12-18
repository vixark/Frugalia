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
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.Json;


namespace Frugalia {


    public static class General { // Funciones y constantes auxiliares genéricas, sin lógica de negocio de Frugalia. Se podrían copiar y pegar en otros proyectos.


        private static readonly Random AleatorioCompartido = new Random();

        public static readonly string DobleLínea = $"{Environment.NewLine}{Environment.NewLine}";


        public static string Reemplazar(this string texto, string valorAnterior, string nuevoValor, StringComparison comparisonType) {

            if (nuevoValor == null) nuevoValor = "";
            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(valorAnterior)) return texto;

            if (comparisonType == StringComparison.Ordinal) return texto.Replace(valorAnterior, nuevoValor);

            int idx = 0;
            int longitudAnterior = valorAnterior.Length;
            var respuesta = "";

            while (idx < texto.Length) {

                int found = texto.IndexOf(valorAnterior, idx, comparisonType);
                if (found < 0) {
                    respuesta += texto.Substring(idx);
                    break;
                }
                respuesta += texto.Substring(idx, found - idx) + nuevoValor;
                idx = found + longitudAnterior;

            }

            return respuesta;

        } // Reemplazar>


        public static List<T> CompilarElementosPermitidos<T>(List<T> permitidos, List<T> noPermitidos) where T : struct {

            var permitidosNulos = permitidos == null;
            var noPermitidosNulos = noPermitidos == null;
            var nombre = typeof(T).Name;
            var todos = (T[])Enum.GetValues(typeof(T));

            if (permitidosNulos && noPermitidosNulos) return new List<T>(todos); // Si ambas listas son nulas, se permiten todos los valores.

            var permitidosRespuesta = new HashSet<T>();
            var noPermitidosRespuesta = new HashSet<T>();

            void Agregar<T2>(List<T2> origen, HashSet<T2> destino) where T2 : struct {
                foreach (var valor in origen) {
                    if (!destino.Add(valor)) throw new Exception($"Valor duplicado en la lista de permitidos o no permitidos de {nombre}: {valor}.");
                }
            }

            if (!permitidosNulos) Agregar(permitidos, permitidosRespuesta);
            if (!noPermitidosNulos) Agregar(noPermitidos, noPermitidosRespuesta);

            foreach (var valor in permitidosRespuesta) {
                if (noPermitidosRespuesta.Contains(valor)) 
                    throw new Exception($"Incoherencia en configuración de {nombre}: el valor {valor} está tanto en permitidos como en no permitidos.");
            }

            if (!permitidosNulos && noPermitidosNulos) return new List<T>(permitidosRespuesta); // Solo se definieron permitidos.

            if (permitidosNulos && !noPermitidosNulos) {

                var respuesta = new List<T>();
                foreach (var valor in todos) {
                    if (!noPermitidosRespuesta.Contains(valor)) respuesta.Add(valor); // Se permiten todos excepto los no permitidos.
                }
                return respuesta;

            }

            return new List<T>(permitidosRespuesta); // Si ambas listas tienen datos, ya validamos que no haya intersección.

        } // CompilarElementosPermitidos>


        public static string FormatearEnMonedaLocal(decimal usd, decimal tasaCambioUsd) => FormatearMoneda(usd * tasaCambioUsd);


        public static string FormatearPesosColombianos(decimal pesos) => $"{FormatearMoneda(pesos)} COP";


        public static string FormatearDólares(decimal pesos, decimal tasaCambioUSD) => $"{Redondear(pesos / tasaCambioUSD, 2):0.#####} USD";


        public static string FormatearMoneda(decimal pesos) => $"{pesos:#,0.##} $";


        /// <summary>
        /// Devuelve el valor redondeado a la cantidad de cifras significativas indicada conservando el orden de magnitud.
        /// </summary>
        /// <param name="valor">Valor que se desea redondear.</param>
        /// <param name="cifrasSignificativas">Número de cifras significativas (>= 1). Por defecto 1.</param>
        /// <returns>Valor redondeado a las cifras significativas especificadas.</returns>
        public static decimal Redondear(decimal valor, int cifrasSignificativas = 1) {

            if (cifrasSignificativas < 1) throw new ArgumentOutOfRangeException(nameof(cifrasSignificativas), "cifrasSignificativas debe ser mayor o igual a 1.");
            if (valor == 0) return 0;
            var signo = Math.Sign(valor);
            var absoluto = Math.Abs(valor);

            var potencia = Math.Pow(10, Math.Floor(Math.Log10((double)absoluto))); // potencia = 10^floor(log10(absoluto))
            var escala = (decimal)potencia;

            var normalizado = absoluto / escala; // normalizado en [1,10)

            var factor = (decimal)Math.Pow(10, cifrasSignificativas - 1); // factor para desplazar cifras significativas (p.ej. cifrasSignificativas=2 -> factor=10)

            var normalizadoMultiplicado = normalizado * factor; // redondeamos el valor normalizado a cifrasSignificativas y volvemos a escalar
            var redondeadoMultiplicado = Math.Round(normalizadoMultiplicado, 0, MidpointRounding.ToEven);
            var redondeado = redondeadoMultiplicado / factor;

            return signo * redondeado * escala;

        } // Redondear>


        /// <summary>
        /// Agrega una nueva línea a un texto guardado en un StringBuilder.
        /// Si el texto es nulo, lanza excepción.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevaLínea"></param>
        public static void AgregarLínea(this StringBuilder texto, string nuevaLínea) {

            if (texto == null) throw new ArgumentNullException(nameof(texto), "El método AgregarLínea() no permite nulos. Usa AgregarLíneaPosibleNulo().");
            texto.AppendLine(nuevaLínea);

        } // AgregarLínea>


        public static void AgregarLíneaSiNoEstá(this StringBuilder texto, string nuevaLínea) {

            if (!texto.ContieneLínea(nuevaLínea)) texto.AgregarLínea(nuevaLínea);

        } // AgregarLíneaSiNoEstá>


        /// <summary>
        /// Agrega varias nuevas líneas a un texto guardado en un StringBuilder.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevasLíneas"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AgregarLíneas(this StringBuilder texto, StringBuilder nuevasLíneas) {

            if (texto == null) throw new ArgumentNullException(nameof(texto), "El método AgregarLíneas() no permite nulos. Usa primero AgregarLíneaPosibleNulo().");
            if (nuevasLíneas != null) texto.Append(nuevasLíneas); // No agrega si nuevaLíneas es nulo.

        } // AgregarLíneas>


        /// <summary>
        /// Agrega varias nuevas líneas a un texto guardado en un StringBuilder, evitando duplicados de línea.
        /// Recorre cada línea de <paramref name="nuevasLíneas"/> y solo agrega aquellas que no existan ya como línea completa en <paramref name="texto"/>.
        /// </summary>
        /// <param name="texto">Texto destino donde se agregarán las líneas.</param>
        /// <param name="nuevasLíneas">Texto de origen cuyas líneas se intentarán agregar.</param>
        /// <exception cref="ArgumentNullException">Si <paramref name="texto"/> es nulo.</exception>
        public static void AgregarLíneasSiNoEstán(this StringBuilder texto, StringBuilder nuevasLíneas) { // Función escrita por GPT 5.1. No probada extensivamente.

            if (texto == null) throw new ArgumentNullException(nameof(texto), "El método AgregarLíneasSiNoEstán() no permite nulos.");
            if (nuevasLíneas == null || nuevasLíneas.Length == 0) return;

            int len = nuevasLíneas.Length;
            int pos = 0;

            while (pos <= len) {

                int inicio = pos;
                int fin = inicio;

                while (fin < len && nuevasLíneas[fin] != '\r' && nuevasLíneas[fin] != '\n') fin++;

                int longitudLínea = fin - inicio;
                if (longitudLínea > 0) {

                    string línea = nuevasLíneas.ToString(inicio, longitudLínea);
                    if (!texto.ContieneLínea(línea)) texto.AgregarLínea(línea);

                } else {

                    if (!texto.ContieneLínea(string.Empty)) texto.AgregarLínea(string.Empty);

                }

                if (fin < len) {
                    if (nuevasLíneas[fin] == '\r' && fin + 1 < len && nuevasLíneas[fin + 1] == '\n') pos = fin + 2;
                    else pos = fin + 1;
                } else {
                    pos = fin + 1;
                }

            }

        } // AgregarLíneasSiNoEstán>


        /// <summary>
        /// Indica si el StringBuilder es nulo o está vacío (Length == 0).
        /// Equivalente a string.IsNullOrEmpty pero para StringBuilder.
        /// </summary>
        /// <param name="texto">Instancia a evaluar (puede ser nula).</param>
        /// <returns>true si es nulo o está vacío; false en caso contrario.</returns>
        public static bool EsNuloOVacío(this StringBuilder texto) => texto == null || texto.Length == 0;


        /// <summary>
        /// Agrega una nueva línea a un texto guardado en un StringBuilder.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevaLínea"></param>
        public static void AgregarLíneaPosibleNulo(ref StringBuilder texto, string nuevaLínea) { // Es necesaria esta función auxiliar porque C# 7.3 no permite 'ref this StringBuilder texto' para objetos que no son struct.

            if (texto == null) texto = new StringBuilder();
            texto.AgregarLínea(nuevaLínea);

        } // AgregarLíneaPosibleNulo>


        /// <summary>
        /// Ayudante para evitar el estado inestable en el que puede quedar el depurador después de una excepción en algunos casos.
        /// No se recomienda usar frecuentemente. Lo recomendable es usar en la medida de lo posible las excepciones normales. 
        /// Solo usar esta función cuando el editor esté poniendo problema.
        /// </summary>
        /// <param name="mensaje"></param>
        internal static void LanzarExcepción(string mensaje) {

            Suspender();
            #if !DEBUG
                throw new Exception(mensaje);
            #endif

        } // LanzarExcepciónYSuspender>


        internal static void Suspender() {

            #if DEBUG
                Debugger.Break();
            #endif

        } // Suspender>


        public static int ObtenerAleatorio(int mínimo, int máximo) {

            lock (AleatorioCompartido) {
                return AleatorioCompartido.Next(mínimo, máximo);
            }

        } // ObtenerAleatorio>


        /// <summary>
        /// Normaliza una cadena para usarla como nombre de archivo o identificador seguro.
        /// </summary>
        /// <param name="texto"></param>
        /// <returns></returns>
        public static string Normalizar(string texto) {

            if (string.IsNullOrWhiteSpace(texto)) return texto;

            texto = texto.Trim().ToLowerInvariant();

            var formD = texto.Normalize(NormalizationForm.FormD); // Quita diacríticos (tildes/ñ -> n, ó -> o, etc.)
            var sb = new StringBuilder(formD.Length);

            foreach (char ch in formD) {

                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc == UnicodeCategory.NonSpacingMark) continue;

                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_' || ch == ':' || ch == '.') { // Permite solo caracteres seguros para APIs problemáticas.
                    sb.Append(ch);
                } else if (char.IsWhiteSpace(ch)) {
                    sb.Append('-');
                } else {
                    sb.Append('-');
                }

            }

            var respuesta = sb.ToString().Trim('-');
            while (respuesta.Contains("--")) respuesta = respuesta.Replace("--", "-");
            return respuesta;

        } // Normalizar>


        /// <summary>
        /// Detecta si una línea exacta ya está presente en el contenido del StringBuilder.
        /// No realiza asignaciones de cadena para comparaciones Ordinal y OrdinalIgnoreCase.
        /// Soporta saltos de línea CRLF (\r\n), LF (\n) y CR (\r).
        /// </summary>
        /// <param name="texto">Buffer a inspeccionar.</param>
        /// <param name="línea">Línea a buscar (sin el salto de línea).</param>
        /// <param name="comparison">Tipo de comparación. Optimizado para Ordinal y OrdinalIgnoreCase.</param>
        /// <returns>true si existe una línea exactamente igual; false en caso contrario.</returns>
        public static bool ContieneLínea(this StringBuilder texto, string línea, StringComparison comparison = StringComparison.Ordinal) { // Función escrita por GPT 5.1. No probada extensivamente.

            if (texto == null) throw new ArgumentNullException(nameof(texto));
            if (línea == null) throw new ArgumentNullException(nameof(línea));

            int len = texto.Length;
            int objetivoLen = línea.Length;
            if (objetivoLen == 0) {

                int i = 0; // Considera que una línea vacía existe si hay dos saltos consecutivos o una línea final vacía
                while (i <= len) {

                    int inicio = i; // Encuentra fin de línea
                    int fin = inicio;
                    while (fin < len && texto[fin] != '\r' && texto[fin] != '\n') fin++;
                    if (fin == inicio) return true; // línea vacía

                    if (fin < len) { // Avanza sobre separador de línea
                        if (texto[fin] == '\r' && fin + 1 < len && texto[fin + 1] == '\n') i = fin + 2;
                        else i = fin + 1;
                    } else {
                        i = fin + 1;
                    }
                }

                return false;

            }

            bool ignoreCase = comparison == StringComparison.OrdinalIgnoreCase; // Comparación optimizada para Ordinal y OrdinalIgnoreCase
            bool usarOptimizado = comparison == StringComparison.Ordinal || comparison == StringComparison.OrdinalIgnoreCase;

            int pos = 0;
            while (pos <= len) {

                int inicio = pos;
                int fin = inicio;

                while (fin < len && texto[fin] != '\r' && texto[fin] != '\n') fin++; // Encuentra fin de la línea actual

                int longitudLíneaActual = fin - inicio;
                if (longitudLíneaActual == objetivoLen) {

                    if (usarOptimizado) {

                        bool iguales = true; // Comparación carácter por carácter
                        for (int k = 0; k < objetivoLen; k++) {

                            char a = texto[inicio + k];
                            char b = línea[k];
                            if (ignoreCase) {
                                a = char.ToUpperInvariant(a); // ToUpperInvariant minimiza coste cultural
                                b = char.ToUpperInvariant(b);
                            }
                            if (a != b) { iguales = false; break; }

                        }
                        if (iguales) return true;

                    } else {
                        var tramo = texto.ToString(inicio, longitudLíneaActual); // Fallback: convierte solo el tramo a string y compara con reglas culturales
                        if (string.Equals(tramo, línea, comparison)) return true;
                    }

                }

                if (fin < len) { // Avanza sobre separador de línea
                    if (texto[fin] == '\r' && fin + 1 < len && texto[fin + 1] == '\n') pos = fin + 2; // CRLF
                    else pos = fin + 1; // LF o CR
                } else {
                    pos = fin + 1; // Fin del buffer
                }

            }

            return false;

        } // ContieneLínea>


        internal static string AgregarLineaJson(string jsonActual, string líneaJson) {

            if (string.IsNullOrWhiteSpace(líneaJson)) throw new ArgumentException("La línea json no puede estar vacía.", nameof(líneaJson));
            return (jsonActual ?? string.Empty) + líneaJson + "\n";

        } // AgregarLineaJson>


        internal static DateTime ObtenerFecha(long segundosUnix) => DateTimeOffset.FromUnixTimeSeconds(segundosUnix).UtcDateTime;


        internal static string ObtenerTexto(JsonElement elementoJson, string nombre) {

            if (elementoJson.TryGetProperty(nombre, out JsonElement e) && e.ValueKind != JsonValueKind.Null)
                return e.GetString();
            return null;

        } // ObtenerTexto>


        internal static long ObtenerLong(JsonElement elementoJson, string nombre) {

            if (elementoJson.TryGetProperty(nombre, out JsonElement e) && e.ValueKind == JsonValueKind.Number)
                return e.GetInt64();
            return 0;

        } // ObtenerLong>


        internal static List<(string Nombre, string Valor)> ExtraerParámetros(JsonDocument json) {

            if (json == null) return new List<(string Nombre, string Valor)> { };

            var resultado = new List<(string Nombre, string Valor)>();
            foreach (var propiedad in json.RootElement.EnumerateObject()) {
                var nombre = propiedad.Name.ToLowerInvariant();
                var valor = ATexto(propiedad.Value);
                resultado.Add((nombre, valor));
            }

            return resultado;

        } // ExtraerParámetros>


        internal static string ATexto(JsonElement elemento) {

            switch (elemento.ValueKind) {
            case JsonValueKind.String:
                return elemento.GetString();
            case JsonValueKind.Number:
                Suspender(); // Verificar cuando suceda.
                return elemento.GetRawText(); // Devuelve el texto crudo: 123, 3.14, etc.
            case JsonValueKind.True:
            case JsonValueKind.False:
                Suspender(); // Verificar cuando suceda.
                return elemento.GetBoolean().ToString();
            case JsonValueKind.Null:
                Suspender(); // Verificar cuando suceda.
                return null;
            default:
                Suspender(); // Verificar cuando suceda.
                return elemento.GetRawText(); // Objetos, vectores, etc: se devuelve crudo.
            }

        } // ATexto>


    } // General>


} // Frugalia>
