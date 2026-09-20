# Agua, enemigos Tiny RPG y Combat FX

## Cambios

- El río conserva la animación del atlas LivingWater únicamente en la orilla
  superior. Todos los tramos de esa orilla comparten altura, velocidad y fotograma,
  incluidos los extremos del río. La orilla inferior usa césped estático de
  TerrainAtlas. Se retiran los reflejos y juncos que ocupaban la parte inferior.
  Las colisiones del río y el paso del puente se conservan.
- Soldado y Orco proceden del paquete 01 v2.0. Demonio y Monstruo de sangre
  proceden del paquete 02 v1.01. Las otras tres versiones de Soldier/Orc son
  revisiones anteriores de los mismos personajes y no crean duplicados.
- Cada personaje usa sus cinco secuencias originales: reposo, caminar, ataque,
  daño y muerte. Se mantienen los píxeles y sombras de los 20 PNG originales.
  Sus vistas laterales se reflejan al mirar a la izquierda.
- Los cuatro enemigos aparecen progresivamente en las oleadas de campaña.
  También están disponibles en Arena → F6 → Bestiario y en el circuito de cinco
  oleadas. Se conserva el encuentro con el Custodio.
- La reutilización de un enemigo muerto restaura sus colisiones antes de cambiar
  de personaje. Las barras de vida se colocan junto a la cabeza visible, sin usar
  los márgenes transparentes de los fotogramas de 100 píxeles.
- Los cortes normales, el remate, la carga, la descarga y los impactos usan
  secuencias originales de Combat FX 1.1. Las duraciones se ajustan a los tiempos
  del combate existente; se respetan las proporciones temporales de los
  fotogramas de origen. Se sustituyen el shader de espada, las partículas
  generadas y el destello blanco de material.
- Cada golpe contra una defensa muestra un solo impacto del material adecuado.
  La creación de los componentes visuales respeta los objetos nulos especiales
  de Unity, tanto en ataques directos como en llamadas del motor durante Update.
- Los círculos que anuncian el radio de daño siguen siendo indicadores de área:
  su tamaño representa la zona real del ataque. El audio y las reglas de daño
  siguen utilizando los sistemas existentes.

## Créditos

Combat FX: Raphael Hatencia (RagnaPixel), CC BY 4.0. Atribución visible en Opciones,
licencia original en Resources/CombatFX y copia de los créditos en la distribución.
Los archivos SOURCE.md de los personajes registran los ZIP utilizados.

## Comprobación

La validación usa una copia aislada en QA/p. Los informes de esta entrega están
en Design/Validation/AssetPacks. El modo de captura se activa únicamente con
`--qa --asset-pack-smoke`, prepara escenas independientes y no usa guardados de
campaña. Las capturas automáticas no equivalen a un playtest humano de equilibrio.
