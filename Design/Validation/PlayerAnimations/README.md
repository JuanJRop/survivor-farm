# Animaciones del jugador

Biblioteca: `Assets/SurvivorFarm/Resources/JoshAnimationLibrary.asset`, enlazada al jugador de Main y a `Prefabs/Characters/Player.prefab`.

Se conservó Josh, el personaje que ya usa el proyecto. Sus 54 secuencias completas aparecen en `clips.tsv`. Las variantes de color sueltas, sombras, piezas sin personaje y el pez suelto son capas auxiliares del paquete, no acciones adicionales del jugador.

## Conectadas al juego

- Reposo, caminar y correr: velocidad real y orientación del jugador.
- Espada y arco: botón Atacar, con animación también al atacar sin objetivo.
- Hacha y pico: interacción válida con árboles y rocas.
- Pala, azada, plantar y regar: inicio y duración del trabajo de la parcela.
- Recoger: cosecha y objetos recogidos.
- Daño y muerte: pérdida de vida; la muerte conserva su último fotograma y bloquea el movimiento.
- Dormir: descanso válido en la cama.

Las acciones bloquean el desplazamiento durante la animación. Las direcciones del atlas son frente, espalda y lateral; el lateral se refleja para mirar al otro lado. Los fotogramas de 64 píxeles y los de montura tienen su propio recorte y pivote.

## Extras disponibles

Pescar, nadar, montar, bicicleta, escoba, paraguas, magia, flauta, sentarse, acariciar, trepar, atrapar insectos, hoz y transportar objetos están preparados en la biblioteca. Las mecánicas que aún no existen no se han añadido: pueden activar el clip con `PlayNamedAction(nombre)` o `PlayAction(nombre, duración, objetivo)`.

Selecciona el componente **PlayerCharacterAnimator** del jugador para ver el selector de animación, orientación y vista animada. En modo Jugar, el botón del inspector la reproduce sobre el jugador. El menú **Survivor Farm > Apply Complete Player Animations** vuelve a conectar la biblioteca.

El controlador funciona con referencias serializadas y `Resources`, sin cargar assets mediante `UnityEditor`. `result.txt` registra la prueba real en modo Jugar. El GIF muestra los fotogramas originales del catálogo; `sword.png`, `plant.png` y `watering.png` son capturas de Unity durante las comprobaciones. No se ha ejecutado una compilación completa del juego exportado.
