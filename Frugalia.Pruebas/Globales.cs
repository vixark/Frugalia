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

using System.Reflection;
namespace Frugalia.Pruebas;
using static Frugalia.GlobalFrugalia;


public class Globales {


    #region AgregarSumando()

    [Fact]
    public void AgregarSumando_CreaDiccionarioCuandoEsNulo() {

        var tókenes = new Tókenes(Modelos["gpt-5-mini"], lote: false, 10, 5, 2, 1, 0, 0);
        Dictionary<string, Tókenes>? diccionarioNulo = null;
        AgregarSumandoPosibleNulo(ref diccionarioNulo, tókenes);
        var clave = ObtenerClave(tókenes);

        Assert.NotNull(diccionarioNulo);
        Assert.Single(diccionarioNulo);
        Assert.Equal(tókenes, diccionarioNulo[clave]);

    } // AgregarSumando_CreaDiccionarioCuandoEsNulo>


    [Fact]
    public void AgregarSumando_SumaTokensCuandoExisteLaClave() {

        var diccionario = new Dictionary<string, Tókenes>();
        var inicial = new Tókenes(Modelos["gpt-5-mini"], lote: true, 100, 50, 10, 20, 5, 0);
        var adicional = new Tókenes(Modelos["gpt-5-mini"], lote: true, 50, 20, 5, 5, 3, 0);

        diccionario.AgregarSumando(inicial);
        diccionario.AgregarSumando(adicional);

        var combinado = diccionario[ObtenerClave(inicial)];

        Assert.Equal(150, ObtenerPropiedadEntera(combinado, "EntradaTotal"));
        Assert.Equal(70, ObtenerPropiedadEntera(combinado, "SalidaTotal"));
        Assert.Equal(15, ObtenerPropiedadEntera(combinado, "SalidaRazonamiento"));
        Assert.Equal(25, ObtenerPropiedadEntera(combinado, "EntradaCaché"));
        Assert.Equal(8, ObtenerPropiedadEntera(combinado, "EscrituraManualCaché"));

    } // AgregarSumando_SumaTokensCuandoExisteLaClave>


    private static int ObtenerPropiedadEntera(Tókenes tókenes, string nombrePropiedad) {

        var propiedad = typeof(Tókenes).GetProperty(nombrePropiedad, BindingFlags.Public | BindingFlags.Instance);
        return propiedad == null ? throw new InvalidOperationException($"No se encontró la propiedad interna {nombrePropiedad} en Tókenes.")
            : (int)(propiedad.GetValue(tókenes) ?? 0);

    } // ObtenerPropiedadEntera>


    private static string ObtenerClave(Tókenes tókenes) {

        var propiedad = typeof(Tókenes).GetProperty("Clave", BindingFlags.Public | BindingFlags.Instance);
        return propiedad == null ? throw new InvalidOperationException("No se encontró la clave de Tókenes.")
            : (string)(propiedad.GetValue(tókenes) ?? string.Empty);

    } // ObtenerClave>

    #endregion


    #region Reemplazar()

    [Fact]
    public void Reemplazar_Ordinal_ReemplazaTodasLasCoincidencias() {

        var entrada = "Hola mundo. Mundo grande. mundo pequeño.";
        var salida = entrada.Reemplazar("mundo", "planeta", StringComparison.Ordinal); // Solo minúsculas exactas.
        Assert.Equal("Hola planeta. Mundo grande. planeta pequeño.", salida);

    } // Reemplazar_Ordinal_ReemplazaTodasLasCoincidencias>


    [Fact]
    public void Reemplazar_CaseInsensitive_ReemplazaTodasLasVariantes() {

        var entrada = "ABC abc Abc aBc";
        var salida = entrada.Reemplazar("abc", "X", StringComparison.OrdinalIgnoreCase);
        Assert.Equal("X X X X", salida);

    } // Reemplazar_CaseInsensitive_ReemplazaTodasLasVariantes>


    [Fact]
    public void Reemplazar_SinCoincidencias_RegresaIgual() {

        var entrada = "Sin cambios aquí";
        var salida = entrada.Reemplazar("zzz", "x", StringComparison.Ordinal);
        Assert.Equal(entrada, salida);

    } // Reemplazar_SinCoincidencias_RegresaIgual>


