# Survivor Farm: estado, potencial y prioridades

Fecha: 9 de septiembre de 2026.

## 1. Diagnostico ejecutivo

**Tenemos un prototipo jugable amplio, con una campana conectada y una idea central atractiva. Todavia no una experiencia consolidada y equilibrada.**

La diferencia importa: implementar inventario, agricultura, combate y misiones demuestra que las piezas existen; conseguir que cada una haga mas interesante a las demas es lo que convierte esas piezas en un juego con identidad.

La oportunidad mas fuerte es esta:

> Llegas a Raizclara sin pertenecer a ella. La alimentas, recuperas sus servicios y te adentras en un valle amenazado. Cuando regresas, el pueblo ha cambiado gracias a ti, y tu lugar entre sus habitantes tambien.

Mi recomendacion es orientar el proyecto a **un RPG de reconstruccion de pueblo y expediciones, con agricultura y supervivencia amable**. No a un survival duro al que se le han agregado una granja y unos NPC.

El principal riesgo actual no es la falta de contenido. Es ampliar el catalogo y el mapa antes de consolidar el motivo por el que el jugador quiere seguir jugando.

### Alcance y limites de esta revision

Se revisaron codigo de campana, mundo, cultivo, recoleccion, combate, equipo, economia, construccion, interfaces y guardado; pruebas automatizadas y sus resultados existentes; documentacion y capturas recientes del pueblo.

No se modifico la jugabilidad, no se ejecuto una nueva partida humana completa y no se midieron FPS ni retencion. Los problemas de implementacion se separan de las hipotesis de experiencia. Las propuestas y objetivos de prueba de este documento no son funcionalidades ya construidas ni resultados observados.

## 2. Lo que realmente tenemos

| Area | Base implementada | Lo que todavia no demuestra |
| --- | --- | --- |
| Pueblo | Raizclara, casas organizadas, plaza, ramales, cinco aldeanos, cinco proyectos y cambios visuales | Un asentamiento con actividad cotidiana y servicios que evolucionan profundamente |
| Historia | Inicio, cinco espacios de aventura, accesos condicionados, sellos, guardian, Rey Limo y recuperacion de la semilla | Una campana extensa, misiones variadas o consecuencias narrativas ramificadas |
| Influencia | Cinco rangos, umbrales y recompensas de ascenso | Autoridad real, responsabilidades o decisiones sobre el pueblo |
| Agricultura | Preparar tierra con azada, plantar, regar, esperar y cosechar; seleccion de semillas | Cultivos con ritmos y usos suficientemente distintos |
| Recoleccion | Acciones temporizadas, varios impactos, animacion, recompensas y mejoras de herramientas | Que repetirlas durante una sesion larga no resulte tedioso |
| Objetos | 37 alimentos, 37 semillas o brotes, 9 gemas o fragmentos y 7 esencias en el catalogo | 90 decisiones distintas o una economia equilibrada |
| Equipo | Ocho ranuras, armaduras, armas, accesorios y 29 recetas de equipo | Estilos de combate claramente diferenciados y balanceados |
| Supervivencia | Corazones, hambre, dia y noche, comida, descanso y reaparicion | Una presion de supervivencia coherente con el resto de incentivos |
| Hogar | Mejoras de casa, interiores, muebles, colocacion, traslado y almacenamiento | Un constructor de pueblos completo o una logistica compartida entre todos los objetos |
| Interfaz | HUD mas compacto, mochila visual, equipo, taller, diario e interaccion contextual | Una direccion visual uniforme y una experiencia validada con jugadores nuevos |
| Persistencia | Guardado version 20, migraciones y comprobaciones de ida y vuelta | Recuperacion robusta ante un archivo corrupto o una escritura interrumpida |

Los recuentos describen definiciones del codigo, no objetos necesariamente descubiertos por un jugador durante el primer capitulo. El hierro y algunos recursos basicos viven fuera del catalogo general.

## 3. Puntos fuertes

### 3.1. Hay una fantasia que puede dar sentido a todos los sistemas

