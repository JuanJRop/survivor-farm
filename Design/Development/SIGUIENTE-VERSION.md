# Siguiente versión para PC

1. Primer día — completado. Se probaron recolección, cavar, labrar, sembrar, regar, crecimiento natural, cosecha, cocina y cama con los métodos reales del juego. Se recorrieron 25 minutos de reloj del motor con tiempo acelerado.
2. Sensaciones — completado como primera versión. Sonidos breves originales sintetizados y textos flotantes al recolectar, golpear, recibir daño y fabricar; se pueden desactivar desde P.
3. Meta de exploración — completado. Diario J: entrar en ruinas, vencer al gólem, recuperar la gema, regresar a casa y construir una baliza. La vuelta concede 25 monedas una sola vez.
4. Construcción — completado. G: fogata, cerca, cofre, banco de trabajo y baliza con sprites existentes, vista previa y costes. Validación de distancia, terreno bloqueado, caminos y ocupación; cancelar no gasta. Cofres permiten transferir madera, piedra, comida y hierro en lotes de hasta 10.
5. Progresión y facilidades — completado. Pico 2 habilita cuatro vetas de hierro en el este, con reposición diaria. Tras la expedición, el banco permite gastar 6 hierro para reforjar la espada (+2 daño). Baliza: 10 hierro, 12 madera y 8 piedra. Pausa, ayuda, guardado manual, ventana/pantalla completa y nueva partida en otra ranura.
6. Windows x64 — completado. Compilación con cero errores (95,7 MB según Unity). Arranque, diario, gráficos originales y guardado/carga comprobados fuera del editor, con salida 0. ZIP portable: `Builds/SurvivorFarm-Windows.zip`; no contiene la ranura QA ni los símbolos de depuración.

## Persistencia y pruebas

Guardado versión 12; conserva compatibilidad con partidas de versiones 5–11. Se incluyen hierro, estado de vetas, objetivos, edificios, contenido de cofres y espada reforjada. La prueba de editor excluye el archivo del jugador; la prueba del ejecutable utiliza una ranura QA separada.

Evidencias: `Design/Validation/PcRelease/playtest.txt`, `construccion.png`, `diario.png`, `build.txt` y `Builds/Windows/QA/result.txt`.

## Límites

La prueba es automatizada con tiempo acelerado; no sustituye la valoración humana de una partida de 20–30 minutos. Los sonidos son señales breves de esta primera versión. Los cofres almacenan cuatro recursos; la construcción no incluye demolición, rotación ni traslado. La baliza usa la gema del paquete como representación. Las ranuras anteriores se conservan como archivos, pero aún no existe un selector de partidas antiguas.
