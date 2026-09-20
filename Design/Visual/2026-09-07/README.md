# Survivor Farm — propuesta visual
Fecha: 7 de septiembre de 2026.

**Selección posterior del usuario, 8 de septiembre:** centrarse en inicio, inventario, taller, organización del personaje e iconos de equipo. El alcance elegido está en [SELECCION-UI.md](SELECCION-UI.md).

Paquete de dirección artística para el proyecto Unity de la raíz: seis pantallas móviles, desarrollo de dos personajes, siete familias de armas y herramientas y tres enemigos.

Las imágenes son **conceptos visuales**, no capturas de una build ni sprites animados listos para importar. Se generaron con la herramienta integrada `image_gen`. Los servicios gratuitos de abajo se investigaron como alternativas para reutilizar los prompts; estas láminas no se generaron en sus webs.

## Ver las láminas

Abre [GALERIA.md](GALERIA.md) para ver las cinco imágenes seguidas.

| Archivo | Contenido |
|---|---|
| [01-pantallas-aventura.png](01-pantallas-aventura.png) | Inicio, granja y mazmorra; controles táctiles y HUD. |
| [02-pantallas-gestion-v2.png](02-pantallas-gestion-v2.png) | Inventario, tienda/taller y propuesta de progresión del personaje. |
| [03-desarrollo-personajes.png](03-desarrollo-personajes.png) | Josh y Lyria, tres etapas de equipo y vistas de referencia. |
| [04-armas-herramientas.png](04-armas-herramientas.png) | Espada, arco, hacha, pico, azada, pala y regadera, con tres variantes visuales por familia. |
| [05-enemigos.png](05-enemigos.png) | Limo, murciélago y gólem, anticipación de ataques y botín propuesto. |

Los prompts completos están en [PROMPTS.md](PROMPTS.md). El alcance y la preparación para Unity están en [DESARROLLO.md](DESARROLLO.md).

## Generadores con opción gratuita

Mi primera elección para explorar personajes y objetos sería **Leonardo**; para probar muchas composiciones de pantallas, **Bing Image Creator**.

| Servicio | Oferta gratuita verificada | Uso sugerido para este juego |
|---|---|---|
| [Leonardo AI](https://www.leonardo.ai/pricing) | 150 tokens diarios; el coste varía por modelo y acción. Las generaciones gratuitas son públicas. Su página describe una licencia comercial no exclusiva para el contenido gratuito. | Explorar personajes, armas y dirección artística con un mismo prompt base. |
| [Bing Image Creator](https://www.bing.com/images/create) | Generación gratuita con cuenta Microsoft personal y límites de uso; dispone de velocidad estándar. Las páginas oficiales consultadas difieren sobre la cuota rápida, por eso no se fija aquí un número. | Probar pantallas, ambientes y composiciones sin contratar una suscripción. |
| [Adobe Firefly](https://www.adobe.com/products/firefly.html) | Generaciones diarias gratuitas limitadas en una selección de modelos; Adobe indica que límites, funciones y modelos pueden cambiar. | Fondos, conceptos de escenarios y variaciones de objetos. |

Fuentes oficiales: [planes de Leonardo](https://www.leonardo.ai/pricing), [funcionamiento de Bing Image Creator](https://www.microsoft.com/en-us/bing/features/bing-image-creator/), [créditos gratuitos de Adobe](https://helpx.adobe.com/creative-cloud/apps/generative-ai/generative-credits-faq.html). Consulta: 07/09/2026.

Gratis significa utilizar la modalidad gratuita disponible dentro de sus límites. Los tokens no equivalen a un número fijo de imágenes. La recomendación de uso es mi valoración para Survivor Farm, no una prueba comparativa de resultados entre proveedores.

## Criterio visual

Formato móvil vertical, siguiendo la referencia actual de 450 × 800 del Canvas. Vista de juego cenital, personajes pequeños y siluetas claras. Granja cálida con verdes y terracota; mazmorra violeta con zonas de ataque rojas. Paneles de madera y pergamino, botones grandes, mochila accesible y barra de siete herramientas.

Se mantiene la lectura de cinco corazones, hambre, monedas y recursos. Las pantallas toman como base las mecánicas presentes en el código de la raíz. El subproyecto anidado contiene otro prototipo de combate y no es la referencia de estas láminas.

Las texturas generadas tienen más detalle que el Tiny Asset Pack actual. Antes de convertirlas en arte final hay que ajustar la cuadrícula, la paleta y el tamaño de los personajes al pack existente. Los textos de las imágenes son referencias: en Unity deben ser texto real y localizable.
