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


namespace Frugalia {


    public class Restricciones { // Encapsula todas las restricciones aplicables (razonamiento y tókenes) en un único objeto. 


        public RestricciónRazonamiento RazonamientoMuyAlto = RestricciónRazonamiento.ModelosPequeños;

        public RestricciónRazonamiento RazonamientoAlto = RestricciónRazonamiento.ModelosPequeños; // Se ha encontrado con GPT que los modelos pequeños y muy pequeños con alto razonamiento no funcionan muy bien porque terminan gastando muchos tókenes de razonamiento para cubrir sus limitaciones, reduciendo la ventaja económica de usar este modelo muy pequeño en primer lugar.
        
        public RestricciónRazonamiento RazonamientoMedio = RestricciónRazonamiento.ModelosMuyPequeños; // No se han realizados pruebas suficientes para sugerir un valor predeterminado para este parámetro, pero se establece este valor predeterminado para ser gradual con el anterior.

        public RestricciónTókenesSalida TókenesSalida = RestricciónTókenesSalida.Alta; // Predeterminadamente se establecen estas restricciones altas. El usuario de la librería podría relajarlas para evitar respuestas incompletas si en su caso de uso está obteniendo respuestas incompletas frecuentemente, pero teniendo en cuenta que se incrementan los costos.

        public RestricciónTókenesRazonamiento TókenesRazonamiento = RestricciónTókenesRazonamiento.Alta;
       
   
        public Restricciones() { } // Constructor que inicia el objeto con los valores predeterminados.


        public Restricciones(RestricciónRazonamiento razonamientoMuyAlto, RestricciónRazonamiento razonamientoAlto, RestricciónRazonamiento razonamientoMedio, 
            RestricciónTókenesSalida tókenesSalida, RestricciónTókenesRazonamiento tókenesRazonamiento) {

            RazonamientoMuyAlto = razonamientoMuyAlto;
            RazonamientoAlto = razonamientoAlto;
            RazonamientoMedio = razonamientoMedio;
            TókenesSalida = tókenesSalida;
            TókenesRazonamiento = tókenesRazonamiento;

        } // Restricciones>


        public static Restricciones Crear(RestricciónRazonamiento razonamientoMuyAlto, RestricciónRazonamiento razonamientoAlto, 
            RestricciónRazonamiento razonamientoMedio, RestricciónTókenesSalida tókenesSalida, RestricciónTókenesRazonamiento tókenesRazonamiento)
            => new Restricciones(razonamientoMuyAlto, razonamientoAlto, razonamientoMedio, tókenesSalida, tókenesRazonamiento);


        /// <summary>
        /// Deconstrucción para facilitar pasar valores a métodos que aún esperan parámetros separados.
        /// Uso: var (rrMuyAlto, rrAlto, rrMedio, rtSalida, rtRazonamiento) = restricción;
        /// No se debe cambiar el nombre de este procedimiento.
        /// </summary>
        public void Deconstruct(out RestricciónRazonamiento razonamientoMuyAlto, out RestricciónRazonamiento razonamientoAlto, 
            out RestricciónRazonamiento razonamientoMedio, out RestricciónTókenesSalida tókenesSalida, out RestricciónTókenesRazonamiento tókenesRazonamiento) {

            razonamientoMuyAlto = RazonamientoMuyAlto;
            razonamientoAlto = RazonamientoAlto;
            razonamientoMedio = RazonamientoMedio;
            tókenesSalida = TókenesSalida;
            tókenesRazonamiento = TókenesRazonamiento;

        } // Deconstruct>


        public override string ToString() => 
            $"Restricción tókenes de salida {TókenesSalida} y de razonamiento {TókenesRazonamiento}." +
            Environment.NewLine +
            $"{(RazonamientoMuyAlto != RestricciónRazonamiento.Ninguna ? $"No se usará razonamiento muy alto en modelos {RazonamientoMuyAlto}." : "")}" +
            Environment.NewLine +
            $"{(RazonamientoAlto != RestricciónRazonamiento.Ninguna ? $"No se usará razonamiento alto en modelos {RazonamientoAlto}." : "")}" +
            Environment.NewLine +
            $"{(RazonamientoMedio != RestricciónRazonamiento.Ninguna ? $"No se usará razonamiento medio en modelos {RazonamientoMedio}." : ".")}";


    } // Restricciones>


} // Frugalia>