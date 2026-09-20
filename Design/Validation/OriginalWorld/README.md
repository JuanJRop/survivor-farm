# Gráficos originales integrados

La escena `Assets/SurvivorFarm/Scenes/Main.unity` usa los sprites que ya estaban en el proyecto. No se generó arte con IA. Los 57 recortes y sus coordenadas de origen están en `Assets/SurvivorFarm/Art/WorldSprites/manifest.json`; se pueden reproducir con `Design/Previews/OriginalSprites/export_world_slices.py`.

- Césped y caminos: tilemaps de primavera, píxeles sin suavizado.
- Casa: edificio completo situado al norte del jugador, con colisión en la base y orden de dibujo por altura.
- Tienda: edificio del herrero del pack, con mostrador, dependiente y suelo de madera en su interior. Se conservan compras, ventas y mejoras.
- Árboles, rocas, pozo, cofre de envíos y señalización con sprites originales.
- Ciervos: reposo y caminar en tres direcciones más reflejo lateral; pequeños recorridos con pausas y comprobación de obstáculos. Conservan daño, recolección y reaparición.
- Tres gallinas ambientales animadas recorren la zona inicial; no conceden recursos.
- Limos: gráfico y animación conectados al movimiento existente.
- Cultivos: tierra, tierra húmeda y etapas de zanahoria según el estado de la parcela. No se sustituyen los estados guardados de los cultivos.

El jugador mantiene `(-0.47, 0.83, 0)` y la cámara `(-0.47, 0.83, -10)`. Se actualizan también los prefabs de recursos y enemigos para conservar el aspecto al reaparecer.

## Verificación

`result.txt` contiene la comprobación real en modo Jugar: tiles, ubicación inicial, fotogramas y desplazamiento de animales, gráficos de parcelas regadas/maduras e interior de tienda. Las visitas de prueba y los cambios temporales de parcelas se excluyen del guardado.

Capturas: `gameplay.png`, `animals-moving.png`, `shop-exterior.png`, `shop-interior.png` y `shop-room.png`. La última oculta los menús solamente durante la captura para permitir inspeccionar la habitación.

Para volver a aplicar los gráficos: menú **Survivor Farm > Apply Original World Graphics**. El ensamblador de Main también aplica este paso al reconstruir. La copia `Main-before-world-art.unity.backup` conserva la escena anterior a esta intervención.
