# Area D: combate y recoleccion

Estado: implementacion entregada; compilacion Unity y QA integrado pendientes de coordinacion. Fecha: 2026-09-09.

## Contratos para C, E y coordinacion

- `EquipmentItems.DamageBonusFor(PlayerInventory inventory, FarmTool tool)` calcula dano de equipo aplicable al arma activa. Mantiene gemas/accesorios/elementos y excluye solamente armas de otro tipo. `PlayerInventory.EquipmentDamage` conserva su API y significado agregado.
- `PlayerCombatController.GetAttackDamage(FarmTool tool)` devuelve dano final de espada/arco, incluida mascota; las mejoras de espada de aventura y fabricacion siguen siendo exclusivas de espada. `ProgressionDamage` se conserva como bono de espada, sin mascota ni dano base.
- `HarvestableResource.CancelGathering()` cancela sin recompensa y libera solo su propia animacion. `ValleyEnemy.AttackRadius` y `PhaseRemaining` exponen radio real y segundos restantes para consulta/QA.
- Espada conserva impacto inmediato, circulo completo, deduplicacion por objetivo y bloqueo por paredes, como exige `SwordAreaTests`.
- B: `ValleyInteraction` en `ValleyCampaign.cs` tiene su propia recoleccion temporizada y no queda cubierta por `HarvestableResource`. Necesita cancelacion al alejarse/desactivarse, comprobar que sigue su animacion y evitar `Pulse("Golpe ...")`. D no modifica ese archivo. `PlayerCharacterAnimator.CancelAction()` ya existe.
- Auxiliares nuevos con prefijo `CombatFeel`; `Resources/CombatFeelVisuals.asset` referencia el sprite importado `Arrow_17`. Sin regenerar imagenes ni tocar importadores. Incluir el asset y sus `.meta` al integrar.

## Cambios entregados

- Espada: contorno circular tenue de 1.25 unidades durante 0.18 s, reutilizado entre ataques; material liberado al destruir el componente. Conserva impacto inmediato y reglas de `SwordAreaTests`.
- Armas: bono especifico segun espada/arco. Gemas, accesorios, elementos y mascota siguen sumando; temple y nivel de fabricacion siguen aplicandose a espada. `EquipmentItems.Effect` identifica los bonos de espada/arco.
- Arco: salida durante la animacion, con cancelacion si se interrumpe antes de soltar; sin animador conserva disparo inmediato. Revalida generacion, alcance y cobertura al soltar. Cooldown visual usa la duracion real reservada.
- Flechas: sprite existente compartido, sin texturas/sprites nuevos por disparo; orientacion inicial correcta. Comprueban paredes tambien durante el vuelo, ignoran objetivos destruidos/reutilizados y reservan el impacto antes de callbacks para evitar dano duplicado.
- Enemigos: marca fina con radio real (1.1 cuerpo a cuerpo, 1.7 salto) y aviso de tiempo restante. Agotamiento del jefe distinguible por postura, color y segundos restantes. Se mantienen preparacion de 1.3 s, vuelo de 0.55 s, agotamiento de 2.5 s, inmunidad en vuelo, dano doble y pulso de curacion de brotes por ciclo.
- Reinicios: salir de zona, morir o desactivar limpia aviso/postura/HUD del encuentro. Destruir el enemigo libera su material y sus objetos auxiliares.
- Recoleccion: varios impactos sincronizados al ciclo del clip; una sola respuesta visual si se acumulan pasos por un frame lento. Cancela por distancia mayor de 1.35, muerte, desactivacion, restauracion o cambio de animacion. La muerte se observa por evento para impedir continuar tras revivir en el mismo frame.
- Feedback: eliminados textos por golpe, recarga y disparo; se conserva una notificacion de recompensa al completar. Mantiene cantidades, monedas, bonus de herramienta y recompensas unicas. No se cambia economia ni reaparicion amable.

## Archivos

Modificados: `Runtime/Player/PlayerCombatController.cs`, `Runtime/Player/EquipmentItems.cs`, `Runtime/Gameplay/ValleyEnemy.cs`, `Runtime/Gameplay/HarvestableResource.cs`, `Runtime/Gameplay/ArrowProjectile.cs` (todos bajo `Assets/SurvivorFarm/Scripts`).

