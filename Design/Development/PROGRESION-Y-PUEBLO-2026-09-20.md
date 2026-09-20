# Progresión, botín y defensa del pueblo — 20/09/2026

## Experiencia de juego

- Partida nueva: espada inicial; el arco se encuentra como objeto físico en el cofre de una mazmorra, tras sus criaturas y miniboss. Las partidas anteriores conservan el equipo que ya tenían.
- El arco apunta al cursor, no persigue enemigos, consume una flecha por disparo y hace 8/12/16 de daño base según maestría. Los arqueros dejan 3–6 flechas. En F se fabrican paquetes de 8 flechas por 2 maderas y 1 piedra.
- Espada de nivel 1: combate físico. Nivel 2: ataque cargado. Nivel 3: ataque cargado con golpe de área. Cada nivel tiene una espada visible diferente, también en la barra de herramientas. K abre el árbol de habilidades y muestra experiencia, requisitos y costes.
- Un enemigo normal con un cuarto de vida o menos permite ejecutar con E a menos de 3 unidades; el desplazamiento no supera 2,4 unidades, exige un recorrido libre y vuelve a comprobar el objetivo antes del golpe. Los minibosses no se ejecutan.
- Cada mazmorra contiene una criatura que deja dos versiones menores al morir; los fragmentos tienen menos vida, daño y tamaño. El miniboss aparece al eliminar a sus criaturas y fragmentos; el cofre permanece cerrado hasta vencerlo. Deja un diamante.
- Objetos físicos: dispersión inicial, flotación, sombra y atracción rápida desde 2,2 unidades. La atracción respeta paredes. Los diamantes tienen mayor tamaño visual.
- Las vacas dejan cuero y muestran golpe y caída, como los demás animales. Hay caballos con sprites originales de jinete. En F se fabrica una montura reutilizable por 8 cueros y 4 maderas; E monta y desmonta. La montura aumenta la velocidad un 65 %. Las colisiones del jugador siguen activas.
- El pozo abre la administración del pueblo. Pozo y casas tienen tres niveles. Se contratan hasta 2–4 guardias según el nivel del pozo. Fuerza y resistencia se mejoran por separado, con costes de recursos. Los guardias patrullan y luchan cerca del pueblo mientras el jugador explora; los civiles conservan su conducta de huida.
- Las mejoras, guardias, salud, materiales, maestrías, encuentros y botín pendiente se guardan. Cargar una partida o viajar desmonta de forma segura.

## Presentación

La interacción próxima usa la mano original y una tecla E pequeña con fondo claro. Las casas indican Reparar o Mejorar según su estado. Se elimina el rectángulo oscuro grande; el indicador evita la cara del jugador. Los arbustos también se atenúan cuando ocultan al personaje.

La carga conserva el destello del atlas original con muestreo nítido. El corte cargado usa una estela opaca de tres colores sin halo difuminado; la habilidad de nivel 3 añade un aro dorado para mostrar el alcance del golpe de área.

## Validación

Resultados de compilación, PlayMode y ejecución nativa en `Design/Validation/Progression-20260920`. Las capturas nativas usan escenarios preparados por el comprobador QA; no representan una partida completa jugada manualmente. No se alteran las partidas personales: QA usa su propia copia y sus propios archivos de guardado.