Reconstruir un lugar permite unir acciones que de otro modo serian independientes. La madera puede servir para recuperar una cocina; esa cocina prepara provisiones; las provisiones permiten explorar; la expedicion trae materiales para la siguiente reparacion.

Lo valioso no es solamente conseguir una espada mejor. Es regresar y reconocer algo que existe porque ayudaste. Esa puede ser la recompensa distintiva del juego.

### 3.2. La cadena de aventura ya tiene principio y cierre

Los espacios no son unicamente decoracion: existen puertas de progreso, recursos, encuentros, regreso a casa y un objetivo final. Se puede trabajar sobre una estructura conectada, sin empezar de cero cada sistema.

El guardian y el Rey Limo son especialmente aprovechables. El jefe ya anuncia su salto, tiene ventanas de vulnerabilidad, cobertura y brotes que lo curan. Son decisiones de combate, no solo una barra de vida mas larga.

### 3.3. La interaccion empieza a tener un lenguaje consistente

Herramientas contextuales, indicador junto al objetivo, azada como modo de cultivo y acciones con duracion reducen la necesidad de gestionar una barra enorme de herramientas. La transparencia de los arboles ayuda a mantener visible al personaje.

Hay una buena base para que talar, minar y cultivar compartan reglas faciles de aprender. Conviene conservarla, aunque todavia haya que pulir interrupciones, mensajes y tiempos.

### 3.4. Existe una base visual coherente

Trabajar con el mismo paquete de pixel art ayuda a compartir contornos, perspectiva y materiales. La reorganizacion reciente del pueblo mejora la escala de las casas y separa circulacion, viviendas y cultivos.

No hace falta sustituir los sprites para mejorar mucho la presentacion. Composicion, escala de pixel, jerarquia de elementos, iluminacion y seleccion de variantes tienen mas recorrido inmediato.

### 3.5. Hay infraestructura que merece conservarse

Inventario, guardado, interiores, fabricacion y eventos de acciones ya conectan varias actividades. Las definiciones compartidas de recursos mediante ScriptableObjects son una base util para editar contenido sin duplicar toda su logica.

Tambien existen pruebas de combate, recursos, tutorial y otras mecanicas, ademas de verificaciones de la campana y del pueblo. Los resultados disponibles comprueban accesos, recompensas unicas, fases del jefe, guardado y disposicion del mapa. Eso reduce regresiones; no reemplaza un playtest humano.

## 4. Debilidades de diseno y sus consecuencias

### 4.1. La reconstruccion promete mas de lo que cambia

Actualmente, reparar suele significar entregar recursos, activar un estado, recibir objetos y mostrar algunos cambios de color o decoracion. Los aldeanos del pueblo son interacciones estaticas creadas a partir del mismo personaje con tintes distintos.

En `ValleyWorld.Refresh`, el taller y otras viviendas cambian de color, pero sus fachadas no se sustituyen por una version reparada. El resultado puede seguir mostrando tablas aunque el texto diga que el edificio funciona.

**Consecuencia:** el jugador puede sentir que completa una lista, no que devuelve la vida a un pueblo.

**Mejora prioritaria:** cada reparacion importante debe producir tres resultados: cambio visual reconocible, servicio util y reaccion de un aldeano. El primer caso deberia ser uno solo, construido completo, antes de multiplicar edificios.

### 4.2. La jerarquia aun es una recompensa numerica

Los rangos aparecen a 0, 3, 8, 16 y 28 puntos de influencia. La ruta principal puede conceder 28 al derrotar al Rey Limo sin completar ninguno de los cinco proyectos vecinales: nota 1, primer dia 4, cargamento 2, banco 3, sello 4, guardian 4, portal 5 y jefe 5.

**Consecuencia:** es posible ser Lider de Raizclara sin haber resuelto sus reparaciones. No es un fallo aritmetico; es un desajuste con la fantasia principal solicitada.

**Mejora:** mantener la influencia, pero exigir hitos comunitarios para ascender. El rango debe desbloquear una capacidad concreta: proponer una obra, organizar suministros o priorizar una mejora. No necesitamos simular un gobierno completo.

