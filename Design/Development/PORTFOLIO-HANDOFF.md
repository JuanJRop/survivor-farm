# Survival Farm · entrega de la vertical slice

Actualizado el 15 de septiembre de 2026 · Unity 6000.3.9f1 · Windows x64.

## Laboratorios de práctica

El menú añade **Escenas de práctica**: `ArenaCombate` (cinco oleadas, todos los
arquetipos de la demo y Custodio) y `TallerGranja` (cultivos, construcción,
recolección, minería y cocina sin cuenta atrás). F6 abre sus controles; no
modifican los guardados normales. Las tres escenas están incluidas en la build.

Ver `Design/Development/PRACTICE-SCENES.md`. Paquete actualizado:
`Builds/SurvivalFarm-Practicas-Windows.zip`.

## Jugar

Ejecutar `Builds/Portfolio/SurvivalFarm.exe` conservando sus archivos y carpetas
acompañantes. No requiere el editor. En Unity, abrir la escena Main y pulsar Play.
El menú inicial ofrece Continuar, Nueva partida, Cargar partida, Opciones y Salir,
con el rótulo DEMO. Cargar enumera las partidas válidas; Nueva partida no borra las
anteriores. Volumen e impacto reducido se pueden ajustar antes de jugar.

La experiencia conecta tres días de preparación, tres defensas nocturnas y el
Custodio final. La duración prevista es 18–23 minutos sin adelantar los días;
no es una duración medida con jugadores. La noche llega automáticamente: ya no
hay botón para adelantar el día. El tutorial inicial pausa el reloj durante
la lectura y la práctica: movimiento real, combo sobre un rival seguro y descarga.
Proteger el pozo es tan importante como mantener vivo al personaje.

WASD mueve; Shift corre; E interactúa y elige herramienta contextual; clic ataca;
1/2 seleccionan espada/arco; Espacio esquiva; Q consume ración; F abre recetas;
I abre mochila. Z abre la paleta continua; X/C seleccionan trampa/ballesta;
mantener clic 1,05 s con espada y soltar ejecuta una descarga potente.
R gira muros, clic coloca otra pieza; Esc o clic derecho terminan la construcción.
M selecciona y mueve piezas sin coste; K abre maestrías y permite comprar niveles
previamente desbloqueados por uso de espada, arco, hacha, pico, azada y regadera.
La mochila y el taller también tienen acceso a la paleta. Esc fuera de menús abre la pausa.
B activa desmontar y devuelve el 69% de cada material pagado, redondeado hacia
abajo. Las mejoras acumulan inversión; las defensas iniciales gratuitas no dan
materiales. Al mover, la pieza queda en el cursor hasta confirmar; Esc restaura
la original. Un cofre debe vaciarse antes de desmontarlo.
La pestaña inferior despliega la paleta al acercar el cursor y la retrae al
volver al mundo; el modo de colocación sigue activo. El encaje toma los extremos
de los muros existentes, también los originales fuera de la retícula global.

Ampliación de fortaleza: módulos horizontales de 2 unidades y verticales de 4,
empalizada → piedra → reforzado
(hierro y oro). Seleccionar material superior sobre un muro permite mejorarlo
con el coste completo, conservando el porcentaje de daño. El valle ahora abarca
64 × 36 unidades sobre el mismo terreno: bosque al oeste, cantera al este y
salientes minerales al noreste/sureste. El puente permanece abierto en la demo,
también al cargar partidas anteriores. El pico contextual extrae 3 de hierro por
veta de nivel 2; los salientes de nivel 3 dan además 2 de oro. Se exige el nivel
correspondiente de pico y cada veta se recupera al día siguiente. Los recursos
ordinarios también tienen tres niveles. Los vecinos deambulan con colisión,
reciben ataques enemigos y cada muerte reduce 3 puntos la vida máxima del pozo.
Las gallinas ahora pueden cazarse. Muros y casas dejan ver al jugador tras ellos.
Los troncos son sólidos y los árboles conservan su opacidad y profundidad por
los pies; también los del pueblo son recursos con nivel. Orillas, fogata, árboles,
mariposas y liebres usan fotogramas del pack; el puente usa su variante de tablones.
E retira flores o arbustos a cambio de semillas/frutos, guardando el despeje.
El gato no se entrega de entrada: Rolo lo vende desde el día 2 por 180 monedas,
12 hierro y 3 oro mineral. Las casas tienen 32 puntos de vida y los demoledores
pueden derribarlas; el daño y las ruinas se guardan. Perder una casa no termina
automáticamente la partida. Espada: alcance base de 1,60 unidades.

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

Valle renovado (15/09): 355/355 PlayMode, build Windows con cero errores y
recorrido automático completo PASS. Evidencias y 42 capturas:
`Design/Validation/Portfolio/WorldRenewal`; informe de pruebas:
`Design/Validation/Portfolio/renewal-delivery-tests.xml`.
Detalle y limitaciones: `WORLD-RENEWAL-2026-09-15.md`. No se ha realizado un
playtest humano completo; la comprobación por teclado/ratón espera permiso
para abrir una ventana visible de QA. ZIP actual:
`Builds/SurvivalFarm-ValleRenovado-Windows.zip`.

Validación de Impacto (15/09): suite completa 346/346, familias afectadas 83/83;
build Windows con 0 errores. Recorrido automático del tutorial, combate,
construcción, recolección y tres noches hasta el final: PASS. Se revisaron las
capturas renderizadas por el ejecutable, incluido el shader y ambos estados
del cajón. La comprobación de teclado/ratón por control de ventanas no pudo
realizarse: el sistema no consiguió activar la ventana de la demo tras reintentar.
No se atribuye a esta versión la revisión manual de la iteración anterior.
Detalle, informes y límites: `COMBAT-JUICE-2026-09-15.md`.

Los siguientes informes documentan la integración inicial:

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
