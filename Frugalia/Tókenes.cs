using System;
using System.Collections.Generic;
using static Frugalia.Global;


namespace Frugalia {


    public readonly struct Tókenes {


        internal const int FactorPrecioFuturo = 1; // Factor de seguridad que incrementa los precios de los tókenes para proyecciones de presupuesto en el futuro. Por el momento en 2025 no se aplica recargo a precio futuro porque a mediano plazo los modelos pequeños y medianos están fuertemente presionados por modelos código abierto cada vez mejores lo que limita su capacidad de subir precios sin perder usuarios. Aunque los medianos menos, de todas maneras para simplificar se deja 1 para ambos. Si se quisiera ser más estricto se podría poner pequeños x1-x1.5 y medianos x1.5-x2. Los modelos grandes, en cambio, ya se ofrecen con márgenes altos y difícilmente aumenten más sus tarifas. En conjunto ante tanta incertidumbre una simplificación razonable es asumir un factor de 1 para todos los modelos.

        internal int EntradaTotal { get; }

        internal int SalidaTotal { get; }

        internal int SalidaRazonamiento { get; }

        internal int EntradaCaché { get; }

        internal int SalidaNoRazonamiento { get; }

        internal int EntradaNoCaché { get; }

        internal bool Lote { get; }

        internal string NombreModelo { get; }

        internal int EscrituraManualCaché { get; }

        /// <summary>
        /// Para Claude solo es válido 0, 5 o 60. Para Gemini puede ser cualquier valor entero.
        /// </summary>
        internal int MinutosEscrituraManualCaché { get; }

        /// <summary>
        /// Clave que identifica un grupo de tókenes sumables.
        /// </summary>
        internal string Clave => $"{NombreModelo}§{Lote}§{MinutosEscrituraManualCaché}";


        internal Tókenes(string nombreModelo, bool lote, int? entradaTotal, int? salidaTotal, int? salidaRazonamiento, int? entradaCaché,
            int? escrituraManualCaché, int? minutosEscrituraManualCaché) {

            MinutosEscrituraManualCaché = minutosEscrituraManualCaché ?? 0;
            var modelo = Modelo.ObtenerModelo(nombreModelo);
            var minutosCachéVálidosClaude = new List<int>() { 0, 5, 60 };
            if (modelo.Familia == Familia.Claude && !minutosCachéVálidosClaude.Contains(MinutosEscrituraManualCaché))
                throw new Exception("No se pueden usar MinutosEscrituraManualCaché diferentes de 0, 5 o 60 para modelos de tipo Claude.");

            Lote = lote;
            NombreModelo = nombreModelo;
            EntradaTotal = entradaTotal ?? 0;
            SalidaTotal = salidaTotal ?? 0;
            SalidaRazonamiento = salidaRazonamiento ?? 0;
            EntradaCaché = entradaCaché ?? 0;
            EscrituraManualCaché = escrituraManualCaché ?? 0;
            SalidaNoRazonamiento = SalidaTotal - SalidaRazonamiento;
            EntradaNoCaché = EntradaTotal - EntradaCaché;

        } // Tókenes>


        public static Tókenes operator +(Tókenes tókenes1, Tókenes tókenes2) {

            if (tókenes1.NombreModelo != tókenes2.NombreModelo) throw new Exception("No se pueden sumar tókenes usados de diferente modelo.");
            if (tókenes1.Lote != tókenes2.Lote) throw new Exception("No se pueden sumar tókenes de lote y no lote.");
            if (tókenes1.MinutosEscrituraManualCaché != tókenes2.MinutosEscrituraManualCaché)
                throw new Exception("No se pueden sumar tókenes que tuvieron escritura manual en caché con diferente duración de almacenamiento en caché.");

            return new Tókenes(
                tókenes1.NombreModelo,
                tókenes2.Lote,
                tókenes1.EntradaTotal + tókenes2.EntradaTotal,
                tókenes1.SalidaTotal + tókenes2.SalidaTotal,
                tókenes1.SalidaRazonamiento + tókenes2.SalidaRazonamiento,
                tókenes1.EntradaCaché + tókenes2.EntradaCaché,
                tókenes1.EscrituraManualCaché + tókenes2.EscrituraManualCaché,
                tókenes1.MinutosEscrituraManualCaché
            );

        } // +>


        public override string ToString() => $"{NombreModelo}{(Lote ? "-lote" : "")}: ENC={EntradaNoCaché} EC={EntradaCaché} SNR={SalidaNoRazonamiento} " +
            $"SR={SalidaRazonamiento} EMC={EscrituraManualCaché} MEMC={MinutosEscrituraManualCaché}";


        internal static decimal CalcularCostoMonedaLocalTókenes(int tókenes, decimal costoPorMillón, decimal tasaCambioUsd)
            => tókenes * (FactorPrecioFuturo * costoPorMillón / (decimal)1E6) * tasaCambioUsd;


        internal static string FormatearMoneda(decimal pesos) => $"{pesos:#,0.##} $";


        public static string ObtenerTextoCostoTókenes(Dictionary<string, Tókenes> listaTókenes, decimal tasaCambioUsd) {

            var totalTodos = 0.0m;
            var texto = "";
            foreach (var kv in listaTókenes) {

                var tókenes = kv.Value;
                if (tókenes.Lote) Suspender(); // Verificar funcionamiento.
                var clave = tókenes.Clave;
                var nombreModelo = clave.Substring(0, clave.IndexOf("§"));
                var modelo = Modelo.ObtenerModelo(nombreModelo);
                var pesosNoCaché = CalcularCostoMonedaLocalTókenes(tókenes.EntradaNoCaché, modelo.PrecioEntradaNoCaché, tasaCambioUsd)
                    * modelo.FracciónDescuentoEntradaYSalidaPorLote;
                var pesosCaché = CalcularCostoMonedaLocalTókenes(tókenes.EntradaCaché, modelo.PrecioEntradaCaché, tasaCambioUsd)
                    * modelo.FracciónDescuentoLecturaCachePorLote;
                var pesosNoRazonamiento = CalcularCostoMonedaLocalTókenes(tókenes.SalidaNoRazonamiento, modelo.PrecioSalidaNoRazonamiento, tasaCambioUsd)
                    * modelo.FracciónDescuentoEntradaYSalidaPorLote;
                var pesosRazonamiento = CalcularCostoMonedaLocalTókenes(tókenes.SalidaRazonamiento, modelo.PrecioSalidaRazonamiento, tasaCambioUsd)
                    * modelo.FracciónDescuentoEntradaYSalidaPorLote;

                var pesosEscrituraManualCaché = 0m;

                switch (modelo.Familia) {
                case Familia.GPT:
                    break; // 0.
                case Familia.Claude:

                    Suspender(); // Verificar funcionamiento.
                    var costoEscrituraCachéClaude = 0m;
                    if (modelo.PrecioEscrituraManualCachéRefrescablePor5Minutos == null || modelo.PrecioEscrituraManualCachéRefrescablePor60Minutos == null)
                        throw new Exception("No se esperaba que un modelo Claude tenga PrecioEscrituraManualCachéRefrescablePor5Minutos o Por60Minutos vacíos.");
                    if (tókenes.MinutosEscrituraManualCaché == 0) {
                        // 0.
                    } else if (tókenes.MinutosEscrituraManualCaché == 5) {
                        costoEscrituraCachéClaude = (decimal)modelo.PrecioEscrituraManualCachéRefrescablePor5Minutos;
                    } else if (tókenes.MinutosEscrituraManualCaché == 60) {
                        costoEscrituraCachéClaude = (decimal)modelo.PrecioEscrituraManualCachéRefrescablePor60Minutos;
                    } else {
                        throw new Exception("No se esperaba que MinutosEscrituraManualCaché fuera diferente de 0, 5 o 60 para modelos Claude.");
                    }
                    pesosEscrituraManualCaché = CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, costoEscrituraCachéClaude, tasaCambioUsd);
                    break;

                case Familia.Gemini:

                    Suspender(); // Verificar funcionamiento.
                    if (modelo.PrecioAlmacenamientoCachéPorHora == null)
                        throw new Exception("No se esperaba que un modelo Gemini tenga PrecioAlmacenamientoCachéPorHora vacío.");
                    pesosEscrituraManualCaché = CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, modelo.PrecioEntradaNoCaché, tasaCambioUsd)
                        + tókenes.EscrituraManualCaché * (tókenes.MinutosEscrituraManualCaché / 60m) * (decimal)modelo.PrecioAlmacenamientoCachéPorHora; // Al escribir en caché Gemini cobra los tókenes de entrada normalmente y le costo del tiempo de almacenamiento 
                    break;

                default:
                    throw new NotImplementedException();
                }
                pesosEscrituraManualCaché *= (modelo.FracciónDescuentoEscrituraCachéPorLote ?? 1); // Si es vacío no hay descuento, entonces el factor es 1.

                var totalPesos = pesosNoCaché + pesosCaché + pesosNoRazonamiento + pesosRazonamiento + pesosEscrituraManualCaché;
                totalTodos += totalPesos;
                texto += $"{clave}: {tókenes.EntradaNoCaché} tókenes de entrada no caché a {FormatearMoneda(pesosNoCaché)} " + Environment.NewLine +
                   $"{clave}: {tókenes.EntradaCaché} tókenes de entrada caché a {FormatearMoneda(pesosCaché)}" + Environment.NewLine +
                   $"{clave}: {tókenes.SalidaNoRazonamiento} tókenes de salida no razonamiento a {FormatearMoneda(pesosNoRazonamiento)}" + Environment.NewLine +
                   $"{clave}: {tókenes.SalidaRazonamiento} tókenes de salida razonamiento a {FormatearMoneda(pesosRazonamiento)}" + Environment.NewLine +
                   $"{clave}: {tókenes.EscrituraManualCaché} tókenes de escritura manual en caché por {tókenes.MinutosEscrituraManualCaché} minutos " +
                   $"a {FormatearMoneda(pesosEscrituraManualCaché)}" + Environment.NewLine +
                   $"Total {clave}: {FormatearMoneda(totalPesos)}" + Environment.NewLine;

            }

            return $"{texto}{Environment.NewLine}Total Todos:{FormatearMoneda(totalTodos)}";

        } // ObtenerTextoCostoTókenes>


    } // TókenesUsados>


} // Frugalia>
