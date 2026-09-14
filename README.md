# Survivor Farm

Proyecto Unity 2D existente. La escena Main incluye ahora la modalidad de
portafolio **Survival Farm: tres noches**, construida sobre su jugador, mapa,
combate, recursos, inventario, construcción y arte originales.

## Demo de portafolio

Abre `Builds/Portfolio/SurvivalFarm.exe`. Conserva toda la carpeta junto al
ejecutable. En Unity, abre la escena Main y pulsa Play.

Prepara la granja durante tres días, defiende el pozo durante las noches y vence
al Custodio. Duración de diseño: aproximadamente 18–23 minutos; «Listo para la
noche» permite adelantar la preparación. El ritmo final requiere playtests humanos.

| Control | Acción |
| --- | --- |
| WASD / Shift | Caminar / correr |
| E / clic derecho | Interacción contextual, recoger, plantar, regar, cosechar, reparar |
| Clic izquierdo / 1 / 2 | Atacar / espada / arco |
| Espacio | Esquiva, con recarga breve |
| Q | Comer una ración |
| F / I | Recetas y mejoras / mochila |
| Z / X / C | Colocar barricada / trampa / ballesta automática |
| R / clic / Esc | Girar barricada / confirmar / cancelar colocación |
| Esc | Pausa |

Las trampas y la ballesta se desbloquean el día 2. Cosechar devuelve semillas y
fruta; dos frutas producen una ración en las recetas. Las reparaciones cuestan
dos maderas y recuperan seis puntos de estructura. Las marcas del jefe muestran
el área que dañará; su recuperación permite daño doble.

La demo guarda automáticamente durante la preparación, en ranuras `portfolio_*`
separadas de la campaña histórica. Continuar recupera el comienzo de la preparación
del día guardado con sus recursos y construcciones. Una derrota no sobrescribe ese
punto. Las pruebas usan archivos QA independientes.

## Desarrollo de la vertical slice

- Auditoría previa: `Design/Development/VERTICAL-SLICE-AUDIT-2026-09-14.md`.
- Configuración: `Data/ScriptableObjects/PortfolioSettings.asset` (duraciones,
  composiciones, límite de enemigos, vida del pozo y jefe, crecimiento visual).
- `PortfolioSession` controla estados; `FarmRaidDirector` controla oleadas;
  `RaidEnemy` extiende `EnemyAIBase`; `DungeonBoss` reutiliza la base del Custodio
  con los patrones de `FarmBossPattern`.
- `FarmingPlot` habilita seis parcelas de Main y mantiene dormidas las parcelas
  históricas. `ConstructionSystem` coloca las defensas y `FarmDefense` las conecta
  al contrato `IDamageable`.
- `SliceHud` complementa corazones/armas y reutiliza mochila y recetas.
  `FarmSoundscape` contiene música sintetizada original; `FarmAtmosphere` añade
  luz cálida local. El arte procede del pack ya presente.
- Para generar Windows: `Survivor Farm/Portfolio/Build Windows demo`.
- Pruebas: `PortfolioIntegrationTests` y las pruebas históricas en PlayMode.
- Copia anterior a los cambios: `Design/Validation/pre-vertical-slice-2026-09-14.zip`.
  Conserva scripts, escenas, pruebas y ProjectSettings; los assets gráficos no se
  sustituyeron. Los cambios previos sin commit pertenecen al proyecto original.

La campaña, las zonas de expedición y sus sistemas siguen en el proyecto.
Desactivar `PortfolioSession` en Main recupera el recorrido histórico. No ejecutar
el generador de escena para actualizar esta demo: su configuración es aditiva.

## Estructura

- `Assets/SurvivorFarm/Scenes`: escenas del juego.
- `Assets/SurvivorFarm/Scripts`: codigo separado por responsabilidad.
- `Assets/SurvivorFarm/Art`: sprites, tiles, animaciones, audio, materiales y VFX.
- `Assets/SurvivorFarm/UI`: pantallas, widgets, fuentes e iconos.
- `Assets/SurvivorFarm/Prefabs`: personajes, entorno y elementos de UI reutilizables.
- `Assets/SurvivorFarm/Data`: ScriptableObjects y datos de balance.
- `Assets/SurvivorFarm/Tests`: pruebas de editor y gameplay.

## Punto de inicio

La escena inicial es `Assets/SurvivorFarm/Scenes/Main.unity`.

## Recursos del mundo

Arboles, rocas y animales heredan de `HarvestableResource`: aparecen, reciben una
accion o dano, entregan su recompensa una sola vez y se desactivan con todos sus
renderers y colliders. `TreeResource` requiere hacha, `RockResource` requiere pico
y `AnimalResource` recibe ataques de espada o arco mediante `IDamageable`.

En `Main`, los `ResourceSpawnPoint` estan dentro de `02 Mundo/Outdoor World`.
Cada punto tiene un prefab, una instancia y un identificador de guardado. La
resistencia, la cantidad de recurso y el oro se ajustan en el asset `Definition`
referenciado por el prefab. Los animales dan comida, visible y consumible desde el inventario.

No hay regeneracion automatica. El menu contextual `Respawn Resource` del punto
de aparicion permite reutilizar su instancia. Entrar y salir de la tienda o
cargar una partida conserva los recursos agotados y el dano parcial.

`Survivor Farm/Upgrade Resource Lifecycle In Main` actualiza las referencias y
los puntos de aparicion de la escena existente. Las pruebas de esta mecanica
estan en `Assets/SurvivorFarm/Tests/PlayMode/ResourceLifecycleTests.cs`.

## Flyweights

Los assets de `Assets/SurvivorFarm/Data/Flyweights` almacenan los datos compartidos:
items (madera, piedra, fruta, comida, oro y las tres semillas), configuraciones de
recursos, animaciones y tiempos de cultivo. `ResourceFlyweights` reutiliza una
definicion por configuracion; `Data/Resources/FlyweightCatalog.asset` incluye esas
referencias en la build. No se clonan ScriptableObjects al generar instancias.

Vida actual, posicion, temporizadores, cantidad de un pickup y estado de cosecha
siguen perteneciendo a cada instancia. Los guardados no contienen definiciones
ni copias de sprites. Cambiar un asset en el editor cambia todos sus usuarios;
para otra recompensa o resistencia se usa una definicion diferente.

El menu `Survivor Farm/Migrate Main To Resource Flyweights` migra escena y prefabs
conservando las variantes por zona. La reconstruccion de Main tambien realiza
esta conversion. La optimizacion evita datos repetidos; GameObjects, colisiones
y texturas siguen teniendo su propio coste, por lo que no equivale a una
reduccion garantizada del APK. Las pruebas de identidad compartida y estado
independiente estan en `ResourceFlyweightTests.cs`.
