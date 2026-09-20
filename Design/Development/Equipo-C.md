# Area C: economia y agricultura

Estado: primera entrega implementada, 2026-09-09. Validacion integrada pendiente de coordinacion. No se ha lanzado Unity ni se han cambiado ramas o realizado commits.

## Contrato para E (UI)

Namespace `SurvivorFarm.Runtime.Player`.

- `PlayerCraftingController.GetRecipeDescriptors(): IReadOnlyList<EconomyRecipeDescriptor>`.
- `PlayerCraftingController.TryGetRecipeDescriptor(string id, out EconomyRecipeDescriptor recipe): bool`.
- Descriptor: propiedades publicas `string Id, Name, OutputId, UnavailableReason`; `int OutputAmount`; `IReadOnlyList<EconomyIngredientDescriptor> Ingredients`; `IReadOnlyList<EconomyRequirementDescriptor> Requirements`; `bool IsAvailable, CanCraft`.
- `IsAvailable` evalua requisitos; `CanCraft` anade ingredientes suficientes; `UnavailableReason` devuelve la primera condicion bloqueante o los ingredientes faltantes, y cadena vacia si se puede fabricar.
- Ingrediente: `string ItemId, Name`; `int Required, Owned, Missing`.
- Requisito: `string Id, Description`; `bool IsMet`.
- Son instantaneas: volver a consultar al refrescar la UI. Ejecutar mediante `Craft(descriptor.Id)`.
- IDs de cocina: `Food` (compatibilidad y seleccion automatica, prioriza Fruit), `CarrotSoup`, `TravelRations`, `WorkshopRations`.
- Todas producen el recurso existente `Food`, incrementan `MealsCooked` por accion y conservan el tutorial. No se crean objetos nuevos de comida.
- La lista incluye Sword, las 29 recetas actuales de equipo y Campfire/Fence/Chest/Workbench/Beacon/Bed/Cabinet/Furnace. Construccion consulta `ConstructionSystem.Cost`, `Label` y `HouseSystem.Requirement`, sin copiar precios de construccion. `OutputId` permite resolver el icono con la ruta actual de UI (`Food` para todas las comidas).
- E: los edificios tambien llegan ahora por descriptor; clasificar `BackpackActions.IsBuilding(descriptor.OutputId)` como hogar. El ID de ingrediente monetario es `Coins`, su icono existente es `Coin`. Se ha comprobado por lectura que CraftingWindow ya consume la API de descriptores; no se ha editado ese archivo.
- `SurvivalItemCatalog.Definition.Price` conserva su significado de venta; se anaden `BuyPrice` y `SellPrice`.
- `SimpleShopSystem.CanBuyCatalogItem(string id, out string requirement): bool` y `GetAvailableCatalogItems(): IEnumerable<SurvivalItemCatalog.Definition>` para disponibilidad progresiva.

## Enlace con B y A

- B: verificada propiedad publica `ValleyCampaign.WorkshopRestored` en codigo. `WorkshopRations` la consume; no bloquea cocina inicial ni equipo existente.
- D: `EquipmentDamage` conserva `public int`; `EquippedDefinitions` se expone como `public IEnumerable<EquipmentItems.Definition>` (consulta sin setter). No se modifica EquipmentItems.
- A/coordinacion: `FarmingPlot.PersistentId` se conserva intacto. El enlace al helper corresponde a coordinacion tras esta entrega.
- Reserva: `TryReserveSeed(out EconomySeedReservation)`, `CommitSeedReservation(EconomySeedReservation)` y `ReleaseSeedReservation(EconomySeedReservation)` en PlayerInventory. El token expone Rarity, SeedItemId, CropItemId e IsActive. Conteos disponibles mediante `GetAvailableItemCount(string)` y `GetAvailableSeedCount(SeedRarity)`; los conteos originales y ItemStacks mantienen la cantidad serializable, incluidas reservas.
- FarmingPlot conserva estado sin plantar durante la reserva. Al completar descuenta una vez y publica SeededDry; movimiento superior a 0.45, dano, muerte, desactivacion o `CancelAction()` liberan la reserva. Cargar invalida reservas anteriores. Guardar a mitad conserva semilla y tierra sin plantar usando los campos actuales; no se cambia GameSaveSystem ni el formato.

