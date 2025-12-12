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
using System.Linq;
using static Frugalia.GlobalFrugalia;


namespace Frugalia {


    public sealed class Función {


        public string Nombre { get; }

        public string Descripción { get; }

        /// <summary>
        /// El orden de adición de los parámetros en esta lista es el orden en el que se usan en la función. Si algún parámetro no es requerido,
        /// igual ocupa espacio en la lista de parámetros y se llama la función con null en su valor sin generar error.
        /// </summary>
        public IReadOnlyList<Parámetro> Parámetros { get; }

        public const int ParámetrosMáximos = 5;

        public delegate string FunciónDelegate(out (string ParámetroConError, string Descripción) error, string valor1 = null, string valor2 = null, 
            string valor3 = null, string valor4 = null, string valor5 = null);

        public FunciónDelegate DelegadoFunción { get; }


        public Función(string nombre, FunciónDelegate función, string descripción, List<Parámetro> parámetros) {

            Nombre = nombre.ToLowerInvariant();
            Descripción = descripción;
            Parámetros = parámetros?.ToList() ?? new List<Parámetro>(); // Copia de la lista para que quien llamó la función no pueda modificar posteriormente la lista Parámetros al alterar parámetros, lo que sucedería si se reutilizara el mismo objeto. La lista de parámetros podría ser una lista vacía si la función no requiere parámetros, por ejemplo, dar la hora.
            if (Parámetros.Count > ParámetrosMáximos) throw new ArgumentException($"La lista parámetros no puede tener más de {ParámetrosMáximos} parámetros.");
            DelegadoFunción = función;

        } // Externa>


        /// <summary>
        /// No importa el orden de los parámetros que pase el usuario de esta función mientras se pasen con el nombre y valor correcto.
        /// La función elige el orden correcto según la lista Parámetros. Se pueden pasar parámetros con nombre incorrecto y serán ignorados.
        /// </summary>
        /// <param name="funciones">Lista de funciones disponibles.</param>
        /// <param name="nombreFunción">Nombre de la función a ejecutar.</param>
        /// <param name="error">Mensaje de error en caso de fallo, null si todo sale bien.</param>
        /// <param name="parámetrosUsuario">
        /// Lista de parámetros proporcionados por el usuario: tupla (Nombre, Valor).
        /// El nombre se compara con Parámetros.Nombre (ignorando mayúsculas/minúsculas).
        /// </param>
        internal static string ObtenerResultado(List<Función> funciones, string nombreFunción, out 
            (string ParámetroConError, string Descripción) error, List<(string Nombre, string Valor)> parámetrosUsuario) {

            if (funciones == null) {
                error = ("", "La lista funciones no puede ser nula.");
                return null;
            } else if(string.IsNullOrEmpty(nombreFunción)) {
                error = ("", "El nombre de la función no puede ser vacío.");
                return null;
            }

            var función = funciones.FirstOrDefault(f => f.Nombre.Equals(nombreFunción, StringComparison.InvariantCultureIgnoreCase));
            if (función == null) {
                error = ("", $"El nombre de la función {nombreFunción} no se encuentra en la lista de las funciones disponibles.");  
                return null;
            } else {

                var valoresParámetros = new string[ParámetrosMáximos];
                var índiceParámetro = 0;

                foreach (var parámetro in función.Parámetros) {

                    var valorUsuario = parámetrosUsuario?.FirstOrDefault(nv => 
                        (nv.Nombre ?? "").Equals(parámetro.Nombre, StringComparison.InvariantCultureIgnoreCase)).Valor;
                    if (string.IsNullOrEmpty(valorUsuario)) {

                        if (parámetro.Requerido) {
                            error = (parámetro.Nombre, $"Falta el parámetro requerido {parámetro.Nombre} en la función {función.Nombre}.");
                            return null;
                        } // else: se ignora. valorParámetro[índiceParámetro] queda con null en esa posición.

                    } else {
                        valoresParámetros[índiceParámetro] = valorUsuario;
                    }

                    índiceParámetro++;

                }

                return función.DelegadoFunción(out error, valoresParámetros[0], valoresParámetros[1], valoresParámetros[2], valoresParámetros[3], 
                    valoresParámetros[4]);

            }

        } // ObtenerResultado>


        internal double EstimarTókenes() {

            var totalTókenesTextos = ((Nombre?.Length ?? 0) + (Descripción?.Length ?? 0))/ (double)CarácteresPorTokenTípicos;
            var totalTókenesParámetros = Parámetros.Sum(p => p.EstimarTókenes());
            return totalTókenesTextos + totalTókenesParámetros;

        } // EstimarTókenes>


        internal static double EstimarTókenes(List<Función> funciones) => funciones?.Sum(f => f.EstimarTókenes()) ?? 0;


    } // Función>


} // Frugalia>