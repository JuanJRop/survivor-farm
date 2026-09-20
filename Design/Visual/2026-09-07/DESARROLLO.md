# Desarrollo visual y preparación para Unity

## Qué existe y qué se propone

| Elemento | Base comprobada en el proyecto | Propuesta de este paquete |
|---|---|---|
| Granja | Cultivos, recursos, animales, casa y desbloqueo de terreno. | Nueva composición visual y jerarquía del HUD. |
| Supervivencia | Cinco puntos de salud y medidor de hambre. | Corazones y barra legibles en móvil. |
| Inventario y tienda | Inventario, compra/venta de semillas y recursos, construcciones. | Mochila en cuadrícula y taller por tarjetas. La primera construcción de almacén cuesta 8 maderas y 4 piedras; campamento, 6 y 6. |
| Herramientas | Espada, arco, hacha, pico, azada, pala y regadera. | Apariencia diferenciada de cada familia y posibles variantes. |
| Mejoras actuales | Hacha, pico y pala hasta nivel 3. Nivel 2 cuesta 15 monedas, 4 maderas y 4 piedras; nivel 3, 35/8/8. | Las tarjetas del taller usan el coste de nivel 2. |
| Personaje | El animador intenta George y usa Josh como alternativa. El pack presente incluye Josh y Lyria. | Progresión de equipo para Josh y Lyria; Lyria como opción visual, aún sin selector implementado. |
| Enemigos | Limo, murciélago y gólem configurados en el constructor. | Diseños propios, anticipaciones, áreas de ataque y botín como propuesta. |
| Progresión del personaje | No se ha comprobado un sistema de clases o experiencia en esta base. | Pantalla de especialización en cultivo, exploración y combate. Requiere implementación y balance. |
| Inicio | Hay escena Main y sistema de guardado. | Menú de inicio con continuar, nueva partida y ajustes. |

La evolución de espada, arco, azada y regadera es una exploración visual futura. El código actual solo mejora hacha, pico y pala. Los atuendos no asignan estadísticas ni desbloqueos automáticamente.

## Personajes

**Josh**: cabello negro, camisa azul y accesorios marrones. Novato con útiles de granja; explorador con mochila y equipo práctico; veterano con cuero reforzado y un pequeño acento mineral. La identidad y las proporciones se conservan entre etapas.

**Lyria**: propuesta de variante jugable con cabello cobrizo y camisa verde. Misma escala y lectura que Josh. La propuesta de apariencia requiere contrastarse con las animaciones y retratos originales antes de sustituirlos.

Prioridad de producción: una base de personaje completa en cuatro direcciones; después capas de equipo y variantes. Las vistas de la lámina orientan la apariencia y no constituyen fotogramas de animación.

## Progresión jugable sugerida

Propuesta pendiente de diseño y balance: vincular la primera etapa al tutorial de recolección y cultivo; desbloquear el aspecto de explorador al completar una primera expedición y fabricar equipo; reservar el aspecto veterano para mejoras avanzadas de la granja y las herramientas. Mantener apariencia y estadísticas separadas permite cambiar de atuendo sin perder progreso. Cultivo puede favorecer cosechas, exploración la obtención de recursos y combate la supervivencia en mazmorras. Los efectos y valores concretos deben definirse antes de programar estas ramas.

## Pantallas

1. **Inicio**: comprobar si hay partida guardada para habilitar Continuar.
2. **Granja**: información de supervivencia arriba, misión compacta y controles al borde. Movimiento por toque, como la configuración actual.
3. **Mazmorra**: mantener posiciones del HUD; distinguir los avisos enemigos del botín y del terreno.
4. **Inventario**: selección visible, información del objeto y acción contextual.
5. **Taller**: mostrar coste, recursos disponibles, nivel actual y estado máximo; reutilizar los datos reales del controlador de mejoras.
6. **Personaje**: propuesta de equipo y especializaciones; determinar primero su efecto en el diseño del juego.

Botones y etiquetas dibujados en las láminas son parte del concepto. Sus estados pulsado, seleccionado, bloqueado y deshabilitado se construirán como componentes de UI.

## Producción de assets

- Elegir una resolución de trabajo compatible con los sprites existentes antes de dibujar o recortar. Propuesta inicial: personajes sobre celda de 32 × 32 e iconos de 24 × 24 o 32 × 32; confirmar tamaño, pivote y escala con los assets importados.
- Crear PNG individuales con transparencia real para objetos y personajes. Estas láminas tienen fondo de presentación.
- Preparar animaciones por acción y dirección con celda fija y pies alineados. Como punto de partida: idle 4 fotogramas, caminar 6, correr 6, acciones 6 y daño 2; ajustar a la lógica del animador actual. No son requisitos ya implementados.
- Conservar por separado cuerpo, herramientas, proyectiles y efectos cuando el flujo del proyecto lo permita.
- En Unity, revisar `Sprite (2D and UI)`, filtro `Point`, compresión, PPU, pivote y corte. Mantener los ajustes coherentes con el importador existente.
- Componer las pantallas con paneles reutilizables y texto real. Evitar usar una pantalla completa rasterizada como interfaz interactiva.
- Validar siluetas y legibilidad al tamaño real del teléfono; comprobar que botones y HUD no cubran los objetivos de interacción.
- Animar el anticipo antes del ataque y comprobar que el área dibujada coincida con el daño real. Los avisos del bestiario son ideas de diseño.

## Referencias locales revisadas

- `Assets/SurvivorFarm/Scripts/Runtime/World/FarmPrototypeBuilder.cs`: escena, enemigos y Canvas de 450 × 800.
- `Assets/SurvivorFarm/Scripts/Runtime/Gameplay/FarmTool.cs`: siete familias.
- `Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerToolUpgradeController.cs`: niveles y costes.
- `Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerSurvivalStats.cs`: salud y hambre.
- `Assets/SurvivorFarm/Scripts/Runtime/Player/PlayerCharacterAnimator.cs`: clips y selección de personaje.
- `Assets/SurvivorFarm/Art/Sprites/FarmRPGTinyAssetPack`: base visual ya disponible.

Este paquete se guarda en Design para su revisión. La escena y el código jugable mantienen su estado previo a este trabajo.
