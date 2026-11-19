using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using PriceMap.Adaptadores.Datos;
using PriceMap.Aplicacion;
using PriceMap.Dominio;
using PriceMap.EditorTools;
using PriceMap.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.Accessibility;
using UnityEngine.UIElements;

namespace PriceMap.Tests.EditMode
{
    /// <summary>
    /// Buscador (historia 2.2, parte 1): la física de la hoja (FisicaHoja, una prueba por fila de la matriz) y la
    /// cápsula y la hoja en la pantalla real (PantallaMapa.uxml): gestos, botón atrás y exclusión con la ficha.
    /// Sin panel (EditMode) la hoja no se dispone: se le da el alto del hueco con Medir.
    /// </summary>
    public class PruebasBuscador
    {
        static readonly DateTime Ahora = new DateTime(2026, 11, 19, 12, 0, 0, DateTimeKind.Utc);
        const float AltoUtil = 600f;

        // ───────────────────────── Física (FisicaHoja) ─────────────────────────

        static FisicaHoja Fisica(float fraccion = 0f)
        {
            var f = new FisicaHoja();
            f.PonerAltoUtil(AltoUtil);
            f.Poner(fraccion);
            return f;
        }

        /// <summary>
        /// Arrastra desde y0 hasta y1 a velocidad constante (dp/ms; muestras cada 10 ms) y suelta <paramref name="esperaMs"/>
        /// después del último movimiento.
        /// </summary>
        static FisicaHoja.Suelta Arrastrar(FisicaHoja f, float y0, float y1, float velocidad, double esperaMs = 5.0)
        {
            f.Bajar(new Vector2(200f, y0), 0.0);
            float signo = Mathf.Sign(y1 - y0), distancia = Mathf.Abs(y1 - y0);
            double fin = distancia / velocidad; // el último movimiento llega justo a y1 a la misma velocidad
            double t = 0.0;
            while (t < fin)
            {
                t = Math.Min(t + 10.0, fin);
                float y = t >= fin ? y1 : y0 + signo * velocidad * (float)t;
                f.Mover(new Vector2(200f, y), t);
            }
            return f.Soltar(t + esperaMs);
        }

