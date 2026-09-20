# Escenas de práctica · Survival Farm

Dos escenas independientes para explorar los sistemas de la demo sin tocar las partidas.

## Entrar

Desde Main: **Escenas de práctica** en el menú principal.
También se pueden abrir directamente en Unity y pulsar Play:

- `Assets/SurvivorFarm/Scenes/ArenaCombate.unity`
- `Assets/SurvivorFarm/Scenes/TallerGranja.unity`

F6 o el botón superior derecho abre los controles del laboratorio y pausa la acción.
Esc también pausa/reanuda si no hay otra ventana abierta. El panel permite cambiar
de área, reiniciar la escena o regresar al menú principal. Los controles normales
del juego siguen funcionando.

## Arena de combate

Un claro delimitado por vegetación y rocas, sin casas ni aldeanos que distraigan a
los enemigos. Reutiliza el combate, animaciones, feedback y proyectiles de la demo.

El circuito encadena cinco encuentros, con seis segundos de descanso y curación
entre oleadas. Se puede elegir cualquier oleada directamente:

1. Limos y murciélagos.
2. Rastreadores y arqueros.
3. Gólem, demoledor y rastreador.
4. Los seis arquetipos combinados.
5. El Custodio, con sus patrones y segunda fase originales.

El bestiario permite practicar contra cada uno de los siete tipos individualmente.
Las apariciones se anuncian antes de activar sus ataques. Elegir otro encuentro
retira los enemigos, proyectiles y marcas del anterior. Morir termina el encuentro,
cura al jugador y lo devuelve al punto de práctica; no bloquea la escena.

## Taller de granja

Reutiliza el valle de la demo con luz diurna fija, sin oleadas ni cuenta atrás.
Accesos rápidos al huerto, solar de construcción, bosque, cantera, oro y cocina.
Cada traslado busca suelo libre cercano para no aparecer dentro de un tronco o muro.

- Seis parcelas listas para plantar, regar y cosechar con E.
- Madera, piedra y recursos de niveles I–III, incluidos hierro y oro.
- Construcción continua, movimiento, mejoras y demolición originales; trampas y
  ballestas disponibles desde el comienzo de esta práctica.
- Mochila, recetas, cocina y maestrías originales.
- Botón para adelantar 60 segundos el crecimiento de cultivos ya regados.

Ambas escenas empiezan con suministros. **Reponer / curar** completa mínimos de
180 madera, 140 piedra, 250 monedas, 40 fruta, 30 semillas, 20 raciones, 80 hierro
y 30 oro; no duplica indefinidamente las existencias ni borra lo construido.
**Nivel 1 / 2 / 3** permite comparar todo el equipo sin completar la progresión.
También se puede progresar por uso y compra con K.

Los árboles y rocas conservan su regeneración normal. Las vetas diarias agotadas
se recuperan al **reiniciar la escena**; el taller no simula días sucesivos.
Reiniciar también descarta cultivos, construcciones e inventario de esta práctica.

## Arquitectura y mantenimiento

`PracticeSession`: ciclo local, suministros, niveles y traslados.
`PracticeWaveDirector`: secuencias y transición de oleadas.
`PracticeEnemyFactory`: adapta las plantillas de criaturas y los roles existentes.
`PracticeWorld`: montaje del claro y sus límites.
`PracticeHud`: selección, pausa y estado compacto.

`PortfolioSession` conserva el arranque común, pero no avanza la campaña en estas
escenas. `GameSaveSystem` rechaza tanto lectura como escritura en prácticas; no
cambia la selección de guardado de la demo. No hay tutorial obligatorio al entrar.

Las escenas conservan las referencias serializadas del Main existente. La arena
desactiva sus secciones de mundo al entrar en Play y monta el claro; el taller
mantiene el valle. Por eso la vista previa de edición conserva la base de Main.

`Survivor Farm > Practice > Rebuild practice scenes from Main` regenera **solo**
las dos escenas de práctica a partir de Main y las registra en Build Settings.
No guarda cambios sobre Main. Esta regeneración reemplaza las personalizaciones
manuales de las escenas de práctica: conservarlas aparte antes de ejecutarla.
La compilación de portfolio incluye las tres escenas.

## Verificación

`PracticeSceneTests` prueba montaje, siete tipos de enemigo, circuito completo,
reinicio/cambio de encuentros, aislamiento de guardados, cultivo, construcción,
niveles, suministros, traslados, derrota, pausa y vuelta a la demo.

El ejecutable admite `--qa --practice-smoke` para verificar los cambios de escena
y producir capturas renderizadas bajo `QA/Practice`. No se activa al jugar
normalmente y no sustituye una prueba manual de controles ni de balance.
