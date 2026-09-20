# Area A: persistencia

Estado: primera entrega implementada. Unity no se ha ejecutado y las escenas no se han modificado por A.

## Contrato para coordinacion y C

- Nuevo proveedor implementado: `SurvivorFarm.Runtime.Core.StableSaveId`.
- `StableSaveId.Resolve(Component owner, string legacyId)` devuelve el ID serializado si existe; de lo contrario conserva exactamente la clave anterior.
- `StableSaveId.Matches(Component owner, string savedId, string legacyId)` acepta ID actual o alias serializados anteriores.
- Sobrecarga implementada `public static bool Matches(Component owner, string savedId)`: consulta exclusivamente GUID/alias serializados. `GameSaveSystem` usa `p.PersistentId == saved.id || StableSaveId.Matches(p, saved.id)`. No necesita `LegacyPersistentId` ni pasa el GUID como clave heredada.
- Integracion requerida en `FarmingPlot.cs` (propiedad C): conservar el calculo/cache actual de `persistentId` y devolver `StableSaveId.Resolve(this, persistentId)` desde `PersistentId`. No cambiar `LegacySortKey`.
- La busqueda por alias ya esta incorporada en `GameSaveSystem` y en `ResourceSpawnPoint`.
- La herramienta editor es explicita: registra la clave anterior y el indice posicional de parcelas antes de asignar GUID, rechaza duplicados/ambiguedades, multiples escenas, escenas sin guardar, Play y Prefab Mode. Registra Undo y deja el cambio pendiente de revision; no guarda la escena automaticamente. No se ha ejecutado en esta ronda. No reordenar escenas antes de capturar los alias; no existe una migracion fiable para mapas antiguos ya reorganizados sin su correspondencia original.

## Comportamiento entregado

- Se conserva JSON version 20, sin contenedor nuevo, y las migraciones existentes: recursos posicionales v5, reloj v7, mascota v10, tutorial v11/v13, fogata v17, puente v18, cama v19 y catalogo v20. Las claves y rutas de slots y `--qa` permanecen iguales.
- `SaveFileStore`: escribe `.tmp`, fuerza `Flush(true)` y usa `File.Replace` para conservar el principal previo en `.bak`. Primera escritura mediante `File.Move` en el mismo directorio. Si el sistema no soporta el reemplazo o hay errores de permisos/bloqueo, informa fallo sin recurrir a borrar el principal.
- Carga en orden principal, respaldo, temporal. La lectura no escribe ni elimina archivos. Tras recuperar, protege primero la copia recuperada; el principal corrupto queda en `.rejected-<guid>` en el siguiente guardado. Tambien conserva bytes UTF8 invalidos. Un fallo total bloquea todas las escrituras de esa instancia/ranura, incluso manuales; borrar los archivos durante la sesion no elimina el bloqueo. Solo una recarga valida o una ranura distinta permite continuar guardando.
- Valida estructura/tipos, cierre del documento, campos duplicados, numeros finitos y limites, secciones actuales obligatorias, listas con entradas nulas, identidades duplicadas y valores de dominio invalidos antes de restaurar. Versiones 5..19 conservan valores iniciales y normalizan campos opcionales ausentes. Limites defensivos: 16 MiB por archivo y 64 niveles JSON.
- Protege contra guardados durante la restauracion y callbacks de guardado anteriores a la primera carga. Expone `IsSavingBlocked`, `LastSaveError`, `LastLoadSource` y `public static bool ValidateSaveJson(string json, out string error)`.
- Los estados de parcelas/recursos sin correspondencia se conservan para guardados posteriores. Los alias permiten mover/reordenar tras una asignacion revisada. Las parcelas sin proveedor mantienen exactamente su ID anterior; la integracion de `PersistentId` sigue a cargo del coordinador despues de C.

## Archivos de A

- Modificados: `Assets/SurvivorFarm/Scripts/Runtime/Core/GameSaveSystem.cs`, `Assets/SurvivorFarm/Scripts/Runtime/Gameplay/ResourceSpawnPoint.cs`.
- Nuevos: `Runtime/Core/SaveFileStore.cs`, `Runtime/Core/SaveJsonValidation.cs`, `Runtime/Core/StableSaveId.cs`, `Scripts/Editor/StableSaveIdAssignment.cs` (bajo `Assets/SurvivorFarm/Scripts` segun corresponda). Sus `.meta` ya estan presentes y se han conservado.
- Pruebas: `Assets/SurvivorFarm/Tests/PlayMode/PersistenceSaveTests.cs`, `Tools/PersistenceFileChecks.cs`, `Tools/PersistenceFileChecks.ps1`.
- Parte: `Design/Development/Equipo-A.md`.

## Verificacion realizada

Ejecutado `./Tools/PersistenceFileChecks.ps1` en PowerShell: once grupos de archivo aprobados. Incluyen ranura ausente, reemplazos repetidos, respaldo anterior, corrupcion/recuperacion, conservacion de evidencias, bloqueo total y reintento, temporal interrumpido, prioridad del principal, bloqueo IO, fallo de temporal, UTF8 invalido, archivo sobredimensionado y aislamiento de slots. El validador estructural rechazo 17 entradas invalidas y todos los truncados de un JSON de referencia; acepto fixtures estructurales 5..20. Tambien se reviso sintaxis C# 9 con Roslyn de los siete archivos Unity propios, incluyendo codigo de editor. El script compila y ejecuta solo el almacenamiento/validador independientes; no carga Unity.

Carpeta de una ejecucion verificada conservada: `C:/Users/JUANJO~1/AppData/Local/Temp/SurvivorFarm-Persistence-56c5347c1e2b4b729ad65f7db954afe0`. No se accedio a guardados personales ni se escribio en QA compartido.

`git diff --check` sobre los archivos modificados propios: sin errores de espacios (Git avisa de normalizacion futura LF/CRLF).

## Pendiente de coordinacion

- Compilar runtime/editor y ejecutar `SurvivorFarm.Tests.PersistenceSaveTests`: versiones 5..20 con `JsonUtility` real, versiones no admitidas, valores invalidos, recuperacion, bloqueo y alias serializados tras renombrar/reordenar. Estos NUnit estan escritos pero NO ejecutados por A.
- Ejecutar las verificaciones de guardado ya existentes y QA integrado. Las pruebas fuera de Unity no prueban restauracion de escenas, IL2CPP ni garantias de durabilidad del hardware ante corte de corriente.
- Conectar `FarmingPlot.PersistentId` como se especifica arriba, sin introducir `LegacyPersistentId`. No ejecutar la herramienta de asignacion sobre la escena original en esta ronda.
