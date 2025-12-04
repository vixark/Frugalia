using System;
using System.Collections.Generic;
using System.Linq;


namespace Frugalia {


    public sealed class Función {


        internal string Nombre { get; }

        internal string Descripción { get; }

        /// <summary>
        /// El orden de adición de los parámetros en esta lista es el orden en el que se usan en la función. Si algún parámetro no es requerido, 
        /// igual ocupa espacio en la lista de parámetros. En estos casos se llama la función con null en su valor sin sacar error.
        /// </summary>
        internal IReadOnlyList<Parámetro> Parámetros { get; }

        internal const int ParámetrosMáximos = 5;

        public delegate string FunciónDelegate(out (string ParámetroConError, string Descripción) error, string valor1 = null, string valor2 = null, 
            string valor3 = null, string valor4 = null, string valor5 = null);

        public FunciónDelegate DelegadoFunción { get; }


        public Función(string nombre, FunciónDelegate función, string descripción, List<Parámetro> parámetros) {

            Nombre = nombre.ToLowerInvariant();
            Descripción = descripción;
            Parámetros = parámetros?.ToList() ?? new List<Parámetro>(); // Copia de la lista para que no el que llamó la función no pueda modificar posteriormente la lista Parámetros al modificar parámetros que sería el mismo objeto si no se hiciera la copia. La lista de parámetros podría ser una lista vacía si la función no requiere parámetros, por ejemplo, dar la hora.
            if (Parámetros.Count > ParámetrosMáximos) throw new ArgumentException($"La lista parámetros no puede tener más de {ParámetrosMáximos} parámetros.");
            DelegadoFunción = función;

        } // Externa>


        /// <summary>
        /// No importa el orden de los parámetros que pase el usuario de esta función mientras se pasen con el nombre y valor correcto. 
        /// La función elige el orden correcto según la lista Parámetros. Se pueden pasar parámetros con nombre incorrecto y serán ignorados.
        /// </summary>
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

            var función = funciones.FirstOrDefault(f => f.Nombre == nombreFunción.ToLowerInvariant());
            if (función == null) {
                error = ("", $"El nombre de la función {nombreFunción} no se encuentra en la lista de las funciones disponibles.");  
                return null;
            } else {

                var valoresParámetros = new string[ParámetrosMáximos];
                var índiceParámetro = 0;

                foreach (var parámetro in función.Parámetros) {

                    var valorUsuario = parámetrosUsuario?.FirstOrDefault(nv => (nv.Nombre ?? "").ToLowerInvariant() == parámetro.Nombre).Valor;
                    if (valorUsuario == null) {

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


    } // Función>


} // Frugalia>