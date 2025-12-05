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


namespace Frugalia.Pruebas {


    public class Globales {


        [Theory]
        [InlineData(0, Razonamiento.Ninguno)]
        [InlineData(499, Razonamiento.Ninguno)]
        [InlineData(500, Razonamiento.Bajo)]
        [InlineData(1999, Razonamiento.Bajo)]
        [InlineData(2000, Razonamiento.Medio)]
        [InlineData(5000, Razonamiento.Medio)]
        public void ObtenerRazonamientoEfectivo_NingunoOMayor_SeAdaptaPorLargo(int largo, Razonamiento esperado) {

            var resultado = Global.ObtenerRazonamientoEfectivo(Razonamiento.NingunoOMayor,
                RestricciónRazonamiento.Ninguna,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-pro", // Tamaño Grande, sin restricciones aplicables.
                largo);

            Assert.Equal(esperado, resultado);

        } // ObtenerRazonamientoEfectivo_NingunoOMayor_SeAdaptaPorLargo>


        [Theory]
        [InlineData(0, Razonamiento.Bajo)]
        [InlineData(499, Razonamiento.Bajo)]
        [InlineData(500, Razonamiento.Medio)]
        [InlineData(1999, Razonamiento.Medio)]
        [InlineData(2000, Razonamiento.Alto)]
        [InlineData(8000, Razonamiento.Alto)]
        public void ObtenerRazonamientoEfectivo_BajoOMayor_SeAdaptaPorLargo(int largo, Razonamiento esperado) {

            var resultado = Global.ObtenerRazonamientoEfectivo(Razonamiento.BajoOMayor,
                RestricciónRazonamiento.Ninguna,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-pro",
                largo);

            Assert.Equal(esperado, resultado);

        } // ObtenerRazonamientoEfectivo_BajoOMayor_SeAdaptaPorLargo>


        [Theory]
        [InlineData(0, Razonamiento.Medio)]
        [InlineData(499, Razonamiento.Medio)]
        [InlineData(500, Razonamiento.Alto)]
        [InlineData(5000, Razonamiento.Alto)]
        public void ObtenerRazonamientoEfectivo_MedioOMayor_SeAdaptaPorLargo(int largo, Razonamiento esperado) {

            var resultado = Global.ObtenerRazonamientoEfectivo(Razonamiento.MedioOMayor,
                RestricciónRazonamiento.Ninguna,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-pro",
                largo);

            Assert.Equal(esperado, resultado);

        } // ObtenerRazonamientoEfectivo_MedioOMayor_SeAdaptaPorLargo>


        [Fact]
        public void ObtenerRazonamientoEfectivo_Alto_RestricciónModelosPequeños_DegradaASegúnTamaño() {

            // MuyPequeño: gpt-5-nano -> Alto debe bajar a Medio.
            var nano = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-nano",
                3000);
            Assert.Equal(Razonamiento.Medio, nano);

            // Pequeño: gpt-5-mini -> Alto debe bajar a Medio.
            var mini = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-mini",
                3000);
            Assert.Equal(Razonamiento.Medio, mini);

            // Medio: gpt-5.1 -> Alto permanece Alto (no se degrada).
            var gpt51 = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5.1",
                3000);
            Assert.Equal(Razonamiento.Alto, gpt51);

            // Grande: gpt-5-pro -> Alto permanece Alto (no se degrada).
            var pro = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-pro",
                3000);
            Assert.Equal(Razonamiento.Alto, pro);

        } // ObtenerRazonamientoEfectivo_Alto_RestricciónModelosPequeños_DegradaASegúnTamaño>


        [Fact]
        public void ObtenerRazonamientoEfectivo_Alto_RestricciónModelosMuyPequeños_DegradaSoloMuyPequeños() {

            // MuyPequeño: gpt-5-nano -> Alto baja a Medio.
            var nano = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosMuyPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-nano",
                3000);
            Assert.Equal(Razonamiento.Medio, nano);

            // Pequeño: gpt-5-mini -> Alto no se degrada.
            var mini = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosMuyPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-mini",
                3000);
            Assert.Equal(Razonamiento.Alto, mini);

        } // ObtenerRazonamientoEfectivo_Alto_RestricciónModelosMuyPequeños_DegradaSoloMuyPequeños>


        [Fact]
        public void ObtenerRazonamientoEfectivo_Medio_RestricciónModelosPequeños_DegradaABajo() {

            // MuyPequeño y Pequeño con restricción para Medio deben bajar a Bajo.
            var nano = Global.ObtenerRazonamientoEfectivo(Razonamiento.Medio,
                RestricciónRazonamiento.Ninguna,
                RestricciónRazonamiento.ModelosPequeños,
                "gpt-5-nano",
                1000);
            Assert.Equal(Razonamiento.Bajo, nano);

            var mini = Global.ObtenerRazonamientoEfectivo(Razonamiento.Medio,
                RestricciónRazonamiento.Ninguna,
                RestricciónRazonamiento.ModelosPequeños,
                "gpt-5-mini",
                1000);
            Assert.Equal(Razonamiento.Bajo, mini);

            // Medio: gpt-5.1 no se degrada.
            var gpt51 = Global.ObtenerRazonamientoEfectivo(Razonamiento.Medio,
                RestricciónRazonamiento.Ninguna,
                RestricciónRazonamiento.ModelosPequeños,
                "gpt-5.1",
                1000);
            Assert.Equal(Razonamiento.Medio, gpt51);

        } // ObtenerRazonamientoEfectivo_Medio_RestricciónModelosPequeños_DegradaABajo>


        [Fact]
        public void ObtenerRazonamientoEfectivo_DobleRestricción_AltoEnMuyPequeño_TerminaEnBajo() {

            // Alto con doble restricción en modelo MuyPequeño:
            // 1) Alto con restricción de Alto -> baja a Medio.
            // 2) Medio con restricción de Medio -> baja a Bajo.
            var resultado = Global.ObtenerRazonamientoEfectivo(Razonamiento.Alto,
                RestricciónRazonamiento.ModelosMuyPequeños,
                RestricciónRazonamiento.ModelosMuyPequeños,
                "gpt-5-nano",
                5000);

            Assert.Equal(Razonamiento.Bajo, resultado);

        } // ObtenerRazonamientoEfectivo_DobleRestricción_AltoEnMuyPequeño_TerminaEnBajo>


        [Fact]
        public void ObtenerRazonamientoEfectivo_AdaptableConRestricción_AplicaDegradaciónTrasAdaptación() {

            // Caso: BajoOMayor con texto muy largo => Alto (por adaptación),
            // pero en modelo Pequeño con restricción para Alto => se degrada a Medio.
            var resultado = Global.ObtenerRazonamientoEfectivo(Razonamiento.BajoOMayor,
                RestricciónRazonamiento.ModelosPequeños,
                RestricciónRazonamiento.Ninguna,
                "gpt-5-mini",
                3000); // >= 2000 => Alto

            Assert.Equal(Razonamiento.Medio, resultado);

        } // ObtenerRazonamientoEfectivo_AdaptableConRestricción_AplicaDegradaciónTrasAdaptación>


    } // Globales>


} // Frugalia.Pruebas>