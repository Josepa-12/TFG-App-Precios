using UnityEngine;

namespace PrecioLocal
{
    /// <summary>
    /// Cámara de mapa en 2.5D (como el prototipo web: inclinada unos 50° y algo girada).
    /// Gira alrededor de un punto del suelo (pivote).
    ///  - Ratón: arrastrar con el botón izquierdo = mover; rueda = zoom; botón derecho = girar e inclinar.
    ///  - Teclado: WASD / flechas = mover; Q/E = girar; R/F = inclinar.
    ///  - Táctil: un dedo = mover; dos dedos = zoom (pellizco) y giro.
    /// </summary>
    [RequireComponent(typeof(Camera))]
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

        Camera cam;
        Vector3? sueloAnterior;
        static readonly Plane Suelo = new Plane(Vector3.up, Vector3.zero);

        void Awake() { cam = GetComponent<Camera>(); Aplicar(); }

        void LateUpdate()
        {
            if (Input.touchSupported && Input.touchCount > 0) Tactil();
            else Raton();
            Teclado();
            Aplicar();
        }

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
        void Tactil()
        {
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
            if (b.phase == TouchPhase.Began || a.phase == TouchPhase.Began) { distDedosAnterior = dist; anguloDedosAnterior = ang; return; }
            if (distDedosAnterior > 1f) distancia *= distDedosAnterior / Mathf.Max(1f, dist);
            giro -= Mathf.DeltaAngle(anguloDedosAnterior, ang);
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

        /// <summary>Centra la cámara en una coordenada geográfica.</summary>
        public void CentrarEn(double lat, double lon) { pivote = Geo.ALocal(lat, lon); Aplicar(); }
    }
}
