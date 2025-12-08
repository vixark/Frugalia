# Frugalia

## Librería .NET para consultas optimizadas y económicas a modelos de IA

Frugalia es una librería para .NET que permite integrar modelos de inteligencia artificial con un enfoque en la optimización de costos, manteniendo la calidad de las respuestas.

Para ahorrar al máximo, lo mejor es usar modelos gratuitos. Aún así, en muchos casos es necesario usar modelos de pago más potentes, pero el gasto se puede subir rápidamente si no eres precavido.

La idea es ayudarte a:

- Gastar menos en tókenes y dinero.
- Exprimir mejor las características de la API (razonamiento, caché, funciones, búsqueda web, etc).
- Tener métricas claras y detalladas de cuánto te cuesta cada llamada.
- Encontrar tu propio equilibrio entre calidad de las respuestas y costo.
   
Actualmente está implementado OpenAI con una arquitectura preparada a corto plazo para Claude y Gemini.

## ¿Por qué otro encapsulador/wrapper más?

La mayoría de los encapsuladores de modelos de IA hacen:

> Consulta → Llamada a la API de la IA seleccionada → Respuesta

Frugalia está pensada para actuar como capa intermedia y ayudarte a resolver problemas de optimización de costos que surgen de la variedad de opciones que ofrecen las API de IA.

> Consulta con más información del caso de uso → Optimizaciones internas de Frugalia → Llamada optimizada a la API de la IA → Respuesta

## Características Diferenciadoras

### 1. Optimización de costos por diseño
   
•	Relleno inteligente de instrucciones de sistema (Prompt Padding Optimization): La librería implementa un mecanismo inteligente que rellena las instrucciones de sistema para activar la caché de tókenes en los modelos compatibles (como GPT). Esto reduce el costo de las consultas repetidas, aprovechando los descuentos de la caché de entrada.

•	Control y visualización de costos: Cada consulta devuelve el costo estimado en moneda local, desglosando el uso de tókenes en caché, no caché, razonamiento y almacenamiento, para una gestión transparente y precisa.

### 2. Arquitectura de modelo portero mejorado
   
•	Uso de modelos pequeños como filtro: Permite configurar un flujo donde un modelo pequeño económico actúa como portero respondiendo al usuario y a la vez autoevaluando su respuesta. Si la autoevaluación es positiva, acepta la respuesta y si no lo es, escala la consulta a modelos superiores. Este método de portero mejorado supera al tradicional porque requiere una sola respuesta autoevaluada para consultas simples en vez de dos.

•	Ajuste inteligente de razonamiento: El nivel de razonamiento se adapta automáticamente según la longitud y complejidad de la consulta, evitando el gasto innecesario en tareas simples y garantizando calidad en las complejas.

### 3. Adaptación dinámica de modelo
   
•	Mejora automática de modelo y razonamiento: Si la respuesta inicial no es satisfactoria, la librería puede escalar automáticamente a modelos más potentes y/o aumentar el nivel de razonamiento, todo de forma transparente para el usuario.

•	Restricciones configurables: Permite limitar el uso de razonamiento alto en modelos pequeños para evitar sobrecostos y respuestas ineficientes.

### 4. Multimodalidad y extensibilidad
   
•	Consultas con archivos: Soporte para imágenes y PDF, permitiendo análisis multimodal. Eliminación automática de archivos en el servidor del modelo para evitar sobrecostos.

•	Búsqueda en internet: Integración nativa de herramientas de búsqueda web para enriquecer las respuestas.

•	Funciones externas: Ejecución de funciones personalizadas solicitadas por la IA. Se implementan con delegados para una alta flexibilidad y tienen soporte para validación y manejo de errores.

### 5. Transparencia y control
    
•	Desglose detallado de tókenes y costos: El usuario puede ver exactamente cómo se distribuyen los tókenes y el coste en cada consulta.

•	Configuración granular: Permite ajustar parámetros como verbosidad, modo de calidad adaptable, restricciones de razonamiento y más.

## Ejemplo

```C#

var rutaClave = @"C:\Rutas\No\Versionadas\openai-key.txt";

var servicio = new Servicio(
   nombreModelo: "gpt-5.1",
   lote: false,
   razonamiento: Razonamiento.NingunoOMayor,
   verbosidad: Verbosidad.Media,
   calidadAdaptable: CalidadAdaptable.MejorarModelo,
   restricciónRazonamientoAlto: RestricciónRazonamiento.ModelosMuyPequeños,
   tratamientoNegritas: TratamientoNegritas.Eliminar,
   rutaArchivoClaveAPI: rutaClave,
   out string errorInicio
);

if (!string.IsNullOrEmpty(errorInicio)) {
   Console.WriteLine($"Error al iniciar servicio: {errorInicio}");
   return;
}

var rellenoInstruccionSistema = "";
var respuesta = servicio.Consulta(
   consultasEnPocasHoras: 10,
   instrucciónSistema: "Eres un asistente breve y directo.",
   ref rellenoInstruccionSistema,
   instrucción: "Explícame qué hace Frugalia en dos frases.",
   out string error,
   out var tokens,
   buscarEnInternet: false
);

if (!string.IsNullOrEmpty(error)) {
   Console.WriteLine($"Error de consulta: {error}");
   return;
}

Console.WriteLine("Respuesta:");
Console.WriteLine(respuesta);

Console.WriteLine();
Console.WriteLine("Costos estimados:");
Console.WriteLine(Tókenes.ObtenerTextoCostoTókenes(tokens, tasaCambioUsd: 4000));

```

Nota: Por diseño, el constructor del servicio tiene muchos parámetros no opcionales para obligarte a pensar qué quieres optimizar.


## ¿Por qué elegir Frugalia?

Frugalia no solo facilita la integración de la IA, sino que te permite controlar y reducir los costos de operación de forma inteligente, gracias a sus estrategias de optimización y arquitectura flexible. Es ideal para proyectos donde el control de presupuesto es muy importante.


