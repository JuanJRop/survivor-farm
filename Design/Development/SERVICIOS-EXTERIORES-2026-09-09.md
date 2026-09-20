# Casas y comercios como servicios exteriores

## Cambio

- Las entradas de tienda abren un menu sin desplazar al jugador, ocultar el pueblo ni modificar la camara. El antiguo interior permanece desactivado.
- El comercio se cierra directamente con su boton X o Escape. Ya no depende de una referencia a ShopEntrance. Se integra en el bloqueo de movimiento/ataque de los otros menus y se cierra al abrir mochila, equipo, diario o recetas.
- La tienda usa el estilo comun del HUD, categorias con scroll y los sprites existentes. Los precios de venta del catalogo y los costes de mejora se consultan a sus sistemas.
- Las fachadas del pueblo permiten consultar a Mara, abrir semillas de Dalia, provisiones de Rolo y equipo en la armeria de Nico. La armeria requiere reparar el taller.
- El refugio se gestiona desde su fachada. No hay entrada a una habitacion; las mejoras de casa se conservan.
- Camas, armarios y hornos se pueden colocar fuera, respetando caminos, alcance y colisiones. El descanso exterior mantiene el avance del primer dia.

## Partidas existentes

Los datos anteriores se conservan. Al restaurar una partida guardada dentro de tienda o casa, el jugador vuelve al exterior correspondiente. No se reabre un interior ni se altera el saldo para resolver la salida.

Los muebles colocados anteriormente dentro de la casa siguen guardados, incluidos sus recursos. Se gestionan desde la opcion Muebles del refugio: usar, mover al exterior o guardar los muebles vacios en la mochila. Cancelar un movimiento conserva su posicion y contenido anterior.

No se cambian las entradas a mazmorras ni las rutas de exploracion. No se reescribieron escenas ni guardados personales durante las pruebas, y no se genero un ejecutable nuevo.

## Verificacion

- Compilacion de Runtime, Editor y PlayModeTests aprobada.
- 186 pruebas PlayMode aprobadas: `Design/Validation/Team/playmode-results.xml`.
- Pruebas especificas: posicion/camara invariables, cierre sin entrada asignada, reapertura y cierre repetidos, actualizacion del saldo, precios de venta reales, recuperacion de interiores y acceso a muebles anteriores.
- Escena real en copia aislada: cuatro servicios de fachada, armeria, provisiones, semillas, refugio, colocacion y descanso en cama exterior, y restauracion de ambos tipos de partida antigua con recursos y muebles conservados.
- La regresion de distribucion y el recorrido automatizado de campana siguen aprobados.
- Capturas de comercio a 1280x720 y 1920x1080 revisadas visualmente.

Informes: `Design/Validation/Team/Integrated/ExteriorServices/result.txt`, `VillageLayout/test.txt` y `Valley/test.txt` dentro de la misma carpeta Integrated.

Capturas: `Design/Validation/Team/Integrated/TeamUi/provisiones-1280.png` y `mercado-1920.png`.

Las pruebas de escena son controladas y usan recursos de QA. No equivalen a una sesion humana completa de balance.