### 4.3. Hay mucha variedad de inventario, pero poca diferenciacion funcional

Los alimentos difieren sobre todo en precio y recuperacion. El crecimiento usa la definicion del terreno, no un tiempo propio de cada especie del catalogo. Los cultivos comparten la ruta de representacion de sus fases; un brote de frutal no tiene un ciclo independiente de arbol permanente en esta implementacion.

La cocina basica consume dos unidades de `Fruit`, el recurso generico. No acepta automaticamente los 37 alimentos del catalogo. Tener mas ingredientes no significa que ya existan mas recetas o usos conectados.

**Consecuencia:** muchos objetos pueden sentirse como variaciones del mismo numero. Se incrementan el esfuerzo de balance y el espacio de interfaz sin aumentar proporcionalmente las decisiones.

**Mejora:** empezar con pocas familias claras: alimento rapido, ingrediente para provision de viaje y cultivo valioso para intercambio. Darles diferencias de tiempo, rendimiento o receta. Conservar el resto para descubrimientos posteriores, no borrarlo.

### 4.4. El hambre tiene reglas que debilitan su propio valor

El pozo recupera 6 puntos de hambre por interaccion sin coste ni limite en su accion. La reaparicion restaura vida y hambre y conserva los objetos; existe una opcion de continuar donde se murio.

Estos comportamientos son muy permisivos. La reaparicion amable puede ser una buena decision de accesibilidad, pero combinada con la reposicion gratuita hace dificil justificar la preparacion de comida como necesidad constante.

**Mejora:** decidir el tono antes de subir el desgaste. Recomiendo que la comida ayude a preparar expediciones y que el pueblo ofrezca seguridad. El pozo puede atender riego o una recuperacion limitada, sin crear otra barra de sed. Mantendria la conservacion del inventario al morir; cualquier coste de tiempo o regreso debe probarse, no imponerse como castigo automatico.

### 4.5. La economia todavia no hace imprescindible al pueblo

La mochila permite vender directamente, sin visitar el mercado. Rolo, tras su proyecto, ofrece principalmente un trueque fijo de madera por comida. Comprar y vender objetos del catalogo usa el mismo valor en `SimpleShopSystem`.

Nada de esto constituye por si mismo una duplicacion de dinero. El problema es de papel dentro del juego: si la mochila resuelve la venta y el taller global resuelve recetas, necesitamos explicar que aporta reconstruir los establecimientos.

**Mejora:** conservar la comodidad para ventas comunes si resulta agradable, y reservar al mercado contratos, pedidos y objetos exclusivos. Separar valor de venta, coste de compra y coste de fabricacion cuando haya una razon de balance. Documentar de donde entra y donde sale cada material importante.

### 4.6. Recolectar con tiempo aporta peso, pero no variedad por si solo

La espera y los golpes sucesivos son una mejora frente a borrar un arbol instantaneamente. Sin embargo, repetir barras de progreso no constituye una nueva decision.

**Mejora:** hacer que importen la ruta, la herramienta, el rendimiento y la seguridad del lugar. Las mejoras de herramienta ya pueden reducir tiempos: conviene utilizarlas como progresion perceptible. No agregaria un minijuego obligatorio a cada recurso. Mediria cuantas acciones hacen falta para una obra y cuanta parte de la sesion es espera inmovil.

La renovacion tambien necesita reglas comprensibles: los recursos diarios de campana y las vetas de hierro se renuevan por dia; los recursos base tienen otro ciclo. No conviene presentar todos los objetos parecidos como si obedecieran necesariamente la misma regla.

### 4.7. El combate necesita claridad antes que mas armas

La espada aplica dano en un circulo completo alrededor del jugador, aunque la presentacion orienta el ataque. Hay pruebas que fijan deliberadamente ese comportamiento: no debe corregirse como si fuera un accidente. Hay que decidir si queremos un barrido circular visible o ataques direccionales.

Arco y espada tambien reciben bonos compartidos del equipo; la espada suma otras mejoras de progresion. Conviene aclarar la relacion entre el arma equipada y el arma seleccionada en la barra antes de agregar mas niveles.

