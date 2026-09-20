# Demo: proteger el pueblo y explorar el valle

## Dirección de juego

El centro de la demo es defender a los vecinos y sus casas. Durante el día puedes buscar suministros en campamentos y mazmorras, cultivar, cocinar y mejorar tus herramientas. Durante la noche debes decidir a quién proteger y cuándo curarte. Se conservan tres noches y el encuentro final con el Custodio.

## Cambios implementados

- Retiradas la fabricación, colocación, recetas y accesos de murallas, cercas, trampas y torretas. La carga de partidas antiguas descarta esas defensas y conserva los muebles domésticos.
- Seguridad del pueblo: promedio de la proporción de vida de cada casa y aldeano. Las bajas permanecen en el cálculo; una muerte no mejora artificialmente el porcentaje.
- Casas: 32 de vida. Reparar o reconstruir resta 2 de madera y devuelve hasta 8 de vida.
- Vecinos: 8 de vida. Auxiliar a uno herido consume una ración y recupera hasta 4. Los fallecidos no resucitan al reparar el pueblo.
- Cuando toda la integridad llega a cero, el pueblo queda ocupado, aparecen marcas de invasores y una guarnición de hasta cuatro enemigos. El reloj se detiene. Para liberar la plaza hay que vencerlos, reconstruir al menos una casa e interactuar junto al pozo.
- Los asaltantes eligen vecinos y casas; acercarse permite atraer parte de la oleada. El pozo ya no es la condición de derrota.
- Mapa ampliado a 88 × 52 unidades, con caminos, señales y límites físicos coherentes. Se mantiene la animación sincronizada solo en la orilla superior del río.
- Tres campamentos: bosque al suroeste, cantera al sureste y refugio de demonios al noreste.
- Dos portales al norte: cripta del bosque y santuario de sangre. Cada uno conecta con una cámara custodiada, cofre y portal de regreso siempre accesible.
- Los cofres se abren con E después de derrotar a los guardianes. Sus premios y los enemigos vencidos se guardan por identificadores estables; cargar no permite cobrarlos otra vez.
- Cinco minutos de día y 45 segundos de aviso antes de la noche. Las mazmorras suspenden el reloj del pueblo y bloquean el guardado hasta regresar. El progreso exterior se guarda al superar guardianes o cobrar cofres.
- Guardado versión 21, compatible con las versiones antiguas admitidas. La ocupación conserva el último punto seguro para continuar tras una derrota.

## Controles

WASD: moverse. Clic: atacar; mantener y soltar: golpe cargado. Espacio: esquivar. E: interactuar, abrir cofres, reparar y auxiliar. Q: curarse con una ración. I: mochila. F: recetas. K: maestrías.

## Próxima prioridad de diseño

Jugar partidas completas para ajustar presión nocturna, supervivencia de aldeanos y duración de las excursiones. La base ya permite medir decisiones útiles: desviarse por un cofre, gastar una ración en un vecino o conservarla para el siguiente combate. Antes de añadir más contenido, conviene comprobar que esas decisiones sean claras y que el combate siga siendo divertido al repetirlas.

## Verificación

Los resultados de compilación, pruebas de Unity y comprobación del ejecutable se guardan en `Design/Validation/VillagePivot/`. Las capturas nativas se preparan automáticamente para inspeccionar la presentación; no sustituyen una sesión manual de balance.
