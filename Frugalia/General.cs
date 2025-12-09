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
using System.Diagnostics;
using System.Text;


namespace Frugalia {


    public static class General { // Funciones y constantes auxiliares genéricas, sin lógica de negocio de Frugalia. Se podrían copiar y pegar en otros proyectos.


        private static readonly Random AleatorioCompartido = new Random();

        public static readonly string DobleLínea = $"{Environment.NewLine}{Environment.NewLine}";


        public static string Reemplazar(this string texto, string valorAnterior, string nuevoValor, StringComparison comparisonType) {

            if (nuevoValor == null) nuevoValor = "";
            if (string.IsNullOrEmpty(texto) || string.IsNullOrEmpty(valorAnterior)) return texto;

            if (comparisonType == StringComparison.Ordinal) return texto.Replace(valorAnterior, nuevoValor);

            int idx = 0;
            int largoAnterior = valorAnterior.Length;
            var respuesta = "";

            while (idx < texto.Length) {

                int found = texto.IndexOf(valorAnterior, idx, comparisonType);
                if (found < 0) {
                    respuesta += texto.Substring(idx);
                    break;
                }
                respuesta += texto.Substring(idx, found - idx) + nuevoValor;
                idx = found + largoAnterior;

            }

            return respuesta;

        } // Reemplazar>


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


        /// <summary>
        /// Agrega varias nuevas líneas a un texto guardado en un StringBuilder.
        /// </summary>
        /// <param name="texto"></param>
        /// <param name="nuevasLíneas"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public static void AgregarLíneas(this StringBuilder texto, StringBuilder nuevasLíneas) {

            if (texto == null) throw new ArgumentNullException(nameof(texto), "El método AgregarLíneas() no permite nulos. Usa primero AgregarLíneaPosibleNulo().");
            if (nuevasLíneas != null) { // No agrega si nuevaLíneas es nulo.
                if (texto.Length > 0) texto.AgregarLínea(""); // Si ya hay algo escrito, separa con una línea en blanco.
                texto.Append(nuevasLíneas);
            }

        } // AgregarLíneas>


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


    } // General>


} // Frugalia>
