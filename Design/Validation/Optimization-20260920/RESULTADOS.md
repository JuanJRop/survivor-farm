# Validación de reutilización y escalabilidad — 20/09/2026

- Unity 6000.3.9f1; compilación correcta de Runtime, Editor y PlayModeTests (`Compile`).
- QA ejecutado en la copia aislada `QA/p`; se mantienen intactos el editor principal y los guardados personales.
- Primera regresión: **273/274** aprobadas (`playmode.xml`). La única diferencia fue una celda en el margen de contacto de una valla, detectada por la comparación de navegación con las consultas físicas originales.
- Se corrigió esa diferencia consultando el motor únicamente en el perímetro ambiguo; la prueba conserva su comparación original.
- La reserva genérica completó **5.000 préstamos/devoluciones**, conservó las cuatro instancias iniciales y registró **0 bytes administrados** asignados en el bucle calentado.
- En el primer pase aprobaron las pruebas de reutilización de flechas, saturación sin gasto de munición, grupos densos, recogida única de botín, saturación sin perder recompensas, guardado de encuentros ocultos, limpieza de escenas y registros de interacción.
- Resultado consolidado: **279/279 pruebas en 31 clases**. `pruebas-consolidadas.csv` toma la ejecución completa más reciente de cada clase, sin sumar repeticiones.
- `playmode-final.xml`: 97/99; incluye navegación corregida, combate, HUD y cinco pruebas nuevas de etiquetas. Las dos diferencias eran del fixture de etiquetas (contador compartido con otra clase y precisión Color32 de TextMesh).
- `playmode-labels.xml`: 5/5 con escena propia por prueba y comparación de color en su formato real. Se conservan los requisitos de reutilizar la misma instancia, límite de capacidad y limpieza al descargar.
- Build Windows correcto: `Succeeded | errors=0 | bytes=98844649 | duration=00:01:00.2163463` (`build-result.txt`).
- Prueba nativa de reutilización: **PASS** (`native-optimization-result.txt`, `metrics.json`). Por cada sistema se ejecutaron 5.000 ciclos tras preparar los recursos:

| Reserva | Objetos antes/después | Bytes administrados nuevos en el bucle |
| --- | --- | --- |
| Flechas | 4 / 4 | 0 |
| Botín | 1 / 1 | 0 |
| Impactos | 6 / 6 | 0 |
| Etiquetas flotantes | 4 / 4 | 0 |

- Navegación nativa: 3 actualizaciones, 3 consultas del conjunto y 339 consultas de borde; **114 consultas por actualización en este escenario**, frente a las 18.585 del barrido anterior. Conserva 24 buffers de rutas y registra 0 bytes administrados nuevos en los dos pases posteriores al calentamiento.
- La comprobación nativa confirma que las reservas desaparecen al descargar su escena y que ya no existe el objeto de espada superpuesta.
- Recorrido nativo del pueblo: **PASS** (`native-village-result.txt`). Verifica interacción, transparencia, animales, montura, árbol de habilidades, guardias, mejoras, botín, mazmorra, miniboss y guardado. Se revisaron las capturas del jugador sin espada añadida y el cofre con recompensas físicas.
- Recorrido nativo de arte y combate: **PASS** (`native-combat-result.txt`). Comprueba río/puente, los cuatro tipos importados de enemigos y ataques normal/cargado con daño real. Capturas finales en `Capturas/Pueblo` y `Capturas/Combate` (escenarios QA preparados, no una partida manual prolongada).
- Versión entregada en `Builds/Portfolio`. Ejecutable y ensamblado Runtime coinciden por SHA-256 con QA (`release-hashes.json`); se conservan licencias e instrucciones.

Las cifras de memoria se refieren a los bucles indicados después de preparar sus recursos; no representan la memoria total del juego ni una medición de FPS.