        [Test]
        public void ArrastrarHastaLaMitadYSoltarDespacioLaDejaAhi()
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 700f - 0.5f * AltoUtil, 0.1f);
            Assert.IsTrue(r.FueArrastre);
            Assert.AreEqual(DestinoHoja.Quedar, r.Destino);
            Assert.AreEqual(0.5f, f.Fraccion, 0.001f, "Sin imán: se queda exactamente donde se suelta.");
            Assert.AreEqual(-0.1f, f.Velocidad, 0.001f, "Velocidad medida hacia arriba.");
        }

        [Test]
        public void LaHojaSigueAlDedo()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            Assert.IsTrue(f.Mover(new Vector2(200f, 640f), 50.0));
            Assert.AreEqual(60f / AltoUtil, f.Fraccion, 0.0001f);
            f.Mover(new Vector2(200f, 400f), 300.0);
            Assert.AreEqual(0.5f, f.Fraccion, 0.0001f);
            f.Mover(new Vector2(200f, 900f), 600.0);
            Assert.AreEqual(0f, f.Fraccion, "No baja de plegada.");
            f.Mover(new Vector2(200f, -200f), 900.0);
            Assert.AreEqual(1f, f.Fraccion, "No sube de abierta del todo.");
        }

        [Test]
        public void LanzarHaciaArribaLaAbreDelTodo()
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 650f, 0.9f); // al 8 %: sin lanzamiento se plegaría
            Assert.AreEqual(DestinoHoja.Abierta, r.Destino);
            Assert.AreEqual(1f, f.Fraccion);
        }

        [Test]
        public void LanzarHaciaAbajoDesdeEl70LaPliega()
        {
            var f = Fisica(0.7f);
            var r = Arrastrar(f, 300f, 360f, 0.9f); // al 60 %: sin lanzamiento se quedaría
            Assert.AreEqual(DestinoHoja.Plegada, r.Destino);
            Assert.AreEqual(0f, f.Fraccion);
        }

        [Test]
        public void SoltarAl10LaPliega()
        {
            var f = Fisica();
            Arrastrar(f, 700f, 700f - 0.1f * AltoUtil, 0.1f, esperaMs: 200.0);
            Assert.AreEqual(0f, f.Fraccion);
        }

        [Test]
        public void SoltarAl92LaAbreDelTodo()
        {
            var f = Fisica();
            Arrastrar(f, 700f, 700f - 0.92f * AltoUtil, 0.1f, esperaMs: 200.0);
            Assert.AreEqual(1f, f.Fraccion);
        }

        [TestCase(0.16f)]
        [TestCase(0.3f)]
        [TestCase(0.87f)]
        public void EntreLosLimitesNoHayIman(float fraccion)
        {
            var f = Fisica();
            Arrastrar(f, 700f, 700f - fraccion * AltoUtil, 0.05f, esperaMs: 200.0);
            Assert.AreEqual(fraccion, f.Fraccion, 0.001f);
        }

        [Test]
        public void SiElDedoSeParaAntesDeSoltarNoEsUnLanzamiento()
        {
            var f = Fisica();
            var r = Arrastrar(f, 700f, 400f, 2f, esperaMs: 91.0);
            Assert.AreEqual(0f, r.Fraccion - 0.5f, 0.001f, "Parado más de 90 ms: velocidad 0 y se queda al 50 %.");
            Assert.AreEqual(DestinoHoja.Quedar, r.Destino);
            Assert.AreEqual(0f, f.Velocidad);

            var g = Fisica();
            Arrastrar(g, 700f, 400f, 2f, esperaMs: 80.0);
            Assert.AreEqual(1f, g.Fraccion, "Soltado a los 80 ms: es un lanzamiento hacia arriba.");
        }

        [Test]
        public void LaVelocidadSoloSeMuestreaPasados4Ms()
        {
            var f = Fisica();
            f.Bajar(new Vector2(0f, 700f), 0.0);
            f.Mover(new Vector2(0f, 690f), 10.0); // empieza el arrastre: la primera muestra abarca desde que bajó el dedo
            Assert.AreEqual(-1f, f.Velocidad, 0.001f, "(690 − 700) / 10 ms: recorrido y tiempo del mismo tramo.");
            f.Mover(new Vector2(0f, 680f), 20.0);
            Assert.AreEqual(-1f, f.Velocidad, 0.001f, "(680 − 690) / 10 ms.");
            f.Mover(new Vector2(0f, 600f), 23.0); // 3 ms: no se vuelve a medir
            Assert.AreEqual(-1f, f.Velocidad, 0.001f);
            f.Mover(new Vector2(0f, 590f), 25.0); // 5 ms desde la última muestra (20 ms)
            Assert.AreEqual(-18f, f.Velocidad, 0.001f, "(590 − 680) / 5 ms.");
        }

        [Test]
        public void UnArrastreLentoQueAcabaDePasarElUmbralNoEsUnLanzamiento()
        {
            var f = Fisica(0.5f);
            f.Bajar(new Vector2(200f, 400f), 0.0);
            f.Mover(new Vector2(200f, 397f), 30.0);  // 0,1 dp/ms, aún toque
            f.Mover(new Vector2(200f, 393f), 70.0);  // pasa los 6 dp: arrastre
            var r = f.Soltar(75.0);
            Assert.AreEqual(-0.1f, f.Velocidad, 0.001f, "La velocidad real, no la del recorrido previo dividido por un instante.");
            Assert.AreEqual(DestinoHoja.Quedar, r.Destino);
        }

        [Test]
        public void UnLanzamientoDeUnSoloMovimientoCuenta()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            f.Mover(new Vector2(200f, 670f), 20.0); // 1,5 dp/ms de una vez
            var r = f.Soltar(25.0);
            Assert.AreEqual(DestinoHoja.Abierta, r.Destino);
        }

        [Test]
        public void UnGestoHorizontalNoMueveLaHoja()
        {
            var f = Fisica(1f);
            f.Bajar(new Vector2(200f, 120f), 0.0); // en el campo, p. ej. moviendo el cursor
            Assert.IsFalse(f.Mover(new Vector2(215f, 122f), 30.0));
            Assert.IsTrue(f.Ignorado);
            var r = f.Soltar(60.0);
            Assert.IsFalse(r.EsToque, "Tampoco es un toque.");
            Assert.AreEqual(1f, f.Fraccion);
        }

        [Test]
        public void LosValoresSonLosDelPrototipo()
        {
            Assert.AreEqual(64f, FisicaHoja.HuecoArriba);
            Assert.AreEqual(76f, FisicaHoja.AltoPlegado);
            Assert.AreEqual(0.6f, FisicaHoja.Lanzamiento);
            Assert.AreEqual(0.15f, FisicaHoja.PlegarDebajo);
            Assert.AreEqual(0.88f, FisicaHoja.AbrirEncima);
            Assert.AreEqual(6f, FisicaHoja.UmbralToque);
            Assert.AreEqual(800f - 76f - 64f, FisicaHoja.AltoUtilDe(800f, 0f, 0f));
            Assert.AreEqual(800f - 24f - 76f - 64f - 30f, FisicaHoja.AltoUtilDe(800f, 30f, 24f), "Con las zonas seguras.");
        }

        [Test]
        public void UnMovimientoDe3DpEsUnToque()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            Assert.IsFalse(f.Mover(new Vector2(202f, 697f), 30.0));
            var r = f.Soltar(60.0);
            Assert.IsTrue(r.EsToque);
            Assert.AreEqual(DestinoHoja.Abierta, r.Destino);
            Assert.AreEqual(1f, f.Fraccion, "Un toque la abre del todo.");
        }

        [Test]
        public void UnMovimientoDe10DpEsUnArrastre()
        {
            var f = Fisica();
            f.Bajar(new Vector2(200f, 700f), 0.0);
            Assert.IsTrue(f.Mover(new Vector2(200f, 690f), 30.0));
            Assert.IsTrue(f.Arrastrando);
            var r = f.Soltar(200.0);
            Assert.IsFalse(r.EsToque);
            Assert.IsTrue(r.FueArrastre);
        }

        [TestCase(false, 0f, -10f, true)]   // no abierta del todo: siempre
        [TestCase(false, 50f, 10f, true)]
        [TestCase(true, 0f, 10f, true)]     // abierta, lista arriba del todo y hacia abajo
        [TestCase(true, 0f, -10f, false)]   // abierta y hacia arriba: se desplaza la lista
        [TestCase(true, 40f, 10f, false)]   // abierta con la lista desplazada
        public void LaListaMueveLaHoja(bool abierta, float desplazamiento, float dy, bool mueve)
        {
            Assert.AreEqual(mueve, FisicaHoja.ListaMueveHoja(abierta, desplazamiento, dy));

            var f = Fisica(abierta ? 1f : 0.5f);
            f.Bajar(new Vector2(200f, 400f), 0.0, enLista: true, desplazamientoLista: desplazamiento);
            f.Mover(new Vector2(200f, 400f + dy), 20.0);
            Assert.AreEqual(mueve, f.Arrastrando);
            Assert.AreEqual(!mueve, f.Ignorado, "Si no la mueve, la lista se desplaza y la hoja no hace nada.");
        }

        [Test]
        public void UnToqueEnLaListaNoEsUnToqueDelCampo()
        {
            var f = Fisica(1f);
            f.Bajar(new Vector2(200f, 400f), 0.0, enLista: true);
            var r = f.Soltar(50.0);
            Assert.IsFalse(r.EsToque);
            Assert.AreEqual(1f, f.Fraccion);
        }

        // ───────────────────────── Pantalla ─────────────────────────

        class MapaFalso : IMapa
        {
            public void Centrar(Coordenada destino) { }
            public void Centrar(Coordenada destino, PuntoPantalla enPantalla) { }
            public void AplicarTema(Tema tema) { }
            public void Acercar() { }
            public void Alejar() { }
            public PuntoPantalla Proyectar(Coordenada punto) => PuntoPantalla.Ninguno;
            public IReadOnlyList<PuntoInteres> PuntosInteres => Array.Empty<PuntoInteres>();
            public event Action PuntosInteresCambiados { add { } remove { } }
            public float DistanciaCamara => 900f;
            public float Giro => 0f;
            public Coordenada Centro => default;
            public void OrientarNorte() { }
            public event Action<PuntoPantalla> Toque { add { } remove { } }
        }

        class PreferenciasFalsas : IPreferencias
        {
            readonly Dictionary<string, string> datos = new Dictionary<string, string>();
            public string Leer(string clave) => datos.TryGetValue(clave, out var v) ? v : null;
            public void Guardar(string clave, string valor) => datos[clave] = valor;
            public void Borrar(string clave) => datos.Remove(clave);
        }

        class SistemaClaro : ITemaSistema { public bool EsOscuro => false; }

        static ServicioLugares lugares;
        static ServicioLugares Lugares()
        {
            if (lugares != null) return lugares;
            var l = new ServicioLugares(new RepositorioLugaresJson());
            Assert.IsTrue(l.Cargar().Ok);
            return lugares = l;
        }

        static string T(string clave) => PrepararInterfaz.TextosEs[clave];

        GameObject objeto;
        PantallaMapa pantalla;
        VisualElement arbol;
        HojaBuscador hoja;

        [SetUp]
        public void Crear()
        {
            Textos.Forzados = T; // en EditMode Localization no tiene locale: textos es-ES reales
            objeto = new GameObject("Pantalla de prueba");
            pantalla = objeto.AddComponent<PantallaMapa>();
            arbol = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/PantallaMapa.uxml").Instantiate();
            var prefs = new PreferenciasFalsas();
            var tema = new ServicioTema(prefs, new SistemaClaro());
            var filtro = new ServicioFiltro(prefs);
            pantalla.Inicializar(new MapaFalso(), tema, Lugares(), Ahora, arbol, filtro);
            tema.Iniciar();
            filtro.Iniciar();
            hoja = pantalla.Buscador;
            Assert.IsNotNull(hoja, "La pantalla crea el buscador.");
            hoja.Medir(800f); // sin panel no hay disposición: hueco de 800 dp
        }

        [TearDown]
        public void Limpiar()
        {
            UnityEngine.Object.DestroyImmediate(objeto);
            Textos.Forzados = null;
            AccesibilidadPantalla.LectorForzado = null;
            AssistiveSupport.activeHierarchy = null;
        }

        /// <summary>Toque en la cápsula: baja y sube el dedo moviéndolo 3 dp.</summary>
        void TocarCapsula()
        {
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -3f), 30.0);
            hoja.Soltar(60.0);
        }

        [Test]
        public void LaCapsulaVaAbajoConElCampoYElAvatar()
        {
            Assert.AreSame(arbol.Q("hueco-buscador"), hoja.parent);
            Assert.IsTrue(hoja.Visible);
            Assert.IsFalse(hoja.Abierta, "Al arrancar, plegada.");
            Assert.AreEqual("Buscar en Sanlúcar", hoja.Campo.textEdition.placeholder);
            Assert.AreEqual(PickingMode.Position, hoja.pickingMode, "La cápsula recoge los toques: no pasan al mapa.");
            Assert.AreEqual(PickingMode.Position, hoja.Avatar.pickingMode);
            Assert.IsTrue(hoja.Avatar.ClassListContains("buscador__avatar"));
            Assert.IsTrue(SombraSuave.Tiene(hoja), "Sombra suave de los controles flotantes.");
            Assert.AreEqual(HojaBuscador.AltoCapsulaMin, hoja.AltoCapsula, "56 dp como mínimo.");
            Assert.AreEqual(800f - 56f - 20f, hoja.TopPlegada);
            Assert.AreEqual(800f - 76f - 64f, hoja.Fisica.AltoUtil, "Recorre de la cápsula a 64 dp de arriba.");
        }

        [Test]
        public void ElContenidoInicialSonLasCategoriasYEncuentraCerca()
        {
            var esperados = new[] { "Restaurantes", "Bares", "Supermercados", "Farmacias", "Gasolineras", "Gimnasios", "Salud", "Cultura" };
            CollectionAssert.AreEqual(esperados, hoja.Chips.Select(c => c.Q<Label>().text).ToArray());
            for (int i = 0; i < HojaBuscador.Categorias.Length; i++)
            {
                var icono = hoja.Chips[i].Q(className: "buscador__chip-icono");
                Assert.IsTrue(icono.ClassListContains("buscador__chip-icono--" + HojaBuscador.Nombre(HojaBuscador.Categorias[i])),
                    $"El icono de {esperados[i]} lleva el color de su categoría.");
            }
            CollectionAssert.AreEqual(new[] { CategoriaPoi.Comer, CategoriaPoi.Bares, CategoriaPoi.Super, CategoriaPoi.Farmacia,
                CategoriaPoi.Gasolina, CategoriaPoi.Deporte, CategoriaPoi.Salud, CategoriaPoi.Cultura }, HojaBuscador.Categorias);
            Assert.AreEqual("Encuentra cerca", hoja.Cerca.Q<Label>().text);
            Assert.IsTrue(hoja.Panel.contentContainer.Contains(hoja.Cerca));
        }

        [Test]
        public void LosGlifosDelBuscadorSonVectoriales()
        {
            CollectionAssert.Contains(IconosLugar.Glifos.ToList(), IconosLugar.Lupa);
            CollectionAssert.Contains(IconosLugar.Glifos.ToList(), IconosLugar.Avatar);
            Assert.IsNotEmpty(IconosLugar.Interpretar(IconosLugar.Lupa));
            Assert.IsNotEmpty(IconosLugar.Interpretar(IconosLugar.Avatar));
            foreach (var c in HojaBuscador.Categorias)
                Assert.AreNotEqual(IconosLugar.Generico, IconosLugar.GlifoDe(c), c.ToString());
        }

        [Test]
        public void UnToqueEnLaCapsulaLaAbreDelTodoYPideElTeclado()
        {
            TocarCapsula();
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.IsTrue(hoja.TecladoPedido, "Foco al campo y teclado.");
            Assert.IsTrue(pantalla.BuscadorAbierto);
            Assert.AreEqual(hoja.TopAbierta, hoja.Top, 0.001f, "Abierta del todo, a 64 dp de arriba.");
        }

        [Test]
        public void UnArrastreCortoNoSacaElTeclado()
        {
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -10f), 30.0);
            Assert.IsTrue(hoja.Arrastrando, "10 dp: arrastre.");
            Assert.IsTrue(hoja.ClassListContains(HojaBuscador.ClaseArrastrando), "Sin transición mientras sigue al dedo.");
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)), "Mientras se arrastra, el mapa no recibe el gesto.");
            hoja.Soltar(300.0);
            Assert.IsFalse(hoja.TecladoPedido);
            Assert.IsFalse(hoja.ClassListContains(HojaBuscador.ClaseArrastrando));
        }

        [Test]
        public void ArrastrarYSoltarALaMitadLaDejaAMediaAltura()
        {
            float y0 = hoja.TopPlegada + 20f, recorrido = hoja.Fisica.AltoUtil;
            hoja.Bajar(new Vector2(150f, y0), 0.0);
            for (int i = 1; i <= 10; i++) hoja.Mover(new Vector2(150f, y0 - recorrido * 0.05f * i), 500.0 * i);
            hoja.Soltar(5200.0);
            Assert.AreEqual(0.5f, hoja.Fraccion, 0.001f);
            Assert.AreEqual(hoja.TopPlegada - recorrido * 0.5f, hoja.Top, 0.01f);
            Assert.AreEqual(hoja.Top, hoja.style.top.value.value, 0.01f, "La hoja se pone en su sitio.");
        }

        [Test]
        public void LaFormaPasaDeCapsulaAHoja()
        {
            void Forma(float left, float bottom, float arriba, float abajo, bool abierta, string caso)
            {
                Assert.AreEqual(left, hoja.style.left.value.value, 0.01f, caso + ": izquierda");
                Assert.AreEqual(left, hoja.style.right.value.value, 0.01f, caso + ": derecha");
                Assert.AreEqual(bottom, hoja.style.bottom.value.value, 0.01f, caso + ": abajo");
                Assert.AreEqual(arriba, hoja.style.borderTopLeftRadius.value.value, 0.01f, caso + ": esquinas de arriba");
                Assert.AreEqual(abajo, hoja.style.borderBottomRightRadius.value.value, 0.01f, caso + ": esquinas de abajo");
                Assert.AreEqual(abierta, hoja.ClassListContains(HojaBuscador.ClaseAbierta), caso + ": fondo opaco");
            }
            Forma(12f, 20f, 28f, 28f, false, "Plegada");
            hoja.Poner(0.05f); // s = 1/3
            Forma(8f, 20f * 2f / 3f, 28f - 4f / 3f, 28f * 2f / 3f, false, "Al 5 %");
            hoja.Poner(0.15f);
            Forma(0f, 0f, 24f, 0f, true, "Al 15 %");
            hoja.Abrir();
            Forma(0f, 0f, 24f, 0f, true, "Abierta");
        }

        [Test]
        public void LasZonasSegurasMuevenLosExtremos()
        {
            hoja.PonerZonaSegura(30f, 24f);
            Assert.AreEqual(800f - 24f - 20f - 56f, hoja.TopPlegada, "Plegada, encima de la barra de gestos.");
            Assert.AreEqual(30f + 64f, hoja.TopAbierta, "Abierta, 64 dp por debajo de la barra de estado.");
            Assert.AreEqual(44f, hoja.style.bottom.value.value, 0.01f);
            hoja.Abrir();
            Assert.AreEqual(24f, hoja.style.paddingBottom.value.value, 0.01f, "Abierta, el contenido termina encima de la barra de gestos.");
        }

        [Test]
        public void AtrasPliegaLaHoja()
        {
            hoja.Abrir();
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(hoja.Abierta);
            Assert.IsFalse(hoja.TecladoPedido);
            Assert.IsFalse(pantalla.Atras(), "Plegada, atrás no hace nada más.");

            hoja.Poner(0.5f);
            Assert.IsTrue(pantalla.Atras(), "A media altura también se pliega.");
            Assert.AreEqual(0f, hoja.Fraccion);
        }

        [Test]
        public void TocarLaCapsulaConLaFichaAbiertaCierraLaFichaYAbreLaHoja()
        {
            pantalla.AbrirFicha(Lugares().Lugares.First());
            Assert.IsTrue(pantalla.FichaAbierta);
            Assert.IsFalse(hoja.Visible, "La cápsula se oculta mientras la ficha está abierta.");
            Assert.AreEqual(DisplayStyle.None, hoja.style.display.value);

            TocarCapsula();
            Assert.IsFalse(pantalla.FichaAbierta, "Abrir la hoja cierra la ficha.");
            Assert.IsTrue(hoja.AbiertaDelTodo);
            Assert.IsTrue(hoja.Visible, "Al cerrar la ficha vuelve la cápsula.");
        }

        [Test]
        public void AbrirLaFichaPliegaLaHojaYCerrarlaDevuelveLaCapsula()
        {
            hoja.Poner(0.5f);
            pantalla.AbrirFicha(Lugares().Lugares.First());
            Assert.AreEqual(0f, hoja.Fraccion, "Nunca abiertas a la vez.");
            Assert.IsFalse(hoja.Visible);
            Assert.IsTrue(pantalla.Atras());
            Assert.IsFalse(pantalla.FichaAbierta);
            Assert.IsTrue(hoja.Visible);
        }

        [Test]
        public void DesplegarLaHojaCierraElMenuPrecios()
        {
            pantalla.AlternarMenu();
            Assert.IsTrue(pantalla.MenuAbierto);
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -40f), 30.0);
            Assert.IsFalse(pantalla.MenuAbierto, "Al empezar a desplegarla se cierra el menú.");
            hoja.Soltar(200.0);
        }

        [Test]
        public void AbrirElMenuPreciosPliegaLaHoja()
        {
            hoja.Poner(0.5f);
            pantalla.AlternarMenu();
            Assert.IsTrue(pantalla.MenuAbierto);
            Assert.AreEqual(0f, hoja.Fraccion, "A media altura la hoja taparía el menú.");
        }

        [Test]
        public void OcultarLaCapsulaAMitadDeGestoNoLaBloquea()
        {
            var p = new Vector2(150f, hoja.TopPlegada + 20f);
            hoja.Bajar(p, 0.0);
            hoja.Mover(p + new Vector2(0f, -40f), 30.0);
            hoja.Mostrar(false); // p. ej. se abre la ficha con el dedo aún abajo
            Assert.IsFalse(hoja.Fisica.Pulsado);
            Assert.IsFalse(hoja.Arrastrando);
            hoja.Mostrar(true);
            TocarCapsula();
            Assert.IsTrue(hoja.AbiertaDelTodo, "La hoja sigue respondiendo.");
        }

        [Test]
        public void ConLaHojaAbiertaDelTodoElMapaNoRecibeToques()
        {
            Assert.IsFalse(pantalla.TapaPunto(new Vector2(10f, 10f)), "Plegada (y sin panel), el mapa sigue libre.");
            hoja.Abrir();
            Assert.IsTrue(pantalla.TapaPunto(new Vector2(10f, 10f)));
        }

        [Test]
        public void LaCapsulaEsDeCristalEnClaroYOpacaEnOscuro()
        {
            var tokens = System.IO.File.ReadAllText(System.IO.Path.Combine(Application.dataPath, "UI/Estilos/Tokens.uss"));
            var claro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            var oscuro = System.Text.RegularExpressions.Regex.Match(tokens, @"\.pm-raiz\.tema-oscuro\s*\{(?<c>[^}]*)\}").Groups["c"].Value;
            StringAssert.Contains("--color-capsula: rgba(255, 255, 255, 0.90)", claro, "Cristal, como --color-glass.");
            StringAssert.Contains("--color-capsula: #1A2238", oscuro, "Opaca (surface) en oscuro.");
        }
    }
}
