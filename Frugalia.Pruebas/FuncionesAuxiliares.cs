using System.Reflection;
namespace Frugalia.Pruebas;


public class FuncionesAuxiliares {


    [Fact]
    public void AgregarSumando_CreaDiccionarioCuandoEsNulo() {

        var tokens = CrearTókenes("gpt-5-mini", lote: false, entradaTotal: 10, salidaTotal: 5, salidaRazonamiento: 2, entradaCaché: 1);
        var resultado = Global.AgregarSumando(null, tokens);
        var clave = ObtenerClave(tokens);

        Assert.NotNull(resultado);
        Assert.Single(resultado);
        Assert.Equal(tokens, resultado[clave]);

    } // AgregarSumando_CreaDiccionarioCuandoEsNulo>


    [Fact]
    public void AgregarSumando_AgregaNuevaClaveEnDiccionarioExistente() {

        var diccionario = new Dictionary<string, Tókenes>();
        var tokens = CrearTókenes("gpt-5-mini", lote: false, entradaTotal: 20, salidaTotal: 10, salidaRazonamiento: 4, entradaCaché: 2, escrituraManualCaché: 1);
        var clave = ObtenerClave(tokens);
        var resultado = diccionario.AgregarSumando(tokens);

        Assert.Same(diccionario, resultado);
        Assert.Single(diccionario);
        Assert.Equal(tokens, resultado[clave]);

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

        var ctor = typeof(Tókenes).GetConstructor(
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            new[] { typeof(string), typeof(bool), typeof(int?), typeof(int?), typeof(int?), typeof(int?), typeof(int?), typeof(int?) },
            modifiers: null);

        if (ctor == null) {
            throw new InvalidOperationException("No se encontró el constructor esperado de Tókenes.");
        }

        return (Tókenes)ctor.Invoke(new object?[] {
            modelo,
            lote,
            entradaTotal,
            salidaTotal,
            salidaRazonamiento,
            entradaCaché,
            escrituraManualCaché,
            minutosEscrituraManualCaché
        });

    } // CrearTókenes>


    private static int ObtenerPropiedadEntera(Tókenes tokens, string nombrePropiedad) {

        var propiedad = typeof(Tókenes).GetProperty(nombrePropiedad, BindingFlags.NonPublic | BindingFlags.Instance);

        if (propiedad == null) {
            throw new InvalidOperationException($"No se encontró la propiedad interna {nombrePropiedad} en Tókenes.");
        }

        return (int)(propiedad.GetValue(tokens) ?? 0);

    } // ObtenerPropiedadEntera>


    private static string ObtenerClave(Tókenes tokens) {

        var propiedad = typeof(Tókenes).GetProperty("Clave", BindingFlags.NonPublic | BindingFlags.Instance);

        if (propiedad == null) {
            throw new InvalidOperationException("No se encontró la clave de Tókenes.");
        }

        return (string)(propiedad.GetValue(tokens) ?? string.Empty);

    } // ObtenerClave>


} // FuncionesAuxiliares>
