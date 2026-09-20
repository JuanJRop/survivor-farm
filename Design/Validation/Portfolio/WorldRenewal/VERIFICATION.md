# Valle renovado · validación

15/09/2026. Unity 6000.3.9f1, Windows x64, RTX 3050 Laptop.
El proyecto de pruebas es `QA/p`: el editor original y Main no se han guardado
ni regenerado. Las partidas normales no se utilizan para QA.

## Ejecutable

Suite completa de entrega: **355/355 aprobadas**, cero fallos ni omitidas
(`../renewal-delivery-tests.xml`). Incluye el caso que impedía mantener ocultas
las cajas y vallas descartadas tras cargar.

Compilación correcta, cero errores (`build.txt`). Recorrido completo automático:
PASS (`result.txt`). Cuarenta y dos capturas de menú, tutorial, combate, entorno,
construcción, exploración, cultivos, carga, noches, jefe y final.

Revisadas especialmente: Inicio/Opciones/Cargar; tronco opaco delante y detrás;
orillas sin árboles arraigados en agua; puente de tablas rojizas; muros sin brazos
laterales; traslado; modo desmontar; vida de casas y ruinas; impacto del combo;
noche, jefe y final. La última captura nocturna confirma que cargar no resucita
las cajas y vallas decorativas retiradas.

La prueba usa las interfaces reales de combate, cosecha, construcción y guardado,
pero acelera el reloj, concede materiales y teletransporta al jugador. No demuestra
el balance ni sustituye una partida humana completa. La comprobación manual con
teclado/ratón quedó pendiente de autorización para abrir una ventana visible; la
sesión oculta no expuso una ventana controlable a la herramienta de aplicaciones.
Se cerró sólo ese proceso de QA, no el editor del usuario.

El registro gráfico contiene `d3d12: failed to query info queue interface`; no se
observaron excepciones de scripts ni errores de juego durante el recorrido. No se
presenta el registro como absolutamente libre de avisos del motor.

## Integridad de esta compilación

- `SurvivorFarm.Runtime.dll` SHA-256:
  `C4ECEEEE052A61CE267E159C8342C1E9497E5D4D010FE8F88E2E58841E10FF05`
- `data.unity3d` SHA-256:
  `8623DD890B7D12030485CE3EC58A0FCBB9F40B90EB5EF9EBEE6534D6C15CAAEE`
- Main original SHA-256:
  `71CB23CEA38395C2D6B1E7923EEFB873AD1FCC6C2C4AFA591B0F2100F2645C3E`

Los informes XML de la batería están un nivel arriba, con prefijo `renewal-`.
Los primeros registran las regresiones encontradas y corregidas; el informe de
entrega es `renewal-delivery-tests.xml`.

## Paquete publicado

`Builds/SurvivalFarm-ValleRenovado-Windows.zip`: 38.845.539 bytes, 176 archivos.
Se verificó que contiene LEEME, que la DLL coincide con la compilación comprobada
y que no incluye partidas QA, símbolos PDB ni directorios DoNotShip.
SHA-256: `7F20CC9F8E42D17626D13E81B4EB10D0F7D6D33D9B64716A5420043F91126A65`.
