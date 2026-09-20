# Equipo de trabajo de Survivor Farm

Referencia obligatoria: ANALISIS-DEL-JUEGO-2026-09-09.md, en este directorio.

## Objetivo compartido

RPG de reconstruccion de Raizclara y expediciones, con agricultura y supervivencia amable. Profundizar los sistemas existentes antes de ampliar catalogos o superficie de mapa. Usar exclusivamente el arte disponible del proyecto para las mejoras visuales.

El equipo trabaja por propiedad exclusiva de archivos. Independencia significa poder implementar y comprobar cada area sin editar los archivos de otra; no significa ignorar los contratos entre sistemas.

## Responsables

| Area | Encargo | Propiedad de escritura |
| --- | --- | --- |
| A. Persistencia | Guardado recuperable, respaldos, errores controlados, herramientas de identidad estable y pruebas | GameSaveSystem.cs, ResourceSpawnPoint.cs y nuevos archivos de persistencia/identidad con nombres propios |
| B. Pueblo e historia | Reparaciones visibles y utiles, taller de Nico, rangos con hitos comunitarios, personalidad y actividad basica de aldeanos, pozo coherente | ValleyCampaign.cs, ValleyWorld.cs y nuevos archivos Village* de narrativa/servicios; no VillageLayout.cs |
| C. Economia y cultivo | Integrar alimentos reales en cocina, cultivos diferenciados, adquisicion y venta, servicios de taller conectados sin quitar comodidad innecesaria | FarmingPlot.cs, CultivationDefinition.cs, SurvivalItemCatalog.cs, PlayerInventory.cs, PlayerCraftingController.cs, SimpleShopSystem.cs, BackpackActions.cs |
| D. Combate y recoleccion | Alcance y presentacion coherentes, papel de armas, legibilidad de encuentros, recoleccion con buena respuesta e interrupciones | PlayerCombatController.cs, ValleyEnemy.cs, HarvestableResource.cs, ArrowProjectile.cs, EquipmentItems.cs, TreeResource.cs, RockResource.cs y nuevos auxiliares exclusivos |
| E. UI y experiencia | Lenguaje visual comun con sprites existentes, diario organizado, comparaciones de equipo, ingredientes claros y feedback sin saturacion | Runtime/UI, excepto SimpleShopSystem.cs; nuevos auxiliares exclusivos de interfaz |
| Coordinacion | Previsualizacion del pueblo en editor, contratos, revision de cambios, verificacion integrada y documentacion | VillageLayout.cs, nuevas herramientas de previsualizacion Editor/Raizclara*, nuevas pruebas integradas y este documento |

Cada responsable puede crear pruebas con un prefijo propio. No editar escenas, prefabs, ProjectSettings, Packages, archivos de otros responsables ni guardados personales sin coordinacion. Los cambios de formato de partida corresponden exclusivamente a A.

## Contratos de integracion

- Mantener los metodos publicos actuales durante esta entrega; cualquier API nueva se documenta en el parte individual.
- A puede crear un proveedor de identificadores estables. FarmingPlot.cs pertenece a C; A comunica la llamada necesaria y coordinacion incorpora ese enlace despues, sin escritura simultanea.
- B expone servicios de pueblo mediante metodos publicos de ValleyCampaign; C consume sus requisitos solo cuando el contrato este confirmado. Ningun agente inventa una dependencia aun no implementada.
- C expone recetas y requisitos como datos consultables. E los presenta sin duplicar reglas economicas.
- D mantiene las interfaces de dano e inventario; cualquier cambio de significado de bonos se explica y prueba.
- E lee estado y requisitos existentes y no concede progreso desde la UI.
- La previsualizacion es de editor, no ejecuta Awake de la campana, no carga partidas ni entrega recompensas.
- Los originales y cambios previos del usuario se conservan. No realizar commits, resets, limpiezas recursivas ni regeneraciones de escena en paralelo.
- Solo coordinacion lanza comprobaciones integradas de Unity. No cerrar el editor del usuario ni compartir la misma carpeta de QA entre procesos.

## Primera entrega de cada area

### A. Persistencia

Escritura temporal y reemplazo recuperable, respaldo anterior, lectura validada y errores controlados. Una carga fallida no debe sobrescribir silenciosamente el archivo afectado. Compatibilidad con las versiones admitidas y pruebas de archivo ausente, corrupto, respaldo y guardado repetido.

### B. Pueblo e historia

