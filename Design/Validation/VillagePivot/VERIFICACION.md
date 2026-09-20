# Verificación del cambio de pueblo y exploración

Fecha: 19 de septiembre de 2026. Unity 6000.3.9f1, Windows 64 bits.

## Pruebas automatizadas

- Compilación de runtime, editor y PlayMode: PASS. Registros en `Compile/`.
- Primera batería completa: 388 casos, 383 correctos. Cinco fixtures de muebles seguían usando posiciones de senderos admitidas para las antiguas murallas.
- Revisión dirigida: 16/16 correctos, incluyendo los cinco casos corregidos, seguridad, recuperación, compatibilidad de aldeanos, rutas de todos los campamentos y portales, río y campaña completa de tres noches con jefe.
- Resultado combinado, tomando la última ejecución de cada caso: **389 casos verificados y cero fallos pendientes**. Informes `all-tests.xml` y `final-tests.xml`.

## Comprobación del ejecutable

El modo optativo `--qa --village-pivot-smoke` usa guardados independientes. Comprueba seguridad y reparación, ausencia de defensas retiradas, tres campamentos, dos mazmorras, cofres que no repiten premios al cargar, pausa del reloj dentro de mazmorras, regreso al exterior e invasión y recuperación del pueblo manteniendo fallecidos.

El resultado nativo está en `Screenshots/result.txt`; las capturas muestran escenarios preparados automáticamente para revisar su presentación. El informe final de compilación se conserva en `build.txt`.

## Alcance

La demo conserva tres noches y el Custodio final. Las mazmorras son dos cámaras breves y los premios son suministros. Estas verificaciones cubren lógica e integración, pero no sustituyen una sesión manual de balance de dificultad y ritmo.

Las partidas normales del usuario no se utilizan en las pruebas. La versión anterior del ejecutable se conserva en `Builds/Portfolio-AntesPueblo-20260919`.
