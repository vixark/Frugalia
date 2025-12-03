# Frugalia [En Progreso]

# Librería .NET para consultas optimizadas y económicas con modelos de IA

Frugalia es una librería para .NET que permite integrar modelos de inteligencia artificial (OpenAI GPT, Claude, Gemini, etc.) en aplicaciones empresariales, con un enfoque especial en la optimización de costos y el uso eficiente de recursos.

# Características Diferenciadoras

1. Optimización de costos por diseño
   
•	Relleno inteligente de instrucciones de sistema (Prompt Padding Optimization): La librería implementa un mecanismo que aumenta las instrucciones de sistema para activar la caché de tókenes en los modelos compatibles (como GPT). Esto reduce el costo de las consultas repetidas, aprovechando los descuentos de la caché de entrada.

•	Control y visualización de costos: Cada consulta muestra el costo estimado en moneda local, desglosando el uso de tókenes en caché, no caché, razonamiento y almacenamiento, para una gestión transparente y precisa.

2. Arquitectura de modelo portero
   
•	Uso de modelos pequeños como filtro (Porter Model Strategy): Permite configurar un flujo donde un modelo pequeño y económico actúa como portero, filtrando o resolviendo consultas sencillas. Solo las consultas complejas se escalan a modelos superiores, maximizando el ahorro.

•	Ajuste inteligente de razonamiento: El nivel de razonamiento se adapta automáticamente según la longitud y complejidad de la consulta, evitando el gasto innecesario en tareas simples y garantizando calidad en las complejas.

3. Adaptación dinámica de modelo
   
•	Mejora automática de modelo y razonamiento: Si la respuesta inicial no es satisfactoria, la librería puede escalar automáticamente a modelos más potentes y/o aumentar el nivel de razonamiento, todo de forma transparente para el usuario.

•	Restricciones configurables: Permite limitar el uso de razonamiento alto en modelos pequeños para evitar sobrecostos y respuestas ineficientes.

4. Multimodalidad y extensibilidad
   
•	Consultas con archivos: Soporte para imágenes y PDF, permitiendo análisis multimodal.

•	Búsqueda en internet: Integración nativa de herramientas de búsqueda web para enriquecer las respuestas.

•	Funciones externas: Ejecución de funciones personalizadas solicitadas por la IA, con validación y manejo de errores.

5. Transparencia y control
    
•	Desglose detallado de tókenes y costos: El usuario puede ver exactamente cómo se distribuyen los tókenes y el coste en cada consulta.

•	Configuración granular: Permite ajustar parámetros como verbosidad, modo de calidad adaptable, restricciones de razonamiento y más.

# ¿Por qué elegir Frugalia?
Frugalia no solo facilita la integración de IA, sino que te permite controlar y reducir los costes de operación de forma inteligente, gracias a sus estrategias de optimización y arquitectura flexible. Es ideal para proyectos donde el control de presupuesto es muy importante.
