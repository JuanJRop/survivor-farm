# Survival Farm · auditoría previa a cambios de gameplay

14 de septiembre de 2026. Fuente: escena Main, prefabs, código runtime/editor,
pruebas, paquetes, datos, arte importado, paquete externo y capturas existentes.
La ejecución de las pruebas de referencia está en curso; «funciona» aquí indica
implementación y conexiones verificadas por inspección, no un playtest humano nuevo.

## EXISTE Y FUNCIONA

- **Mantener** Unity 6000.3.9f1, C#, pixel art 2D y escena `Main`. No hay
  Blueprints ni C++: esas partes del encargo se traducen a componentes C#.
- **Mantener** jugador Rigidbody2D, WASD/sprint, cámara ortográfica, animación por
  hojas de sprites, espada circular y arco, proyectiles con colisión y daño.
- **Mantener** `IDamageable`, `EnemyAIBase`, animación de enemigos, anticipación,
  retroceso, muerte, pools y goblins lancero/arquero con comportamientos distintos.
- **Mantener** árboles/rocas/animales, recolección contextual, inventario, recetas,
  mejora de espada, muebles, preview de construcción y validación de espacio.
- **Mantener** parcela y caminos existentes, cinco edificios del pueblo, pozo,
  vegetación, río, sprites/animaciones del Farm RPG Tiny Asset Pack y compañero.
- **Mantener** guardado versionado y `SaveFileStore` con validación y recuperación.
  La escena contiene 957 parcelas de compatibilidad, 22 spawn points, 12 enemigos
  de mazmorra y cinco exteriores. No se deben regenerar ni sobrescribir sus assets.

## EXISTE PERO NECESITA MEJORAS

- **Mejorar** movimiento: esquiva corta, inmunidad breve y bloqueo de acciones
  coherente durante colocación, pausa y final. Daño actual permite varios impactos
  en un mismo instante; no aplica inmunidad tras un golpe normal.
- **Mejorar** construcción: rotación, vida/reparación de cercas, trampa y defensa
  auxiliar. Las cercas actuales son obstáculos sin salud ni efecto de defensa.
- **Extender/conectar** `DayNightCycle`: ya tiñe mundo/UI y afecta población;
  actualmente es un reloj libre de 900 s, con sueño que evita la noche completa.
- **Mejorar** presentación: HUD original reutilizable, pero muchos menús/objetivos
  describen una campaña extensa. Audio existente: tres tonos generados; no hay
  archivos de música ni ambientes. VFX de daño/cultivo son reutilizables.
- **Refactorizar de forma localizada** acceso a daño y selección de objetivos.
  No sustituir la IA base ni reescribir las 2.052 líneas del generador histórico.

## EXISTE PERO ESTÁ DESCONECTADO

- **Reconectar** cultivo: `FarmingPlot` y `CultivationGrid` se redujeron a
  compatibilidad de guardados. `Awake` oculta/desactiva parcelas; el interactor
  las excluye. Quedan definición, sprites, estados y eventos. La nueva petición
  sustituye la decisión histórica de eliminar agricultura, conservando guardados.
- **Conectar** combate, recursos, preparación y noche mediante un director único.
  `ValleyCampaign`, tutorial y reloj actualmente persiguen objetivos separados.
- **Reutilizar/extender** jefe: `DungeonBoss` tiene carga anticipada/recuperación
  y `ValleyEnemy` salto, vulnerabilidad y brotes; ninguno ofrece el combate final
  de cuatro patrones y dos fases solicitado. Reutilizar base, arte y animación.

## FALTA

- Tres ciclos finitos, oleadas con composición/entradas/timing, defensa de un
  objetivo con derrota, progresión entre noches, introducción y final intencional.
- Defensas con impacto táctico, boss final de dos fases, ambiente musical por
  estado, onboarding específico y una build verificada de esta experiencia.

## PROBLEMAS TÉCNICOS DETECTADOS

- Muchísimos cambios previos sin commit y archivos sin seguimiento. Se conservarán;
  la auditoría no atribuye esos cambios al trabajo actual. El README está obsoleto.
- La escena válida es Main; `_Recovery/0.unity` es recuperación, no otro nivel.
- `FarmPlayerInteractor` busca y ordena todos los interactuables cada frame.
- Combate e interacción pueden activarse al confirmar/cancelar una construcción.
- Hay tres fuentes de progreso y población exterior continua: deben ceder el
  control de la sesión al director, sin borrar la campaña histórica.
- El mapa y UI se amplían durante ejecución. Las pruebas deben abrir Main y
  comprobar runtime y build, no únicamente contar scripts.
- La prueba histórica `NoFarmingGameplayTests` fija la antigua ausencia de cultivo:
  conservar ese comportamiento fuera de las parcelas habilitadas para la demo.

## SCOPE RECOMENDADO

Una modalidad de portafolio sobre Main y su granja actual: 3 días de preparación
de 4 / 3,5 / 3 minutos, avisos de 25 s, noches de 1,5 / 2 / 2 minutos y boss
de ~2–3 minutos (objetivo total 18–23 min, sujeto a playtest). Madera, piedra,
fruta/ración y hierro existente; seis parcelas, espada/arco, cerca reparable,
trampa y torreta limitada. Tres roles normales (lancero, arquero, demoledor),
boss Custodio con cuatro ataques y cambio de patrones al 50%.

**Sustituir únicamente en la sesión demo** los objetivos de expedición por el
director; conservar sistemas/escena histórica y archivos. **Excluir del recorrido**
la campaña de cinco zonas, comercio/equipo masivo y misiones sociales. **No eliminar**
assets ni código funcional. Empezar por control/daño, después flujo y defensas,
cultivo, jefe, presentación, pruebas de integración y build Windows independiente.

## Cierre de verificación

La referencia previa pasó 301/301 pruebas. Tras la integración, la última suite
PlayMode pasó 304/304, sin fallos ni pruebas omitidas. La build de Windows se
generó con cero errores y su recorrido automatizado terminó en PASS, incluyendo
los cuatro patrones del jefe, segunda fase y final. Se revisaron las capturas
renderizadas y se corrigieron el suelo y la escala del huerto. Esto no sustituye
un playtest humano de balance ni una medición de FPS. Ver `PORTFOLIO-HANDOFF.md`
para alcance entregado, arquitectura, controles, guardado y límites de validación.
