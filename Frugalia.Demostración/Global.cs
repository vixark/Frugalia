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
namespace Frugalia.Demostración;


internal static class Global {


    internal static readonly string Separador = new('_', 100);


    internal static void EscribirMultilínea(string mensaje, int espaciosMargen = 3) => Console.WriteLine(AgregarMargen(mensaje, espaciosMargen));


    internal static void EscribirSeparador(int espaciosMargen = 3) {
        
        EscribirGris(Separador, espaciosMargen);
        EscribirGris("", espaciosMargen);

    } // EscribirSeparador>


    internal static void EstablecerAltoVentana(int líneasVerticales = 60) {

        if (!OperatingSystem.IsWindows()) return;

        try {
            Console.WindowHeight = líneasVerticales; // Se quería también establecer la posición de la ventana en Windows, pero fue un lio que no se pudo solucionar porque sacaba error con la recarga activa del depurador.
        } catch {
            // Solo para mi computador, en otros es posible que no funcione. No importa.
        }

    } // EstablecerAltoVentana>


    internal static void DesplazarContenidoHaciaArriba(int líneasVisibles = 2) {

        if (!OperatingSystem.IsWindows()) return;

        if (Console.BufferHeight < Console.WindowHeight) Console.BufferHeight = Console.WindowHeight;
        var líneaCursor = Console.CursorTop;
        var topDeseado = líneaCursor - líneasVisibles;
        if (topDeseado < 0) topDeseado = 0;
        var topMáximo = Console.BufferHeight - Console.WindowHeight;
        if (topDeseado > topMáximo) topDeseado = topMáximo;
        Console.SetWindowPosition(0, topDeseado);

    } // DesplazarContenidoHaciaArriba>


    internal static void EscribirMensajes(string? instrucciónSistema, string? rellenoInstrucciónSistema, string? instrucción, string? respuesta, string? archivo) {

        Console.SetCursorPosition(0, Console.CursorTop);

        if (!string.IsNullOrEmpty(instrucciónSistema)) {
            EscribirSeparador();
            EscribirMultilíneaGrisOscuro($"Sistema: {instrucciónSistema}");
        }

        if (!string.IsNullOrEmpty(rellenoInstrucciónSistema)) {
            EscribirSeparador();
            EscribirMultilíneaGrisOscuro($"Relleno Sistema: {rellenoInstrucciónSistema}");
        }

        EscribirSeparador();
        EscribirMultilíneaGrisOscuro($"Usuario: {instrucción}");

        if (!string.IsNullOrEmpty(archivo)) {
            EscribirSeparador();
            EscribirMultilíneaGrisOscuro($"Archivo Usuario: {archivo}");
        }

        EscribirSeparador();
        EscribirMultilíneaGrisOscuro($"AI: {respuesta}");

    } // EscribirMensajes>


    internal static void Escribir(string mensaje, int espaciosMargen = 3) => Console.WriteLine($"{new string(' ', espaciosMargen)}{mensaje}");


    internal static int LeerNúmero(int espaciosMargen = 3) {

        var textoUsuario = Leer(espaciosMargen);
        int número = int.TryParse(textoUsuario, out var número2) ? número2 : 0;
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

        return resultado.TrimEnd();

    } // AgregarMargen>


    #region Escribir en Colores

    internal static void EscribirMagenta(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Escribir(mensaje, espaciosMargen);
    } // EscribirMagenta>


    internal static void EscribirNegro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Black;
        Escribir(mensaje, espaciosMargen);
    } // EscribirNegro>

    internal static void EscribirAzul(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Blue;
        Escribir(mensaje, espaciosMargen);
    } // EscribirAzul>


    internal static void EscribirCian(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        Escribir(mensaje, espaciosMargen);
    } // EscribirCian>


    internal static void EscribirGris(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Gray;
        Escribir(mensaje, espaciosMargen);
    } // EscribirGris>


    internal static void EscribirVerde(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Green;
        Escribir(mensaje, espaciosMargen);
    } // EscribirVerde>


    internal static void EscribirRojo(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Red;
        Escribir(mensaje, espaciosMargen);
    } // EscribirRojo>


    internal static void EscribirAmarillo(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Escribir(mensaje, espaciosMargen);
    } // EscribirAmarillo>


    internal static void EscribirBlanco(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.White;
        Escribir(mensaje, espaciosMargen);
    } // EscribirBlanco>


    internal static void EscribirAzulOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        Escribir(mensaje, espaciosMargen);
    } // EscribirAzulOscuro>


    internal static void EscribirCianOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        Escribir(mensaje, espaciosMargen);
    } // EscribirCianOscuro>


    internal static void EscribirGrisOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Escribir(mensaje, espaciosMargen);
    } // EscribirGrisOscuro>


    internal static void EscribirVerdeOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        Escribir(mensaje, espaciosMargen);
    } // EscribirVerdeOscuro>


    internal static void EscribirMagentaOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        Escribir(mensaje, espaciosMargen);
    } // EscribirMagentaOscuro>


    internal static void EscribirRojoOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Escribir(mensaje, espaciosMargen);
    } // EscribirRojoOscuro>


    internal static void EscribirMultilíneaMagenta(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Magenta;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaMagenta>


    internal static void EscribirMultilíneaNegro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Black;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaNegro>


    internal static void EscribirMultilíneaAzul(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Blue;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaAzul>


    internal static void EscribirMultilíneaCian(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Cyan;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaCian>


    internal static void EscribirMultilíneaGris(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Gray;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaGris>


    internal static void EscribirMultilíneaVerde(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Green;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaVerde>


    internal static void EscribirMultilíneaRojo(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Red;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaRojo>


    internal static void EscribirMultilíneaAmarillo(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.Yellow;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaAmarillo>


    internal static void EscribirMultilíneaBlanco(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.White;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaBlanco>


    internal static void EscribirMultilíneaAzulOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkBlue;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaAzulOscuro>

    internal static void EscribirMultilíneaCianOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkCyan;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaCianOscuro>


    internal static void EscribirMultilíneaGrisOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkGray;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaGrisOscuro>


    internal static void EscribirMultilíneaVerdeOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkGreen;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaVerdeOscuro>


    internal static void EscribirMultilíneaMagentaOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkMagenta;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaMagentaOscuro>


    internal static void EscribirMultilíneaRojoOscuro(string mensaje, int espaciosMargen = 3) {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        EscribirMultilínea(mensaje, espaciosMargen);
    } // EscribirMultilíneaRojoOscuro>

    #endregion


} // Global>