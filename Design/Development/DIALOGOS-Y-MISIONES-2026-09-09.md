# Conversaciones y decisiones de mision

## Comportamiento

- Interactuar con Mara, Nico, Dalia, Rolo o Iria abre una conversacion, sin entregar materiales ni ejecutar trueques automaticamente.
- Opciones: preguntar por trabajo, charlar sobre el pueblo, consultar servicios y despedirse.
- La oferta muestra titulo, requisitos de historia, cantidades disponibles/requeridas y recompensa. Se puede aceptar o responder Ahora no.
- Aceptar solo registra el encargo. La entrega exige volver a consultar el encargo y pulsar Entregar materiales; los costes se comprueban nuevamente en ese momento.
- Ahora no no bloquea el encargo ni consume recursos. Una mision ya aceptada sigue en curso al cerrar la conversacion o seguir reuniendo materiales.
- El diario distingue encargos por aceptar, en curso y completados. Los requisitos de la historia siguen vigentes.
- Los servicios de aldeano tienen acciones propias: los herrajes de Nico y el trueque de Rolo ya no son efectos secundarios de saludar.
- Cierre con X, Escape, despedida, apertura de otro menu, muerte, cambio de partida o alejamiento del interlocutor. El dialogo participa en el bloqueo de movimiento y ataque de la UI.
- Se usan los personajes originales del catalogo VillageNpcArt, sin sprites nuevos generados.

## Persistencia

`ValleyData.acceptedVillageQuests` guarda los cinco encargos aceptados como bits independientes. Es un campo adicional del formato actual; las partidas anteriores conservan sus obras ya completadas y no vuelven a cobrar sus recompensas. Los encargos anteriores sin terminar se pueden aceptar mediante la conversacion.

Las banderas existentes de obras siguen siendo la autoridad para determinar si una mision se completo. Repetir la conversacion, la entrega o una restauracion no vuelve a pagar recompensas.

## Comprobaciones

- Compilan Runtime, Editor y PlayModeTests.
- 197 pruebas PlayMode aprobadas, incluidas decisiones independientes para los cinco aldeanos, guardado del encargo aceptado, rechazo sin efectos, requisitos incompletos, cierre sin aceptar, interlocutor fuera de alcance, invalidacion tras recarga y entrega reentrante sin recompensa duplicada.
- En la escena real de una copia aislada: se abren los cinco dialogos con sus sprites originales; se pulsan los botones de rechazo, aceptacion y entrega; se guarda/restaura una mision en curso y se comprueba que no se duplica la recompensa.
- Capturas de conversacion y oferta revisadas a 1280x720 y 1920x1080. El recorrido automatizado de campana, las casas exteriores y la distribucion del pueblo siguen aprobados.

Resultados: `Design/Validation/Team/playmode-results.xml` y `Design/Validation/Team/Integrated/VillageDialogue/result.txt`.

Capturas: `Design/Validation/Team/Integrated/TeamUi/conversacion-1920.png`, `oferta-mision-1280.png` y `mision-en-curso-1920.png`.

Las pruebas se hicieron con recursos y posiciones controlados, sin tocar partidas personales ni cerrar el editor del usuario. No se genero un nuevo ejecutable; los cambios estan en el proyecto Unity.
