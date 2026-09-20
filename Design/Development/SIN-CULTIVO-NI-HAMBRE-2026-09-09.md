# Juego sin cultivo ni hambre

## Reglas actuales

- Espada y arco son los dos elementos de la barra. La tecla 3 ya no equipa la azada.
- Hacha y pico siguen siendo contextuales; talar y picar conservan su tiempo y golpes.
- No hay labrado, siembra, riego, crecimiento ni cosecha. Las antiguas parcelas no se ven ni se pueden seleccionar.
- No existe desgaste de hambre ni dano por inanicion. El HUD conserva los corazones y retira la barra de hambre.
- Comer es opcional: la fruta recupera 1 punto de vida y una racion 2. Cada alimento del catalogo recupera al menos 1, mas los bonos del equipo. No se consume con la vida completa ni estando muerto.
- Los anteriores bonos de hambre del equipo ahora aportan curacion al comer.
- Se mantienen todos los alimentos, su comercio y las recetas. Los ingredientes del catalogo se pueden comprar desde el inicio.
- Semillas y mejoras de azada desaparecen de mochila, tienda y taller. Comprar o seleccionar semillas ya no es una accion disponible.

## Progresion

- Primer dia: nota, madera, piedra, fogata, cocinar una racion, cama y descanso. Comer o cosechar no es requisito.
- El cofre de suministros del bosquecillo contiene 4 frutas y una racion. Tambien se pueden comprar ingredientes a Dalia.
- Dalia pide 2 frutas y 4 madera para recuperar la cocina comunal. Entrega 2 zanahorias, 1 tomate, 2 raciones y 4 influencia.
- Mara entrega madera en lugar de semillas. Los cofres y descubrimientos sustituyen semillas por alimentos o minerales.
- El portal solo requiere los dos sellos. El final consiste en devolver el Corazon del Valle al altar, sin herramientas de cultivo.
- Enemigos exteriores: posiciones obtenidas del tilemap Spring Grass, independientes de las antiguas parcelas; se mantienen comprobaciones de proteccion, distancia y colisiones.

## Compatibilidad

No se borran partidas ni inventarios existentes. Los identificadores de semillas, estados de parcelas y campos narrativos antiguos siguen siendo legibles, pero no reactivan el cultivo. FarmingPlot es un registro inerte para conservar sus identificadores y datos al guardar. El antiguo porcentaje de hambre se acepta al cargar y se normaliza a 1, sin efectos en la vida.

Los campos de Dalia y del altar mantienen sus identificadores internos para conservar misiones ya aceptadas, completadas e influencia. Los requisitos y recompensas pendientes usan las reglas nuevas.

## Verificacion

- Compilacion de runtime, editor y pruebas con Tools/Verify-TeamChanges.ps1.
- Suite PlayMode con Tools/Run-TeamQA.ps1 -UnitTests: pruebas de todos los alimentos, vida completa, muerte, semillas antiguas, misiones, comercio y aparicion de enemigos sin parcelas.
- RaizclaraTeamVerification.Run: escena Main en una copia aislada, capturas de UI a 1280x720 y 1920x1080, misiones, servicios exteriores, guardado/carga y recorrido de la campana hasta el final sin cosechar.
- No se regenera Main.unity, no se modifica la partida personal y no se cierra el editor del usuario. Detener y volver a iniciar Play aplica la interfaz y los objetos del mundo actualizados.