El taller debe cambiar de apariencia al repararse y ofrecer algo util para la siguiente expedicion. Dar a los cinco NPC funciones reconocibles, dialogo dependiente del progreso y actividad sencilla sin bloquear entregas. Los rangos altos requieren reconstruccion ademas de influencia. Revisar la recuperacion ilimitada de hambre del pozo. Mantener la campana jugable y las recompensas unicas.

### C. Economia y cultivo

Dar a un grupo inicial de cultivos tiempos/usos claros e integrar ingredientes del catalogo con la cocina. No eliminar objetos ni partidas. Aclarar el significado de compra y venta y ofrecer motivos para reservar recursos. Mantener un camino garantizado de comida y progreso inicial, sin depender de gemas aleatorias.

### D. Combate y recoleccion

Mantener el barrido circular de espada como comportamiento existente, haciendolo visible; diferenciar arco y espada sin acumular accidentalmente el bono del arma contraria. Mejorar avisos de enemigos, recuperacion del jefe e interrupcion segura de acciones. Evitar notificaciones redundantes por cada golpe. No endurecer la muerte ni introducir minijuegos obligatorios.

### E. UI y experiencia

Conservar el HUD compacto. Homogeneizar ventanas y fuentes, estructurar el diario, aclarar comparaciones de equipo y costes de recetas. Evitar texto cortado, paneles acumulados y arte inventado. Respetar la interaccion junto al objeto y mostrar requisitos en lugar de ocultar por que una accion esta bloqueada.

### Coordinacion

Crear una vista del pueblo en el editor con estados de ruina y reconstruccion sin modificar progreso. Revisar interfaces y resultados de cada area, compilar, ejecutar las verificaciones apropiadas y registrar limitaciones reales. Las pruebas humanas de diversion se preparan, pero nunca se presentan como realizadas por una comprobacion automatica.

## Criterios comunes

- No basta un documento de propuesta: cada area implementa una mejora acotada y comprobable.
- Cada parte informa archivos modificados, comportamiento entregado, pruebas realizadas y pendientes.
- No declarar el analisis entero completado solo por cerrar una primera entrega.
- No construir las alternativas descartadas del analisis ni convertir recomendaciones de largo plazo en una ampliacion sin limites.

## Estado

Primera ronda implementada con cinco agentes y un coordinador, el 2026-09-09:

- Maxwell: A, persistencia.
- Huygens: B, pueblo e historia.
- Kant: C, economia y cultivo.
- Wegener: D, combate y recoleccion.
- Archimedes: E, UI y experiencia.
- Codex coordinador: previsualizacion, contratos y comprobaciones integradas.

Los cinco responsables entregaron sus cambios y sus agentes se cerraron tras la revision. No hay trabajo autonomo programado en segundo plano. Coordinacion integro los contratos, corrigio las incidencias detectadas y verifico el conjunto en una copia aislada de Unity, sin cerrar el editor del usuario ni cargar sus partidas personales.

## Resultado integrado

| Area | Entrega implementada |
| --- | --- |
| A | Guardado temporal con reemplazo, respaldo y recuperacion validada; bloqueo de escritura ante carga irrecuperable; proveedor de IDs estables y herramienta de asignacion con alias antiguos |
| B | Fachadas reparadas, suministro diario de hierro en el taller, rangos condicionados por obras, dialogos y actividad visual de cinco aldeanos con personajes originales; pozo sin hambre infinita |
| C | Cuatro cultivos iniciales diferenciados, recetas con ingredientes reales, reserva de semillas hasta completar la accion, compra/venta con precios separados y requisitos del taller |
| D | Bonos separados por arma, alcance de espada visible, arco y proyectiles con validacion de objetivo/cobertura, avisos de ataque y cancelacion consistente de recoleccion |
| E | Ventanas con estilo compartido, diario con objetivos/proyectos/aldeanos, ingredientes y requisitos consultados al sistema de recetas, comparaciones de equipo y mochila reorganizada |
| Coordinacion | Datos comunes para lotes y fachadas; previsualizacion del pueblo; catalogo de animaciones NPC conectado al arte existente; integracion de IDs de parcelas; correccion de iconos de cosecha en tiras de cultivo; compilacion y QA |

La correccion de iconos usa los sub-sprites originales de las tiras de cultivo de 16 px y filtrado Point; no genera dibujos. Los atlas de arboles frutales usan otra distribucion y quedan fuera de esa regla.

## Verificacion

