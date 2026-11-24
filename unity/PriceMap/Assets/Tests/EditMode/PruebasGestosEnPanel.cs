using System.Reflection;
using NUnit.Framework;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Los gestos como en el móvil (24/11/2026): la hoja en un panel del tamaño del Redmi (1080 × 2400 px a 2,75) y los
    /// eventos del dedo repartidos por el panel, sin decirle a quién van. Así cuentan la captura del puntero, el foco, los
    /// botones de dentro y el redondeo a píxel, que las pruebas que llaman a Bajar/Mover/Soltar no ven.
    /// </summary>
    public class PruebasGestosEnPanel
    {
        GameObject objeto;
        RenderTexture pantalla;
        PanelSettings ajustes;
        VisualElement raiz, hueco;
        HojaBuscador hoja;

        [SetUp]
        public void Crear()
        {
            Textos.Forzados = c => PrepararInterfaz.TextosEs[c];
            pantalla = new RenderTexture(1080, 2400, 0);
            ajustes = Object.Instantiate(AssetDatabase.LoadAssetAtPath<PanelSettings>("Assets/UI/PanelSettings.asset"));
            ajustes.targetTexture = pantalla;
            ajustes.scaleMode = PanelScaleMode.ConstantPixelSize;
            ajustes.scale = 2.75f;
            objeto = new GameObject("Panel del móvil");
            objeto.SetActive(false);
            var documento = objeto.AddComponent<UIDocument>();
            documento.panelSettings = ajustes;
            documento.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml");
            objeto.SetActive(true);
            raiz = documento.rootVisualElement;
            Disponer();
            hueco = raiz.Q("hueco-buscador");
            hoja = new HojaBuscador(c => PrepararInterfaz.TextosEs[c]);
            hoja.AddToClassList("buscador--sin-transicion"); // en EditMode el tiempo del panel no corre
            hueco.Add(hoja);
            hoja.Medir(hueco.layout.height);
            Disponer();
        }

        [TearDown]
        public void Limpiar()
        {
            Object.DestroyImmediate(objeto);
            Object.DestroyImmediate(ajustes);
            pantalla.Release();
            Object.DestroyImmediate(pantalla);
            TecladoAndroid.PasarToques(false);
            Textos.Forzados = null;
        }

        void Disponer()
        {
            var panel = raiz.panel;
            panel.GetType().GetMethod("ValidateLayout", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).Invoke(panel, null);
        }

        // Sin destino: lo decide el panel (quien tiene el puntero o lo que hay debajo)
        void Dedo(EventType tipo, Vector2 punto)
        {
            var sistema = new Event { type = tipo, mousePosition = punto, button = 0, clickCount = 1, pointerType = UnityEngine.PointerType.Touch };
            using (EventBase e = tipo == EventType.MouseDown ? PointerDownEvent.GetPooled(sistema)
                : tipo == EventType.MouseDrag ? PointerMoveEvent.GetPooled(sistema)
                : (EventBase)PointerUpEvent.GetPooled(sistema))
                raiz.panel.visualTree.SendEvent(e);
            Disponer();
        }

        void Tocar(Vector2 punto)
        {
            Dedo(EventType.MouseDown, punto);
            Dedo(EventType.MouseUp, punto);
        }

        /// <summary>Arrastra de un punto a otra altura, despacio y parándose al final (sin lanzamiento).</summary>
        void Arrastrar(Vector2 desde, float hastaY)
        {
            Dedo(EventType.MouseDown, desde);
            for (int i = 1; i <= 20; i++) Dedo(EventType.MouseDrag, new Vector2(desde.x, Mathf.Lerp(desde.y, hastaY, i / 20f)));
            System.Threading.Thread.Sleep(120);
            Dedo(EventType.MouseUp, new Vector2(desde.x, hastaY));
        }

        float YDe(float fraccion) => hoja.TopPlegada - fraccion * (hoja.TopPlegada - hoja.TopAbierta);

        Vector2 Campo => hoja.CajaCampo.worldBound.center;
        Vector2 Tirador => new Vector2(hoja.worldBound.center.x, hoja.worldBound.yMin + 10f);

        [Test]
        public void ArrastresSeguidosLaHojaLosSigueTodos()
        {
            var agarre = Campo;
            float dentro = agarre.y - hoja.worldBound.yMin;
            Arrastrar(agarre, YDe(0.5f) + dentro);
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f, "De la cápsula a la mitad.");
            Assert.IsFalse(hoja.Fisica.Pulsado, "Al soltar, el gesto ha terminado.");
            Assert.IsNull(raiz.panel.GetCapturingElement(UnityEngine.UIElements.PointerId.touchPointerIdBase), "Y nadie se queda el dedo.");

            Arrastrar(Tirador, YDe(1f));
            Assert.AreEqual(1f, hoja.Fisica.Fraccion, 0.001f, "Enseguida, de la mitad a completa, sin ningún toque entre medias.");
            Arrastrar(Tirador, YDe(0.5f));
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
            Arrastrar(Tirador, YDe(0f));
            Assert.AreEqual(0f, hoja.Fisica.Fraccion, 0.001f);
            Assert.IsFalse(hoja.Fisica.Pulsado);
        }

        [Test]
        public void LaHojaSigueAlDedoTodoElArrastre()
        {
            var agarre = Campo;
            float dentro = agarre.y - hoja.worldBound.yMin;
            Dedo(EventType.MouseDown, agarre);
            foreach (float k in new[] { 0.1f, 0.3f, 0.6f, 0.9f, 0.4f })
            {
                Dedo(EventType.MouseDrag, new Vector2(agarre.x, YDe(k) + dentro));
                Assert.AreEqual(k, hoja.Fisica.Fraccion, 0.001f);
                Assert.AreEqual(YDe(k), hoja.worldBound.yMin, 1f, "Y se ve donde va el dedo.");
            }
            System.Threading.Thread.Sleep(120); // parado: no es un lanzamiento
            Dedo(EventType.MouseUp, new Vector2(agarre.x, YDe(0.4f) + dentro));
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
        }

        [Test]
        public void ArrastrarDesdeUnChipODesdeLaListaMueveLaHojaYNoElige()
        {
            int elegidas = 0;
            hoja.CategoriaElegida += _ => elegidas++;
            hoja.Poner(0.5f);
            Disponer();
            var chip = raiz.Query("buscador-chip").First().worldBound.center;
            Arrastrar(chip, chip.y - (YDe(0.5f) - YDe(1f)));
            Assert.AreEqual(1f, hoja.Fisica.Fraccion, 0.001f, "Desde un chip.");
            Assert.AreEqual(0, elegidas, "Un arrastre no elige la categoría.");

            var lista = new Vector2(hoja.worldBound.center.x, hoja.Cerca.worldBound.yMax + 40f);
            Arrastrar(lista, lista.y + (YDe(0.5f) - YDe(1f)));
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f, "Desde el hueco de la lista, hacia abajo.");
            Assert.AreEqual(0, elegidas);
        }

        [Test]
        public void TocarUnChipLoEligeYDespuesSeArrastraALaPrimera()
        {
            int elegidas = 0;
            hoja.CategoriaElegida += _ => elegidas++;
            hoja.Poner(0.5f);
            Disponer();
            Tocar(raiz.Query("buscador-chip").First().worldBound.center);
            Assert.AreEqual(1, elegidas);
            Assert.IsFalse(hoja.Fisica.Pulsado, "El toque lo tenía el chip y la hoja se entera de que se suelta.");
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
            Arrastrar(Tirador, YDe(1f));
            Assert.AreEqual(1f, hoja.Fisica.Fraccion, 0.001f);
        }

        [Test]
        public void TocarElCampoAbreYEnfocaAlSoltarYElArrastreNo()
        {
            Dedo(EventType.MouseDown, Campo);
            Assert.IsFalse(Enfocado, "Bajar el dedo aún no saca el teclado.");
            Dedo(EventType.MouseUp, Campo);
            Assert.AreEqual(1f, hoja.Fisica.Fraccion, "Se abre en el mismo toque.");
            Assert.IsTrue(Enfocado);
            Assert.IsTrue(TecladoAndroid.Pasados, "Con el teclado la app sigue dibujando.");

            hoja.Plegar();
            Disponer();
            Assert.IsFalse(Enfocado);
            Assert.IsFalse(TecladoAndroid.Pasados);

            bool seEnfoco = false;
            hoja.Campo.RegisterCallback<FocusInEvent>(_ => seEnfoco = true);
            Arrastrar(Campo, YDe(0.5f));
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
            Assert.IsFalse(seEnfoco, "Un arrastre que empieza en el campo nunca saca el teclado.");
        }

        [Test]
        public void ConElCampoEnfocadoUnArrastreDesdeElLoDesenfocaYLoSigue()
        {
            Tocar(Campo);
            Assert.IsTrue(Enfocado);
            Arrastrar(Campo, Campo.y + (YDe(0.5f) - YDe(1f)));
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
            Assert.IsFalse(Enfocado);
            Arrastrar(Tirador, YDe(0f));
            Assert.AreEqual(0f, hoja.Fisica.Fraccion, 0.001f);
        }

        [Test]
        public void CanceladoPorElSistemaEnPlenoArrastreVaAUnaParadaYNoSeQuedaElGesto()
        {
            var agarre = Campo;
            Dedo(EventType.MouseDown, agarre);
            for (int i = 1; i <= 20; i++) Dedo(EventType.MouseDrag, new Vector2(agarre.x, Mathf.Lerp(agarre.y, YDe(0.6f), i / 20f)));
            Assert.IsTrue(hoja.Arrastrando);
            System.Threading.Thread.Sleep(120);
            var sistema = new Event { type = EventType.MouseUp, mousePosition = new Vector2(agarre.x, YDe(0.6f)), pointerType = UnityEngine.PointerType.Touch };
            using (var e = PointerCancelEvent.GetPooled(sistema)) raiz.panel.visualTree.SendEvent(e);
            Disponer();
            Assert.IsFalse(hoja.Fisica.Pulsado);
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
            Arrastrar(Tirador, YDe(1f));
            Assert.AreEqual(1f, hoja.Fisica.Fraccion, 0.001f);
        }

        [Test]
        public void UnDedoQueNoLlegoASoltarseNoBloqueaElSiguiente()
        {
            Dedo(EventType.MouseDown, Campo); // y Android se lleva el resto del gesto
            Arrastrar(Campo, YDe(0.5f));
            Assert.AreEqual(0.5f, hoja.Fisica.Fraccion, 0.001f);
            Assert.IsFalse(hoja.Fisica.Pulsado);
        }

        [Test]
        public void SiLaFilaTocadaDesapareceALaMitadLaHojaSigueAlDedo()
        {
            hoja.Poner(0.5f);
            Disponer();
            var chip = raiz.Query("buscador-chip").First();
            var p = chip.worldBound.center;
            Dedo(EventType.MouseDown, p);
            chip.RemoveFromHierarchy();
            Disponer();
            for (int i = 1; i <= 20; i++) Dedo(EventType.MouseDrag, new Vector2(p.x, p.y - i * 10f));
            System.Threading.Thread.Sleep(120);
            Dedo(EventType.MouseUp, new Vector2(p.x, p.y - 200f));
            Assert.IsFalse(hoja.Fisica.Pulsado);
            Arrastrar(Tirador, YDe(0f));
            Assert.AreEqual(0f, hoja.Fisica.Fraccion, 0.001f);
        }

        bool Enfocado => raiz.panel.focusController.focusedElement is VisualElement v && (v == hoja.Campo || hoja.Campo.Contains(v));

        /// <summary>Un error «Layout update is struggling» haría fallar la prueba.</summary>
        [TestCase(2.75f)] // el Redmi
        [TestCase(1f)]
        [TestCase(1.5f)]
        [TestCase(3.5f)]
        public void LaDisposicionNoEntraEnBucleConLaHojaEnCualquierAltura(float escala)
        {
            ajustes.scale = escala;
            Disponer();
            hoja.Medir(hueco.layout.height);
            Disponer();
            int pase = 0;
            var propiedad = typeof(GeometryChangedEvent).GetProperty("layoutPass", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(propiedad);
            raiz.Query<VisualElement>().ForEach(e => e.RegisterCallback<GeometryChangedEvent>(g => pase = Mathf.Max(pase, (int)propiedad.GetValue(g))));
            var p = Campo;
            hoja.Bajar(p, 0);
            for (int i = 1; i <= 200; i++)
            {
                hoja.Mover(new Vector2(p.x, p.y - i * 3.7f), i * 16);
                Disponer();
            }
            Assert.LessOrEqual(pase, 3, "Pocas pasadas en cada fotograma.");
        }

        [Test]
        public void LaCapsulaSigueALaLineaSiCreceOEncoge()
        {
            float antes = hoja.AltoCapsula;
            hoja.Linea.style.minHeight = 70f; // la fuente grande
            Disponer();
            Disponer();
            Assert.AreEqual(80f, hoja.AltoCapsula, 1f);
            Assert.AreEqual(80f, hoja.worldBound.height, 1f);
            hoja.Linea.style.minHeight = StyleKeyword.Null;
            Disponer();
            Disponer();
            Assert.AreEqual(antes, hoja.AltoCapsula, 1f, "Y vuelve.");
        }

        // ───────────────────────── Hoja de la ruta (mismo fallo) ─────────────────────────

        [Test]
        public void HojaDeLaRuta_ArrastresSeguidosYDespuesElBoton()
        {
            hoja.Mostrar(false);
            var tarjeta = new TarjetaRuta(c => PrepararInterfaz.TextosEs[c]);
            tarjeta.style.position = Position.Absolute;
            tarjeta.style.left = 0f;
            tarjeta.style.right = 0f;
            tarjeta.style.bottom = 0f;
            tarjeta.style.display = DisplayStyle.Flex;
            hueco.Add(tarjeta);
            tarjeta.PonerDestino("Clínica Areas", "Clínica privada · en coche", IconosLugar.Generico, null, "Destino");
            int finalizadas = 0;
            tarjeta.FinalizarPulsado += () => finalizadas++;
            Disponer();
            for (int i = 0; i < 3; i++)
            {
                var c = tarjeta.Cuerpo.worldBound.center;
                Arrastrar(c, c.y - 60f);
                Assert.IsTrue(tarjeta.Desplegada, "Hacia arriba la despliega a la primera.");
                c = tarjeta.Cuerpo.worldBound.center;
                Arrastrar(c, c.y + 60f);
                Assert.IsFalse(tarjeta.Desplegada, "Hacia abajo la pliega a la primera.");
            }
            Tocar(tarjeta.Cuerpo.worldBound.center);
            Assert.IsTrue(tarjeta.Desplegada);
            var boton = tarjeta.Finalizar.worldBound.center;
            Arrastrar(boton, boton.y + 30f);
            Assert.AreEqual(0, finalizadas, "Un arrastre desde el botón no lo pulsa.");
            Assert.IsFalse(tarjeta.Desplegada);
            Tocar(tarjeta.Cuerpo.worldBound.center);
            Tocar(tarjeta.Finalizar.worldBound.center);
            Assert.AreEqual(1, finalizadas);
        }
    }
}