**Mejora:** establecer dos estilos legibles, cuerpo a cuerpo y distancia, y pocos enemigos que pidan respuestas diferentes. Validar tiempos, alcance, animacion, ventanas del jefe y bonos acumulados. Los elementos pueden seguir siendo pasivos modestos por ahora; no hace falta prometer un sistema elemental completo.

### 4.8. Falta que el pueblo sea reconocible sin leer nombres

La distribucion reciente es mas limpia, pero sigue siendo muy regular. Las viviendas repiten pocas fachadas y los NPC se parecen mucho al protagonista. En la captura revisada, la vegetacion y los objetos del mundo conservan mas presencia visual que algunos elementos interactivos importantes.

**Mejora:** mantener una escala de pixel comun, pero dar funcion visual a cada parcela: herramientas en el taller, suministros en el mercado, un huerto identificable. Usar los personajes y variantes disponibles del paquete. Consistencia no significa que todos los edificios deban terminar siendo identicos.

La ruina debe ser localizada y comprensible: puerta tapiada, puesto vacio, cerca rota, cultivo abandonado. No volver a llenar el suelo de objetos dispersos para comunicar abandono.

### 4.9. La interfaz mejoro, pero aun mezcla soluciones

El HUD compacto deja mas mundo visible. Las ventanas de diario, construccion y otras acciones mantienen paneles, tipografia y distribuciones propias; algunas concentran instrucciones y varias clases de informacion en la misma superficie.

**Mejora:** un lenguaje comun de marcos, fuentes, estados e iconos del paquete. Separar objetivo activo, proyectos y aldeanos en el diario; comparar equipo por diferencias; mostrar ingredientes y faltantes graficamente. Una accion normal necesita una senal breve, no un mensaje por cada golpe.

Para fuentes, importa mas la nitidez y la lectura a resolucion real que parecer pixel art a cualquier precio. Verificar acentos, cantidades, contraste, escala de interfaz y texto largo. No resolver cada problema agrandando paneles.

## 5. Riesgos tecnicos prioritarios

### Alto: proteger las partidas antes de ampliar el mundo

`GameSaveSystem.SaveGame` escribe directamente sobre el JSON final. `TryLoadGame` lee y deserializa sin una recuperacion local ante errores de archivo o formato. No hay en esos metodos un archivo temporal, copia anterior ni restauracion de respaldo.

Esto es un riesgo observable de implementacion, no una afirmacion de que ya se haya perdido una partida. Una interrupcion de escritura o un JSON danado puede dejar al jugador sin una carga recuperable.

**Accion recomendada:** escritura temporal y reemplazo, respaldo anterior, validacion, errores controlados y pruebas de corrupcion. No sobrescribir una partida no recuperada con un estado inicial silenciosamente.

### Alto al editar mapas: identidad persistente de parcelas

El identificador de `FarmingPlot` se construye con el nombre y los indices de hermanos de su jerarquia. Permite mover el objeto sin cambiar el ID calculado durante esa ejecucion, pero no garantiza estabilidad entre versiones si reorganizamos hermanos o nombres.

**Accion recomendada:** identificadores serializados estables y migracion explicita de los anteriores. Probar una partida vieja despues de reorganizar la escena. No cambiar el formato de identificadores sin preservar la correspondencia existente.

### Medio: varias fuentes de progreso y datos

Conviven `TutorialQuestSystem`, `AdventureProgress` y `ValleyCampaign`. El hierro esta en el progreso de aventura; otros materiales, en inventario. Objetivos, recetas y desbloqueos contienen cadenas y condiciones distribuidas.

Eso aumenta el coste de hacer que diario, HUD, mundo y guardado coincidan. Por ejemplo, el tutorial menciona comer y registra `ate`, pero la condicion central de apertura del campamento comprueba otras acciones y no ese indicador.

**Accion recomendada:** definir el rol de cada ruta, reunir reglas de desbloqueo y normalizar requisitos de recursos. Migrar por partes con pruebas, aprovechando las definiciones compartidas existentes; no reescribir todo el proyecto.

