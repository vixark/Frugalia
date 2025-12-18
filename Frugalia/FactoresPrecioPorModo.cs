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


namespace Frugalia {


    /// <summary>Conjunto de factores de ajuste de precios aplicados a consultas en diferentes modos de servicio: Lote, Económico o Prioritario.</summary>
    internal sealed class FactoresPrecioPorModo {


        /// <summary>Factor aplicable para los PreciosEntrada y PreciosSalida, pero no para PreciosEscrituraCaché</summary>
        internal double EntradaYSalida { get; } = 1;

        internal double EscrituraCaché { get; } = 1;

        internal double LecturaCache { get; } = 1; // A noviembre 2025: Gemini y OpenAI no aplican descuento, pero Claude sí.

        internal bool Disponible { get; } = true;


        internal FactoresPrecioPorModo(double entradaYSalida, double lecturaCache, double escrituraCaché) {

            Disponible = true;
            EntradaYSalida = entradaYSalida;
            EscrituraCaché = escrituraCaché;
            LecturaCache = lecturaCache;

        } // FactoresPrecioPorModo>


        internal FactoresPrecioPorModo() { } // Se crean con 1 como valor por defecto y disponible verdadero.


        internal FactoresPrecioPorModo(bool disponible) => Disponible = disponible; // Se crean con 1 como valor por defecto.


    } // FactoresPrecioPorModo>


} // Frugalia>