    [Fact]
    public void Reemplazar_AlInicioYFinal_ReemplazaCorrectamente() {

        var entrada = "foo bar foo";
        var salida = entrada.Reemplazar("foo", "baz", StringComparison.Ordinal);
        Assert.Equal("baz bar baz", salida);

    } // Reemplazar_AlInicioYFinal_ReemplazaCorrectamente>


    [Fact]
    public void Reemplazar_MultiplesCoincidenciasContiguas_Correcto() {

        var entrada = "aaaa";
        var salida = entrada.Reemplazar("aa", "b", StringComparison.Ordinal);
        Assert.Equal("bb", salida);

    } // Reemplazar_MultiplesCoincidenciasContiguas_Correcto>


    [Fact]
    public void Reemplazar_ValorAnteriorVacio_RegresaTextoOriginal() {

        var entrada = "texto";
        var salida = entrada.Reemplazar("", "x", StringComparison.Ordinal);
        Assert.Equal(entrada, salida);

    } // Reemplazar_ValorAnteriorVacio_RegresaTextoOriginal>


    [Fact]
    public void Reemplazar_TextoVacio_RegresaVacio() {

        var entrada = "";
        var salida = entrada.Reemplazar("a", "b", StringComparison.Ordinal);
        Assert.Equal("", salida);

    } // Reemplazar_TextoVacio_RegresaVacio>


    [Fact]
    public void Reemplazar_TextoNull_RegresaNull() {

        string? entrada = null;
        var salida = entrada.Reemplazar("a", "b", StringComparison.Ordinal);
        Assert.Null(salida);

    } // Reemplazar_TextoNull_RegresaNull>


    [Fact]
    public void Reemplazar_CoincidenciasSeparadas_PreservaRestoDelTexto() {

        var entrada = "123-abc-456-abc-789";
        var salida = entrada.Reemplazar("abc", "X", StringComparison.Ordinal);
        Assert.Equal("123-X-456-X-789", salida);

    } // Reemplazar_CoincidenciasSeparadas_PreservaRestoDelTexto>

    #endregion


    #region CompilarElementosPermitidos()

    private enum EstadoPrueba {
        Ninguno = 0,
        Uno = 1,
        Dos = 2,
        Tres = 3
    }


    [Fact]
    public void CompilarElementosPermitidos_AmbasListasNulas_DevuelveTodosLosValoresDelEnum() {

        List<EstadoPrueba>? permitidos = null;
        List<EstadoPrueba>? noPermitidos = null;

        var resultado = General.CompilarElementosPermitidos(permitidos, noPermitidos);

        var todos = Enum.GetValues<EstadoPrueba>();
        Assert.Equal(todos.Length, resultado.Count);
        foreach (var valor in todos) {
            Assert.Contains(valor, resultado);
        }

    } // CompilarElementosPermitidos_AmbasListasNulas_DevuelveTodosLosValoresDelEnum>


    [Fact]
    public void CompilarElementosPermitidos_SoloPermitidos_DevuelveListaSinCambios() {

        var permitidos = new List<EstadoPrueba> { EstadoPrueba.Uno, EstadoPrueba.Tres };
        List<EstadoPrueba>? noPermitidos = null;

        var resultado = General.CompilarElementosPermitidos(permitidos, noPermitidos);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(EstadoPrueba.Uno, resultado);
        Assert.Contains(EstadoPrueba.Tres, resultado);
        Assert.DoesNotContain(EstadoPrueba.Dos, resultado);

    } // CompilarElementosPermitidos_SoloPermitidos_DevuelveListaSinCambios>


