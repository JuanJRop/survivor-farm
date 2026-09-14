# Survival Farm · entrega de la vertical slice

14 de septiembre de 2026 · Unity 6000.3.9f1 · Windows x64.

## Jugar

Ejecutar `Builds/Portfolio/SurvivalFarm.exe` conservando sus archivos y carpetas
acompañantes. No requiere el editor. En Unity, abrir la escena Main y pulsar Play.
La pantalla inicial ofrece una partida nueva y, cuando existe, continuar.

La experiencia conecta tres días de preparación, tres defensas nocturnas y el
Custodio final. La duración prevista es 18–23 minutos sin adelantar los días;
no es una duración medida con jugadores. «Listo para la noche» adelanta el día.
Proteger el pozo es tan importante como mantener vivo al personaje.

WASD mueve; Shift corre; E interactúa y elige herramienta contextual; clic ataca;
1/2 seleccionan espada/arco; Espacio esquiva; Q consume ración; F abre recetas;
I abre mochila. Z abre la paleta continua; X/C seleccionan trampa/ballesta;
R gira muros, clic coloca otra pieza; Esc o clic derecho terminan la construcción.
La mochila y el taller también tienen acceso a la paleta. Esc fuera de menús abre la pausa.

Ampliación de fortaleza: módulos de 2 unidades, empalizada → piedra → reforzado
(hierro y oro). Seleccionar material superior sobre un muro permite mejorarlo
con el coste completo, conservando el porcentaje de daño. El valle ahora abarca
64 × 36 unidades sobre el mismo terreno: bosque al oeste, cantera al este y
salientes minerales al noreste/sureste. El puente permanece abierto en la demo,
también al cargar partidas anteriores. El pico contextual extrae 3 de hierro por
veta; los salientes dan además 2 de oro. Cada veta se recupera al día siguiente.

## Recorrido y decisiones

- Día 1: madera al oeste, piedra al este, seis cultivos al sureste y barricada
  dañada al sur. Plantar y regar; crecimiento de 35 segundos; cosechar devuelve
  dos frutas y una semilla. Dos frutas permiten fabricar una ración.
- Noche 1: nueve enemigos y primera defensa. Las entradas se anuncian antes del
  spawn. Reparar cuesta dos maderas y recupera seis puntos de estructura.
- Día 2: se desbloquean trampa y ballesta; cuatro unidades de hierro permiten
  elegir entre mejora de arma y apoyo defensivo.
- Noche 2: catorce enemigos; los demoledores presionan estructuras, los arqueros
  mantienen distancia y los perseguidores obligan a moverse.
- Día 3: preparación final y revelación del Corazón bajo el pozo.
- Noche 3: dieciocho enemigos y después el Custodio. Salto, abanico de proyectiles,
  raíces e invocación tienen anticipación y recuperación. Al 50% cambia la
  secuencia y el ritmo; atacar durante la recuperación hace daño doble.
- Victoria: transición y pantalla de cierre. Muerte del jugador o destrucción
  del pozo: derrota y opción de volver al inicio.

El límite es ocho enemigos normales simultáneos; las invocaciones del jefe
tienen un límite adicional. Las oleadas son finitas. Las defensas ayudan, pero
no sustituyen al jugador: las trampas tienen recarga y la ballesta exige visión.

## Qué se conservó y cómo se conectó

La auditoría se escribió antes de modificar gameplay. No se regeneró Main:
se añadió un componente al bootstrap existente. Sus objetos serializados pasan
de 9.868 a 9.869; Unity también normalizó la serialización al guardar la escena.
La granja y sus edificios, caminos, arte, animaciones, movimiento, cámara,
combate, recursos, inventario, recetas y persistencia se reutilizan.

