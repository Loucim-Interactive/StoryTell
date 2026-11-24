# Personal y objetos de comunicaciones

Se incorporan dos personajes ficticios al puesto `Comunicaciones del campamento` de Level1:
- Luis: operador de radio. Posición X64/Z288, orientado hacia la mesa principal.
- Ernesto: revisa cables y baterías. Posición X60.5/Z286.5.

Cada uno tiene una conversación de tres líneas y cinco comentarios ambientales propios. Se registran sus nombres para N después de conocerlos y su estado en los checkpoints. Se reutiliza el soldado del proyecto, con referencias de interacción, animación y apoyo al suelo vinculadas a cada nuevo actor. No se duplican las pistas de Simón ni se agregan encargos. Los nombres se añaden al final del enum de interlocutores, manteniendo intactos sus valores anteriores.

Se agregan una libreta de mensajes, auriculares, bobina y tramos de cable provisionales; los detalles pequeños no bloquean el paso. La bobina tiene colisión. Una segunda mesa contiene un receptor auxiliar que reutiliza el modelo `Defensive_props/models/radio.fbx`; sus materiales de esta instancia se adaptan a URP y también tiene colisión. Los modelos originales no se modifican. Todos los elementos nuevos se agrupan bajo el puesto de comunicaciones existente.

## Verificación

Unity 6000.4.6f1 / Windows / URP. Tres casos distintos aprobados, tomando la última ejecución de cada uno: registro de soldados/campamento; diálogos independientes, nombres, guardado y objetos; y apoyo/estabilidad de las botas de los 17 NPCs. Los diálogos nuevos no inician las misiones de equipo ni de carta.

Resultados: Library/CommunicationsCrew-results.xml (3/3) y Library/CommunicationsCrew-final-results.xml (2/2 tras separar a Luis de la esquina de la mesa). Captura revisada: Library/CommunicationsCrew-player.png. Partida y fuentes temporales restauradas al terminar. Respaldo inicial: Library/CommunicationsCrew-backup. No se regeneró el ZIP de prueba.

Los objetos indicados como provisionales se pueden reemplazar por modelos del equipo de diseño. Los diálogos son ficción y no se presentan como testimonios históricos ni incluyen doblaje nuevo.