Nuevos: `Runtime/Gameplay/CombatFeelVisuals.cs`, `Runtime/Gameplay/CombatFeelRangeCue.cs`, `Assets/SurvivorFarm/Resources/CombatFeelVisuals.asset`, `Assets/SurvivorFarm/Tests/PlayMode/CombatFeelTests.cs`, sus cuatro `.meta`, `Tools/CombatFeelStaticChecks.ps1` y este parte.

`TreeResource.cs` y `RockResource.cs` revisados sin cambios: mantienen tiempos/recompensas y reciben las mejoras de su base. No se editaron archivos compartidos de pruebas ni archivos de otros responsables.

## Validacion

Ejecutado: `& 'Tools/CombatFeelStaticChecks.ps1'`, resultado satisfactorio. Comprueba sintaxis C# mediante Roslyn en 10 archivos, enlace del sprite `Arrow_17`, GUID del ScriptableObject, presencia de `.meta` y ausencia de generacion de sprites por flecha. Ademas ejecuta `EquipmentItems.cs` real con un adaptador minimo de inventario: ocho combinaciones para ambas armas y el caso de inventario nulo. No es una compilacion Unity.

Ejecutado: `git diff --check` acotado a los tres archivos modificados ya versionados, sin errores de espacios (solo avisos LF/CRLF). Inspeccion visual del atlas original de flechas para verificar la variante horizontal.

Anadidas, NO ejecutadas: 22 casos PlayMode en `CombatFeelTests`. Cubren armas y progresion, circulo y colliders duplicados, sprite compartido, disparo/interrupcion, cobertura en vuelo, generacion obsoleta, reentrada de impacto, cancelaciones de recoleccion, pago unico, mejora de herramientas, inmunidad/ventana del jefe, curacion de brotes y radios/reinicio.

Coordinacion: ejecutar `CombatFeelTests`, `SwordAreaTests`, `ResourceLifecycleTests`, `CombatLootTests` y la verificacion existente de campana. Revisar en Play visibilidad de contornos, encaje del HUD del jefe y ritmo de primera expedicion. No se han medido FPS ni realizado pruebas humanas.

No se ha lanzado Unity ni modificado escenas, prefabs, Packages, ProjectSettings, ramas o commits.

## Correccion tras el primer QA integrado

Revisado `Design/Validation/Team/playmode-results.xml` y, en lectura, el IL del ensamblado ejecutado. El fallo de `LegacySwordProgressionStaysExclusiveToSword` no procede de parametros de Restore equivocados: esa version usaba reflexion. `PlayerInventory.Awake` ya anade `AdventureProgress`; la prueba anadia otro y activaba el temple en la segunda instancia, mientras combate consultaba la primera. Se reutiliza la instancia existente y se preparan los datos con `AdventureProgress.Restore` y `PlayerCraftingController.Restore(savedStorageLevel: 0, savedCampLevel: 0, savedWeapon: 3)`. Se conservan espada=5/arco=3 y se anaden precondiciones de instancia unica, temple, nivel 3 y bono de progresion 4. No cambia el calculo de dano.

`ResourceLifecycleTests.AnimalInteractionUsesSwordCooldownAndBowProjectile` registraba "No hay un objetivo al alcance". La seleccion sin camara aplicaba por defecto un cono hacia abajo, descartando el animal a la derecha. `PlayerCombatController` usa ahora direccion cero cuando no hay `Camera.main`: busca el objetivo visible mas cercano, conserva radio/paredes y sigue aplicando el cono cuando existe camara. Se anaden dos casos D (animal con/sin cobertura) sin tocar la prueba legacy.

Archivos de esta correccion: `PlayerCombatController.cs`, `CombatFeelTests.cs` y este parte. Los errores de importacion `Animator.Library`/`CombatFeelVisuals` quedan exclusivamente en manos de coordinacion; no se modifican assets, GUID, cache ni copia QA. Los nuevos casos PlayMode quedan pendientes de ejecucion por coordinacion.

Verificacion de esta correccion: `Tools/CombatFeelStaticChecks.ps1` satisfactorio (sintaxis de 10 archivos, referencias y ocho combinaciones de equipo para ambas armas mas inventario nulo); `git diff --check` del controlador sin errores. Coordinacion confirma despues auditoria Unity `Animation=True Arrow=True Villagers=True` en el clon, resuelta mediante su cache/importacion sin cambios a recursos fuente. D no ejecuta Unity ni atribuye esa auditoria a sus comprobaciones. Pendiente repetir PlayMode con estas ultimas fuentes.