### Medio: el mundo se construye principalmente al ejecutar

Parte del pueblo, sus NPC, las zonas y la interfaz aparecen por codigo durante Play. Eso explica el problema real de no ver en la escena editada todo lo que termina mostrandose en el juego.

**Accion recomendada:** una previsualizacion de editor o una generacion explicita hacia prefabs/escena, separada del estado de la partida. Debe poder mostrar ruina y reconstruccion sin alterar el guardado, duplicar objetos ni conceder recompensas. No prometer recarga instantanea de cualquier cambio de codigo.

### Riesgo de crecimiento, no rendimiento medido

La campana crea de entrada cinco superficies de 32 x 20 celdas como objetos individuales; el generador de caminos agrega objetos por cuadrante. Es una carga estructural importante si seguimos ampliando de la misma forma, aunque aqui no se ha medido una caida de FPS.

**Accion recomendada:** perfil de arranque, cantidad de objetos, memoria y pausas de guardado; despues, Tilemaps para terreno estatico y activacion por regiones donde aporte valor. No introducir optimizaciones complejas sin medir.

### Documentacion y reproducibilidad

Hay documentos que aun describen guardado version 12 o 15, mientras el codigo usa 20, y limitaciones de muebles que ya no son actuales. La documentacion debe distinguir historico de estado vigente. Tambien conviene consolidar un punto de control versionado con los archivos nuevos y sus `.meta`, sin descartar el trabajo existente.

## 6. En que puede transformarse

| Direccion | Encaje con lo existente | Coste y riesgo | Recomendacion |
| --- | --- | --- | --- |
| RPG de reconstruccion y expediciones | Alto: aprovecha pueblo, cultivo, equipo y campana | Exige conectar servicios, retorno y progreso social | Identidad principal |
| Simulador tranquilo de aldea y granja | Parcial: ya hay hogar y cultivo | Exige profundidad agricola, rutinas y relaciones; el combate pasa a segundo plano | Tomar su calidez, sin asumir toda su simulacion |
| Survival de combate exigente | Parcial: hay hambre y combate | Exige rehacer presion, recuperacion, recursos y dificultad; puede competir con decorar y reparar | No priorizar ahora |

No son tres modos para construir a la vez. Son opciones de enfoque. Intentar satisfacerlas simultaneamente multiplicaria sistemas y expectativas.

### El ciclo que deberia mandar

1. Descubrir una necesidad concreta del pueblo.
2. Elegir como preparar recursos, comida y equipo.
3. Salir a una expedicion con objetivo y alguna decision de ruta.
4. Resolver una amenaza o conseguir algo especial.
5. Volver y completar una obra visible.
6. Aprovechar el servicio nuevo para afrontar una expedicion diferente.

La granja sostiene las salidas; las salidas sostienen la reconstruccion; la reconstruccion cambia la granja y las siguientes salidas. La influencia reconoce esa contribucion.

### Un ejemplo de evolucion de los cinco aldeanos

Lo siguiente es una propuesta, no una descripcion de funciones actuales.

| Aldeano | Papel narrativo sugerido | Servicio concreto al recuperarlo |
| --- | --- | --- |
| Mara | Memoria del pueblo; primero necesita confiar en que te quedaras | Despensa y preparacion limitada de provisiones |
| Nico | Quiere volver a trabajar, pero necesita pruebas de que vale la pena | Forja con una mejora exclusiva que se nota en la siguiente salida |
| Dalia | Conserva lo poco que queda de la agricultura del valle | Semillas seleccionadas y un cultivo util para una receta nueva |
| Rolo | Busca viabilidad, no promesas | Pedidos de compra y trueques que dan salida a excedentes |
| Iria | No quiere que se repita el desastre | Ruta mas segura o ayuda concreta en un acceso recuperado |

Para darles vida bastarian inicialmente dos o tres posiciones diarias y unas lineas de dialogo dependientes del progreso. Los horarios no deben impedir entregar una mision importante. No necesitamos de entrada un simulador social con decenas de necesidades.

