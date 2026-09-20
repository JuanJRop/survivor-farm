# Regeneracion de recursos y combate

## Recursos

- `WildResourceRegrowth` reutiliza los `ResourceSpawnPoint` existentes de arboles y rocas. En Main hay 30 instancias. No aumenta la poblacion ni crea objetos en cada ciclo.
- Se mantiene Flyweight: definiciones, recompensas y animaciones compartidas mediante `ResourceFlyweights`. Salud, ubicacion y temporizadores siguen siendo estado individual.
- Despues de talar, un arbol espera entre 45 y 90 segundos de juego. Las rocas esperan entre 60 y 110 segundos. La recoleccion conserva sus golpes y animaciones temporizadas.
- Reaparecen en una posicion aleatoria a un maximo de 6 unidades de su ancla original y al menos a 1 unidad del ultimo emplazamiento. Maximo dos regeneraciones por actualizacion de un segundo.
- Se comprueban suelo, agua, caminos, centro del pueblo, edificios, accesos a servicios, campamentos, otros recursos y distancia minima de 4 unidades al jugador. Si no hay espacio, se reintenta en 5 segundos sin forzar una aparicion.
- El tiempo sigue pasando mientras el mapa exterior esta oculto. Los recursos pendientes se activan y validan cuando vuelve a estar activo. Pausar el juego o estar muerto detiene el avance automatico; no hay crecimiento por tiempo real con el juego cerrado.
- No se incluye a los animales ni se reintroduce el cultivo.

## Guardado

`ResourceSpawnSaveData.regrowth` guarda posicion y tiempo restante. El ancla no se mueve, por lo que conserva la identidad estable. Los archivos anteriores sin este campo utilizan el emplazamiento original y empiezan un temporizador si el recurso estaba agotado. Se rechazan posiciones no finitas o fuera del radio local. La instancia, su salud y el estado agotado siguen usando el sistema existente.

## Resistencia y proyectiles

- Limos de campamento: 8 HP. Goblins arqueros: 10 HP. Lanceros: 12 HP. Recursos de enemigos normales en los pools exterior/mazmorra: minimo 8 HP. Golems: 16 HP.
- Limos normales de la historia: 8 HP. Guardian y jefe conservan sus valores y sus fases. Las mejoras del equipo siguen aumentando el dano: no hay un limite artificial de dano por golpe ni escalado con el equipo del jugador.
- El arquero fija la direccion al preparar el disparo. Al terminar la preparacion lanza una flecha real aunque el jugador haya esquivado fuera del alcance inicial o haya aparecido cobertura.
- La flecha no gira ni persigue al jugador. Usa velocidad constante y barrido de colision; los obstaculos la detienen y apartarse de su trayectoria evita el dano.
- Los aliados no absorben las flechas de su propio grupo. Se mantienen cancelacion por muerte/desactivacion del tirador, limites de vida, proteccion del refugio y reutilizacion del pool.
- El sprite original tiene una escala ligeramente mayor y se dibuja por encima de los actores para hacer visible su trayectoria.

## Verificacion

`WildResourceRegrowthTests` cubre espera, ubicacion aleatoria, zonas excluidas, reintento sin espacio, identidad/Flyweight, posiciones y temporizadores guardados, compatibilidad y mapa oculto. `EnemyRosterTests` cubre disparo tras salir del alcance, cobertura nueva, trayectoria fija, aliados y resistencia a la espada de hierro.

`WildResourceRegrowthChecks` recoge recursos con sus animaciones en Main y comprueba regeneracion, pool y recorrido completo de `GameSaveSystem`. `EnemyRosterChecks` comprueba una flecha moviendose en pantalla mientras el jugador cambia de posicion y la esquiva. La verificacion integrada conserva las pruebas de pueblos, misiones, campamentos, servicios y jefes.

Resultados y capturas: `Design/Validation/Team/playmode-results.xml`, `Design/Validation/Team/Integrated/WildResourceRegrowth/result.txt` y `Design/Validation/Team/Integrated/TeamUi/`.
