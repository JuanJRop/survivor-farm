# Dungeon explorable

## Recorrido

La antigua habitacion se sustituye en ejecucion por una dungeon independiente en (0, -160). No se eliminan aldeanos del pueblo: sus objetos permanecen fuera de la dungeon, sin superposicion fisica ni visual.

`DungeonLayout` define seis salas conectadas: vestibulo, cuartel, deposito, galeria del mineral, antesala y camara del Custodio. La galeria occidental ofrece una segunda ruta a la antesala. La rama oriental contiene botin opcional. La camara sigue al jugador con un tamano de 5.6, sin mostrar todo el mapa a la vez. Durante el jefe encuadra a ambos combatientes, ampliando el tamano entre 6.4 y 9; al terminar recupera el encuadre de exploracion.

`DungeonExpedition` desactiva exclusivamente la geometria del interior antiguo y reutiliza sus cofres y pool. Construye suelo y paredes con Tilemaps y colisiones. `DungeonArt` referencia los atlas originales del proyecto, sin imagenes generadas ni nuevas dependencias.

## Combate y botin

- 12 guardias reutilizables, distribuidos entre cuatro salas: lanceros, arqueros con proyectiles y criaturas animadas. La deteccion y el desplazamiento se limitan al entorno de cada puesto.
- 17 destructibles: cajas de 5 HP y escombros de 7 HP. La espada y el arco usan el contrato existente `IDamageable`. Las cajas dan madera y monedas; los escombros dan piedra y hierro. No cuentan como enemigos derrotados.
- 3 cofres de exploracion y un relicario final. El relicario entrega 80 monedas, 8 hierro, 4 oro bruto, un rubi, una gema de equipo y una racion.
- La puerta del jefe requiere despejar la antesala, no todos los ramales opcionales.
- El Custodio tiene 56 HP, animaciones del paquete, impacto anunciado, una embestida con destino fijo y una ventana de recuperacion. Por debajo de media vida acelera su ciclo. La victoria abre la puerta y un retorno al exterior.

## Estado y compatibilidad

`GameSaveSystem` guarda enemigos derrotados, salud de destructibles y victoria del jefe. Los cofres incorporan identidad por nombre con fallback al indice antiguo. Se mantienen los nombres y el orden de los tres cofres anteriores.

Las partidas antiguas dentro de la habitacion original se recuperan en el vestibulo. El botin de suelo antiguo fuera de la nueva geometria se traslada a esa entrada. Cargar durante el combate coloca al jugador delante de la puerta y reinicia al jefe con toda la vida. Salir, morir o recargar no restaura recursos ni premios ya obtenidos. El jefe derrotado no reaparece. No hay reinicio diario ni regeneracion de botin en la dungeon.

Para ampliar el contenido, mantener estables las posiciones de los indices de guardias y destructibles guardados, o anadir una migracion. El contador de guardias usa una mascara de 32 bits.

## QA

`DungeonExpeditionTests` cubre conectividad, ruta alternativa, pooling, deteccion local, destructibles, cofres, puerta, esquiva del jefe y reintento.

`DungeonExpeditionChecks` recorre colisiones reales en Main, camina por un pasillo con Rigidbody2D, usa la espada real y verifica guardado completo, migracion antigua, reintento y recompensas unicas. Captura las salas y al jefe a 1280x720 y 1920x1080. Se ejecuta con `--qa` en `Temp/TeamQA`, sin tocar partidas personales ni cerrar el editor principal.

"Sala de voz" se interpreto provisionalmente como "sala de boss", coherente con la peticion de combate. No se ha incorporado chat de voz ni servicios externos.