### Una historia mas personal

La semilla perdida proporciona un misterio, pero falta fortalecer por que el protagonista llega y decide quedarse. Una invitacion antigua o una deuda con alguien de Raizclara podrian hacerlo; habria que elegir una, no sumar explicaciones.

El primer vinculo debe ser una persona y un problema pequeno. El misterio del valle crece despues. Una reparacion puede revelar una pista; un guardian puede estar protegiendo algo, no solo bloqueando un camino. La historia actual ya insinua esa ambiguedad con los sellos.

Cada jefe importante deberia devolver algo al mundo: una ruta, un suministro o una funcion de la tierra. Su botin puede incluir equipo, pero su consecuencia principal debe poder verse al regresar.

La jerarquia puede seguir cinco escalones, pasando de recibir ayuda a proponer proyectos. Es mejor una decision real como priorizar la despensa o la forja que veinte ascensos que solo entreguen monedas.

## 7. Orden de trabajo recomendado

### Fase 0: confianza y herramientas de iteracion

Proteger guardados, fijar identidades persistentes y preparar una vista de editor del pueblo. Consolidar una referencia reproducible del proyecto actual.

**Criterio de salida:** un archivo danado tiene una recuperacion controlada; una partida conserva parcelas y obras tras editar el mapa; la previsualizacion no toca el progreso del jugador.

### Fase 1: una reparacion que cambie la experiencia

Elegiria el taller de Nico como primera demostracion: encargo, materiales, fachada realmente reparada, cambio de actividad, servicio exclusivo y reconocimiento de influencia. La mejora debe tener una utilidad clara en una salida ya existente.

**Criterio de salida:** una persona puede explicar que gano al reparar el taller y usarlo sin consultar el diario constantemente. La diferencia del pueblo se reconoce sin un cartel que la describa.

### Fase 2: conectar comida, materiales y rangos

Acotar los cultivos iniciales, definir dos o tres recetas con ingredientes reales, revisar el pozo, clarificar la venta y hacer que los rangos exijan contribuciones comunitarias. Resolver la convivencia entre recursos genericos y alimentos del catalogo.

**Criterio de salida:** hay una razon practica para cocinar, una para vender y una para guardar materiales; no se alcanza el liderazgo dejando intacta toda la reconstruccion. No se obliga a depender de un botin aleatorio raro para una tarea inicial imprescindible.

### Fase 3: pulir la primera expedicion y su retorno

Conservar las zonas existentes. Afinar alcance visual y real de armas, pocos comportamientos enemigos, lectura del jefe, recompensas y atajo de vuelta. Reducir viajes de recadero que no agregan una decision.

**Criterio de salida:** el jugador entiende por que recibio dano, reconoce una mejora de equipo y encuentra al regresar una consecuencia de su expedicion.

### Fase 4: primer capitulo presentable

Unificar UI, caracterizar los cinco NPC con arte existente, reforzar sonido y reacciones, y recorrer la experiencia sin ayudas de desarrollo. Como objetivo de prueba, preparar unos primeros 30-45 minutos claros y satisfactorios; esa duracion no esta medida ni garantizada en el estado actual.

**Criterio de salida:** el juego se sostiene con un jugador nuevo, no solo con quien conoce las coordenadas, dependencias y soluciones.

## 8. Como comprobar que mejora de verdad

Haria una pequena ronda con jugadores que no hayan seguido el desarrollo, partiendo de una ranura nueva y sin teletransporte, recursos regalados ni reloj acelerado.

Registrar tiempos y observaciones, no interpretar una muestra pequena como garantia comercial:

- Tiempo hasta comprender el primer objetivo y completar la primera interaccion.
- Veces que preguntan que hacer, donde ir o por que una accion no funciona.
- Tiempo de viaje, accion bloqueada, gestion de menus y decisiones libres.
- Comida producida y consumida, materiales sobrantes y motivos para volver al pueblo.
- Si distinguen a los NPC y reconocen al menos una reparacion sin explicacion.
- Por que eligen espada o arco y si entienden las fases del jefe.
- Si quieren seguir al terminar y que objetivo elegirian por iniciativa propia.

