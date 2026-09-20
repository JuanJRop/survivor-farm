# Survivor Farm

Proyecto Unity 2D existente. La escena Main incluye ahora la modalidad de
portafolio **Survival Farm: tres noches**, construida sobre su jugador, mapa,
combate, recursos, inventario, construcción y arte originales.

## Demo de portafolio

Abre `Builds/Portfolio/SurvivalFarm.exe`. Conserva toda la carpeta junto al
ejecutable. En Unity, abre la escena Main y pulsa Play.
Paquete actual: `Builds/SurvivalFarm-TinyRPG-CombatFX-Windows.zip`.

## Escenas de práctica

En el menú principal, abre **Escenas de práctica**. También puedes abrir en Unity
`Assets/SurvivorFarm/Scenes/ArenaCombate.unity` o `TallerGranja.unity` y pulsar Play.

- **Arena:** cinco oleadas repetibles y bestiario individual: limo, murciélago,
  gólem, rastreador, arquero, demoledor, Custodio, soldado, orco, demonio y monstruo
  de sangre. Los cuatro personajes de Tiny RPG usan sus animaciones originales.
- **Taller:** día sin límite para cultivar, plantar, regar, construir, mover,
  desmontar, talar, minar y cocinar. Incluye accesos rápidos a cada zona.

**F6** abre los controles de práctica: curar/reponer suministros, elegir niveles
de equipo, reiniciar, cambiar de área o volver al menú. No se leen ni escriben
partidas de campaña. Reiniciar descarta solo lo realizado en esa práctica.
Detalles y mantenimiento: `Design/Development/PRACTICE-SCENES.md`.

## Campaña de la demo

El menú distingue **Continuar, Nueva partida, Cargar partida y Opciones** y marca
claramente la versión como demo. Nueva partida conserva los guardados anteriores.

Prepara la granja durante tres días, defiende el pozo durante las noches y vence
al Custodio. Duración de diseño: aproximadamente 18–23 minutos. La noche llega
automáticamente, sin botón para adelantarla. El ritmo final requiere playtests humanos.
Una partida nueva enseña jugando: WASD libera el movimiento, un rival seguro
permite practicar el combo y después la descarga. El reloj espera durante la lección.

| Control | Acción |
| --- | --- |
| WASD / Shift | Caminar / correr |
| E / clic derecho | Interacción contextual, recoger, plantar, regar, cosechar, reparar |
| Clic izquierdo / 1 / 2 | Atacar / espada / arco |
| Mantener clic ~1 s y soltar (espada) | Cargar y descargar un golpe potente |
| Espacio | Esquiva, con recarga breve |
| Q | Comer una ración |
| F / I | Recetas y mejoras / mochila |
| Z / X / C | Paleta de construcción / trampa / ballesta |
| R / clic / Esc | Girar muro / colocar otra pieza / salir de construcción |
| M (en construcción) | Seleccionar una pieza colocada y moverla sin coste |
| B (en construcción) | Desmontar: devuelve el 69% de los materiales invertidos |
| K | Maestrías: usar, desbloquear y comprar el siguiente nivel |
| Esc | Pausa |

Las trampas y la ballesta se desbloquean el día 2. Cosechar devuelve semillas y
fruta; dos frutas producen una ración en las recetas. Las reparaciones cuestan
dos maderas y recuperan seis puntos de estructura. Las marcas del jefe muestran
el área que dañará; su recuperación permite daño doble.

La construcción permanece activa al colocar piezas; su cajón se abre al acercar
el cursor a la pestaña inferior y se retrae al volver al mapa. Usa primero las que hay en la
mochila y después los materiales indicados por los iconos. Los muros nuevos son
módulos de 2 unidades en horizontal y 4 en vertical: gira con R para cerrar esquinas y
deja un acceso. Puedes elegir piedra o reforzado y hacer clic sobre un muro
inferior para mejorarlo (coste de pieza completa; conserva la proporción de daño).
La mochila incluye iconos grandes, cantidades y una pestaña de construcción.

El valle jugable se extiende a 64 × 36 unidades, manteniendo la granja y el río.
Hay más madera al oeste y piedra al este. Las dos vetas de la cantera dan hierro;
los dos salientes alejados también dan oro mineral, usado en muros reforzados.
Cruza el puente abierto para llegar al saliente norte. E elige automáticamente el
pico: las vetas de hierro exigen nivel 2 y las de oro nivel 3, indicados al acercarte.
Las cuatro vetas se recuperan cada día. Regresa al pozo antes de que termine la preparación.

Las rocas y los árboles tienen tres niveles. Usa cada herramienta para desbloquear
su siguiente maestría y después cómprala en K; desbloquear no entrega equipo gratis.
Espada, arco, hacha, pico, azada y regadera disponen de tres niveles. Los objetivos
son cortos para la demo; recolectar recursos de nivel inferior no entrena el siguiente.
Los vecinos caminan con colisión, pueden morir a manos de enemigos y cada baja
reduce tres puntos la resistencia máxima del pozo. Las gallinas se pueden cazar.
La orilla superior del río está animada y sincronizada; el borde inferior muestra
césped estático. Las orillas mantienen al jugador sobre tierra. Los cortes, la
carga y los impactos de combate usan las animaciones originales de Combat FX.
Muros y casas se atenúan cuando ocultan al jugador. Al señalar un árbol o roca
aparece su nivel; un reloj pequeño junto al personaje muestra el trabajo en curso.
Los asaltantes priorizan vecinos vivos cercanos (7 unidades); sin esa prioridad,
se reparten de forma alterna entre pozo y jugador. Rodean muros abiertos y solo
buscan una brecha cuando los muros cierran realmente el paso al objetivo.

La espada encadena **corte → retorno → remate**. Haz clic de nuevo durante la
recuperación o en los 0,6 s siguientes. El tercer golpe hace daño ×2 +1 y tiene
impacto más fuerte; acabar con un demoledor así permite un remate visual breve.
Mantén clic 1,05 s hasta la señal dorada y suelta: descarga de daño ×3 +2,
mayor alcance y retroceso. Es una acción independiente que reinicia la cadena;
daño recibido, esquiva, menús o pérdida de foco interrumpen la carga.
En pausa puedes reducir shake, zoom y cámara lenta con «Impacto de cámara».
Los sonidos y partículas distinguen combate, madera, piedra, cajas y loot.
Detalle técnico y validación: `Design/Development/COMBAT-JUICE-2026-09-15.md`.

La demo guarda automáticamente durante la preparación, en ranuras `portfolio_*`
separadas de la campaña histórica. Continuar recupera el comienzo de la preparación
del día guardado con sus recursos y construcciones. Una derrota no sobrescribe ese
punto. Las pruebas usan archivos QA independientes.

## Desarrollo de la vertical slice

- Última iteración y validación: `Design/Development/WORLD-RENEWAL-2026-09-15.md`.
- Distribución actual: `Builds/SurvivalFarm-ValleRenovado-Windows.zip`.
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
