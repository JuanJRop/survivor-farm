# B. Pueblo e historia

Estado: implementacion y correccion del primer QA entregadas, 2026-09-09. Verificacion aislada aprobada; volver a compilar/ejecutar PlayMode tras las ultimas correcciones corresponde a coordinacion. B no ha ejecutado Unity.

## Contratos publicados

- C: `ValleyCampaign.WorkshopRestored` ya implementado, devuelve `Data.nicoWorkshopRepaired`. Las recetas exclusivas pueden consultar este bool; C decide cuales y conserva las recetas iniciales necesarias para avanzar.
- E: `ValleyCampaign.WorkshopService` devuelve `VillageServiceStatus` con `Id`, `Title`, `Unlocked`, `UsedToday`, `CanUse`, `WoodCost`, `StoneCost`, `IronReward`, `Cost`, `Effect`, `UnavailableReason`, `Summary`. Es una consulta sin efectos. `VillageServicesSummary` y `NextRankRequirement` ya implementados. La UI no debe conceder recursos: el servicio se usara mediante la interaccion existente `village:workshop`.
- Servicio confirmado: 4 madera + 6 piedra por 2 hierro, una vez por dia; 3 hierro desde Protector. Nuevo `ValleyData.nicoSupplyDay` conserva el uso diario por el guardado existente. No se cambia SaveVersion ni se crea municion.
- Rangos: Ayudante 3; Vecino 8 + 1 proyecto; Protector 16 + 3 proyectos incluyendo taller y despensa; Lider 28 + los 5 proyectos. `rankRewardMask` conserva las recompensas historicas. El titulo se recalcula con los hitos; los objetos viejos no se retiran ni se vuelven a entregar.
- Coordinacion: `ValleyWorld` consume `VillageLayout.GetLot(id)`, posiciones y fachadas compartidas. Nico usa fachada 0 -> 1, ancho `HouseWidth`.
- C: la entrega de Dalia consulta el metodo ya implementado `PlayerInventory.GetAvailableSeedCount(SeedRarity.Common)` para excluir semillas reservadas.
- D: equivalencia integrada en `ValleyInteraction`, con `public bool IsGathering` y `public void CancelGathering()`. Libera solo su animacion propia y no modifica inventario ni discoveries al cancelar.

## Cambios entregados

- Taller: fachada original reparada, retirada de escombros de su obra, horno y herrajes del pack sin colliders nuevos. NPC con dialogo y trabajo tras reparacion. `TryUseWorkshopService()` aplica costes, reserva el dia antes de callbacks del inventario y guarda mediante `Changed`; el servicio no concede influencia.
- Cinco vecinos con tres momentos diarios, animacion y dialogo segun reconstruccion y campana. Se conservan anclas de NPC, pozo, banco, tablon, nota, altar y salidas. Interacciones disponibles tambien por la noche.
- Rangos comunitarios conservan `rankRewardMask`; las recompensas nuevas se marcan antes de concederse. Repetir la nota ya no genera influencia. El estado visible del pueblo refleja obras reales, aunque la historia principal este avanzada.
- El pozo no modifica hambre ni introduce sed; conserva el acceso al riego automatico existente. Nico al hablar explica el encargo; se paga al interactuar con su banco, no al conversar.
- Recoleccion de campana: cancela a distancia >1.35, al desactivar recurso/inventario/animador, al morir (observando StatsChanged incluso si revive en el mismo frame), al perder su clip o restaurar los datos de campana. No reemplaza Damage/Dead al cancelar. Acepta Idle si la animacion termina naturalmente en el plazo. Un solo impacto visual por frame tardio, sin Pulse ni notificaciones por golpe. Conserva tiempos, cantidades y claves diarias.
- Correccion QA: una despensa ya completada prevalece sobre la nota ausente en el dialogo de Mara. Atiende estados heredados sin alterar note ni simular progreso.

## Integracion de arte necesaria

`VillageNpcArtCatalog.cs` implementado. Coordinacion ya creo `Assets/SurvivorFarm/Resources/VillageNpcArt.asset`, clase `SurvivorFarm.Runtime.Gameplay.VillageNpcArtCatalog`; B comprobo sus referencias por lectura. GUID de script confirmado con coordinacion: `784a17facc494b4484c1a325c403f185`. Campo publico `Entry[] Entries`; clase anidada `[Serializable] public sealed class Entry { public string Id; public Texture2D Idle, Walk, Work; }`. No otros campos serializados. Referencias Texture2D: fileID 2800000, type 3. Fotogramas de 32x32, filas abajo/arriba/lateral; originales sin regenerar. El runtime carga `Resources.Load<VillageNpcArtCatalog>("VillageNpcArt")`.

Prefijo de todas las rutas de la tabla: `Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack/Character and Portrait/Character/Pre-made/`.