## Comportamiento entregado

| Cultivo inicial | Crecimiento tras regar | Rendimiento | Papel |
| --- | --- | --- | --- |
| Zanahoria | 12 s | 2 | Sopa rapida |
| Patata | 28 s | 3 | Provisiones |
| Trigo | 40 s | 4 | Cocina en cantidad |
| Fresa | 60 s | 3 | Venta de cosecha valiosa |

Se conservan los 91 IDs (37 alimentos, 37 semillas/brotes, 9 gemas, 7 esencias y oro bruto). El surtido de semillas y alimentos y los hallazgos de semillas de catalogo se amplian al cocinar 1 y 6 veces. Una semilla ya poseida se puede plantar siempre. Los brotes siguen siendo cultivos de una cosecha; su descripcion ya lo aclara. Cultivos genericos conservan tiempos y recompensas previos. No se generan ni sustituyen sprites.

| Craft ID | Ingredientes | Raciones Food | Requisitos |
| --- | --- | --- | --- |
| Food | 2 Fruit; si no alcanza, selecciona una receta basica asequible | 1 o 2 segun descriptor | Fogata |
| CarrotSoup | 2 Carrot | 1 | Fogata |
| TravelRations | 2 Potato + 1 Wheat | 2 | Fogata |
| WorkshopRations | 2 Potato + 2 Wheat + 1 Wood | 3 | Fogata + WorkshopRestored |

MealsCooked aumenta una vez por accion, incluida la cocina con catalogo. Ingredientes y resultado se aplican juntos; el evento de inventario ya ve el contador de cocina actualizado. Los faltantes o el limite de raciones no consumen ingredientes.

Compra del catalogo = techo(1.5 x precio de venta); la venta conserva Price. Comprar ingredientes y revender las raciones no genera oro gratis con estas cuatro recetas. Mochila y tienda comparten venta y controles de desbordamiento. No se descuenta producto cuando el oro no cabe ni monedas cuando la compra desborda. Las recetas de cocina suman correctamente pilas duplicadas de catalogo.

## Archivos

Modificados dentro de `Assets/SurvivorFarm/Scripts/Runtime`: Gameplay/FarmingPlot.cs, Gameplay/CultivationDefinition.cs, Player/SurvivalItemCatalog.cs, Player/PlayerInventory.cs, Player/PlayerCraftingController.cs, Player/BackpackActions.cs y UI/SimpleShopSystem.cs.

Nuevos: Player/EconomyRecipes.cs, Player/EconomySeedReservation.cs y Player/EconomyTradeRules.cs; `Assets/SurvivorFarm/Tests/PlayMode/EconomyTests.cs`, todos con .meta. Documentacion y verificacion local: este parte y `Design/Development/Economy-StaticChecks.ps1`.

## Verificacion

- Ejecutado `& 'Design/Development/Economy-StaticChecks.ps1'`: PASS, 113 comprobaciones. Compila y ejecuta exclusivamente datos puros de catalogo/recetas/precios en PowerShell; verifica IDs, progresion, perfiles, ingredientes, compra/cocina/reventa y limites. Analiza sintaxis C# de los archivos de C y sus pruebas con Roslyn. No es una compilacion integrada de Unity.
- Ejecutado `git diff --check` limitado a los siete archivos originales del area: sin errores de whitespace; solo avisos de conversion LF/CRLF de Git.
- Creadas 13 pruebas NUnit PlayMode en `SurvivorFarm.Tests.EconomyTests`, NO EJECUTADAS. Cubren ruta Food, fogata, catalogo, faltantes, estado visible en eventos, taller opcional, equipo inicial, compra/venta, reservas, snapshot durante siembra, interrupcion, limites y pilas duplicadas.
- Pendiente de coordinacion: compilar Unity 6000.3.9f1, ejecutar EconomyTests y QA integrada/tutorial, verificar visualmente precios/categorias de UI E, e integrar el helper de identidad de A. La prueba de snapshot usa los campos publicos actuales; no sustituye una prueba integrada de guardado a disco.
- Los tiempos, rendimientos y progresion son un primer balance implementado; no se ha realizado playtest humano.
