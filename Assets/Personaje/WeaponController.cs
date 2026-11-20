using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camara;
    [SerializeField] private Slider barraCarga;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip sonidoDisparo;

    [Header("Disparo y recarga")]
    [SerializeField] private float distancia = 100f;
    [SerializeField] private float tiempoEntreDisparos = 0.25f;
    [SerializeField] private int disparosPorCargador = 12;
    [SerializeField] private float tiempoRecarga = 1.5f;
    [SerializeField] private float danio = 20f;
    [SerializeField] private float retrasoInclinacionRecarga = 0.2f;
    private float tiempoInicioRecarga;

    [Header("Animación")]
    [SerializeField] private float retrocesoPosicion = 0.08f;
    [SerializeField] private float retrocesoRotacion = 5f;
    [SerializeField] private float inclinacionRecarga = 15f;
    [SerializeField] private float velocidadAnimacion = 12f;

    private float disparosRestantes;
    private float proximoDisparo;
    private float tiempoRecargaTranscurrido;
    private bool recargando;

    private Vector3 posicionInicial;
    private Quaternion rotacionInicial;
    private float retrocesoActual;

    private void Start()
    {
        posicionInicial = transform.localPosition;
        rotacionInicial = transform.localRotation;
        disparosRestantes = disparosPorCargador;

        if (camara == null)
            camara = Camera.main;

        ActualizarBarra();
    }

    private void Update()
    {
        ActualizarRecarga();
        AnimarArma();

        if (recargando)
            return;

        if (Input.GetKeyDown(KeyCode.R) && disparosRestantes < disparosPorCargador)
        {
            IniciarRecarga();
            return;
        }

        if (Input.GetButton("Fire1") && Time.time >= proximoDisparo)
            Disparar();
    }

    private void Disparar()
    {
        if (disparosRestantes < 1f)
        {
            IniciarRecarga();
            return;
        }

        proximoDisparo = Time.time + tiempoEntreDisparos;
        disparosRestantes -= 1f;
        retrocesoActual = 1f;
        ActualizarBarra();

        if (audioSource != null && sonidoDisparo != null)
            audioSource.PlayOneShot(sonidoDisparo);

        if (camara != null &&
            Physics.Raycast(
                camara.transform.position,
                camara.transform.forward,
                out RaycastHit impacto,
                distancia))
        {
            impacto.collider.SendMessage(
                "TakeDamage",
                danio,
                SendMessageOptions.DontRequireReceiver);
        }

        if (disparosRestantes <= 0f)
            IniciarRecarga();
    }

    private void IniciarRecarga()
    {
        if (recargando || disparosRestantes >= disparosPorCargador)
            return;

        recargando = true;
        tiempoRecargaTranscurrido = 0f;
        tiempoInicioRecarga = Time.time;
    }

    private void ActualizarRecarga()
    {
        if (!recargando)
            return;

        tiempoRecargaTranscurrido += Time.deltaTime;

        float progreso = tiempoRecarga > 0f
            ? Mathf.Clamp01(tiempoRecargaTranscurrido / tiempoRecarga)
            : 1f;

        // La barra y la munición aumentan gradualmente durante la recarga.
        disparosRestantes = Mathf.Lerp(0f, disparosPorCargador, progreso);
        ActualizarBarra();

        if (progreso >= 1f)
        {
            disparosRestantes = disparosPorCargador;
            recargando = false;
            ActualizarBarra();
        }
    }

    private void ActualizarBarra()
    {
        if (barraCarga == null)
            return;

        barraCarga.maxValue = disparosPorCargador;
        barraCarga.value = disparosRestantes;
    }

    private void AnimarArma()
    {
        retrocesoActual = Mathf.MoveTowards(
            retrocesoActual, 0f, velocidadAnimacion * Time.deltaTime);

        Vector3 posicionObjetivo = posicionInicial
            + Vector3.back * (retrocesoPosicion * retrocesoActual);

        bool inclinando = recargando &&
     Time.time >= tiempoInicioRecarga + retrasoInclinacionRecarga;

        Quaternion inclinacion = inclinando
            ? Quaternion.Euler(0f, 0f, -inclinacionRecarga)
            : Quaternion.identity;

        // Inclina el arma durante la recarga y la devuelve a su pose al terminar.
        Quaternion rotacionObjetivo =
            rotacionInicial
            * inclinacion
            * Quaternion.Euler(-retrocesoRotacion * retrocesoActual, 0f, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            posicionObjetivo,
            velocidadAnimacion * Time.deltaTime);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            rotacionObjetivo,
            velocidadAnimacion * Time.deltaTime);
    }
}
