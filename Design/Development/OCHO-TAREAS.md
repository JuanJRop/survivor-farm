# Ocho tareas: primera versión de supervivencia

1. Base estable y parcelas grandes — completada: parcelas 24×14; 958 celdas cultivables.
2. Primer día jugable — completada: 12 objetivos persistentes, de recolectar a sobrevivir una noche.
3. Fabricación útil — completada: taller F, fogata original, cocina, cama, mejoras de espada y herramientas, casco, pechera y botas.
4. Día, noche y hambre — completada: ciclo de 15 minutos, avisos al anochecer, reloj visible y 0,10 de hambre por segundo.
5. Combate — completada: cono hacia el ratón, aviso naranja antes del impacto, esquiva por distancia, murciélagos que retroceden y gólems resistentes.
6. Equipo y progresión — completada: defensa acumulativa, velocidad, daño, hambre y curación al comer según equipo.
7. Exploración — completada: ocho destinos con descripción y recompensa de descubrimiento única; los gólems dan gemas.
8. Prueba y balance — completada: simulación acelerada de 25 minutos y verificación integrada en Play Mode, incluidos costes, progresión, esquiva, recompensas y guardado/carga. Regresiones de mochila, equipo, muerte y reaparición pasadas. Guardado del usuario excluido.

Se conserva el arte del paquete existente. Es una primera versión del ciclo: falta valoración humana del ritmo y más contenido antes de considerar terminado el juego.


## Controles

WASD para moverse, Shift para correr, clic izquierdo para atacar hacia el ratón y clic derecho para interactuar. B/I abre mochila, C equipo y F taller; Esc cierra las ventanas. Selección de herramienta con 1–7.

## Balance inicial

Un día completo dura 900 segundos. Desde las 08:00 iniciales hasta la noche de las 21:00 hay 487,5 segundos (8 minutos y 7,5 segundos). La noche dura cinco minutos. Sin anillo, una ración de 25 puntos cubre 250 segundos de hambre. El casco y la pechera acumulan un 40% de mitigación: los puntos enteros acumulados se descuentan de golpes posteriores, sin reducir el daño por inanición. Las botas aumentan velocidad un 15%; la gema suma 1 al daño; el anillo reduce hambre un 20%; el amuleto cura 1 al comer.

## Límites de esta entrega

El balance se verificó de forma acelerada; aún requiere una partida humana para valorar dificultad y ritmo. Las recompensas dan identidad inicial a las ocho parcelas; no representan ocho biomas nuevos. El equipo modifica estadísticas, pero todavía no superpone armaduras al sprite del personaje. La casilla de escudo está preparada y no se añadió una receta con una imagen inventada. No se hizo una compilación distribuible para Windows.

Evidencias: `Design/Validation/SurvivalLoop/result.txt`, `taller.png`, `equipo.png`; regresiones en `Design/Validation/DeathMenu` y `Design/Validation/Backpack`.
