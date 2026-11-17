using UnityEngine;

namespace PriceMap.Vectorial
{
    /// <summary>Tipo de marcador 3D de la escena. Los lugares los dibuja la interfaz (CapaMarcadores); aquí solo queda "Tu posición".</summary>
    public enum TipoMarcador { Posicion }

    /// <summary>
    /// Coloca el objeto en una latitud/longitud real y mantiene su etiqueta mirando a la cámara.
    /// Solo lo usa "Tu posición" (azul del prototipo web) hasta la historia 1.7.
    /// </summary>
    [ExecuteAlways]
    public class Marcador : MonoBehaviour
    {
        public string nombre = "Sitio";
        public TipoMarcador tipo = TipoMarcador.Posicion;
        public double latitud = 36.7779;
        public double longitud = -6.3528;
        public Transform etiqueta;            // texto que mira siempre a la cámara
        [Tooltip("Tamaño en pantalla aproximadamente constante al hacer zoom")]
        public bool escalarConDistancia = true;
        public float escalaBase = 1f;

        public static Color ColorDe(TipoMarcador t) => new Color32(0x2F, 0x5B, 0xEA, 0xFF);

        void Start() => Colocar();
        void OnValidate() => Colocar();

        public void Colocar() => transform.position = Geo.ALocal(latitud, longitud);

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