| Id | Campo | Ruta tras prefijo | GUID |
| --- | --- | --- | --- |
| village:elder | Idle | Manu/Idle.png | 717ccdb8aedf746479636549ae347073 |
| village:elder | Walk | Manu/Walk.png | 5f14ba2413326fe46aa0ff6ad71db11a |
| village:elder | Work | Manu/Idle.png | 717ccdb8aedf746479636549ae347073 |
| village:blacksmith | Idle | Alex/Idle.png | d1c87f7629c0c9a4d86c55bcbc821956 |
| village:blacksmith | Walk | Alex/Walk.png | 72e08fe9252bd5842ac38bbddc042b1d |
| village:blacksmith | Work | Alex/Pickaxe.png | d7a9f74e047c68b45b6eaccc6daf40ba |
| village:farmer | Idle | Lyria/Idle.png | 81bbb3a16ecba7742bbf40f3e48b9f57 |
| village:farmer | Walk | Lyria/Walk.png | 7b681e4ac9923dd43a5c3889519667bc |
| village:farmer | Work | Lyria/Watering.png | 6c18cbff3c3e83d46b8ba37164cb4c06 |
| village:merchant | Idle | Josh/Idle.png | 0a9de0dc08e7da44e99e33c7b06916f9 |
| village:merchant | Walk | Josh/Walk.png | 95b562cef37b1c44a85f7668c8fd49b0 |
| village:merchant | Work | Josh/Idle.png | 0a9de0dc08e7da44e99e33c7b06916f9 |
| village:guard | Idle | Tori/Idle.png | 5504620f4606c2c4795a330906e3fc9c |
| village:guard | Walk | Tori/Walk.png | 571405786eb47b4458abf4efee5e979e |
| village:guard | Work | Tori/Sword.png | 9bfd8be59ca660248b8f3ce74c7b3101 |

`ValleyWorld` ya consume `GetLot`, usa fachada real reparada y vincula escombros al proyecto correspondiente. Las rutinas mueven solo el hijo visual (hasta 0.43 unidades) para mantener anclas/alcance/puertas existentes; se detienen cerca del jugador. `GetVillagerDialogue(id)` y `GetVillagerActivity(id)` son consultas adicionales para E. Horarios 05-10 revision, 10-18 trabajo, resto descanso/guardia. Ninguna entrega queda bloqueada por horario.

## Verificacion

Ejecutado: `Tools/VillageServicesStaticCheck.ps1`, resultado PASS. Roslyn analiza sintaxis de los 7 archivos C# de B; ejecuta 30 comprobaciones aisladas usando las declaraciones reales de ValleyData, VillageServices y los metodos narrativos de VillageResidents en memoria, sin dependencias ni simulaciones de Unity. Cubre rangos, falta de hitos/puntos, preservacion del mask en consultas, requisitos y renovacion diaria del servicio, reloj atrasado, ausencia de RestoreHunger en el pozo, dialogo heredado de Mara, cancelacion sin concesion y ausencia de Pulse por golpe. No equivale a compilar el proyecto de Unity.

Inspeccionados visualmente los atlas originales de casas, cuatro variantes de personaje y las animaciones Pickaxe, Watering y Sword. No son capturas del juego ejecutandose ni pruebas humanas de jugabilidad.

Primera tanda ejecutada POR COORDINACION: el XML `Design/Validation/Team/playmode-results.xml` contiene 19 casos de B, 18 aprobados y 1 fallo de dialogo de Mara. Corregido en fuente y comprobado aisladamente; falta repetir ese caso en Unity. Esa instantanea precede a las pruebas de arte/fachada y recoleccion anadidas despues.

Suite actual: 31 casos NUnit en `VillageServicesTests.cs`, NO ejecutados por B. Incluye transacciones reales de inventario, callbacks reentrantes, idempotencia de recompensas/nota, JsonUtility, pozo, dialogos/rutinas, cinco variantes de arte, fachada sin cambiar ancho/posicion/collider y diez casos de cancelacion/finalizacion de recoleccion de campana. Ejecutar en escena de pruebas aislada sin GameSaveSystem activo. El fixture mantiene inactiva la campana y no llama a Awake ni construye el mapa completo.

Pendiente de coordinacion: compilacion Unity y ejecucion PlayMode de `SurvivorFarm.Tests.VillageServicesTests`, regresion ValleyVerification/VillageLayoutVerification y comprobar presentacion/alcance de NPC y acceso al banco en el juego. No se han modificado SaveVersion, escenas, prefabs, UI, inventario/crafting, Packages, ProjectSettings, ramas ni commits.

## Archivos de B

- `Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ValleyCampaign.cs` y `ValleyWorld.cs`.
- Nuevos `Gameplay/VillageServices.cs`, `VillageResidents.cs`, `VillageNpcRoutine.cs`, `VillageNpcArtCatalog.cs` y sus `.meta` (GUID del catalogo acordado con coordinacion).
- Nuevos `Assets/SurvivorFarm/Tests/PlayMode/VillageServicesTests.cs` y `.meta`.
- Nuevo `Tools/VillageServicesStaticCheck.ps1` y este parte.
- El asset `Resources/VillageNpcArt.asset` y VillageLayout.cs son de coordinacion; B solo los ha leido/consumido.
