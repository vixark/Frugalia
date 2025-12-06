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

using System.Text;
namespace Frugalia.Demo;


internal class Global {


    internal static void EscribirMultilínea(string mensaje, int espaciosMargen = 3) => Console.WriteLine(AgregarMargen(mensaje, espaciosMargen));


    internal static void Escribir(string mensaje, int espaciosMargen = 3) => Console.WriteLine($"{new string(' ', espaciosMargen)}{mensaje}");


    internal static int LeerNúmero(int espaciosMargen = 3) {

        var textoUsuario = Leer(espaciosMargen);
        int número = int.TryParse(textoUsuario, out var número2) ? número2 : 1;
        return número;

    } // LeerNúmero>


    internal static string? Leer(int espaciosMargen = 3) {

        Console.SetCursorPosition(espaciosMargen, Console.CursorTop);
        return Console.ReadLine();

    } // Leer>


    internal static string AgregarMargen(string texto, int espaciosMargen = 3, int maxLongitud = 100) {

        ArgumentNullException.ThrowIfNull(texto);
        if (espaciosMargen < 0) throw new ArgumentOutOfRangeException(nameof(espaciosMargen), "Debe ser >= 0.");
        if (maxLongitud <= 0) throw new ArgumentOutOfRangeException(nameof(maxLongitud), "Debe ser > 0.");

        var prefijo = new string(' ', espaciosMargen);
        var sb = new StringBuilder(texto.Length + (texto.Length / maxLongitud) * espaciosMargen);

        var lineas = texto.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n'); // Normaliza saltos de línea y separa por líneas

        foreach (var linea in lineas) {

            if (linea.Length == 0) {
                sb.AppendLine(prefijo); // línea vacía con prefijo
                continue;
            }

            var inicio = 0;
            while (inicio < linea.Length) {
                var longitud = Math.Min(maxLongitud, linea.Length - inicio);

                // Evita partir palabras: busca el último espacio antes de maxLongitud
                if (longitud == maxLongitud && inicio + longitud < linea.Length) {
                    var sub = linea.Substring(inicio, longitud);
                    var últimoEspacio = sub.LastIndexOf(' ');
                    if (últimoEspacio > 0) {
                        longitud = últimoEspacio;
                    }
                }

                sb.Append(prefijo);
                sb.AppendLine(linea.Substring(inicio, longitud).TrimEnd());
                inicio += longitud;

                // Salta espacios al inicio de la siguiente línea
                while (inicio < linea.Length && linea[inicio] == ' ') {
                    inicio++;
                }
            }

        }

        var resultado = sb.ToString().Replace("\n", "\r\n"); // Convertir a CRLF, respetando .editorconfig end_of_line = crlf

        return resultado;

    } // AgregarMargen>


} // Global>