# Entrega: agua, Tiny RPG y Combat FX

Fecha: 15 de septiembre de 2026.

## Resultado

- Agua animada únicamente en la orilla superior, con todos sus tramos
  sincronizados. Césped estático abajo y puente transitable.
- Soldado, Orco, Demonio y Monstruo de sangre integrados en campaña y en el
  bestiario de la arena; 20 hojas originales, 115 fotogramas sin huecos.
- Cortes, carga, descarga e impactos de Combat FX; sin los antiguos efectos
  procedurales activos. Indicadores de radio de daño conservados.

## Pruebas

- Compilación separada de código de juego, editor y pruebas: PASS.
- `final-all-tests.xml`: 380/381 correctas. El único fallo era la expectativa
  anterior de ampliar todos los demoledores a 1,35; los nuevos personajes usan
  su escala original. Se actualizó únicamente esa expectativa de prueba.
- `final-campaign-tests.xml`: 3/3 correctas después de la corrección, incluido el
  recorrido completo de tres noches y el jefe de dos fases. Ningún caso queda
  fallido pendiente entre ambas ejecuciones; 381 casos distintos cubiertos.
- La suite cubre específicamente sincronización de agua en Main y Taller,
  estados y orientación de enemigos, cambio de tipo después de morir, impactos
  reales sobre enemigos reutilizados, altura de barras, creación de FX desde
  Update, fin de efectos con pausa y un solo impacto por golpe a defensas.
- Build Windows: Succeeded, cero errores, 98.599.441 bytes.
- Ejecutable con `--qa --asset-pack-smoke`: PASS. Nueve capturas en Screenshots;
  orillas, cuatro enemigos y golpes normales/cargados reales.
- Ejecutable con `--qa --practice-smoke`: PASS. Menús de prácticas, bestiario,
  encuentro combinado, Custodio, cultivo, construcción y vuelta al inicio.
- Revisión visual de las nueve capturas y de `Practice/03-controles-arena.png`:
  gráficos presentes, agua superior uniforme, parte inferior estática, menú con
  los once enemigos visible y sin textos superpuestos.

## Archivos entregados

- `Builds/Portfolio/SurvivalFarm.exe` y sus dependencias.
- `Builds/SurvivalFarm-TinyRPG-CombatFX-Windows.zip`, con créditos y licencia de FX.
- Versión anterior conservada en `Builds/Portfolio-AntesTinyRPG-20260915`.
- El código fuente y el ensamblado entregado coinciden con la copia probada;
  `package-verification.json` registra tamaño, entradas y hash del ensamblado.
  El ZIP excluye capturas, registros QA y símbolos de depuración Burst.

## Alcance de la verificación

Pruebas y capturas automatizadas en una copia aislada de Unity y en Windows.
No equivalen a un playtest humano de dificultad. El control de la ventana del
editor no estuvo disponible; la revisión visual se hizo sobre capturas nativas
del ejecutable. No se cambiaron guardados de campaña. Al cerrar, Unity emite el
aviso preexistente sobre liberación de ComputeBuffer; los recorridos finalizaron
sin excepciones de juego.
