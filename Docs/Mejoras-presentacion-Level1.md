Mejoras de presentación de Level1

La prueba sigue usando el FN FAL y los blancos cilíndricos existentes. No requiere paquetes nuevos.

Interacción y pantalla

- Mirar el FAL o la munición y pulsar E para recoger. Espacio conserva el salto. El resto de los objetos mantiene sus controles anteriores.
- La recogida utiliza `Weapons/Interact` del Input System, en `Level1WeaponInput.inputactions`.
- Munición compacta en la esquina inferior derecha. Los controles aparecen unos segundos después de equipar; los avisos de recarga, atasco y recogida aparecen cuando hacen falta.
- La munición que no entra en el inventario permanece en el suelo.
- La interfaz usa Scale With Screen Size y no bloquea clics.

Disparo y cierre

- Las ráfagas oscurecen los bordes gradualmente, conservando más visión en el centro. Una pausa permite recuperar la visión.
- El avance de escena se cuenta por separado: 20 disparos efectivos completan la prueba aunque se hagan despacio. Los disparos vacíos o encasquillados no cuentan.
- Al llegar al final, el arma baja y la imagen se funde a negro durante 1,3 segundos. El movimiento y la mirada siguen disponibles hasta llegar al negro.
- El ambiente baja de volumen con el fundido. Después hay 0,45 segundos de pausa y el texto «Finalización escena 1» aparece durante 0,75 segundos.
- No carga escena 2. Para repetir, salir y volver a entrar en Play.

Arma, efectos y audio

- Movimientos procedurales de equipar, bajar el arma, recargar y destrabar. No incluyen manos ni sustituyen unas animaciones de personaje definitivas.
- Retroceso limitado, sacudida más suave y movimiento del arma al caminar o mirar. En `PlayerFAL > WeaponRecoil` se pueden reducir por separado Shake Intensity, Recoil Intensity y Sway Intensity; cero desactiva el efecto correspondiente.
- Fogonazo breve y más pequeño. Cuatro emisores reutilizables producen polvo o chispas en los impactos; no se crea un emisor nuevo por disparo.
- Pequeñas variaciones de tono del disparo, clic mecánico para acciones del arma y respiración existente según la tensión. Son sonidos provisionales del proyecto; no son grabaciones definitivas de un FAL.
- El filtro del arma y los volúmenes afectados por el final se restauran si se desactiva su sistema.

Ajustes y recursos pendientes

- `WeaponTrial_Level1 > Level1WeaponTrial`: cantidad de disparos para terminar, fundido, pausa, aparición del texto e intensidad periférica.
- `WeaponTrial_Level1/WeaponUseZone_MoveAndResize`: ubicación y tamaño de la zona de prueba.
- `WD_Level1_FAL`: cargador y tiempos del arma.
- `StoryTell/Weapons/Polish Level1 Presentation`: vuelve a conectar la presentación en la escena mediante las APIs del editor. Se conserva un respaldo bajo `Library/WeaponPolishBackups` antes de guardar.
- Para mejorar las manos hace falta un modelo de brazos/manos con esqueleto y animaciones compatibles de equipar, disparar, recargar y destrabar.
- Para sustituir el audio provisional hacen falta grabaciones o un paquete con permiso de uso: disparos del FAL, manipulación de cargador, gatillo vacío, encasquillamiento, impactos y respiración.

La prueba automatizada simula un contexto de entrada sin bloqueo físico del puntero cuando Unity se ejecuta en modo batch. La escena guardada exige el bloqueo normal del cursor.
