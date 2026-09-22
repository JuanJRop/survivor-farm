# Menú, progresión y combate — 22 de septiembre de 2026

## Cómo verlo

Abrir `Builds/Portfolio/SurvivalFarm.exe` y empezar o continuar una partida.
**Esc** abre el menú de viaje, también durante el tutorial. **F** abre Taller,
**K** Habilidades y **C** Equipo. Esc vuelve a la partida o al tutorial conservando
su estado de pausa anterior.

El menú tiene cinco pestañas:

- **Equipo:** armaduras, arma equipada, estadísticas y cinco cajones asignables.
- **Taller:** tienda y todas las recetas del sistema de fabricación.
- **Mejoras:** espada, hacha, pico, maestrías de herramientas, armaduras y vitalidad.
- **Habilidades:** árbol conectado de Fuerza, Magia, Supervivencia, Movilidad y Caos;
  selección de nodos bloqueados para consultar requisitos y compra con puntos.
- **Ajustes:** sonido, pantalla completa, límite de FPS, efectos, cámara, guardar,
  volver al título y salir del juego. Salir pide confirmación dentro del juego.

La composición usa fondo de tinta, líneas doradas, pestañas de pergamino,
tipografía con serifas, nodos circulares y un panel de detalle lateral. Las vistas
previas animan al personaje, al objetivo y los sprites del paquete de efectos.
Funcionan mientras la partida está pausada y no necesitan vídeos adicionales.

## Cambios que antes no se veían

K y Esc ya no abren dos menús antiguos distintos. Los accesos del HUD también
abren el mismo menú. Las mejoras explican materiales y práctica requeridos.
El tutorial presenta el botón **Practicar**, enseña carrera y dash y no procesa
entradas mientras está abierto el menú.

La escena principal usa un alcance de espada de 1,35 unidades, compartido con
su indicador visual. El arco se oculta hasta obtenerlo; el prefab inicial tampoco
lo incluye. La muerte deja caer el inventario incluso cuando el controlador
antiguo de reaparición está desactivado. Ambas teclas Shift permiten correr.

Las nuevas técnicas incluyen hemorragia, debilidad, ondas, fuego, rayos, hielo,
gravedad, reacciones de cadáveres, supervivencia y movilidad. El segundo dash
es una segunda carga real; las habilidades de movilidad y Carnicería afectan
los tiempos de recuperación de los controladores.

## Memoria y mantenimiento

- Un único propietario del menú controla navegación y pausa.
- El árbol no se reconstruye ni se actualiza por experiencia mientras está oculto.
- El taller se actualiza por cambios de estado, sin reconstruirlo cada 0,35 segundos.
- Los efectos reutilizan un máximo de 12 objetos por jugador.
- Las filas de sprites se comparten, conservando duración y escala por técnica.
- Los efectos se muestran cuando una técnica aprendida se activa realmente.
- Los objetivos, estados y explosiones pendientes usan colecciones acotadas.
- Las reacciones se procesan en una cola con límites de profundidad y cantidad;
  las operaciones de combate no crean objetos de ámbito por cada impacto.
- Los identificadores de habilidades existentes y las partidas guardadas se conservan.

## Verificación reproducible

`Tools/Verify-TeamChanges.ps1` compila runtime, editor y pruebas.
`Tools/Run-TeamQA.ps1 -UnitTests` ejecuta las pruebas PlayMode en una copia aislada.
`Tools/Run-TeamQA.ps1 -Method SurvivorFarm.Editor.GameMenuVerification.Run`
comprueba la escena Main y captura las cinco pestañas. Los resultados y capturas
se encuentran en `Design/Validation/Team`.

Los ejecutables de `Builds` son artefactos locales de compilación; el repositorio
contiene el código, los recursos y las herramientas para reproducirlos.

## Resultado de la validación

- Suite completa: **484/484 pruebas aprobadas**, sin omitidas.
- Revisión posterior de taller y persistencia de ajustes: **3/3 aprobadas**.
- Escena Main: navegación por las cinco pestañas, selección de un nodo bloqueado,
  pausa y retorno al tutorial verificados; capturas reales en
  `Design/Validation/Team/Integrated/GameMenu`.
- Volumen y movimiento de cámara comparten las preferencias del menú principal,
  por lo que se conservan al reiniciar.
- Compilación Windows reproducible con
  `Tools/Run-TeamQA.ps1 -Method SurvivorFarm.Editor.PortfolioRelease.BuildBatch`.
- Ejecutable Windows: compilación correcta, cero errores; prueba de reutilización
  de **5.000 ciclos aprobada**, con resultados en `Design/Validation/Team/FinalWindows`.
- Las rutas `Builds/Portfolio/SurvivalFarm.exe` y `Builds/Windows/SurvivorFarm.exe`
  contienen la misma versión del juego, actualizada el 23 de septiembre.

Estas pruebas automatizadas verifican comportamiento e integración; no constituyen
una medición de FPS ni una prueba humana de equilibrio de todas las técnicas.
