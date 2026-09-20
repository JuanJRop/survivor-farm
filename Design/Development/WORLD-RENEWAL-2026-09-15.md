# Survival Farm · renovación del valle

## Alcance

Iteración sobre Main y los componentes existentes; no se regenera la escena ni se
reemplaza el asset pack. Se conservan combate, combo, carga, loot, guardados,
maestrías, mapa y oleadas. Los cambios de presentación se conectan desde
`PortfolioFarmSetup` y sólo afectan a la demo.

## Sistemas

- `TreePresentation`: un sprite completo del pack, profundidad por pie y tronco
  sólido. Los árboles decorativos del pueblo pasan por `TreeResource`, `ResourceTier`
  y `ResourceSpawnPoint`. No se difuminan; las casas y muros conservan su transparencia.
- `PackEnvironment`: orillas de cuatro fotogramas, fogata, puente alternativo con
  barandas, mariposas, ramas y liebres usando exclusivamente texturas existentes.
  Los árboles quedan fuera del agua, también al recuperar una posición guardada.
- `ForagePlant`: retirar vegetación con E proporciona semillas o frutos una sola
  vez. El despeje se conserva en el guardado; no ocupa una herramienta nueva.
- `DemoFrontEnd` + `PortfolioSaveCatalog`: Inicio, Continuar, Nueva partida, Cargar,
  Opciones y Salir. Sólo se enumeran guardados propios válidos; crear otra partida
  mantiene los anteriores. Volumen e impacto reducido persisten en preferencias.
- `PetAdoption`: el gato no está equipado de entrada. Desde el día 2, Rolo lo vende
  por 180 monedas, 12 hierro y 3 oro mineral. Propiedad persistente, sin compra doble.
- `ConstructionSystem` + `DemolitionRefund`: M selecciona una pieza; su vista previa
  queda en el cursor hasta otro clic. Esc conserva el original. B activa desmontar:
  devuelve el 69% de cada material, redondeado hacia abajo. Las piezas gratuitas
  iniciales no generan recursos. Las mejoras acumulan el coste invertido. Una pieza
  destruida durante su traslado no se puede resucitar.
- `VillageHouseHealth`: 32 puntos, daño enemigo, ruinas y persistencia; no modifica
  la condición de derrota del pozo. Sin vecinos próximos, los demoledores pueden
  atacar una casa cercana. Perderla desactiva sus servicios y colisión.
- `WorldHealthReadout`: barra pequeña temporal tras recibir daño, sin etiquetas
  permanentes. `PlayerFootsteps` sólo suena con desplazamiento real. AudioFeedback
  añade pisadas y voces distintas de gallina/liebre; conserva variación y pool.

El alcance de espada pasa de 1,25 a 1,60; no se agranda el cuerpo vulnerable del
jugador. La petición «haz una barra» se interpreta como vida temporal de enemigos.

## Límites y decisiones

La devolución de partidas antiguas sin contabilidad usa el coste base de la pieza:
no se puede reconstruir una inversión histórica que nunca se guardó. Recoger una
pieza mejorada en la mochila conserva su tipo, no el historial de mejoras anteriores.
Los materiales del cofre deben retirarse antes de desmontarlo.

Propuesta pendiente de elección: sustituir el protagonismo de murallas cerradas por
defensa de puntos (trampas y puestos mejorables junto a casas/pozo). No se implementa
un cambio de bucle principal sin que el usuario elija esa dirección.

## Validación

Completada en `QA/p`, fuera de `Temp`: 355/355 pruebas PlayMode aprobadas en
`Design/Validation/Portfolio/renewal-delivery-tests.xml`. Incluye una esquiva real
contra un tronco, compra del gato, reembolso acumulado, traslado/cancelación,
destrucción durante traslado, ataque real de demoledor a casa, retarget tras
destrucción, guardados, vegetación y animación de fogata después de cargar.

Windows: compilación correcta con cero errores y recorrido automático hasta el
final de las tres noches: PASS. Se conservaron 42 capturas en
`Design/Validation/Portfolio/WorldRenewal`; allí están los registros y límites de
la validación. La pasada visual corrigió UV de las orillas, salientes de postes,
variante del puente, restos de las casas y decoración antigua que reaparecía
al cargar. El playtest humano completo y el control manual con teclado/ratón
siguen pendientes; no se presentan las pruebas automáticas como equivalentes.

Ejecutable: `Builds/Portfolio/SurvivalFarm.exe`. Distribución:
`Builds/SurvivalFarm-ValleRenovado-Windows.zip`. La versión anterior se conserva
en `Builds/Portfolio-Impacto-Preservado-20260915`; copia previa adicional en
`Builds/Portfolio-BeforeWorldRenewal-20260915`. No se han borrado partidas.
