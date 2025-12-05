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


public class Globales {


    #region AgregarSumando()


    [Fact]
    public void AgregarSumando_CreaDiccionarioCuandoEsNulo() {

        var tókenes = CrearTókenes("gpt-5-mini", lote: false, entradaTotal: 10, salidaTotal: 5, salidaRazonamiento: 2, entradaCaché: 1);
        var resultado = Global.AgregarSumando(null, tókenes);
        var clave = ObtenerClave(tókenes);

        Assert.NotNull(resultado);
        Assert.Single(resultado);
        Assert.Equal(tókenes, resultado[clave]);

    } // AgregarSumando_CreaDiccionarioCuandoEsNulo>


    [Fact]
    public void AgregarSumando_AgregaNuevaClaveEnDiccionarioExistente() {

        var diccionario = new Dictionary<string, Tókenes>();
        var tókenes = CrearTókenes("gpt-5-mini", lote: false, entradaTotal: 20, salidaTotal: 10, salidaRazonamiento: 4, entradaCaché: 2, escrituraManualCaché: 1);
        var clave = ObtenerClave(tókenes);
        var resultado = diccionario.AgregarSumando(tókenes);

        Assert.Same(diccionario, resultado);
        Assert.Single(diccionario);
        Assert.Equal(tókenes, resultado[clave]);

    } // AgregarSumando_AgregaNuevaClaveEnDiccionarioExistente>


    [Fact]
    public void AgregarSumando_SumaTokensCuandoExisteLaClave() {

        var diccionario = new Dictionary<string, Tókenes>();
        var inicial = CrearTókenes("gpt-5-mini", lote: true, entradaTotal: 100, salidaTotal: 50, salidaRazonamiento: 10, entradaCaché: 20, escrituraManualCaché: 5);
        var adicional = CrearTókenes("gpt-5-mini", lote: true, entradaTotal: 50, salidaTotal: 20, salidaRazonamiento: 5, entradaCaché: 5, escrituraManualCaché: 3);

        diccionario.AgregarSumando(inicial);
        diccionario.AgregarSumando(adicional);

        var combinado = diccionario[ObtenerClave(inicial)];

        Assert.Equal(150, ObtenerPropiedadEntera(combinado, "EntradaTotal"));
        Assert.Equal(70, ObtenerPropiedadEntera(combinado, "SalidaTotal"));
        Assert.Equal(15, ObtenerPropiedadEntera(combinado, "SalidaRazonamiento"));
        Assert.Equal(25, ObtenerPropiedadEntera(combinado, "EntradaCaché"));
        Assert.Equal(8, ObtenerPropiedadEntera(combinado, "EscrituraManualCaché"));

    } // AgregarSumando_SumaTokensCuandoExisteLaClave>


    private static Tókenes CrearTókenes(string modelo, bool lote, int entradaTotal, int salidaTotal, int salidaRazonamiento, int entradaCaché, int escrituraManualCaché = 0, int minutosEscrituraManualCaché = 0) {

        var constructor = typeof(Tókenes).GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, binder: null, [typeof(string), typeof(bool), typeof(int?), 
            typeof(int?), typeof(int?), typeof(int?), typeof(int?), typeof(int?)], modifiers: null);
        return constructor == null ? throw new InvalidOperationException("No se encontró el constructor esperado de Tókenes.")
            : (Tókenes)constructor.Invoke([modelo, lote, entradaTotal, salidaTotal, salidaRazonamiento, entradaCaché, escrituraManualCaché, minutosEscrituraManualCaché ]);

    } // CrearTókenes>


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


} // Globales>
