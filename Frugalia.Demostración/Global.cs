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
        
        EscribirGrisOscuro(Separador, espaciosMargen);
        EscribirGrisOscuro("", espaciosMargen);

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
        try { Console.SetWindowPosition(0, topDeseado);
        } catch (Exception) { } // En algunos casos con ciertas posiciones de la ventana, saca excepción. No pasa nada si no se puede desplazar el contenido.
        
    } // DesplazarContenidoHaciaArriba>


    internal static void EscribirMensajes(string? instrucciónSistema, string? rellenoInstrucciónSistema, string? instrucción, string? respuesta, string? archivo) {

        if (!string.IsNullOrEmpty(instrucciónSistema)) {
            Escribir("");
            EscribirMultilíneaGris($"Sistema: {instrucciónSistema}");
        }

        if (!string.IsNullOrEmpty(rellenoInstrucciónSistema)) {
            EscribirSeparador();
            EscribirMultilíneaGris($"Relleno Sistema: {rellenoInstrucciónSistema}");
        }

        if (!string.IsNullOrEmpty(instrucción)) {
            EscribirSeparador();
            EscribirMultilíneaGris($"Usuario: {instrucción}");
        }

        if (!string.IsNullOrEmpty(archivo)) {
            EscribirSeparador();
            EscribirMultilíneaGris($"Archivo Usuario: {archivo}");
        }

        if (!string.IsNullOrEmpty(respuesta)) {
            EscribirSeparador();
            EscribirMultilíneaGris($"Asistente IA: {respuesta}");
        }

    } // EscribirMensajes>


    internal static void EscribirMensajes(string? instrucciónSistema, string? rellenoInstrucciónSistema, List<(TipoMensaje Tipo, string Mensaje)> mensajes) {

        if (!string.IsNullOrEmpty(instrucciónSistema)) {
            EscribirSeparador();
            EscribirMultilíneaGrisOscuro($"Sistema: {instrucciónSistema}");
        }

        if (!string.IsNullOrEmpty(rellenoInstrucciónSistema)) {
            EscribirSeparador();
            EscribirMultilíneaGrisOscuro($"Relleno Sistema: {rellenoInstrucciónSistema}");
        }

        foreach (var (Tipo, Mensaje) in mensajes) {

            EscribirSeparador();
            switch (Tipo) {
            case TipoMensaje.Todos:
                throw new Exception("No se esperaba TipoMensaje = Todos en EscribirMensajes()"); 
            case TipoMensaje.Usuario:
                EscribirMultilíneaGrisOscuro($"Usuario: {Mensaje}");
                break;
            case TipoMensaje.AsistenteIA:
                EscribirMultilíneaGrisOscuro($"AI: {Mensaje}");
                break;
            default:
                throw new Exception("TipoMensaje no reconocido.");
            }           

        }

    } // EscribirMensajes>


    internal static string EscribirTítuloYTexto(string título, string texto, ConsoleColor colorTítulo = ConsoleColor.DarkGreen, 
        ConsoleColor colorMensaje = ConsoleColor.DarkCyan, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) {

        Escribir("");
        EscribirMultilíneaConColor(colorTítulo, $"{título}:", espaciosMargen, agregarLíneasEnBlancoAlrededor);
        Escribir("");
        EscribirMultilíneaConColor(colorMensaje, texto, espaciosMargen, agregarLíneasEnBlancoAlrededor);
        Escribir("");
        return título + Environment.NewLine + texto;

    } // EscribirTítuloYTexto>


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

    private static void EscribirConColor(ConsoleColor color, string mensaje, int espaciosMargen) {

        Console.SetCursorPosition(0, Console.CursorTop);
        Console.ForegroundColor = color;
        Escribir(mensaje, espaciosMargen);

    } // EscribirConColor>


    private static void EscribirMultilíneaConColor(ConsoleColor color, string mensaje, int espaciosMargen, bool agregarLíneasEnBlancoAlrededor) {

        Console.SetCursorPosition(0, Console.CursorTop);

        Console.ForegroundColor = color;
        if (agregarLíneasEnBlancoAlrededor) Escribir("");
        EscribirMultilínea(mensaje, espaciosMargen);
        if (agregarLíneasEnBlancoAlrededor) Escribir("");

    } // EscribirMultilíneaConColor>


    internal static void EscribirMagenta(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Magenta, mensaje, espaciosMargen);

    internal static void EscribirNegro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Black, mensaje, espaciosMargen);

    internal static void EscribirAzul(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Blue, mensaje, espaciosMargen);

    internal static void EscribirCian(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Cyan, mensaje, espaciosMargen);

    internal static void EscribirGris(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Gray, mensaje, espaciosMargen);

    internal static void EscribirVerde(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Green, mensaje, espaciosMargen);

    internal static void EscribirRojo(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Red, mensaje, espaciosMargen);

    internal static void EscribirAmarillo(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.Yellow, mensaje, espaciosMargen);

    internal static void EscribirBlanco(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.White, mensaje, espaciosMargen);

    internal static void EscribirAzulOscuro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.DarkBlue, mensaje, espaciosMargen);

    internal static void EscribirCianOscuro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.DarkCyan, mensaje, espaciosMargen);

    internal static void EscribirGrisOscuro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.DarkGray, mensaje, espaciosMargen);

    internal static void EscribirVerdeOscuro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.DarkGreen, mensaje, espaciosMargen);

    internal static void EscribirMagentaOscuro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.DarkMagenta, mensaje, espaciosMargen);

    internal static void EscribirRojoOscuro(string mensaje, int espaciosMargen = 3) => EscribirConColor(ConsoleColor.DarkRed, mensaje, espaciosMargen);

    internal static void EscribirMultilíneaMagenta(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Magenta, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaNegro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Black, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaAzul(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Blue, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaCian(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Cyan, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaGris(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Gray, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaVerde(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Green, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaRojo(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.Red, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaAmarillo(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false)
        => EscribirMultilíneaConColor(ConsoleColor.Yellow, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaBlanco(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.White, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaAzulOscuro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.DarkBlue, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaCianOscuro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.DarkCyan, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaGrisOscuro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.DarkGray, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaVerdeOscuro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false)
        => EscribirMultilíneaConColor(ConsoleColor.DarkGreen, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaMagentaOscuro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false)
        => EscribirMultilíneaConColor(ConsoleColor.DarkMagenta, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    internal static void EscribirMultilíneaRojoOscuro(string mensaje, int espaciosMargen = 3, bool agregarLíneasEnBlancoAlrededor = false) 
        => EscribirMultilíneaConColor(ConsoleColor.DarkRed, mensaje, espaciosMargen, agregarLíneasEnBlancoAlrededor);

    #endregion


} // Global>