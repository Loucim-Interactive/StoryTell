# Cráteres junto a las carpas rotas del camino

2026-11-23 · StoryTell · rama `MainExperiencePolish`.

Se agregaron cuatro impactos en el tramo previo al frente, próximos a los cuatro refugios dañados. Se conservan los cinco cráteres de combate: nueve impactos en total.

| Referencia | Centro X/Z | Radio | Profundidad |
| --- | --- | --- | --- |
| Primera carpa rota | 52 / 405 | 3,8 m | 0,8 m |
| Refugio adicional del tramo medio | 94 / 449 | 3,8 m | 0,8 m |
| Segunda carpa rota | 34 / 465 | 3,8 m | 0,8 m |
| Refugio anterior al puesto | 40 / 490 | 3,5 m | 0,75 m |

Los impactos tienen depresión real del terreno, borde irregular, tierra húmeda y menor cobertura de pasto. Se utiliza el mismo material del frente. La colocación se validó contra los límites visibles de las carpas antes de aplicar los cambios, para conservar el apoyo de las lonas y sus colisiones.

El relieve parte de una copia del terreno con los cinco impactos anteriores. La nueva copia se encuentra en `Assets/Systems/MainIntegration/Data/RoadsideCraters/RoadsideTerrain.asset`; el terreno de la revisión anterior permanece conservado. Terrain y TerrainCollider utilizan el mismo asset.

La cartografía se actualizó con las nuevas depresiones y mantiene sus ocho destinos circulares. Los nuevos impactos terminan antes del área de navegación del combate, que comienza en Z=500; la navegación del frente se conserva y se comprueba su conectividad.

## Comprobaciones

Resultados: `Library/RoadsideCraters-results.xml`: **5/5 pruebas correctas**. Se comprueban profundidad y colisión; alturas debajo de las cuatro carpas y en puntos del paso principal; conservación de los cráteres del frente; rutas de enemigos y apertura del mapa. Se revisan capturas a altura del jugador en `Library/RoadsideCraters-*.png`.

No se hizo una partida manual completa, un nuevo build, commit ni push.

Respaldo de escena, autoría, fuentes y checkpoints: `Library/RoadsideCraters-backup/`. Los guardados y las fuentes anteriores se restauran al cerrar las pruebas.
