# Impacto, asedios y aprender jugando

Iteración sobre Valle vivo, sin regenerar Main ni sustituir la campaña histórica.

## Integración

- `SwordChargeController` solo mantiene pulsación y tiempo. `ComboController` sigue poseyendo la cadena; `MeleeComboDefinition` define el remate y una descarga independiente. `PlayerCombatController` coordina entrada, animación y resolución.
- Clic corto: combo normal 1 → 2 → 3. Mantener 1,05 s y soltar: descarga ×3 +2, frente a ×2 +1 del tercer corte. La descarga reinicia la cadena. Las cifras se suman sobre el daño del equipo. Se cancela al recibir una animación de daño, esquivar, abrir menús, pausar o perder foco.
- `CombatFeelRangeCue` ahora dibuja estelas y carga con dos mallas reutilizadas y `SwordEnergy.shader`, de aspecto pixelado. `HitFeedback`, `CombatTimeFeedback`, `CameraFeedback`, `CombatHitParticles` y `AudioFeedback` siguen separados. Señales de carga y descarga distintas, variaciones de sonido, retroceso frenado y colisión barrida. Se conserva la opción de reducir el impacto de cámara.
- `RaidTargetPolicy`: aldeano vivo más cercano en 7 unidades; fuera de esa prioridad, asignación alterna estable pozo/jugador. `FarmRaidNavigation` compara caminos normales con caminos sin muros: solo selecciona una brecha si el muro realmente cierra el paso. La forma no necesita ser un cuadrado perfecto. Los cuerpos móviles no se hornean como obstáculos estáticos.
- Un objetivo muerto se puede sustituir antes de la validación de la IA base. Arqueros: proyectiles reales contra aldeanos y estructuras; los muros abiertos sirven de cobertura sin convertirse por ello en objetivos de demolición.
- Construcción: encaje relativo a extremos existentes (incluidos los muros originales fuera de la cuadrícula global), primera pieza sobre retícula de media unidad, comprobación de huella física y motivos de rechazo concretos. Las zonas de interacción sin cuerpo no ocupan terreno. Se conservan bloqueo por obstáculos reales, cultivos, recursos y vecinos, pago tras validación, movimiento y guardado.
- `ConstructionPalette`: pestaña inferior y cajón que se despliega al acercar el cursor, se retrae al salir y no ocupa el mapa con controles invisibles. Colocación continua intacta.
- `WorldActionClock`: esfera y aguja junto al personaje, sin canvas ni barra de caracteres. `ResourceHoverHint`: niveles al señalar el arte completo, no solo el pequeño collider del tronco. Los ciclos de recolección y recompensas siguen perteneciendo a sus recursos.
- `FarmIntroduction`: oscurece alrededor del jugador, libera movimiento cuando se pulsa WASD, exige desplazamiento real, presenta un rival seguro, comprueba el tercer contacto y después una descarga cargada. Reloj de la noche detenido durante toda la práctica. El rival usa reacciones reales, no paga loot ni maestría, y se retira al terminar/saltar.
- HUD: contador ligero y tres marcas de día, ritmo de combo solo mientras se ataca. Maestrías en seis tarjetas con sus tres niveles conectados.

## Seguridad y pruebas

Antes de esta iteración: copia de scripts, pruebas y recursos en `Temp/BeforeCombatJuice-20260915`. El editor del proyecto original permanece fuera de las pruebas de lote; se usa `Temp/p`. No se modifican las partidas normales: ejecutables de comprobación con `--qa`.

La primera ejecución detectó que un `MaterialPropertyBlock` no se puede crear en el inicializador de un componente de Unity; se trasladó a la inicialización bajo demanda. Las pruebas cubren cargas cortas/completas/canceladas, daño sincronizado, retroceso bloqueado por pared, cierre abierto/cerrado, reparto de objetivos, retargeting al morir un aldeano, flechas reales, encaje, volúmenes de interacción, reloj de trabajo y tutorial completo.

## Resultados

