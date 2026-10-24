# Escena 1 — GameplayPath

Implementación en StoryTell, rama GameplayPath, escena `Assets/Scenes/Level1.unity`.

## Recorrido

1. Hablar con Rubén junto al punto de llegada.
2. Recoger casco, FAL, tres cargadores, mapa y radio en el sector EQUIPO. Volver con Rubén.
3. Ir a la cocina y comer con Gómez.
4. Hablar con Rubén, recuperar los cuatro fragmentos cerca de la cocina y devolver la carta. Leerla no es obligatorio.
5. Hablar con el sargento Acosta en la carpa principal. Recoger herramientas y mantas.
6. Seguir por Narration zone 5 hacia el minicampamento nuevo, sobre el terreno cerca de X=114, Z=361. Entregar la caja al oficial.
7. Continuar por los carteles hacia la zona de explosiones existente, cerca de X=50, Z=530. Atravesarla después de entregar la caja; la entrada de combate queda más adelante, cerca de X=56, Z=543, con las coberturas hacia Z=563.
8. Enfrentarse a 45 siluetas, con seis activas como máximo (ajustable entre cinco y ocho). Los recorridos pasan entre y detrás de coberturas. Contacto enemigo: fundido breve a negro. Eliminar las 45: fundido progresivo. Ambos muestran «Finalización escena 1».

Las coordenadas anteriores son referencias del plano horizontal; las alturas se ajustan al terreno. El punto negativo indicado inicialmente fue reemplazado por la ubicación después de Narration zone 5, con autorización del usuario.

## Controles

- E: interacción contextual; también cierra una lectura abierta.
- M: mapa físico y lista de tareas, después de recoger el mapa.
- L con el mapa abierto: leer la carta de la madre después de recuperar sus cuatro fragmentos.
- Clic izquierdo: disparar durante el combate. Clic derecho: apuntar. R: recargar o destrabar.

La caja se oculta temporalmente al consultar el mapa; permanece registrada como transportada. Se entrega exclusivamente al oficial. Las lecturas y conversaciones bloquean temporalmente movimiento, mirada y disparo para evitar acciones superpuestas. No se añade una barra de hambre.

## Autoría

El contenido nuevo vive bajo `====GAMEPLAY====/GameplayPath_Level1`. Los objetos del prototipo anterior se conservan, con sus interacciones o disparadores contradictorios desactivados. La escena abierta se respaldó con sus cambios sin guardar antes de aplicar el recorrido, en `Library/GameplayPathBackups`.

Los diálogos, textos de la carta y parámetros de combate están en `Assets/Systems/GameplayPath/Data`. Cada conversación y cada línea es un asset editable. Los nombres de personajes son provisionales y ficticios; se añadió un nombre visible opcional a las líneas sin alterar los valores del enum de interlocutores existente.

La escena contiene un NavMesh local, tres rutas editables y ocho siluetas reutilizables. Los enemigos aún no disparan: el usuario definió su aproximación por rutas y el contacto como condición de cierre. No hay puntuación ni contador de bajas visible. Las cajas de reserva tienen doce cargadores cada una y admiten recogidas parciales.

El FAL conserva el Input System, recarga, encasquillamiento, retroceso, sacudidas y efectos existentes. El fin por disparos totales del prototipo queda deshabilitado cuando el recorrido controla el arma. La oscuridad por ráfagas es periférica y recuperable; el fundido final usa una capa de pantalla completa.

## Cartas y fuentes

La carta de la madre y las dos cartas opcionales son textos ficticios provisionales, identificados como tales. Los compañeros del usuario pueden reemplazarlos sin cambiar las condiciones de misión.

Una nota contextual indica que la colección temática Malvinas de la Fuerza Aérea Argentina conserva correspondencia y diarios de guerra. Fuente primaria: [Ministerio de Defensa — Colección temática Malvinas, Fuerza Aérea Argentina](https://www.argentina.gob.ar/defensa/archivos-abiertos/malvinas-40-anos/coleccion-tematica-malvinas-fuerza-aerea-argentina). No se reproducen testimonios reales ni se atribuyen situaciones ficticias a una unidad histórica específica.

## Verificación y límites

Las pruebas de GameplayPath cubren orden de misiones, equipo completo, fragmentos anticipados, duplicados, lectura opcional, entrega previa al ataque, referencias de escena, navegación de rutas, restricción del arma, enemigos móviles y ambas condiciones de final. Se ejecutan junto a las pruebas unitarias del arma. Consultar el informe final en `Library/GameplayPath-tests.json` y el detalle en `Library/GameplayPath-tests.xml`.

Los recursos disponibles se reutilizan. El casco y las siluetas son modelos provisionales sencillos; las voces no están grabadas. El ritmo de 15–20 minutos es el objetivo de diseño, no una duración cronometrada y validada. La dificultad, las rutas y la intensidad audiovisual requieren una prueba humana del recorrido completo. No se implementa guardado entre sesiones en esta entrega.

La zona original de bombas se habilita después de la entrega. Rubén cambia de ubicación fuera de la vista cercana del jugador y se comunica por radio durante el combate y el cierre.

Resultado de la verificación final: 24 pruebas aprobadas, 0 fallidas, 0 omitidas, en Unity 6000.4.6f1. Incluye GameplayPath, referencias e Input System del mapa, arma y presentación visual. El defecto de ubicación del mapa físico se corrigió sincronizando su cuerpo físico al abrirlo. El hitscan excluye solo al portador del arma, no a todos los objetos del grupo de escena compartido.

El rifle en primera persona se muestra únicamente dentro de la zona de combate, con el ataque activo. Se guarda al salir, abrir el mapa o sacar la radio. Mapa y radio se pueden usar durante el recorrido después de recogerlos; la radio también oculta y bloquea temporalmente el disparo para evitar superposiciones.
