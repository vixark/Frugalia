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

using static Frugalia.Global;


namespace Frugalia {


    public class Parámetro {


        public string Nombre { get; }

        public string Tipo { get; }

        public string Descripción { get; }

        public bool Requerido { get; }


        public Parámetro(string nombre, string tipo, string descripción, bool requerido) {

            Nombre = nombre.ToLowerInvariant();
            Tipo = tipo.ToLowerInvariant();
            Descripción = descripción;
            Requerido = requerido;

        } // Parámetro>


        public double EstimarTókenes() {
            var totalCarácteres = (Nombre?.Length ?? 0) + (Tipo?.Length ?? 0) + (Descripción?.Length ?? 0);
            return totalCarácteres / (double)CarácteresPorTokenTípicos;
        } // EstimarTókenes>


    } // Parámetro>


} // Frugalia>
