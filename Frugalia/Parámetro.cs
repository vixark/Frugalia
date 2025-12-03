namespace Frugalia {


    public class Parámetro {


        internal string Nombre { get; }

        internal string Tipo { get; }

        internal string Descripción { get; }

        internal bool Requerido { get; }


        public Parámetro(string nombre, string tipo, string descripción, bool requerido) {

            Nombre = nombre.ToLowerInvariant();
            Tipo = tipo.ToLowerInvariant();
            Descripción = descripción;
            Requerido = requerido;

        } // Parámetro>


    } // Parámetro>


} // Frugalia>