    [Fact]
    public void CompilarElementosPermitidos_SoloNoPermitidos_ExcluyeNoPermitidosDelEnum() {

        List<EstadoPrueba>? permitidos = null;
        var noPermitidos = new List<EstadoPrueba> { EstadoPrueba.Dos, EstadoPrueba.Tres };

        var resultado = General.CompilarElementosPermitidos(permitidos, noPermitidos);

        Assert.DoesNotContain(EstadoPrueba.Dos, resultado);
        Assert.DoesNotContain(EstadoPrueba.Tres, resultado);
        Assert.Contains(EstadoPrueba.Ninguno, resultado);
        Assert.Contains(EstadoPrueba.Uno, resultado);

        var todos = Enum.GetValues<EstadoPrueba>();
        Assert.Equal(todos.Length - noPermitidos.Count, resultado.Count);

    } // CompilarElementosPermitidos_SoloNoPermitidos_ExcluyeNoPermitidosDelEnum>


    [Fact]
    public void CompilarElementosPermitidos_PermitidosYNoPermitidosSinInterseccion_DevuelveSoloPermitidos() {

        var permitidos = new List<EstadoPrueba> { EstadoPrueba.Uno, EstadoPrueba.Dos };
        var noPermitidos = new List<EstadoPrueba> { EstadoPrueba.Tres };

        var resultado = General.CompilarElementosPermitidos(permitidos, noPermitidos);

        Assert.Equal(2, resultado.Count);
        Assert.Contains(EstadoPrueba.Uno, resultado);
        Assert.Contains(EstadoPrueba.Dos, resultado);
        Assert.DoesNotContain(EstadoPrueba.Tres, resultado);

    } // CompilarElementosPermitidos_PermitidosYNoPermitidosSinInterseccion_DevuelveSoloPermitidos>


    [Fact]
    public void CompilarElementosPermitidos_ValorDuplicadoEnPermitidos_LanzaExcepcion() {

        var permitidos = new List<EstadoPrueba> { EstadoPrueba.Uno, EstadoPrueba.Uno };
        List<EstadoPrueba>? noPermitidos = null;

        var excepción = Assert.Throws<Exception>(() => General.CompilarElementosPermitidos(permitidos, noPermitidos));
        Assert.Contains("Valor duplicado", excepción.Message, StringComparison.OrdinalIgnoreCase);

    } // CompilarElementosPermitidos_ValorDuplicadoEnPermitidos_LanzaExcepcion>


    [Fact]
    public void CompilarElementosPermitidos_ValorDuplicadoEnNoPermitidos_LanzaExcepcion() {

        List<EstadoPrueba>? permitidos = null;
        var noPermitidos = new List<EstadoPrueba> { EstadoPrueba.Dos, EstadoPrueba.Dos };

        var excepción = Assert.Throws<Exception>(() => General.CompilarElementosPermitidos(permitidos, noPermitidos));
        Assert.Contains("Valor duplicado", excepción.Message, StringComparison.OrdinalIgnoreCase);

    } // CompilarElementosPermitidos_ValorDuplicadoEnNoPermitidos_LanzaExcepcion>


    [Fact]
    public void CompilarElementosPermitidos_ValorEnPermitidosYNoPermitidos_LanzaExcepcion() {

        var permitidos = new List<EstadoPrueba> { EstadoPrueba.Uno, EstadoPrueba.Dos };
        var noPermitidos = new List<EstadoPrueba> { EstadoPrueba.Dos, EstadoPrueba.Tres };

        var excepción = Assert.Throws<Exception>(() => General.CompilarElementosPermitidos(permitidos, noPermitidos));
        Assert.Contains("Incoherencia en configuración", excepción.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(nameof(EstadoPrueba.Dos), excepción.Message, StringComparison.OrdinalIgnoreCase);

    } // CompilarElementosPermitidos_ValorEnPermitidosYNoPermitidos_LanzaExcepcion>


    [Fact]
    public void CompilarElementosPermitidos_PermitidosVaciosYNoPermitidosNulos_DevuelveListaVacia() {

        var permitidos = new List<EstadoPrueba>();
        List<EstadoPrueba>? noPermitidos = null;

        var resultado = General.CompilarElementosPermitidos(permitidos, noPermitidos);

        Assert.NotNull(resultado);
        Assert.Empty(resultado);

    } // CompilarElementosPermitidos_PermitidosVaciosYNoPermitidosNulos_DevuelveListaVacia>

    #endregion


} // Globales>
