using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>
    /// Cámara de mapa en 2.5D (como el prototipo web: inclinada unos 50° y algo girada).
    /// Gira alrededor de un punto del suelo (pivote).
    ///  - Ratón: arrastrar con el botón izquierdo = mover; rueda = zoom; botón derecho = girar e inclinar.
    ///  - Teclado: WASD / flechas = mover; Q/E = girar; R/F = inclinar.
    ///  - Táctil: un dedo = mover; dos dedos = zoom (pellizco) y giro; dos dedos en vertical = inclinar.
    /// La entrada se lee y la cámara se mueve en Update, antes que ningún otro script (orden -100):
    /// así está quieta cuando la UI proyecta los marcadores en LateUpdate, en el mismo fotograma.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)]
    public class CamaraMapa : MonoBehaviour
    {
        public Vector3 pivote = Vector3.zero;
        [Range(60f, 4000f)] public float distancia = 900f;
        [Range(10f, 85f)] public float inclinacion = 50f;  // grados desde la horizontal (85 = vista cenital)
        public float giro = -15f;                            // rumbo en grados (0 = norte arriba)

        [Header("Límites")]
        public float distanciaMin = 60f;
        public float distanciaMax = 4000f;
        public float inclinacionMin = 20f;
        public float inclinacionMax = 85f;

        [Header("Sensibilidad")]
        public float velocidadGiro = 0.25f;
        public float velocidadZoom = 0.12f;
        public float velocidadTeclado = 0.8f;
        [Tooltip("Grados de inclinación por píxel de arrastre vertical con dos dedos")]
        public float velocidadInclinacion = 0.15f;

        /// <summary>Lo pone el Arranque: true si el punto de pantalla está sobre un control de la interfaz.</summary>
        public System.Func<Vector2, bool> entradaBloqueada;

        /// <summary>
        /// Toque sobre el mapa (historia 1.8): el dedo, o el botón izquierdo en PC, se suelta habiéndose movido
        /// menos de 6 dp. Punto de pantalla en px con origen abajo a la izquierda, como Input. No se lanza si el
        /// gesto empezó sobre la interfaz (entradaBloqueada) ni con dos dedos.
        /// </summary>
        public event System.Action<Vector2> Tocado;

        Camera cam;
        Vector3? sueloAnterior;
        bool gestoSobreInterfaz; // el gesto empezó sobre un botón: el mapa no se mueve hasta soltar
        readonly DetectorToque toque = new DetectorToque();
        static readonly Plane Suelo = new Plane(Vector3.up, Vector3.zero);

        void Awake() { cam = GetComponent<Camera>(); Aplicar(); }

        void Update()
        {
            if (Input.touchSupported && Input.touchCount > 0)
            {
                if (Input.GetTouch(0).phase == TouchPhase.Began && Input.touchCount == 1) gestoSobreInterfaz = SobreInterfaz(Input.GetTouch(0).position);
                if (!gestoSobreInterfaz) Tactil();
                ToqueTactil();
            }
            else
            {
                if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) gestoSobreInterfaz = SobreInterfaz(Input.mousePosition);
                if (!Input.GetMouseButton(0) && !Input.GetMouseButton(1) && Input.touchCount == 0) gestoSobreInterfaz = false;
                if (!gestoSobreInterfaz) Raton();
                // En el móvil Unity también simula el ratón con el dedo: ahí el toque solo se lee de los dedos
                if (!Input.touchSupported) ToqueRaton();
            }
            Teclado();
            Aplicar();
        }

        void ToqueTactil()
        {
            var t = Input.GetTouch(0);
            if (t.phase == TouchPhase.Began && Input.touchCount == 1)
            {
                if (gestoSobreInterfaz) toque.Cancelar(); else toque.Empezar(t.position);
            }
            if (Input.touchCount > 1) toque.Anular();
            if (t.phase == TouchPhase.Moved) toque.Mover(t.position);
            else if (t.phase == TouchPhase.Canceled) toque.Cancelar();
            else if (t.phase == TouchPhase.Ended && toque.Soltar(t.position, Screen.dpi)) Tocado?.Invoke(t.position);
        }

        void ToqueRaton()
        {
            Vector2 pos = Input.mousePosition;
            if (Input.GetMouseButtonDown(0))
            {
                if (gestoSobreInterfaz) toque.Cancelar(); else toque.Empezar(pos);
            }
            if (Input.GetMouseButton(1)) toque.Anular();
            if (Input.GetMouseButton(0)) toque.Mover(pos);
            if (Input.GetMouseButtonUp(0) && toque.Soltar(pos, Screen.dpi)) Tocado?.Invoke(pos);
        }

        /// <summary>Solo para las pruebas: lanza Tocado como si el dedo se soltara en ese punto (px, origen abajo).</summary>
        internal void SimularToque(Vector2 pantalla) => Tocado?.Invoke(pantalla);

        bool SobreInterfaz(Vector2 pantalla) => entradaBloqueada != null && entradaBloqueada(pantalla);

        void Aplicar()
        {
            distancia = Mathf.Clamp(distancia, distanciaMin, distanciaMax);
            inclinacion = Mathf.Clamp(inclinacion, inclinacionMin, inclinacionMax);
            var rot = Quaternion.Euler(inclinacion, giro, 0f);
            transform.SetPositionAndRotation(pivote - rot * Vector3.forward * distancia, rot);
            if (cam != null) { cam.nearClipPlane = Mathf.Max(0.5f, distancia * 0.01f); cam.farClipPlane = distancia * 20f; }
        }

        bool PuntoSuelo(Vector2 pantalla, out Vector3 p)
        {
            var rayo = cam.ScreenPointToRay(pantalla);
            if (Suelo.Raycast(rayo, out float d)) { p = rayo.GetPoint(d); return true; }
            p = default; return false;
        }

        // Arrastrar: el punto del suelo que estaba bajo el puntero se queda bajo el puntero
        void Arrastrar(Vector2 pantalla)
        {
            if (!PuntoSuelo(pantalla, out var p)) { sueloAnterior = null; return; }
            if (sueloAnterior.HasValue) { pivote += sueloAnterior.Value - p; Aplicar(); PuntoSuelo(pantalla, out p); }
            sueloAnterior = p;
        }

        void Raton()
        {
            if (Input.GetMouseButton(0)) Arrastrar(Input.mousePosition); else sueloAnterior = null;
            if (Input.GetMouseButton(1))
            {
                giro += Input.GetAxis("Mouse X") * velocidadGiro * 20f;
                inclinacion -= Input.GetAxis("Mouse Y") * velocidadGiro * 20f;
            }
            float rueda = Input.mouseScrollDelta.y;
            if (Mathf.Abs(rueda) > 0.01f) distancia *= 1f - rueda * velocidadZoom;
        }

        float distDedosAnterior, anguloDedosAnterior;
        TipoGesto modoDosDedos; // una vez decidido (inclinar o zoom/giro), se mantiene hasta levantar los dedos
        void Tactil()
        {
            if (Input.touchCount < 2) modoDosDedos = TipoGesto.Nada;
            if (Input.touchCount == 1)
            {
                var t = Input.GetTouch(0);
                if (t.phase == TouchPhase.Began) sueloAnterior = null;
                Arrastrar(t.position);
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) sueloAnterior = null;
                return;
            }
            sueloAnterior = null;
            var a = Input.GetTouch(0); var b = Input.GetTouch(1);
            float dist = Vector2.Distance(a.position, b.position);
            float ang = Mathf.Atan2(b.position.y - a.position.y, b.position.x - a.position.x) * Mathf.Rad2Deg;
            bool empieza = b.phase == TouchPhase.Began || a.phase == TouchPhase.Began;
            bool termina = a.phase == TouchPhase.Ended || a.phase == TouchPhase.Canceled || b.phase == TouchPhase.Ended || b.phase == TouchPhase.Canceled;
            if (empieza || termina) { distDedosAnterior = dist; anguloDedosAnterior = ang; if (empieza) modoDosDedos = TipoGesto.Nada; return; }

            var gesto = GestoDosDedos.Clasificar(a.deltaPosition, b.deltaPosition, distDedosAnterior, dist, anguloDedosAnterior, ang, out float dy);
            if (modoDosDedos == TipoGesto.Nada) modoDosDedos = gesto;      // el primer movimiento claro decide el modo
            else if (gesto != TipoGesto.Nada) gesto = modoDosDedos;        // y se mantiene durante todo el gesto
            if (gesto == TipoGesto.Inclinar)
                inclinacion -= dy * velocidadInclinacion; // subir los dedos inclina el mapa (más 3D), como Google Maps
            else if (gesto == TipoGesto.ZoomGiro)
            {
                if (distDedosAnterior > 1f) distancia *= distDedosAnterior / Mathf.Max(1f, dist);
                giro -= Mathf.DeltaAngle(anguloDedosAnterior, ang);
            }
            distDedosAnterior = dist; anguloDedosAnterior = ang;
        }

        void Teclado()
        {
            float paso = distancia * velocidadTeclado * Time.deltaTime;
            var adelante = Quaternion.Euler(0f, giro, 0f) * Vector3.forward;
            var derecha = Quaternion.Euler(0f, giro, 0f) * Vector3.right;
            pivote += (adelante * Input.GetAxisRaw("Vertical") + derecha * Input.GetAxisRaw("Horizontal")) * paso;
            if (Input.GetKey(KeyCode.Q)) giro -= 60f * Time.deltaTime;
            if (Input.GetKey(KeyCode.E)) giro += 60f * Time.deltaTime;
            if (Input.GetKey(KeyCode.R)) inclinacion += 40f * Time.deltaTime;
            if (Input.GetKey(KeyCode.F)) inclinacion -= 40f * Time.deltaTime;
        }

        /// <summary>Acerca un paso (botón de zoom).</summary>
        public void Acercar() { distancia *= PasoZoom; Aplicar(); }

        /// <summary>Aleja un paso (botón de zoom).</summary>
        public void Alejar() { distancia /= PasoZoom; Aplicar(); }

        const float PasoZoom = 0.6f;

        /// <summary>Centra la cámara en una coordenada geográfica.</summary>
        public void CentrarEn(double lat, double lon) { pivote = Geo.ALocal(lat, lon); Aplicar(); }

        /// <summary>
        /// Deja la coordenada en ese punto de la pantalla (px de la cámara, Y desde abajo, como Input) sin cambiar
        /// zoom, giro ni inclinación. Primero la centra y después desplaza el pivote lo que separa, en el suelo, el
        /// punto objetivo del centro de la vista: como la cámara se mueve con el pivote sin girar, el desplazamiento
        /// es exacto también con el mapa inclinado.
        /// </summary>
        public void CentrarEn(double lat, double lon, Vector2 objetivo)
        {
            CentrarEn(lat, lon);
            if (cam == null) cam = GetComponent<Camera>(); // en EditMode no se ejecuta Awake
            if (cam == null) return;
            var centro = new Vector2(cam.pixelWidth / 2f, cam.pixelHeight / 2f);
            if (!PuntoSuelo(centro, out var enCentro)) return;
            // Muy poco inclinado, el objetivo puede caer por encima del horizonte: se acerca por pasos al centro (en
            // vertical) hasta el primer punto que toque el suelo
            const int Pasos = 20;
            Vector3 enObjetivo = default;
            bool toca = false;
            for (int i = 0; i < Pasos && !toca; i++)
                toca = PuntoSuelo(new Vector2(objetivo.x, Mathf.Lerp(objetivo.y, centro.y, i / (float)Pasos)), out enObjetivo);
            if (!toca) return;
            pivote += enCentro - enObjetivo;
            Aplicar();
        }

        /// <summary>Norte arriba (giro 0) sin cambiar el zoom ni la inclinación; al instante, como CentrarEn.</summary>
        public void OrientarNorte() { giro = 0f; Aplicar(); }
    }
}
