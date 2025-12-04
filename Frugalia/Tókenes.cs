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

        internal string Error { get; } // Error al obtener los tókenes del servicio.

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
            Error = null;

        } // Tókenes>


        /// <param name="nombreModelo"></param>
        /// <param name="lote"></param>
        /// <param name="error"></param>
        internal Tókenes(string nombreModelo, bool lote, string error) : this(nombreModelo, lote, null, null, null, null, null, null) {
            Error = error;
        } // Tókenes>


        public static Tókenes operator +(Tókenes tókenes1, Tókenes tókenes2) {

            if (!string.IsNullOrEmpty(tókenes1.Error)) return tókenes1; // Si alguno de los dos tókenes tiene error, la suma no es posible y se devuelve el objeto tókenes del error.
            if (!string.IsNullOrEmpty(tókenes2.Error)) return tókenes2;
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

            if (listaTókenes == null || listaTókenes.Count == 0) return "No hay tókenes para calcular costos.";

            var totalTodos = 0.0m;
            var texto = "";
            foreach (var kv in listaTókenes) {

                var tókenes = kv.Value;
                if (tókenes.Lote) Suspender(); // Verificar funcionamiento.
                var clave = tókenes.Clave;
                var nombreModelo = clave.Substring(0, clave.IndexOf("§"));
                var modelo = Modelo.ObtenerModelo(nombreModelo);
                var factorEntradaYSalida = tókenes.Lote ? modelo.FracciónDescuentoEntradaYSalidaPorLote : 1m;
                var factorLecturaCaché = tókenes.Lote ? modelo.FracciónDescuentoLecturaCachePorLote : 1m;
                var pesosNoCaché = CalcularCostoMonedaLocalTókenes(tókenes.EntradaNoCaché, modelo.PrecioEntradaNoCaché, tasaCambioUsd) * factorEntradaYSalida;
                var pesosCaché = CalcularCostoMonedaLocalTókenes(tókenes.EntradaCaché, modelo.PrecioEntradaCaché, tasaCambioUsd) * factorLecturaCaché;
                var pesosNoRazonamiento = CalcularCostoMonedaLocalTókenes(tókenes.SalidaNoRazonamiento, modelo.PrecioSalidaNoRazonamiento, tasaCambioUsd) 
                    * factorEntradaYSalida;
                var pesosRazonamiento = CalcularCostoMonedaLocalTókenes(tókenes.SalidaRazonamiento, modelo.PrecioSalidaRazonamiento, tasaCambioUsd)
                    * factorEntradaYSalida;

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
                    var fracciónHora = tókenes.MinutosEscrituraManualCaché / 60m;
                    var precioAlmacenamientoCachéFracciónHora = (decimal)modelo.PrecioAlmacenamientoCachéPorHora * fracciónHora;
                    pesosEscrituraManualCaché = CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, modelo.PrecioEntradaNoCaché, tasaCambioUsd)
                        + CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, precioAlmacenamientoCachéFracciónHora, tasaCambioUsd); // Al escribir en caché Gemini cobra los tókenes de entrada normalmente y el costo del tiempo de almacenamiento
                    break;

                default:
                    throw new NotImplementedException();
                }
                var factorEscrituraCaché = tókenes.Lote ? (modelo.FracciónDescuentoEscrituraCachéPorLote ?? 1) : 1m; // Si es vacío no hay descuento, entonces el factor es 1.
                pesosEscrituraManualCaché *= factorEscrituraCaché; 

                var totalPesos = pesosNoCaché + pesosCaché + pesosNoRazonamiento + pesosRazonamiento + pesosEscrituraManualCaché;
                totalTodos += totalPesos;
                if (!string.IsNullOrEmpty(tókenes.Error)) {
                    texto += $"{clave}: {tókenes.Error}{Environment.NewLine}";
                } else {      
                    texto += $"{clave}: {tókenes.EntradaNoCaché} tókenes de entrada no caché a {FormatearMoneda(pesosNoCaché)} " + Environment.NewLine +
                       $"{clave}: {tókenes.EntradaCaché} tókenes de entrada caché a {FormatearMoneda(pesosCaché)}" + Environment.NewLine +
                       $"{clave}: {tókenes.SalidaNoRazonamiento} tókenes de salida no razonamiento a {FormatearMoneda(pesosNoRazonamiento)}" + Environment.NewLine +
                       $"{clave}: {tókenes.SalidaRazonamiento} tókenes de salida razonamiento a {FormatearMoneda(pesosRazonamiento)}" + Environment.NewLine +
                       $"{clave}: {tókenes.EscrituraManualCaché} tókenes de escritura manual en caché por {tókenes.MinutosEscrituraManualCaché} minutos " +
                       $"a {FormatearMoneda(pesosEscrituraManualCaché)}" + Environment.NewLine +
                       $"Total {clave}: {FormatearMoneda(totalPesos)}" + Environment.NewLine;
                }

            }

            return $"{texto}{Environment.NewLine}Total Todos:{FormatearMoneda(totalTodos)}";

        } // ObtenerTextoCostoTókenes>


    } // TókenesUsados>


} // Frugalia>
