using UnityEngine;

namespace PrecioLocal
{
    public enum TipoMarcador { Posicion, Servicio, Hosteleria, Ocio }

    /// <summary>
    /// Coloca el objeto en una latitud/longitud real y mantiene su etiqueta mirando a la cámara.
    /// Los colores siguen el prototipo web: servicio local azul, hostelería naranja, ocio verde.
    /// </summary>
    [ExecuteAlways]
    public class Marcador : MonoBehaviour
    {
        public string nombre = "Sitio";
        public TipoMarcador tipo = TipoMarcador.Servicio;
        public double latitud = 36.7779;
        public double longitud = -6.3528;
        public Transform etiqueta;            // texto que mira siempre a la cámara
        [Tooltip("Tamaño en pantalla aproximadamente constante al hacer zoom")]
        public bool escalarConDistancia = true;
        public float escalaBase = 1f;
        [Tooltip("Desactivado en la escena de Cesium: allí lo coloca su CesiumGlobeAnchor sobre el globo real")]
        public bool colocarConGeo = true;

        public static Color ColorDe(TipoMarcador t) => t switch
        {
            TipoMarcador.Posicion => new Color32(0x2F, 0x5B, 0xEA, 0xFF),
            TipoMarcador.Servicio => new Color32(0x2F, 0x5B, 0xEA, 0xFF),
            TipoMarcador.Hosteleria => new Color32(0xE0, 0x7B, 0x12, 0xFF),
            _ => new Color32(0x1F, 0x9D, 0x6B, 0xFF),
        };

        void Start() => Colocar();
        void OnValidate() => Colocar();

        public void Colocar() { if (colocarConGeo) transform.position = Geo.ALocal(latitud, longitud); }

        void LateUpdate()
        {
            var cam = Camera.main; if (cam == null) return;
            if (escalarConDistancia)
            {
                float d = Vector3.Distance(cam.transform.position, transform.position);
                transform.localScale = Vector3.one * escalaBase * Mathf.Clamp(d / 900f, 0.15f, 4f);
            }
            if (etiqueta != null) etiqueta.rotation = Quaternion.LookRotation(etiqueta.position - cam.transform.position);
        }
    }
}
