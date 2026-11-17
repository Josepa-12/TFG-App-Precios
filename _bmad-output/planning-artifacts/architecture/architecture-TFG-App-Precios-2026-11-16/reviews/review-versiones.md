# Revisión de versiones y vigencia

## Veredicto
Lo esencial está comprobado (Unity instalado, Firebase y Mobile Notifications buscados hoy, servicios de mapa y rutas probados en el prototipo). Quedan dos versiones con fuentes contradictorias y una capacidad que no es automática.

## Hallazgos
- **[medium] Firebase Unity SDK 14.1.0.** Las fuentes encontradas no coinciden (14.1.0, 14.0.0, 13.17.0). *Fix:* fijar la versión exacta al instalarla desde el registro oficial y anotarla en el Stack.
- **[medium] Accesibilidad de UI Toolkit.** El módulo de accesibilidad de Unity 6 (TalkBack) no se rellena solo desde UI Toolkit: cada pantalla debe construir su árbol de accesibilidad. *Fix:* la convención lo dice expresamente (`AccessibilityHierarchy` por pantalla).
- **[low] Mobile Notifications 2.3.2 frente a 2.4.** Hay documentación de la 2.4. *Fix:* usar la última compatible con 6.3 que ofrezca el gestor de paquetes y anotarla.
- **[low] Textos localizados.** La convención habla de "tabla de localización" sin decir con qué. *Fix:* fijar el paquete Localization de Unity.
- **[low] Unity Test Framework y Android targetSdk** sin comprobar. *Fix:* los da el editor 6.3; anotarlos al configurar.
- **Comprobado contra la realidad:** Unity 6000.3.28f1 (instalado), OpenFreeMap (probado hoy en la escena vectorial), Valhalla, OSRM y Photon (usados por el prototipo), plan Spark (búsqueda de hoy).
