# Build de prueba del 23 de noviembre de 2026

Entrega final: `Builds/StoryTell-Prueba-2026-11-23-r3.zip`.

Ejecutable: `Builds/StoryTell-Prueba-2026-11-23-r3/StoryTell.exe`.

- Windows 64 bits, Unity 6000.4.6f1, Mono, BuildOptions.None.
- Escenas: MainMenu → GameSelect → Level1.
- Rama MainExperiencePolish, commit base fd5f281 más los cambios locales actuales. No se hizo commit ni push.
- Incluye los nueve cráteres (cinco en combate y cuatro junto a las carpas rotas), la cartografía limpia y las revisiones previas del recorrido e interfaz.
- Compilación final: Succeeded, 0 errores. Dos advertencias de shaders existentes sobre el nombre obsoleto `_FORWARD_PLUS`; no impidieron compilar.
- Resultado: `Library/TeamPlaytest-2026-11-23-build-summary.txt`. Registro: `Library/TeamPlaytest-2026-11-23-r3-build.log`.

Se abrió el ejecutable de la entrega final y se comprobó visualmente el menú principal, la selección de Parte 1 y el inicio en el campamento con el objetivo «Hablá con Diego». No aparecieron excepciones en el registro durante esas comprobaciones. No se jugó toda la escena en el ejecutable ni se midió rendimiento en otras computadoras.

La prueba final de persistencia del punto y los controles pasó 1/1: `Library/TeamPlaytest-2026-11-23-reticle-results.xml`.

La primera comprobación visual detectó que el punto central todavía aparecía: la revisión anterior había ocultado decoraciones `Cross` de las respuestas y el punto real era el `Image Dot` del canvas de interacción. Se restauraron las decoraciones y se ocultó el punto correcto, guardando su modificación en la instancia de prefab. Se regeneró el ejecutable y se verificó visualmente que el punto ya no aparece. Las compilaciones intermedias se conservan para diagnóstico y no son el ZIP de entrega. También se corrigió la interacción de una radio que aún utilizaba clic, para mantener E en los objetos inspeccionables.

El ZIP incluye el ejecutable, sus dependencias y una guía de controles. Excluye la carpeta de información de depuración Burst indicada por Unity como DoNotShip. Se verifica por SHA-256 cada archivo comprimido contra su original; el resumen y el hash del ZIP están en `Library/TeamPlaytest-2026-11-23-package.json`.

Los checkpoints y las fuentes previos a las pruebas se conservan en `Library/TeamPlaytest-2026-11-23-backup/` y se restauran tras la comprobación. No se incluyen guardados de la cuenta local en el ZIP.
