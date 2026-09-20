# Area E: UI y experiencia

Estado: implementacion entregada para integracion, 2026-09-09. No se ha ejecutado Unity.

## Contratos para coordinacion

- Se conservan `AdventureWindow.Open(string)`, `OpenChest`, `OpenFurniture`, `Rect`, los metodos de apertura/cierre de mochila, equipo y taller, `PlayerEquipmentWindow.AvailableCount` y los nombres de entradas del HUD.
- Diario lee `ValleyCampaign.Objective`, `InfluenceLine` (incluye `InfluenceTitle`), `VillageStatus`, `VillageBoard`, `VillagerGuide`, `VillageRepairCount`, `Chapter`, `NextRankRequirement` y `VillageServicesSummary`. No calcula rangos ni condiciones de los proyectos. Los servicios se usan junto al objeto; no se conceden recursos desde el diario.
- C: integrados `GetRecipeDescriptors`, `TryGetRecipeDescriptor`, `OutputId`, `OutputAmount`, `Ingredients`, `IsAvailable`, `CanCraft` y `UnavailableReason`. Se consulta una instantanea nueva al refrescar, se reconstruyen los iconos si cambian los ingredientes de `Food`, y se ejecuta `Craft(id)`. El alias `Coins` utiliza el sprite existente `Coin`. No hay costes de cocina/equipo ni desbloqueos del taller duplicados en UI.
- Integrada tambien la ampliacion final de C a todos los edificios: categoria Hogar mediante `BackpackActions.IsBuilding(OutputId)`, costes y bloqueos desde el descriptor. Cama/fogata ya colocadas o en mochila quedan bloqueadas por la consulta de C. Solo las mejoras de herramientas usan el adaptador a `PlayerToolUpgradeController.GetNextCost`. Sin dependencias pendientes de C/B.
- Se conservan `Configure`/`Refresh` existentes y el objeto HUD `Context` inactivo para regresion.
- No se solicitan cambios de escena, prefabs ni sprites.

## Cambios entregados

- HUD conserva tamano, posiciones, acciones iconicas y prompt junto al objeto. Comparte fuente y contraste con ventanas; cabecera de mision corta para evitar cortes; cantidades grandes de oro compactas.
- Diario con `Objetivo`, `Proyectos` y `Aldeanos`; progreso grafico por capitulos y reparaciones; texto de campana con altura medida y desplazamiento vertical. Se muestran requisito de rango y servicio publicados por B.
- Taller con categorias, filas sin marcos interiores, resultado original, cantidades, faltantes y requisitos del controlador. Ingredientes en filas de hasta cinco; razones largas aumentan la altura. Actualiza requisitos cada medio segundo solo mientras esta abierto.
- Equipo muestra diferencias positivas/negativas de las bonificaciones de piezas al pasar el cursor. Usa la misma seleccion de ranura para comparar y equipar; distingue mover un accesorio que ya esta equipado. No promete dano final ni recalcula bonificaciones de combate. Retrato original sin panel interior, nombres mas legibles y detalle desplazable.
- Mochila con nombres a 14 px y dos lineas, celdas recicladas en cuatro columnas, detalle desplazable y fuente/marco comunes. Se conservan cantidades, venta, uso, arrastre, categorias y APIs publicas.
- Tooltips sobre el canvas, fuera de las mascaras de listas, con nombre y cantidades exactas. Se ocultan al cerrar/clicar; barras de ingredientes no generan notificaciones.

## Archivos

En `Assets/SurvivorFarm/Scripts/Runtime/UI`: `AdventureWindow.cs`, `CraftingWindow.cs`, `HudActionTooltip.cs`, `MaterialCostBadge.cs`, `OriginalSpriteHud.cs`, `PlayerEquipmentWindow.cs`, `TutorialQuestSystem.cs`, `VisualBackpack.cs`; nuevos `FarmUiStyle.cs` y `UiEquipmentComparison.cs` con sus `.meta`.

Nuevos: `Assets/SurvivorFarm/Tests/PlayMode/UiConsistencyTests.cs` y `.meta`, `Design/Development/UiConsistency.Check.ps1`, este parte. No se ha escrito fuera de esta lista.

## API para QA

- `AdventureWindow.Open("Journal")`; `SelectJournalTab("Objective"|"Projects"|"Villagers")`; propiedad `JournalTab`.
- `CraftingWindow.Open()`; `SelectCategory("All"|"Cooking"|"Equipment"|"Home")`; `Refresh()` ahora publico.
- `PlayerEquipmentWindow.Preview(id)` permite capturar comparaciones sin simular hover; `AvailableCount` conserva su significado.
- Los nombres raiz `Taller`, `Player Equipment`, `Diario y construccion` (con su acento original en codigo) y entradas HUD se conservan.

## Verificacion

Ejecutado: `Design/Development/UiConsistency.Check.ps1`, resultado PASS de sintaxis Roslyn C# 9 en 11 archivos, presencia de 16 sprites y marco sliced, uso de contratos de recetas/campana y `Context` inactivo. Es analisis estatico, no compilacion ni render. `git diff --check` no informa errores de espacios en archivos rastreados del alcance; no valida archivos nuevos sin rastrear.

Preparado, NO ejecutado: `UiConsistencyTests` (7 casos contando las dos resoluciones): encaje de ventanas a 1280x720/1920x1080, ranura de accesorios, ganancias/perdidas de equipo, comparacion identica, cantidades exactas/sprite de oro y recetas/requisitos/ingredientes dinamicos con limites de celdas.

Pendiente de coordinacion: compilacion Unity, ejecutar las pruebas y QA visual/input a ambas resoluciones. Revisar texto largo de semillas, todos los ingredientes de recetas avanzadas, cocina bloqueada/desbloqueada, cambio automatico de `Food`, scroll del diario/comparaciones, arrastre de mochila/equipo, cierres y ausencia de prompt o `Context` permanente. No se declara validado el aspecto renderizado.
