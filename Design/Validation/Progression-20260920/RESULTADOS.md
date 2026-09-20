# Validación — progresión y pueblo

- Unity 6000.3.9f1. Proyecto QA aislado: `QA/p`; no se modifican los guardados personales.
- 242 pruebas aprobadas en 23 clases. Se toma la ejecución completa más reciente de cada clase, sin sumar dos veces pruebas repetidas.
- `playmode.xml`: 150/162; las clases con fallos iniciales quedan sustituidas por las ejecuciones posteriores.
- `playmode-final.xml`: 140/141; la prueba antigua del HUD asumía arco gratuito, sustituida por el pase siguiente.
- `playmode-lifecycle.xml`: 13/13, incluyendo arco bloqueado inicialmente y cambio correcto de HUD al desbloquearlo.
- `playmode-practice.xml`: 11/11, incluyendo munición de práctica y aislamiento del progreso de campaña.
- Compilación Runtime, Editor y PlayModeTests correcta.
- Comprobación Windows de pueblo: PASS. Incluye montura, animales, maestrías, mejoras, guardia, botín físico, miniboss, guardado y liberación del pueblo.
- Capturas revisadas: tecla E sin panel oscuro, transparencia de vegetación, jinete original, reacción de vaca, árbol de habilidades, pozo, casa, miniboss y cofre. Tras la revisión se ajustó el tamaño de la espada a .45 unidades de mundo y se añadió el icono del pozo.
- `pruebas-consolidadas.csv` enumera las 23 clases y la ejecución vigente de cada una. Las 20 capturas del pueblo están en `Capturas/Pueblo` y su resultado en `pueblo-native-result.txt`.
- Compilación Windows final tras corregir la estela: `Succeeded | errors=0 | bytes=98822494 | duration=00:00:48.4180039`. Registro: `build-crisp-fx.log`.
- La revisión visual adicional detectó un halo azul ya dibujado en el atlas. Se sustituyó por una estela opaca y un aro de área dorado, conservando la animación de carga.
- Una primera comprobación del terreno buscaba el nombre antiguo de la orilla; se actualizó el selector QA para comprobar la orilla de pasto actual. El recorrido corregido aprobó río, puente, cuatro enemigos importados y golpes reales de espada.
- Comprobación Windows final de combate: PASS (`combate-native-result.txt`, `native-crisp-fx.log`). Las nueve capturas están en `Capturas/Combate`; se revisó el corte cargado final, ya sin halo difuminado.
- Copia entregada en `Builds/Portfolio`. El ejecutable y `SurvivorFarm.Runtime.dll` coinciden por SHA-256 con el build QA; hashes en `release-hashes.json`. La carpeta conserva sus licencias y añade las instrucciones de progresión en `LEEME.txt`.

Las capturas se preparan mediante escenarios QA automatizados. No son una partida completa jugada manualmente ni una evaluación prolongada de equilibrio.
