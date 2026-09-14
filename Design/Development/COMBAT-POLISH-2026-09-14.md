# Combate · impacto, combo y remate

## Inspección y reutilización

Se revisaron PlayerCombatController, PlayerCharacterAnimator, PlayerToolbelt,
PlayerMovementController, PlayerSurvivalStats, IDamageable, EnemyAIBase y sus
variantes, DungeonBoss, ValleyEnemy, proyectiles, recursos, destructibles, loot,
GameFeelFeedback, VisibleHitFeedback, partículas, cámara, pausa y pruebas.

Se mantiene el área y la comprobación de cobertura de la espada, el arco,
salud/daño, la IA, el retroceso con colisión y las animaciones del pack. Antes,
la espada dañaba inmediatamente al pulsar y no tenía combo; la cámara no daba
respuesta al impacto. Dos componentes escribían el material de flash del enemigo.
Los sonidos de interacción se reducían a tres tonos. No se creó otro Health,
inventario, EnemyCombat ni LootDropper paralelo.

## Responsabilidades

- `MeleeComboDefinition` / `Resources/SwordCombo.asset`: cadena editable por arma.
- `ComboController`: índice y ventana temporal; cambiar arma o interrumpir resetea.
- `PlayerCombatController`: input, buffer, contacto diferido, área y cobertura.
- `PlayerCharacterAnimator`: retemporiza los fotogramas existentes; no escala el
  collider ni mueve al jugador sin pasar por su controlador.
- `HitFeedback`: recibe únicamente impactos aceptados, distingue golpe fuerte,
  agrupa cámara/tiempo por barrido y reconoce remates sobre élites.
- `VisibleHitFeedback`: único responsable del material blanco temporal.
- `CombatHitParticles`: color por superficie, 7–16 partículas y máximo 18 ráfagas.
- `CombatTimeFeedback`: breve dilatación de tiempo, con recuperación en tiempo
  real y prioridad para pausa, muerte, cambios de escena y desactivación.
- `CameraFeedback`: offset y zoom aditivos sobre CameraFollowTarget, sin cambiar
  objetivo ni encuadre permanente. La pose base se restaura cada frame.
- `AudioFeedback`: banco original sintetizado, reemplazable por clips; ocho voces
  reutilizadas, pequeñas variaciones de pitch/volumen y límite por tipo de sonido.

## Valores iniciales

| Parámetro | Normal | Tercer golpe |
| --- | --- | --- |
| Duración | 0,42 / 0,38 s | 0,58 s |
| Daño | Daño actual del arma | Daño actual × 2 + 1 |
| Momento del contacto | 42% / 40% | 52%, con anticipación sostenida |
| Hit-stop perceptual | 32 ms al 1% de velocidad | 65 ms al 1% |
| Amplitud de cámara | 0,035 unidades | 0,075 unidades |
| Recuperación del enemigo | 0,18 s | 0,32 s |

La ventana para continuar es 0,6 s después de la recuperación. Un clic en los
últimos 0,16 s puede quedar en espera; solo se almacena un ataque. La cadena vuelve
al primer ataque al expirar, cambiar de arma, recibir daño, esquivar o abrir menú.
El ataque fallado muestra el swing, pero no emite sonido ni cámara de impacto.

Matar a un demoledor o jefe con el golpe fuerte permite un remate visual: hasta
0,27 s incluyendo hit-stop y cámara lenta al 35%, zoom máximo del 3,5% y pequeño
desplazamiento hacia el impacto. Tiene enfriamiento de 3 s, no una cinemática larga.
El menú de pausa permite reducir movimiento: elimina shake, zoom y dilatación
temporal, manteniendo daño, animación, partículas y sonido.

Se distinguen swing, tercer swing, golpe normal/fuerte, daño, muerte, rotura de
caja, caída de loot, recogida, minería, madera, fabricación, arco y finisher.
No se añadieron materiales ni armas nuevas ni se modificaron sus recompensas.

## Verificación

Pruebas nuevas: `CombatPolishTests`. Cubren contacto diferido y único, cancelación,
buffer, ventana y cambio de arma, daño 1–1–3, remate, restauración de tiempo/cámara,
pausa, reducción de movimiento, banco de audio, variación y límite de partículas,
rotura de cajas y recompensa única. Se ejecutan junto a las pruebas históricas.

La sesión original de Unity estaba abierta; la validación se ejecuta desde una
copia temporal dentro de `Temp/p`, sin cerrarla ni guardar
por encima de posibles cambios de escena que estén solo en el editor.
El control de la ventana no llegó a autorizarse: las pruebas automáticas no
se deben presentar como un playtest humano ni como una evaluación auditiva.

Primera suite completa: **316/316 aprobadas**, sin fallos ni omitidas.
Informe: `Design/Validation/Portfolio/combat-polish-tests.xml`.
La comprobación del ejecutable incluye una preparación de receptor para golpear
al demoledor con el arco y una cadena cuerpo a cuerpo real; captura los tres
contactos y verifica un finisher. Las oleadas posteriores siguen aceleradas.

## Resultado de la entrega

- Suite final: **317/317 aprobadas**, cero fallos y cero omitidas;
  `Design/Validation/Portfolio/combat-polish-final.xml`.
- Se añadió una regresión para evitar que el daño de trampas/mascotas congele
  al jugador. Solo el contacto de sus armas solicita el impacto de cámara/tiempo.
- Se corrigió una escala que persistía al reciclar un demoledor como enemigo
  normal. La prueba de las tres noches comprueba ahora esas siluetas.
- Build limpia: `Design/Validation/Portfolio/combat-polish-build.txt`, cero
  errores de compilación. Una reconstrucción sin caché resolvió referencias de
  scripts perdidas que aparecieron en el primer paquete de la copia temporal.
  `PortfolioRelease.Build` solicita ahora esa reconstrucción limpia.
- Ejecutable actualizado: `Builds/Portfolio/SurvivalFarm.exe`.
- Comprobación del ejecutable: **PASS**; resultado y diez capturas en
  `Builds/Portfolio/QA/Portfolio`. Se revisaron los contactos normal y fuerte.
  El log `Logs/combat-polish-windows-clean.log` no contiene excepciones ni scripts
  ausentes. Persiste el aviso de ComputeBuffer de Unity al cerrar, ya documentado
  en la entrega anterior; no procede de una excepción del combate.
- Build anterior preservada en `Builds/Portfolio-BeforeCombatPolish-20260914`.

**Pendiente:** una pasada jugada manualmente y escuchar la mezcla en el equipo
objetivo. El permiso de control de la ventana caducó. La prueba automática del
combo y la comprobación acelerada del encuentro no sustituyen ese paso ni miden
la sensación subjetiva de control, ritmo o volumen.
