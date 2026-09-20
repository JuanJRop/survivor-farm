# Ajustes de juego del 20 de septiembre

Cambios realizados a partir de las cuatro capturas y del recorrido del jugador.

- Los enemigos dejan botín visible al morir. El tipo de enemigo y su condición de élite afectan a la probabilidad de monedas, experiencia y gemas. La experiencia alimenta las maestrías de espada/arco existentes.
- Los cofres tienen colisión y expulsan objetos en arco alrededor de su posición. Abrirlos no entrega los materiales directamente ni muestra una lista textual de recompensas. La recogida se habilita después del aterrizaje. Las cajas de las mazmorras se rompen con ataques y dejan experiencia.
- El guardado conserva el tipo, la cantidad, la posición de aterrizaje y el encuentro del botín pendiente. La versión 22 migra las monedas de versiones anteriores. La destrucción de cajas de las dos mazmorras de la demo también persiste.
- Las patrullas detectan desde más lejos y recuerdan la agresión. El jugador cercano tiene prioridad sobre las casas en las incursiones. Los aldeanos huyen ante amenazas cercanas y los invasores mantienen la persecución de su víctima.
- La espada tiene alcance corto y dirección de golpe. Los enemigos disponen de algo más de alcance, manteniendo la preparación y la dirección de sus ataques para permitir esquivarlos.
- Los accesos de mazmorra usan puertas de piedra del pack. Las estatuas bloquean su pedestal completo y reproducen los cuatro cuadros de sus velas, con resplandor cálido mediante un shader aditivo.
- El reloj de la demo avanza dentro de la mazmorra: el anochecer y las incursiones pueden comenzar durante la expedición.
- El indicador de proximidad muestra mano y E; las casas dañadas muestran E · Reparar. Se eliminaron las tarjetas de recursos bajo el ratón y los libros de indicaciones del recorrido.
- La selección del recurso usa el píxel visible bajo el cursor y su orden de dibujo. Los árboles se atenúan al ocultar al jugador. El cursor usa la mano del pack.
- Los caminos toman las esquinas correctas del atlas, eliminando las puntas. La extensión y la cuadrícula del terreno se conservan. Pasto, flores y arbustos comparten escala de píxel; se ajustaron casas, pozo y olla.
- Las dos orillas son de pasto y el agua conserva animación. El puente se refleja horizontalmente, se centra por sus tablones y sus barandas bloquean el paso lateral.

Las comprobaciones de esta entrega están en `Design/Validation/Polish-20260920`; incluyen resultados de Unity y capturas del ejecutable. El comparativo de esquinas está en `Design/Validation/OutdoorPolish/path-corners-before-after.png`.
