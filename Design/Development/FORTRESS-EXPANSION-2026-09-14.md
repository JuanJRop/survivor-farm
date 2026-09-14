# Construcción, exploración e inventario — auditoría incremental

## Existe y funciona
ConstructionSystem valida y paga piezas, guarda BuildingData y recupera objetos embalados. FarmDefense comparte daño, reparación y ataques de enemigos. La mochila ya recicla casillas con iconos; MaterialCostBadge muestra costes visuales. Los árboles y rocas usan ResourceSpawnPoint con identidad y regeneración guardadas. IronVein ya anima la minería y limita cada veta por día.

## Necesita mejoras
La colocación termina incondicionalmente tras un clic. Vallas de 1 unidad, cuadrícula de 0,5, margen de separación de 1 y colisiones diferentes producen huecos y esquinas imposibles. Se gira la ilustración completa. El HUD repite recursos como un párrafo. La mochila concede poco espacio al icono y no tiene acceso directo a una paleta de construcción.

## Desconectado / problemas
Las vetas del este (x21–25,5) están fuera de las barreras de la demo (x±18). Exigen pico Nv.2, pero la demo oculta su mejora. El terreno existente llega más lejos; no es necesario reconstruirlo. Regeneración y navegación mantienen límites diferentes.

## Alcance
Conservar la granja, escenas, guardados y assets. Extender el área jugable a 64×36 unidades con bosque/cantera/salientes minerales y límites visibles. Cuatro vetas persistentes: dos de hierro y dos de hierro/oro, accesibles con el pico básico en la demo. Tres materiales de muro modular, trampa y ballesta, con selección continua y costes con iconos. El oro tiene una utilidad concreta en el muro reforzado. Guardados antiguos conservan las posiciones y dimensiones de vallas antiguas; las piezas nuevas usan módulos de 2 unidades. No añadir economía, regiones separadas, generación aleatoria de mapa ni otro inventario.

## Verificación prevista
Probar encaje recto/esquinas, colisiones, coste único, existencias insuficientes, colocación continua, cambiar pieza, rotación, guardado/carga y compatibilidad. Verificar rutas a minerales, minería diaria y fronteras. Renderizar mochila, paleta y fortificación en el ejecutable. Repetir suite y ciclo completo automatizado; la pasada manual sigue pendiente del permiso de control de ventana.

## Implementación

- FortressPieces comparte geometría, ajuste a extremos, colisión, nivel de material e ilustraciones del pack. Las esquinas permiten el extremo compartido, no solapamientos arbitrarios.
- ConstructionSystem conserva validación, pagos y guardado; PlaceSelected deja la sesión abierta y consume piezas embaladas antes de recursos. Disponible también mejora sobre el mismo módulo, con coste completo y proporción de salud conservada. No se reubican estructuras guardadas antiguas.
- ConstructionPalette presenta cinco piezas, existencias, desbloqueos y cuatro costes con MaterialCostBadge; no duplica reglas de pago. La mochila conserva virtualización y orden persistente.
- FarmExploration extiende el acceso al terreno pintado, añade posiciones estables de recolección y configura las cuatro vetas ya contempladas por AdventureData. IronVein se separó del archivo de construcción; conserva la animación y el guardado diario, cancela al alejarse/interrumpir la acción y muestra agotamiento.
- FarmRaidNavigation y regeneración comparten los nuevos límites. El río se conserva; el puente de la demo está abierto, incluso al cargar guardados anteriores.

Primera suite: 324/325. El chequeo físico encontró el puente cerrado bloqueando el saliente norte; se corrigió el acceso y la señalización. Segunda suite compilada: 326/326, incluidos muros, mejora sobre muro dañado, pago y guardado, rutas a las cuatro vetas y minería diaria. La revisión del ejecutable y la última suite se registrarán al finalizar el polish.

## Verificación final de código

`Design/Validation/Portfolio/fortress-verified-tests.xml`: **328/328 PASS**. Incluye el caso de un demoledor atacando el extremo de un muro grande (distancia a superficie, no al centro), encuadre de cámara y toda la suite histórica de combate/campamentos/inventario/persistencia. Se corrigió la detección de movimiento del animador, que dependía de la distancia por fotograma y podía mostrar Idle a FPS muy altos; ahora usa velocidad.

La revisión renderizada encontró una entrada histórica de mazmorra aún visible y el borde del terreno expuesto al alejarse; se oculta esa entrada solo en la demo y se limita el encuadre conservando el desplazamiento y feedback de cámara. La paleta oculta ingredientes que no necesita la pieza seleccionada. No se han regenerado escenas ni eliminado assets de campaña.

La pasada jugada manual del encuentro permanece pendiente: el permiso de control de ventana no llegó a concederse. Las pruebas automatizadas y las capturas no se presentan como validación humana de ritmo, dificultad o mezcla sonora.

## Entrega

- Ejecutable: `Builds/Portfolio/SurvivalFarm.exe`.
- Paquete Windows: `Builds/SurvivalFarm-Fortress-Windows.zip`, sin QA ni información de depuración.
- Versión anterior conservada: `Builds/Portfolio-BeforeFortress-20260914` y ZIP de combate previo.
- Build limpio: `Design/Validation/Portfolio/fortress-release-build.txt` (Succeeded, 0 errores).
- Comprobación del ejecutable: `Logs/fortress-package-windows.log`; resultado y capturas en `Design/Validation/Portfolio/Fortress`.
- Los marcadores de mineral resuelven el icono del catálogo (GoldOre usa Coin) y se dibujan delante de la roca. El chequeo del ejecutable exige sprite visible y orden correcto antes de capturar cada veta.
- Se mantiene el aviso preexistente de liberación de ComputeBuffer al cerrar el reproductor. No se detectaron excepciones de gameplay ni scripts ausentes en las verificaciones del ejecutable.
