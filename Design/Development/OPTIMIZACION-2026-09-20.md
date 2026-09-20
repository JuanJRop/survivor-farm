# Reutilización y crecimiento del juego

## Cambios

Se retira el objeto `Espada equipada` y su actualización continua. El personaje usa las animaciones originales de ataque. Los iconos de las mejoras permanecen en el inventario y el árbol de habilidades.

| Sistema | Diseño | Presupuesto y vida útil |
| --- | --- | --- |
| Flechas del jugador | Pool de componentes y fábrica de instancias | 4 iniciales, máximo 32 simultáneas. Se reserva antes de gastar munición. |
| Impactos | Servicio por escena y pool compartido | 6 iniciales, máximo 18 impactos visuales simultáneos. |
| Números de daño y mensajes flotantes | Pool de TextMesh por escena | 4 iniciales, máximo 24 etiquetas. Saturar el efecto visual no silencia su sonido. |
| Botín | Pool por escena/prefab | Hasta 64 instancias vacías retenidas por prefab. Las recompensas activas nunca se descartan por saturación. |
| Ballestas de práctica | Mismo pool de proyectiles | 1 inicial, 2 vacías retenidas, máximo 4 simultáneas por emisor. |
| Enemigos e interactuables | Registros de ciclo de vida | Altas/bajas O(1), sin búsquedas completas de escena en los recorridos de selección. |
| Navegación del pueblo | Cuadrícula compartida y caché LRU | Una consulta del conjunto y comprobaciones físicas limitadas a los bordes ambiguos; hasta 24 matrices de ruta reutilizadas. |
| Recursos gráficos | Flyweights/cachés compartidas | Iconos, sombras y animaciones se comparten; los recursos creados por la escena se liberan al descargarla. |

`SceneComponentPool<T>` centraliza préstamos, devoluciones, precarga, límites, métricas y eliminación. Los clientes reinician estado de vuelo, objetivos, generación del enemigo, aspecto y temporizadores. Una devolución repetida no duplica la entrada. Los préstamos se eliminan del registro antes de ejecutar callbacks de desactivación; las devoluciones causadas por desactivar un padre se procesan después de ese callback.

El botín activo mantiene el padre del campamento/mazmorra para conservar la visibilidad y el guardado. Los objetos recogidos se separan del encuentro y pasan al almacén vacío. Descargar una escena elimina tanto objetos prestados como retenidos y sus recursos gráficos nativos.

## Trabajo repetido

- Las consultas de combate, flechas y botín utilizan buffers reutilizados. Las listas pueden crecer cuando la densidad supera su capacidad inicial; no omiten objetivos por truncar un array pequeño.
- La navegación sustituye el barrido de 18.585 consultas por celda y actualización por una consulta del conjunto de obstáculos, seguida de comprobaciones de su geometría real. En los bordes ambiguos consulta el motor físico con un buffer reutilizado para respetar también su margen de contacto. Se conservan rotaciones, huecos, agua, vallas y cambios en las construcciones. Las métricas registran por separado las consultas de conjunto y de borde.
- Los civiles deciden la dirección a 10 Hz y comprueban colisiones al moverse. Las búsquedas de amenazas y de botín tienen intervalos; el vuelo y la atracción continúan por frame.
- Interacción y hover comparten el registro de objetos activos. No ordenan todos los candidatos con LINQ para encontrar el más cercano. Los buffers de sprites se reutilizan y sus referencias se limpian al desactivar objetos o descargar escenas.
- El HUD actualiza reloj, munición y nivel cuando cambia el valor mostrado. Los árboles comparten el descubrimiento del jugador y evitan escribir el mismo color; la profundidad visual sólo se escribe cuando cambia.

## Extender sin aumentar la carga innecesaria

Para otro proyectil o efecto, usar el pool existente o una fábrica con un presupuesto explícito y propietario de escena. Configurar el préstamo inactivo, limpiar las referencias al devolverlo y llamar a `Forget` al destruirlo externamente. Elegir la política de saturación según el contenido: omitir un destello cosmético es válido; perder una recompensa no lo es.

Los nuevos interactuables deben encadenar `base.OnEnable()` y `base.OnDisable()` si sobrescriben esos callbacks. Los nuevos tipos de enemigo siguen el registro de `EnemyAIBase`. Las construcciones que cambian rutas deben invalidar la navegación existente.

Las reservas no establecen un límite artificial al número de recompensas pendientes. Su memoria depende de los objetos activos y de los presupuestos retenidos. La verificación mide reutilización, asignaciones y límites concretos; no promete un porcentaje de FPS para todo hardware.

Evidencia de compilación, pruebas y ejecutable: `Design/Validation/Optimization-20260920`.
