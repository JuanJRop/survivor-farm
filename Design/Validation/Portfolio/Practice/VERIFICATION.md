# Validación · escenas de práctica · 2026-09-15

## Resultado

- ArenaCombate y TallerGranja generadas, registradas en Build Settings y cargables
  directamente; Main y sus referencias serializadas originales se conservaron.
- Suite general: **364/364**, cero fallos, 231,79 s.
  Informe: `../practice-full-tests.xml`.
- La primera comprobación Windows detectó una referencia de interacción destruida
  durante el cambio de escena. Se corrigió `FarmPlayerInteractor`: las referencias
  a interfaces también comprueban la identidad nativa de Unity antes de acceder.
- Después de ese arreglo: **13/13** pruebas específicas, incluyendo una regresión
  nueva que destruye el objetivo resaltado antes de desactivar al jugador.
  Informe: `../practice-final-targeted-tests.xml`.
- Compilación Windows final: **Succeeded**, cero errores, 98.132.078 bytes.
- Ejecutable final con `--qa --practice-smoke`: **PASS**, 11 capturas y sin errores
  ni excepciones registrados durante la navegación de las escenas. Hay una
  advertencia de Unity al cerrar sobre liberación de ComputeBuffer; ver `player.log`.

La suite general de 364 casos es anterior al pequeño arreglo de interacción;
los 13 casos específicos y el ejecutable se verificaron después. No se presenta
como una nueva ejecución completa de 365 casos.

## Recorrido verificado

Menú de prácticas → arena → panel de oleadas → seis arquetipos combinados →
Custodio → victoria de práctica → taller → controles → cultivos maduros → dos
muros continuos → estación de minería y equipo III → vuelta al menú de la demo.

Los tests verifican además el circuito de cinco oleadas completo, repetir y cambiar
enemigos, pausa, recuperación tras morir, no cargar/escribir campaña, traslados
libres de colisión, tres niveles de recursos, maestrías, suministros acotados,
movimiento y demolición de construcción y arranque normal de Main.

Se revisaron visualmente las capturas de la arena, bestiario, jefe, taller y ambos
paneles. Son renders de la ejecución real. El recorrido es automatizado: no hubo
una sesión manual de teclado y ratón ni una validación humana de dificultad.

## Integridad

Main SHA-256, sin cambios:
`71CB23CEA38395C2D6B1E7923EEFB873AD1FCC6C2C4AFA591B0F2100F2645C3E`.

Runtime final SHA-256:
`9B1625DFF1605E2B2B2801ED99701848F596AC8E46F49E531EAB71424487AA3B`.

La compilación se realizó en `QA/p`, no en el editor abierto del usuario.
El paquete excluye capturas QA, símbolos PDB y carpetas DoNotShip.

ZIP final: `Builds/SurvivalFarm-Practicas-Windows.zip`, 176 archivos.
SHA-256: `BCCE28BC5BDB366ACCA394D2E78553512BF29287689C14C3591BC2A2D60CA9E0`.
El runtime dentro del ZIP coincide con el ejecutable comprobado.
Compilación anterior conservada en `Builds/Portfolio-AntesPracticas-20260915`.