| Responsabilidad | Implementación y punto de interés para portafolio |
| --- | --- |
| Flujo global | `PortfolioSession`: único propietario de fase, día, progreso y finales; emite cambios de estado. |
| Balance | `SliceSettings` / `PortfolioSettings.asset`: duraciones, composiciones, límites y salud ajustables. |
| Oleadas | `FarmRaidDirector`: calendario finito, entradas anticipadas y reutilización de enemigos/proyectiles. |
| IA | `RaidEnemy` extiende `EnemyAIBase`; `FarmRaidNavigation` comparte rutas locales entre agentes. |
| Jefe | `DungeonBoss` conserva salud y animación; `FarmBossPattern` compone cuatro ataques y dos fases. |
| Daño | `IDamageable` existente conecta jugador, enemigos, estructuras y jefe; `DamageRules` evita fuego amigo. |
| Agricultura | `FarmingPlot` habilita solamente seis parcelas; el comportamiento histórico dormido se mantiene. |
| Construcción | `ConstructionSystem` conserva preview y validación; `FarmDefense` añade salud, reparación y efectos. |
| Presentación | `SliceHud` complementa HUD/mochila/recetas originales; `DayNightCycle`, `FarmAtmosphere` y `FarmSoundscape` cambian luz y música. |

Se retuvieron las cinco zonas de campaña y sus assets, pero no se activan en la
demo compacta. Comercio, misiones sociales y menús ajenos al recorrido quedan
fuera de esta modalidad. No se añadió multijugador, supervivencia compleja ni
un nuevo inventario. La música es sintetizada original y los efectos reutilizan
el feedback del proyecto; no hay una banda sonora comercial externa.

Para recuperar el modo histórico, desactivar `PortfolioSession` en Main.
No ejecutar el generador histórico de escena para actualizar la demo.

## Guardado y seguridad

Las ranuras `portfolio_*` son independientes de la campaña anterior. Se guarda
durante preparación; continuar restaura el día guardado desde su preparación,
con recursos, cultivos y construcciones guardados. No se reanuda una oleada a
mitad de ataque. La derrota no sobrescribe el último punto seguro.

Las pruebas usan ranuras QA únicas. El snapshot previo se conserva en
`Design/Validation/pre-vertical-slice-2026-09-14.zip`: scripts, escenas,
pruebas y ProjectSettings. No es una copia completa de los assets artísticos,
que no se sustituyeron. La rama de trabajo separa un checkpoint de los cambios
previos del usuario y un commit de integración de esta modalidad.

## Verificación reproducible

- Referencia anterior a cambios: 301 pruebas PlayMode aprobadas.
- Suite ampliada: 304/304 aprobadas, cero fallos y cero omitidas
  (incluye tres pruebas de integración de Main).
  El informe de la última ejecución es `Design/Validation/Portfolio/final-tests.xml`.
- Integración: agricultura/cocina, reparación, inmunidad, daño amigo, guardado,
  tres oleadas finitas, fase dos del jefe, victoria y derrota del pozo.
- Build Windows: `Design/Validation/Portfolio/build.txt`, compilación correcta,
  cero errores. Menú del editor: `Survivor Farm/Portfolio/Build Windows demo`.
- Ejecutable: `SurvivalFarm.exe --qa --portfolio-smoke` ejecuta una comprobación
  acelerada, aislada de las partidas normales. Recorre los cuatro patrones del
  jefe y escribe resultado y siete capturas en `Builds/Portfolio/QA/Portfolio`.
  Último resultado: PASS. Las capturas se revisaron visualmente; se corrigieron
  la profundidad del suelo del huerto y la escala de las plantas.
- Logs: `Logs/portfolio-final-tests.log` y `Logs/portfolio-windows.log`.

## Límites de la validación

La prueba automatizada acelera el tiempo, teletransporta al jugador y elimina
enemigos mediante su interfaz de daño. Comprueba conexiones y estados, no
demuestra que la dificultad o el control resulten divertidos para una persona.
Falta un playtest humano de principio a fin para fijar balance y duración real,
y una medición de rendimiento en el equipo objetivo antes de publicar.

El registro de cierre de Unity muestra un aviso de liberación de ComputeBuffer;
no produjo fallo de ejecución ni errores en las pruebas, pero no se presenta
como un log absolutamente libre de avisos. La entrega no incluye validación en
otros equipos, resoluciones extremas, mando, ni plataformas distintas de Windows.
