Estilo de código C# para este repositorio:

1. Usa comentarios para cerrar las funciones y procedimientos usando su nombre en un comentario al frente de la llave de cierre. Deja una línea en blanco entre la llave de apertura y la primera instrucción, y otra entre la última instrucción y la llave de cierre, así

void Procedimiento() {

	primerainstrucción;
	...
	últimainstrucción;
	
} // Procedimiento>

2. Inicia las llaves de inicio de un bloque en la misma línea, así

void Procedimiento() {

y no así:

void Procedimiento()
{

3. Usa español correcto en la medida de lo posible, por ejemplo prefiere tókenes a tokens.

4. Deja dos líneas en blanco entre funciones/métodos/clases/namespaces. 

5. Deja dos líneas en blanco entre la primera y última función/método y su clase contenedora. 

6. Deja dos líneas en blanco entre el bloque de using al inicio del archivo y el primer elemento (namespace/clase/enum).

7. Si el archivo no es para .NET Standard 2.0, usa el estilo moderno de namespaces al inicio del archivo sin llaves {}. 