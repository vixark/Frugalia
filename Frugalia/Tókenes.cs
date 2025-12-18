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
using static Frugalia.General;


namespace Frugalia {


    public readonly struct Tókenes {


        private const int FactorPrecioFuturo = 1; // Factor de seguridad que incrementa los precios de los tókenes para proyecciones a futuro. En 2025 no se aplica recargo porque los modelos pequeños y medianos siguen presionados por alternativas de código abierto y subir tarifas implicaría perder usuarios. Para simplificar se usa 1 para todos; si se quisiera ser más estricto se podría usar pequeños x1-x1.5 y medianos x1.5-x2. Los modelos grandes ya se ofrecen con márgenes altos y difícilmente aumenten más sus tarifas, así que ante la incertidumbre se asume un factor 1 para todos.

        public int EntradaTotal { get; } // Se hacen públicos para que el que acceda a este objeto desde la aplicación pueda leer estos valores.

        public int SalidaTotal { get; }

        public int SalidaRazonamiento { get; }

        public int EntradaCaché { get; }

        public int SalidaNoRazonamiento { get; }

        public int EntradaNoCaché { get; }

        public ModoServicio Modo { get; }

        public string Error { get; } // Error al obtener los tókenes del servicio.

        private Modelo Modelo { get; }

        public string NombreModelo => Modelo.Nombre;

        public int EscrituraManualCaché { get; }

        /// <summary>
        /// Para Claude solo es válido 0, 5 o 60. Para Gemini puede ser cualquier valor entero.
        /// </summary>
        public int MinutosEscrituraManualCaché { get; }

        /// <summary>
        /// Clave que identifica un grupo de tókenes sumables.
        /// </summary>
        public string Clave => $"{NombreModelo}{(Modo != ModoServicio.Normal ? $"-{Modo.ToString().ToLower()}" : "")}" +
            $"{(MinutosEscrituraManualCaché == 0 ? "" : $"-{MinutosEscrituraManualCaché}mincaché")}";


        public Tókenes(Modelo modelo, ModoServicio modo, int? entradaTotal, int? salidaTotal, int? salidaRazonamiento, int? entradaCaché, 
            int? escrituraManualCaché, int? minutosEscrituraManualCaché) {

            MinutosEscrituraManualCaché = minutosEscrituraManualCaché ?? 0;
            var minutosCachéVálidosClaude = new List<int>() { 0, 5, 60 };
            if (modelo.Familia == Familia.Claude && !minutosCachéVálidosClaude.Contains(MinutosEscrituraManualCaché))
                throw new Exception("No se pueden usar MinutosEscrituraManualCaché diferentes de 0, 5 o 60 para modelos de tipo Claude.");

            Modo = modo;
            Modelo = modelo;
            EntradaTotal = entradaTotal ?? 0;
            SalidaTotal = salidaTotal ?? 0;
            SalidaRazonamiento = salidaRazonamiento ?? 0;
            EntradaCaché = entradaCaché ?? 0;
            EscrituraManualCaché = escrituraManualCaché ?? 0;
            SalidaNoRazonamiento = SalidaTotal - SalidaRazonamiento;
            EntradaNoCaché = EntradaTotal - EntradaCaché;
            if (SalidaNoRazonamiento < 0 || EntradaNoCaché < 0) {
                Error = "El modelo devolvió datos incoherentes de consumo de tókenes. " +
                    "Se generarían valores negativos de salida no razonamiento o entrada no caché.";
            } else {
                Error = null;
            }            

        } // Tókenes>


        internal Tókenes(Modelo modelo, ModoServicio modo, string error) : this(modelo, modo, null, null, null, null, null, null) {
            Error = error;
        } // Tókenes>


        public static Tókenes operator +(Tókenes tókenes1, Tókenes tókenes2) {

            if (!string.IsNullOrEmpty(tókenes1.Error)) return tókenes1; // Si alguno de los dos tókenes tiene error, la suma no es posible y se devuelve el objeto tókenes del error.
            if (!string.IsNullOrEmpty(tókenes2.Error)) return tókenes2;
            if (tókenes1.NombreModelo != tókenes2.NombreModelo) throw new Exception("No se pueden sumar tókenes usados de diferente modelo.");
            if (tókenes1.Modo != tókenes2.Modo) throw new Exception("No se pueden sumar tókenes de diferentes modos de servicio.");
            if (tókenes1.MinutosEscrituraManualCaché != tókenes2.MinutosEscrituraManualCaché)
                throw new Exception("No se pueden sumar tókenes que tuvieron escritura manual en caché con diferente duración de almacenamiento en caché.");

            return new Tókenes(
                tókenes1.Modelo,
                tókenes2.Modo,
                tókenes1.EntradaTotal + tókenes2.EntradaTotal,
                tókenes1.SalidaTotal + tókenes2.SalidaTotal,
                tókenes1.SalidaRazonamiento + tókenes2.SalidaRazonamiento,
                tókenes1.EntradaCaché + tókenes2.EntradaCaché,
                tókenes1.EscrituraManualCaché + tókenes2.EscrituraManualCaché,
                tókenes1.MinutosEscrituraManualCaché
            );

        } // +>


        public override string ToString() => $"{Clave}: E¬C={EntradaNoCaché,4} EC={EntradaCaché,4} S¬R={SalidaNoRazonamiento,4} " +
            $"SR={SalidaRazonamiento,4} EMC={EscrituraManualCaché,4} MEMC={MinutosEscrituraManualCaché,4}";


        internal static decimal CalcularCostoMonedaLocalTókenes(int tókenes, decimal costoPorMillón, decimal tasaCambioUsd)
            => tókenes * (FactorPrecioFuturo * costoPorMillón / (decimal)1E6) * tasaCambioUsd;


        public static string ObtenerTextoCostoTókenes(Dictionary<string, Tókenes> listaTókenes, decimal tasaCambioUsd) {

            if (listaTókenes == null || listaTókenes.Count == 0) return "No hay tókenes para calcular costos.";

            var totalTodos = 0.0m;
            var texto = "";
            foreach (var kv in listaTókenes) {

                var tókenes = kv.Value;
                if (tókenes.Modo == ModoServicio.Lote) Suspender(); // Verificar funcionamiento del cálculo de tókenes de entrada con lote. En especial el caso con caché.
                var clave = tókenes.Clave;
                var modelo = tókenes.Modelo;
                var factorEntradaYSalida = (decimal)modelo.FactoresPrecio[tókenes.Modo].EntradaYSalida;
                var factorLecturaCaché = (decimal)modelo.FactoresPrecio[tókenes.Modo].LecturaCache;
                var pesosNoCaché = CalcularCostoMonedaLocalTókenes(tókenes.EntradaNoCaché, modelo.PrecioEntradaNoCaché, tasaCambioUsd) * factorEntradaYSalida;
                var pesosCaché = CalcularCostoMonedaLocalTókenes(tókenes.EntradaCaché, modelo.PrecioEntradaCaché, tasaCambioUsd) * factorLecturaCaché;
                var pesosNoRazonamiento = CalcularCostoMonedaLocalTókenes(tókenes.SalidaNoRazonamiento, modelo.PrecioSalidaNoRazonamiento, tasaCambioUsd) 
                    * factorEntradaYSalida;
                var pesosRazonamiento = CalcularCostoMonedaLocalTókenes(tókenes.SalidaRazonamiento, modelo.PrecioSalidaRazonamiento, tasaCambioUsd)
                    * factorEntradaYSalida;

                var pesosEscrituraCaché = 0m;
                switch (modelo.Familia) {
                case Familia.GPT:
                    break; // 0.
                case Familia.Claude:

                    Suspender(); // Verificar funcionamiento.
                    var costoEscrituraCachéClaude = 0m;
                    if (modelo.PrecioEscrituraCachéRefrescablePor5Minutos == null || modelo.PrecioEscrituraCachéRefrescablePor60Minutos == null)
                        throw new Exception("No se esperaba que un modelo Claude tenga PrecioEscrituraManualCachéRefrescablePor5Minutos o Por60Minutos vacíos.");
                    if (tókenes.MinutosEscrituraManualCaché == 0) {
                        // 0.
                    } else if (tókenes.MinutosEscrituraManualCaché == 5) {
                        costoEscrituraCachéClaude = (decimal)modelo.PrecioEscrituraCachéRefrescablePor5Minutos;
                    } else if (tókenes.MinutosEscrituraManualCaché == 60) {
                        costoEscrituraCachéClaude = (decimal)modelo.PrecioEscrituraCachéRefrescablePor60Minutos;
                    } else {
                        throw new Exception("No se esperaba que MinutosEscrituraManualCaché fuera diferente de 0, 5 o 60 para modelos Claude.");
                    }
                    pesosEscrituraCaché = CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, costoEscrituraCachéClaude, tasaCambioUsd);
                    break;

                case Familia.Gemini:

                    Suspender(); // Verificar funcionamiento.
                    if (modelo.PrecioAlmacenamientoCachéPorHora == null)
                        throw new Exception("No se esperaba que un modelo Gemini tenga PrecioAlmacenamientoCachéPorHora vacío.");
                    var fracciónHora = tókenes.MinutosEscrituraManualCaché / 60m;
                    var precioAlmacenamientoCachéFracciónHora = (decimal)modelo.PrecioAlmacenamientoCachéPorHora * fracciónHora;
                    pesosEscrituraCaché = CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, modelo.PrecioEntradaNoCaché, tasaCambioUsd)
                        + CalcularCostoMonedaLocalTókenes(tókenes.EscrituraManualCaché, precioAlmacenamientoCachéFracciónHora, tasaCambioUsd); // Al escribir en caché Gemini cobra los tókenes de entrada normalmente y el costo del tiempo de almacenamiento
                    break;

                default:
                    throw new NotImplementedException();
                }

                if (!string.IsNullOrEmpty(tókenes.Error)) {
                    texto += $"{clave}: {tókenes.Error}{Environment.NewLine}";
                } else {

                    var factorEscrituraCaché = (decimal)modelo.FactoresPrecio[tókenes.Modo].EscrituraCaché;
                    pesosEscrituraCaché *= factorEscrituraCaché;

                    var totalPesos = pesosNoCaché + pesosCaché + pesosNoRazonamiento + pesosRazonamiento + pesosEscrituraCaché;
                    totalTodos += totalPesos;

                    texto += $"{clave}: {tókenes.EntradaNoCaché} tókenes de entrada no caché a {FormatearMoneda(pesosNoCaché)} " + Environment.NewLine +
                       $"{clave}: {tókenes.EntradaCaché} tókenes de entrada caché a {FormatearMoneda(pesosCaché)}" + Environment.NewLine +
                       $"{clave}: {tókenes.SalidaNoRazonamiento} tókenes de salida no razonamiento a {FormatearMoneda(pesosNoRazonamiento)}" + Environment.NewLine +
                       $"{clave}: {tókenes.SalidaRazonamiento} tókenes de salida razonamiento a {FormatearMoneda(pesosRazonamiento)}" + Environment.NewLine +
                       $"{clave}: {tókenes.EscrituraManualCaché} tókenes de escritura manual en caché por {tókenes.MinutosEscrituraManualCaché} minutos " +
                       $"a {FormatearMoneda(pesosEscrituraCaché)}" + Environment.NewLine +
                       $"Total {clave}: {FormatearMoneda(totalPesos)}" + DobleLínea;

                }

            }

            return $"{texto}Total todos: {FormatearMoneda(totalTodos)}";

        } // ObtenerTextoCostoTókenes>


    } // TókenesUsados>


} // Frugalia>
