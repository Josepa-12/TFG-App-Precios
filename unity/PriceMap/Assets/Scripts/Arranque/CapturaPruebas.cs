using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.UI;
using PriceMap.Vectorial;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Arranque
{
    /// <summary>
    /// Solo para comprobar la interfaz sin móvil: al lanzar el ejecutable con
    /// "-captura &lt;carpeta&gt;", espera a que cargue el mapa, guarda una captura en cada tema y sale.
    /// Sin ese argumento no hace nada.
    /// </summary>
    public class CapturaPruebas : MonoBehaviour
    {
        string carpeta;
        ServicioTema tema;
        ServicioFiltro filtro;
        IPreferencias preferencias;
        MapaVectorial mapa;
        PantallaMapa pantalla;

        public static void SiSePide(GameObject anfitrion, ServicioTema tema, ServicioFiltro filtro, IPreferencias preferencias, MapaVectorial mapa, PantallaMapa pantalla)
        {
#if !(UNITY_STANDALONE || UNITY_EDITOR || DEVELOPMENT_BUILD)
            return; // nunca en la APK de verdad
#else
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-captura");
            if (i < 0 || i + 1 >= args.Length) return;
            var c = anfitrion.AddComponent<CapturaPruebas>();
            c.carpeta = args[i + 1];
            c.tema = tema;
            c.filtro = filtro;
            c.preferencias = preferencias;
            c.mapa = mapa;
            c.pantalla = pantalla;
#endif
        }

        IEnumerator Start()
        {
            Directory.CreateDirectory(carpeta);
            var temaGuardado = preferencias.Leer(ServicioTema.ClavePreferencia);
            yield return new WaitForSeconds(8f); // teselas descargadas y construidas
            Diagnostico();
            var pildora = pantalla != null ? pantalla.Pildora : null;
            Debug.Log($"PriceMap: filtro al arrancar {filtro.Actual}; píldora \"{pildora?.Texto.text ?? "-"}\"; " +
                      $"marcadores visibles {Visibles()}");
            if (filtro.Actual == FiltroPrecios.Ninguno && Visibles() > 0)
                Debug.LogError("PriceMap: con el filtro Ninguno no debería verse ningún marcador.");
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            yield return new WaitForSeconds(1.5f);
            yield return Capturar(tema.Actual.ToString().ToLowerInvariant());
            tema.Alternar();
            // Deja la preferencia como estaba: si no había ninguna, la app debe seguir al sistema
            if (temaGuardado == null) preferencias.Borrar(ServicioTema.ClavePreferencia);
            else preferencias.Guardar(ServicioTema.ClavePreferencia, temaGuardado);
            yield return new WaitForSeconds(1f); // CaptureScreenshot escribe el fichero al acabar el fotograma

            yield return Marcadores();
            yield return FiltroAbierto();

            // Recorrido: unos 5,5 km al norte y vuelta; las teselas lejanas se liberan (nunca más de 7×7)
            Debug.Log($"PriceMap: teselas cargadas al inicio: {mapa.TeselasCargadas} (cámara a {FindFirstObjectByType<CamaraMapa>().distancia:F0} m, inclinación {FindFirstObjectByType<CamaraMapa>().inclinacion:F0}°)");
            mapa.Centrar(new Coordenada(36.8279, -6.3528));
            yield return new WaitForSeconds(6f);
            Debug.Log($"PriceMap: teselas cargadas a 5,5 km: {mapa.TeselasCargadas}");
            if (mapa.TeselasCargadas > 49) Debug.LogError($"PriceMap: demasiadas teselas cargadas ({mapa.TeselasCargadas} > 7×7)");
            yield return Capturar("lejos");
            mapa.Centrar(new Coordenada(mapa.latitud, mapa.longitud));
            yield return new WaitForSeconds(4f);
            Debug.Log($"PriceMap: teselas cargadas al volver: {mapa.TeselasCargadas}");
            yield return new WaitForSeconds(1f);
            Debug.Log("PriceMap: capturas guardadas en " + carpeta);
            Application.Quit();
        }

        /// <summary>
        /// Captura "marcadores" centrada en Clínica Areas y registra el mayor desvío entre la punta de cada
        /// marcador visible (su worldBound tras la disposición) y la proyección de su coordenada.
        /// </summary>
        IEnumerator Marcadores()
        {
            var capa = pantalla != null ? pantalla.Capa : null;
            if (capa == null) { Debug.LogError("PriceMap: no hay capa de marcadores que capturar."); yield break; }
            // Con Ninguno (por defecto) no hay marcadores: elige Todos para la captura y luego deja la preferencia como estaba
            var filtroGuardado = preferencias.Leer(ServicioFiltro.ClavePreferencia);
            var filtroAntes = filtro.Actual;
            filtro.Elegir(FiltroPrecios.Todos);
            try
            {
                var e = MarcadoresConTodos(capa);
                while (e.MoveNext()) yield return e.Current;
            }
            finally
            {
                // Vuelve al filtro de antes en la app y deja la preferencia tal cual estaba (también "sin clave")
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
                Debug.Log($"PriceMap: preferencia del filtro restaurada ({filtroGuardado ?? "sin guardar"}; antes {filtroAntes}).");
            }
        }

        /// <summary>
        /// Captura "filtro": Hostelería elegida y el menú abierto. Registra el recorrido de TalkBack con el menú
        /// abierto (píldora y las 5 opciones, Hostelería seleccionada) y deja el filtro y la preferencia como estaban.
        /// </summary>
        IEnumerator FiltroAbierto()
        {
            if (pantalla == null || pantalla.Pildora == null) { Debug.LogError("PriceMap: no hay píldora \"Precios\" que capturar."); yield break; }
            var filtroGuardado = preferencias.Leer(ServicioFiltro.ClavePreferencia);
            var filtroAntes = filtro.Actual;
            filtro.Elegir(FiltroPrecios.Hosteleria);
            try
            {
                yield return new WaitForSeconds(1f);
                pantalla.AlternarMenu();
                yield return new WaitForSeconds(0.5f);
                var recorrido = pantalla.Accesibilidad.RecorridoConEstado();
                var texto = new System.Text.StringBuilder();
                foreach (var (etiqueta, seleccionada) in recorrido) texto.Append(" | ").Append(etiqueta).Append(seleccionada ? " (seleccionada)" : "");
                Debug.Log($"PriceMap: recorrido de TalkBack con el menú abierto ({recorrido.Count}):{texto}");

                var esperado = new List<(string, bool)> { (Textos.T(ClavesTexto.PreciosControl), false) };
                foreach (var f in ReglaFiltro.Opciones) esperado.Add((Textos.T(TextosFiltro.ClaveOpcion(f)), f == FiltroPrecios.Hosteleria));
                bool igual = recorrido.Count == esperado.Count;
                for (int i = 0; igual && i < esperado.Count; i++) igual = recorrido[i] == esperado[i];
                if (!igual) Debug.LogError("PriceMap: el recorrido con el menú abierto no es la píldora y las 5 opciones con Hostelería seleccionada.");
                if (!pantalla.MenuAbierto) Debug.LogError("PriceMap: el menú \"Precios\" no está abierto en la captura.");
                var anuncio = PantallaMapa.AnuncioFiltro(FiltroPrecios.Hosteleria);
                Debug.Log($"PriceMap: anuncio al elegir Hostelería: \"{anuncio}\"");
                if (anuncio.Contains(ClavesTexto.PreciosAnuncio)) Debug.LogError("PriceMap: falta el texto del anuncio del filtro en la tabla.");
                yield return Capturar("filtro");
                yield return new WaitForSeconds(1f);

                pantalla.CerrarMenu();
                Debug.Log($"PriceMap: menú cerrado; recorrido de TalkBack con {pantalla.Accesibilidad.Recorrido().Count} entradas (vuelven botones y marcadores).");
            }
            finally
            {
                if (pantalla.MenuAbierto) pantalla.CerrarMenu();
                filtro.Elegir(filtroAntes);
                if (filtroGuardado == null) preferencias.Borrar(ServicioFiltro.ClavePreferencia);
                else preferencias.Guardar(ServicioFiltro.ClavePreferencia, filtroGuardado);
                Debug.Log($"PriceMap: preferencia del filtro restaurada tras la captura del menú ({filtroGuardado ?? "sin guardar"}; antes {filtroAntes}).");
            }
        }

        int Visibles()
        {
            int n = 0;
            var capa = pantalla != null ? pantalla.Capa : null;
            if (capa != null) foreach (var m in capa.Marcadores) if (m.Visible) n++;
            return n;
        }

        IEnumerator MarcadoresConTodos(CapaMarcadores capa)
        {
            MarcadorLugar areas = null;
            foreach (var m in capa.Marcadores) if (m.Lugar.Id == "clinica-areas") areas = m;
            if (areas == null) { Debug.LogError("PriceMap: falta el marcador de Clínica Areas."); yield break; }

            mapa.Centrar(areas.Lugar.Posicion);
            yield return new WaitForSeconds(3f);
            // Al final del fotograma: la disposición ya está hecha y la cámara no se mueve hasta el siguiente Update
            yield return new WaitForEndOfFrame();
            float escala = pantalla.EscalaDp;
            float mayor = 0f; int medidos = 0; string peor = "-";
            foreach (var m in capa.Marcadores)
            {
                if (!m.Visible) continue;
                var b = m.Punta.worldBound;
                if (float.IsNaN(b.x) || escala <= 0f) continue;
                var puntaPx = new Vector2(b.center.x, b.yMax) / escala;
                var p = mapa.Proyectar(m.Lugar.Posicion);
                float desvio = Vector2.Distance(puntaPx, new Vector2(p.X, p.Y));
                medidos++;
                if (desvio > mayor) { mayor = desvio; peor = m.Lugar.Id; }
            }
            Debug.Log($"PriceMap: marcadores visibles {medidos} de {capa.Marcadores.Count}; Clínica Areas con € = {areas.Insignia != null}; " +
                      $"mayor desvío punta-proyección {mayor:F3} px ({peor})");
            if (medidos == 0) Debug.LogError("PriceMap: ningún marcador visible en la captura de marcadores.");
            else if (mayor >= 1f) Debug.LogError($"PriceMap: desvío de los marcadores {mayor:F3} px ≥ 1 px");
            yield return Capturar("marcadores");
            yield return new WaitForSeconds(1f);
        }

        /// <summary>Escribe en el registro dónde está cada elemento de la interfaz, con su texto y su fuente.</summary>
        static void Diagnostico()
        {
            foreach (var doc in FindObjectsByType<UnityEngine.UIElements.UIDocument>(FindObjectsSortMode.None))
            {
                var raiz = doc.rootVisualElement;
                if (raiz == null) continue;
                Debug.Log($"PriceMap: panel {raiz.layout} pantalla {Screen.width}x{Screen.height} dpi {Screen.dpi}");
                raiz.Query<UnityEngine.UIElements.VisualElement>().ForEach(e =>
                {
                    if (e.GetFirstAncestorOfType<MarcadorLugar>() != null) return; // 124 marcadores: solo su raíz
                    var t = e as UnityEngine.UIElements.TextElement;
                    Debug.Log($"PriceMap:   {e.name} [{string.Join(" ", e.GetClasses())}] {e.worldBound} display={e.resolvedStyle.display}" +
                              (t != null ? $" texto=\"{t.text}\" tamaño={t.resolvedStyle.fontSize} fuente={t.resolvedStyle.unityFont?.name ?? "-"} def={t.resolvedStyle.unityFontDefinition.fontAsset?.name ?? "-"} color={t.resolvedStyle.color}" : ""));
                });
            }
        }

        IEnumerator Capturar(string nombre)
        {
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot(Path.Combine(carpeta, nombre + ".png"));
            yield return null;
        }
    }
}
