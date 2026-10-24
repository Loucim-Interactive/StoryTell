Prueba de armas en StoryTell / Level1

- Abrir `Assets/Scenes/Level1.unity` y entrar en Play.
- Junto al punto de inicio hay un FAL independiente de los rifles de inspección y tres cargadores de 20 balas. Mirar cada objeto y pulsar E para recogerlo.
- El rifle empieza con 20 balas cargadas. La munición se conserva si se recoge antes del arma.
- Mantener clic izquierdo dispara automáticamente. Clic derecho apunta; también se puede disparar desde la cadera. R recarga o destraba si se encasquilla. El cargador retirado pierde sus balas restantes, según el sistema original.
- Se puede caminar y mirar mientras se dispara. El mapa y la inspección bloquean el arma mientras se usan.
- Tres cilindros reciben impactos y caen al quedarse sin salud; son blancos estáticos provisionales.
- La ráfaga oscurece los bordes y deja el centro más legible. Tras una pausa se recupera la visión. El avance narrativo se cuenta por separado: 20 disparos efectivos, seguidos o separados, inician un fundido de 1,3 segundos, una pausa de 0,45 segundos y la aparición del texto «Finalización escena 1» durante 0,75 segundos. No carga otra escena.
- El arma funciona dentro del volumen `WeaponTrial_Level1/WeaponUseZone_MoveAndResize`, inicialmente de 36 × 12 × 36 metros centrado en el inicio. Puede moverse y redimensionarse en el editor.
- Los valores del oscurecimiento están en `WeaponTrial_Level1 > Level1WeaponTrial > Stress`; la cadencia y el cargador en `WD_Level1_FAL`; el retroceso y la sacudida en `PlayerFAL > WeaponRecoil`.
- Las acciones reales del Input System están en `Assets/Systems/WeaponSystem/Data/Level1WeaponInput.inputactions` (`Weapons/Fire`, `Aim`, `Reload`).
- No se añadieron zonas de barro, arena ni agua. El sonido de disparo es provisional, sintetizado localmente; puede reemplazarse en `WeaponController`.
- Para repetir después del final, salir de Play y volver a entrar.

Se preserva la escena anterior en `Library/Level1WeaponBackups/20261021-202431/Level1.unity`. El instalador usa APIs de Unity, conserva las instancias de modelo y registra la creación como una operación de Undo. No reemplaza los FAL de inspección existentes.

Detalles de presentación, ajustes y recursos pendientes: ver Mejoras-presentacion-Level1.md.
