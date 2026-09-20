# Mochila y equipamiento separados

- B / I abre la mochila: herramientas agrícolas, comida, semillas y materiales. Las armas y el equipo no aparecen en ninguna categoría de esta mochila.
- C abre Personaje: retrato animado del player original y ocho casillas (casco, pechera, botas, arma, escudo, gema y dos accesorios).
- La lista inferior contiene solo equipo que el jugador posee. Clic para equipar, arrastre para elegir casilla y clic derecho en una casilla para quitar.
- Las casillas validan tipo y propiedad. Un accesorio no puede duplicarse en ambas ranuras. El arma equipada se sincroniza con PlayerToolbelt, incluidos los atajos 1 y 2.
- Las dos pantallas son excluyentes; Esc cierra. Bloquean las entradas de movimiento y combate.
- OwnedEquipment y EquippedEquipment se guardan con la partida; las partidas antiguas conservan espada y arco como equipo inicial.
- PlayerInventory.AddEquipment(id) permite incorporar piezas adquiridas por futuras recompensas o tiendas. No se añaden armaduras gratis a la partida.
- Esta entrega implementa el inventario y las casillas; no añade estadísticas de armadura, botín nuevo ni capas de armadura sobre las animaciones del mundo.
- Los iconos proceden de los sprites existentes (RPG/3 y Gemstones). La casilla Escudo está preparada con un fondo vacío: no se encontró un icono de escudo inequívoco en los recursos revisados.
- La mochila mantiene pilas de 99, nuevas casillas automáticas, arrastre, rueda del ratón y orden persistente.

Las capturas proceden del modo Play con datos de prueba que no se guardan en la partida del usuario.
