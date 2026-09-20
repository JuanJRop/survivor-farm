# El valle de la semilla perdida

La campaña utiliza la granja existente como capítulo inicial y añade cinco espacios delimitados. Los carteles conectan campamento, cantera, bosque y santuario. Solo el portal del santuario lleva a la arena del Rey Limo.

## Recorrido jugable

1. **Granja:** leer la nota del cofre junto al inicio; cosechar, cocinar y construir la fogata. Llegar vivo al siguiente día abre el campamento. La cama permite avanzar la noche.
2. **Campamento:** recuperar el cargamento, rodeando los limos o combatiendo; reparar el banco con 8 madera y 4 piedra. Entrega 30 oro para la mejora del pico. La exploradora intercambia 5 madera por una comida. Rescatar la gallina añade su representación a la granja y dos raciones.
3. **Cantera:** recolectar piedra, mejorar el pico a nivel 2 en F y usarlo en el derrumbe. El primer sello abre el bosque. Hay una veta secreta y un atajo a casa. Las fuentes de recursos se renuevan por día y no duplican su recompensa al guardar y cargar.
4. **Bosque:** un enemigo aislado, un grupo y el guardián. Sus golpes tienen preparación visible. El guardián entrega el segundo sello y tres raciones; vuelve el ciervo al claro.
5. **Santuario:** entregar un fruto cosechado con ambos sellos activa el portal. La entrada fija un punto de reaparición. Hay un atajo de regreso a la granja.
6. **Arena:** el Rey Limo anuncia dónde caerá, salta y queda agotado durante 2,5 segundos. En ese estado recibe daño doble. Dos brotes destruidos con espada o arco dejan de curarlo. Las rocas sirven de cobertura. Tras vencerlo, abrir el cofre, volver por el portal y plantar la semilla en el altar de la granja.

El final añade cultivos decorativos, 100 oro, 20 semillas y 2 puntos de vida máxima una sola vez. Continúan disponibles la agricultura, la construcción y las expediciones anteriores.

## Controles y guardado

WASD para moverse; clic izquierdo para atacar con espada o arco; E o clic derecho para interactuar; F para recetas y mejoras; J para el diario. Los rótulos de salida y las indicaciones de interacción identifican las conexiones. La barra del jefe muestra su salud y fase.

El guardado versión 15 conserva la campaña junto al inventario, las construcciones y la granja existentes. Las partidas previas comienzan la nueva historia con la nota, conservando sus objetos. Un combate interrumpido se reinicia desde la entrada del área al cargar. Los sellos, el portal y la victoria del jefe permanecen desbloqueados.

## Validación

La prueba de Play Mode está en `ValleyVerification.cs`; se inicia con `Library/VerifyValley.request`. Comprueba accesos bloqueados, interacciones, recursos diarios, los seis capítulos, fases del jefe, reaparición, recompensas únicas y guardado/carga. Usa suministros y reloj controlados para recorrer la campaña y evita escribir en la partida personal. Los resultados y capturas se guardan en `Design/Validation/Valley`.

La comprobación automática no sustituye una sesión humana completa para ajustar dificultad, tiempos de viaje y economía.

## Terreno y vegetación

Los caminos de la granja y las cinco zonas de aventura ajustan sus bordes al terreno vecino, incluidas las esquinas de los cruces. La arena tiene un claro de tierra rodeado de pasto. El campamento incluye una ruta que rodea el claro; la cantera y el santuario tienen ramales hacia sus objetivos.

Se usan recortes del tileset de primavera, arbustos y arbustos floridos del bosque y girasoles del paquete original. La vegetación decorativa no incorpora colisiones y evita caminos, objetos importantes y el centro de la arena. Las flores de las casillas de la granja se ocultan al trabajar la tierra. Las parcelas y el guardado conservan sus datos.
