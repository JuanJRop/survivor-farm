# Casa y progresión

Entra por la casa del terreno. Dentro, **H** abre la compra de muebles y las ampliaciones. Los muebles comprados llegan a la mochila (**I**): selecciona **Colocar**, apunta a un espacio libre y confirma con clic izquierdo. Clic derecho o Escape cancela sin perder el objeto.

La primera instalación es una fogata. Después puedes comprar una cama y cofres. Al interactuar con un mueble interior puedes usarlo, moverlo o guardarlo en la mochila. Los cofres conservan sus recursos al moverlos; deben estar vacíos para guardarlos como objeto.

| Etapa | Interior | Requisito y coste para llegar |
|---|---|---|
| Refugio | 8 × 6 | Vivienda inicial |
| Casa de madera | 10 × 8 | Fogata colocada; 30 madera, 20 piedra, 50 oro |
| Casa de piedra | 14 × 10 | Campamento desbloqueado; 80 madera, 60 piedra, 5 hierro, 150 oro |
| Mansión del valle | 18 × 12 | Rey Limo derrotado; 180 madera, 140 piedra, 20 hierro, 400 oro |

Desde la casa de madera puedes cambiar el acabado del suelo y comprar armarios y un banco de trabajo. La casa de piedra permite comprar un horno. El horno transforma 5 piedra y 2 madera en 1 hierro por uso. La cama permite dormir de noche y establece el punto de aparición fuera de la casa.

El guardado conserva ampliación, acabado, muebles, posiciones y contenido de los cofres. Las camas adquiridas en partidas anteriores se convierten en un objeto colocable de la mochila. Se usan gráficos del paquete que ya estaba en el proyecto.

## Validación de esta entrega

- Compilación C# de todos los archivos actuales de ejecución: correcta, con el compilador y las referencias de Unity 6000.3.9f1.
- Compilación de HouseVerification: correcta.
- Pruebas de juego y capturas: pendientes; la instancia abierta de Unity no procesó la solicitud de actualización y no se pudo activar su ventana. Una segunda instancia no pudo abrir el mismo proyecto.
- La versión Windows existente aún corresponde a la entrega anterior.

La prueba `HouseVerification` comprueba entrada/salida, compras, bloqueo de la puerta, colocación, descanso, movimiento de cofres con contenido, ampliaciones, horno, guardado y reaparición. Para ejecutarla con Unity abierto, fuera de Play y tras compilar, crear `Library/VerifyHouse.request`. Usa un estado temporal sin escribir la partida del usuario y deja su resultado en `Design/Validation/House/test.txt`.
