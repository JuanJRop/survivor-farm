# Valle vivo — iteración de las ocho capturas

## Integración

Se conserva Main, su terreno, inventario, guardado v20, recursos/regeneración, oleadas, combate y construcción modular. No se regenera la escena. La configuración aditiva vive en PortfolioFarmSetup; el modo clásico conserva sus reglas de compra.

- FortressPieces: empalizadas/murallas horizontales de 2 unidades y verticales de 4. Extremos sobre una retícula 2 × 4; collider y previsualización comparten dimensiones. Las posiciones guardadas no se trasladan automáticamente.
- ConstructionSystem: botón Mover / M, selección visual, reubicación sin coste y cancelación sin quitar la pieza original. Salud y contenido se conservan. Al terminar un movimiento permite seleccionar otra pieza.
- TreeOcclusionFader reutilizado para muros y edificios. El borde opaco del muro se alinea con su base física. El collider del jugador usa el punto de los pies; las orillas bloquean antes de entrar en agua y mantienen el paso del puente.
- ToolMastery guarda experiencia por oficio y nivel. Las compras de hacha, pico y espada siguen usando sus controladores originales. Arco, azada y regadera tienen sus mejoras en el mismo árbol.
- ResourceTier separa requisito y recompensa de HarvestableResource. Roca Nv.1 da piedra, Nv.2 añade hierro y Nv.3 hierro y oro. Los árboles superiores dan más madera. El tier se recalcula al regenerar y restaurar posiciones.
- VillageResidentHealth posee vida y cuerpo; VillageNpcRoutine mueve el objeto completo. RaidEnemy elige civiles, y los proyectiles enemigos pueden dañarlos. No hay fuego amigo del jugador. Cada baja reduce 3 puntos la resistencia máxima del pozo (mínimo 12); guardar/cargar no repite la penalización.
- Las tres gallinas decorativas ahora usan AnimalResource, dos puntos de vida, comida y guardado del agotamiento.
- FarmIntroduction: siete páginas con texto progresivo, retrato suministrado, avanzar/revelar y saltar. Solo la introducción pausa el reloj. Se retiró el botón que adelantaba la noche.

## Dirección visual

La tercera captura guía el entorno, no la interfaz. Se reaprovechan los bordes cóncavos/convexos de ValleyTerrain y los atlas del mismo pack: arces, abedules, minerales, agua, juncos, arbustos y flores. Vegetación agrupada fuera de caminos/colisiones. Se ocultan las vallas decorativas pequeñas, el tablón y muebles decorativos sobrantes; la cocina usa una olla del pack y no muestra el antiguo aviso flotante.

## Ritmo de demo y controles

K abre Maestrías. Desbloquear no entrega equipo gratis: hay que comprar cada nivel. Los usos de recursos inferiores no avanzan la siguiente maestría. Objetivos: 6/10 recolecciones para herramientas, 18/28 impactos aceptados de espada y 12/20 de arco. Son valores deliberadamente cortos para tres noches. Hacha: rendimiento; pico: acceso/rendimiento; espada/arco: daño; azada: fruta; regadera: crecimiento.

Z construcción continua, R giro, M seleccionar/mover, ESC salir. I mochila, F recetas, K maestrías, Q ración, 1/2 armas, E acción contextual, WASD movimiento, ESPACIO esquiva.

## Verificación

La copia de pruebas Temp/p no toca la sesión abierta del editor. Las pruebas de integración cubren compras bloqueadas, cobro, persistencia, niveles de recursos, gallinas, residentes, penalización no duplicada, reubicación/cancelación, introducción y orillas. PortfolioSmokeCheck recorre el ejecutable con materiales y progresión preparados exclusivamente en QA; no equivale a una partida humana de balance.

- Suite completa final: **334/334**, cero fallos y cero omitidas, `Design/Validation/Portfolio/living-final-all-tests.xml` (15/09/2026, 02:22 Madrid; salida de Unity 0). Ejecución anterior: 333/333 en `living-second-tests.xml`.
- Caso añadido de ataque real contra un vecino: **1/1**, `living-raider-test.xml`.
- Tras los últimos ajustes, repetición de LivingValleyTests y EnemyRosterTests: **29/29**, `living-final-focused-tests.xml`; después se repitió también la suite completa indicada arriba.
- Build Windows final: **Succeeded, errors=0**, 97.344.660 bytes, `Design/Validation/Portfolio/living-final-build.txt`.
- Ejecutable final: **PASS** el 15/09/2026 a las 02:14 (Madrid). Resultado y 24 capturas en `Design/Validation/Portfolio/LivingValley/`. Incluye tutorial, árbol de maestrías, combo real, colocación/mejora/movimiento, mochila, extracción de cuatro vetas, orilla, cultivo, cocina, guardado/carga, tres noches y final.
- Pasada manual con la skill computer-use en una copia interactiva y ranuras QA: avanzar/saltar tutorial, abrir/cerrar maestrías, colocar sin cerrar la paleta, seleccionar y mover un muro sin gastar madera, cancelar selección y abrir mochila. La comprobación de ventanas reales permitió validar el flujo de construcción además de sus pruebas de lógica. Revisión visual adicional de capturas finales: retrato, rocas completas, pies sobre césped, transparencia de muros, río y pantallas.
- Se conservaron Main y la sesión del editor. Copia del ejecutable anterior: `Builds/Portfolio-BeforeLivingValley-20260915`. Ejecutable actual: `Builds/Portfolio/SurvivalFarm.exe`; distribución: `Builds/SurvivalFarm-ValleVivo-Windows.zip`.
- Integridad: todos los scripts/atlas modificados coinciden con la copia compilada, no faltan sus metadatos de Unity y el ensamblado Runtime del ejecutable entregado coincide con el probado. ZIP verificado: 178 entradas, ejecutable y dependencias presentes, sin capturas QA ni información de depuración Burst.

### Límites honestos

La partida completa fue automatizada, acelerando fases y resolviendo enemigos de forma preparada; la pasada manual cubrió interfaz y construcción, no el balance de tres noches completas. Falta un playtest humano integral para fijar dificultad/duración. No se han validado otros equipos ni resoluciones extremas. Los registros del ejecutable no contienen excepciones de gameplay; Unity todavía emite al cerrar el aviso preexistente de liberación de ComputeBuffer. No se afirma que todos los registros estén libres de avisos del motor.