Preguntas utiles: "Que cambio al reparar el taller?", "Para que te llevaste comida?", "Que te gustaria recuperar despues?". No dirigir la respuesta diciendo que deberian haber disfrutado.

En pruebas tecnicas agregaria guardado corrupto, migracion tras mover/reordenar objetos, condiciones de rango, relacion entre arma equipada y seleccionada, requisitos de cocina, y cancelacion o guardado durante acciones largas. Mantendria las pruebas existentes de recompensa unica, accesos, jefe y colocacion.

## 9. Que no priorizaria ahora

- Mas minerales, armaduras y semillas antes de que los actuales tengan papeles claros.
- Un mapa mucho mayor: primero aumentar el significado de sus lugares.
- Multijugador, temporadas completas o simulacion economica autonoma.
- Decenas de NPC, romances o arboles enormes de habilidades.
- Castigos mas duros para intentar hacer importante la supervivencia.
- Sustituir el paquete de sprites o redisenar toda la UI otra vez sin una guia consistente.
- Reescribir toda la arquitectura. Los cambios tecnicos deben servir a problemas concretos y conservar partidas.

## 10. Conclusion

El juego tiene una base real y una direccion aprovechable. Su debilidad principal es que la amplitud de sistemas ha crecido mas rapido que las consecuencias entre ellos.

**La siguiente mejora decisiva no es que haya mas cosas que conseguir, sino que conseguirlas transforme Raizclara de una forma visible, util y personal.**

Cuando el jugador piense "quiero regresar para ver que hemos recuperado", la agricultura, la exploracion, los jefes y la influencia estaran trabajando para el mismo juego.

## Evidencia consultada

Enlaces al estado local revisado; sus numeros de linea pueden cambiar con futuras ediciones.

- [Campana, rangos y recompensas](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ValleyCampaign.cs:126>). Umbrales y ascensos; los eventos de historia estan en el mismo archivo.
- [Pozo y acciones del pueblo](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ValleyCampaign.cs:258>). Recuperacion de hambre, entregas y accesos.
- [NPC y construccion del mundo](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ValleyWorld.cs:60>). Posiciones, variantes y cambios visuales en `Refresh`.
- [Captura reciente del pueblo](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Design/Validation/VillageLayout/pueblo-1920.png>). Estado controlado de verificacion, no captura de una nueva partida humana de esta revision.
- [Catalogo de alimentos y semillas](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/SurvivalItemCatalog.cs:31>) y [recetas y cocina](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerCraftingController.cs:17>).
- [Cultivo e identificadores de parcelas](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Gameplay/FarmingPlot.cs:72>). Ciclos, identificacion y representacion compartida.
- [Venta desde la mochila](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/BackpackActions.cs:18>) y [compra y venta del catalogo](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/UI/SimpleShopSystem.cs:132>).
- [Recoleccion temporizada](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Gameplay/HarvestableResource.cs:162>) y [progreso de la aventura anterior](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/AdventureProgress.cs>).
- [Ataques y bonos](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerCombatController.cs:112>), [estadisticas del equipo](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerInventory.cs:67>) y [prueba del barrido de espada](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Tests/PlayMode/SwordAreaTests.cs:48>).
- [Encuentros y jefe](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ValleyEnemy.cs>) y [reaparicion](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerRespawnController.cs:46>).
- [Ventanas de diario y construccion](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/UI/AdventureWindow.cs:14>) y [generacion del terreno](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/World/ValleyTerrain.cs:77>).
- [Escritura y carga de partida](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Assets/SurvivorFarm/Scripts/Runtime/Core/GameSaveSystem.cs:106>).
- [Resultado existente de campana](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Design/Validation/VillageLayout/campaign-test.txt>) y [resultado existente de distribucion y HUD](<C:/Users/Juan Jose/Documents/ProyectosUnity/Juegos/Survivor Farm/Design/Validation/VillageLayout/test.txt>).