- Familias afectadas: **83/83** aprobadas, tras corregir los fallos de la primera ejecución.
- Suite completa: **346/346**, cero fallos y cero omitidas; repetida después del ajuste de cierre del cajón. Informe: `Design/Validation/Portfolio/juice-final-tests.xml`.
- Main original y Main de comprobación tienen el mismo SHA-256. Los 629 archivos de código, pruebas, recursos serializados, shader y metadatos comparados coinciden con la copia probada. No se sobrescribieron los dos metadatos de muros que ya tenían cambios del usuario.
- Primer ejecutable: recorrido automático completo **PASS**, 32 capturas. Incluye tutorial de movimiento/combo/descarga, tres impactos animados, finisher de élite, construcción continua y reubicación, materiales, cuatro vetas, reloj de trabajo, guardado, tres noches, cuatro patrones del jefe, segunda fase y victoria.
- Revisión de las capturas: energía cargada, estela de descarga y flash visibles; tutorial oscurecido; maestrías sin texto recortado; reloj pequeño junto al personaje; panel de construcción abierto y retraído. La × de cerrar tenía altura insuficiente: se amplió su botón antes de repetir la suite y compilar la entrega.

## Límites y recuperación

La habilidad `computer-use` se utilizó para intentar comprobar teclado y ratón en la demo. El sistema devolvió `failed to activate captured window` también tras localizar de nuevo la ventana y reintentar. Se detuvo esa vía, sin tocar el editor del usuario. La ventana QA creada para la comprobación se cerró por su identificador y ruta exactos.

No se presenta la prueba automática como una partida humana completa: acelera las noches, prepara los recursos, teletransporta al jugador y elimina enemigos para recorrer los estados. Los impactos de práctica y combo sí atraviesan sus animaciones y resolución de daño reales. Falta validar subjetivamente el balance y el control con una partida humana de principio a fin.

El cierre del motor conserva el aviso conocido de liberación de `ComputeBuffer`; no es un error de compilación ni una excepción de gameplay. La ventana de prueba también registró un aviso del controlador Direct3D sobre timestamps, sin excepción. No se promete un registro totalmente libre de avisos.

Backup del ejecutable previo: `Builds/Portfolio-BeforeCombatJuice-20260915`. Se conserva también el ZIP de Valle vivo. El código previo a esta iteración está en `Temp/BeforeCombatJuice-20260915`.

## Entrega final verificada

- Segunda pasada del ejecutable final: **PASS**, terminada a las 03:35 del 15/09/2026 (Madrid), sin errores/excepciones de gameplay registrados. El aviso de cierre del motor descrito arriba permanece.
- Build: **Succeeded, errors=0**, 97.363.704 bytes; informe `Design/Validation/Portfolio/juice-final-build.txt`.
- Evidencias finales: `Design/Validation/Portfolio/CombatJuice/result.txt` y 32 PNG en la misma carpeta. Se confirmó visualmente que la × de cerrar ya aparece completa.
- Ejecutable: `Builds/Portfolio/SurvivalFarm.exe`. Distribución: `Builds/SurvivalFarm-Impacto-Windows.zip`, 38.827.040 bytes, 178 entradas. Excluye QA e información de depuración de Burst.
- Los **175 archivos del ejecutable distribuido** coinciden por SHA-256 con los comprobados. Se leyó además la DLL directamente dentro del ZIP para confirmar su identidad: `27A0033A612ECA0CCEA177392D878BB36CC3BDBD9554DF4492EE98D5799641D1` (`SurvivorFarm.Runtime.dll`). La comprobación detectó inicialmente una copia de carpetas anidada; se apartó en `Temp/CombatJuicePackagingRepair`, se rehízo desde carpetas vacías y se repitió la verificación antes de entregar.

Registros: `Logs/juice-final-tests.log`, `Logs/juice-final-build.log` y `Logs/juice-final-smoke.log`. La comprobación de espacios del diff de código/documentación no añadió incidencias; los espacios serializados de los dos metadatos previos del usuario se conservaron.