- Compilan Runtime, Editor y PlayModeTests con el compilador de la version instalada de Unity.
- 181 pruebas PlayMode aprobadas; 0 fallos y 0 omitidas. Resultado: `Design/Validation/Team/playmode-results.xml`.
- Recorrido integrado aprobado: accesos, seis zonas, encargos, rangos, interacciones reales, guardian, fases y reintento del jefe, recompensas unicas y guardado/carga.
- Distribucion aprobada: caminos conectados, puertas y servicios accesibles, escala de casas coherente, HUD compacto y tablero interactivo.
- Capturas del pueblo y de las ventanas a 1280x720 y 1920x1080. Informes en `Design/Validation/Team/Integrated/`.
- Las verificaciones de campana usan un entorno controlado con recursos y desplazamientos de QA. No son una partida humana sin ayudas ni una medida de diversion o duracion.
- El ultimo log integrado tambien registra una excepcion del indice de busqueda de Unity (`UnityEditor.Search.SearchDatabase`). No impidio generar los informes nuevos ni completar las comprobaciones, pero no se presenta el log del editor como libre de incidencias.

Comandos reproducibles desde la raiz del proyecto:

```powershell
& ./Tools/Verify-TeamChanges.ps1
& ./Tools/Run-TeamQA.ps1 -UnitTests
& ./Tools/Run-TeamQA.ps1 -Method SurvivorFarm.Editor.RaizclaraTeamVerification.Run -SkipCopy
```

La copia de pruebas queda en `Temp/TeamQA`. No abrir simultaneamente dos procesos de QA sobre esa misma carpeta.

## Vista del pueblo

Menu de Unity: `Tools > Survivor Farm > Vista del pueblo`.

Permite comparar ruinas, taller reparado y reconstruccion, con zoom, desplazamiento y lotes compartidos con el juego. No modifica escenas, progreso ni recompensas. Es una vista de distribucion y arte del pueblo: no sustituye a Game View ni reproduce todos los efectos y estados del juego en vivo. Compilada y contrastada con los datos de distribucion; la ventana de editor no tiene una captura de QA propia en esta entrega.

## Continuidad del analisis

Cada pendiente conserva responsable, pero no se declara implementado por haberlo asignado:

| Fase del analisis | Responsables | Pendiente para cerrar su criterio de salida |
| --- | --- | --- |
| 0. Confianza y herramientas | A + coordinacion | Aplicar IDs estables a la escena existente con respaldo y validar una partida antigua despues de mover/reordenar objetos. La herramienta y el enlace de parcelas estan implementados, pero no se ha reescrito la escena del usuario |
| 1. Reparacion significativa | B + E | Comprobar con una persona nueva que reconoce y utiliza el taller sin depender del diario. Extender funciones propias a otras reparaciones, sin confundir cambios de fachada con servicios completos |
| 2. Comida, materiales y rangos | C + B | Medir produccion/consumo, precios y tiempos en juego normal; seguir diferenciando usos del catalogo. Los frutales continuan siendo cultivos de una cosecha, no arboles productivos persistentes |
| 3. Expedicion y retorno | D + B | Probar lectura y dificultad del combate con jugadores; revisar desplazamientos de recadero y consecuencias visibles del retorno. No se ha construido un nuevo sistema general de atajos |
| 4. Capitulo presentable | E + D + coordinacion | Revisar sonido y reacciones, probar usabilidad sin ayudas y preparar una compilacion jugable. La UI esta unificada, pero no se ha hecho una nueva entrega integral de audio ni medido una sesion satisfactoria de 30-45 minutos |
| Comprobacion con jugadores | Coordinacion, cada responsable atiende su area | Registrar comprension inicial, tiempos de viaje/menus/acciones, causas de dano, reconocimiento de NPC y reparaciones, consumo de comida y deseo de continuar |
| Rendimiento | Coordinacion, con D para simulacion y E para UI | Perfilar una build real antes de optimizar. La muestra de QA registra 20.709 GameObjects y 15.619 SpriteRenderers; es una senal para investigar, no un resultado de FPS |

Tambien queda revisar la iconografia de alimentos procedentes de arboles frutales: sus atlas no contienen el alimento en la misma posicion que las tiras de cultivo. No se sustituyeron esos sprites por dibujos inventados.

No se ha generado un nuevo ejecutable de Windows en esta ronda. Las modificaciones estan en el proyecto Unity. Se conserva el alcance del analisis: no se agregan multijugador, estaciones completas, mapas mas grandes ni una reescritura general.

Los partes individuales `Equipo-A.md` a `Equipo-E.md` reflejan lo que comprobo cada agente antes de integracion. Esta seccion registra la verificacion conjunta posterior y prevalece cuando esos partes indican que Unity seguia pendiente.
