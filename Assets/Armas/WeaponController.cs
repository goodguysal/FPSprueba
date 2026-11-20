using UnityEngine;
using UnityEngine.UI;

public class WeaponController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camara;
    [SerializeField] private Slider barraCarga;
    [SerializeField] private AudioClip sonidoDisparo;
    [SerializeField] private GameObject efectoImpacto;

    [Header("Disparo")]
    [SerializeField] private float distancia = 100f;
    [SerializeField] private float danio = 20f;
    [SerializeField] private float tiempoEntreDisparos = 0.25f;

    [Header("Cargador")]
    [SerializeField] private int balasPorCargador = 6;
    [SerializeField] private float tiempoRecarga = 1.5f;

    [Header("Retroceso")]
    [SerializeField] private float fuerzaRetroceso = 0.06f;
    [SerializeField] private float velocidadRetorno = 12f;

    private int balasRestantes;
    private float siguienteDisparo;
    private float progresoRecarga;
    private bool recargando;
    private Vector3 posicionInicial;
    private float retrocesoActual;

    private void Start()
    {
        posicionInicial = transform.localPosition;
        balasRestantes = balasPorCargador;

        if (camara == null)
            camara = Camera.main;

        ActualizarBarra();
    }

    private void Update()
    {
        ActualizarRecarga();
        ActualizarRetroceso();

        // GetButtonDown registra solo el inicio del clic, no cada frame que se mantiene pulsado.
        if (!recargando &&
            Input.GetButtonDown("Fire1") &&
            Time.time >= siguienteDisparo)
        {
            Disparar();
        }

        if (!recargando &&
            Input.GetKeyDown(KeyCode.R) &&
            balasRestantes < balasPorCargador)
        {
            IniciarRecarga();
        }
    }

    private void Disparar()
    {
        if (balasRestantes <= 0)
        {
            IniciarRecarga();
            return;
        }

        if (camara == null)
        {
            Debug.LogError("WeaponController: asigna la cámara en el Inspector.", this);
            return;
        }

        balasRestantes--;
        siguienteDisparo = Time.time + tiempoEntreDisparos;
        retrocesoActual = 1f;
        ActualizarBarra();

        if (sonidoDisparo != null)
            AudioSource.PlayClipAtPoint(sonidoDisparo, transform.position);
        else
            Debug.LogWarning("WeaponController: asigna el clip Sonido Disparo.", this);

        // Este raycast ocurre únicamente una vez por clic.
        if (Physics.Raycast(
            camara.transform.position,
            camara.transform.forward,
            out RaycastHit impacto,
            distancia))
        {
            if (efectoImpacto != null)
            {
                Instantiate(
                    efectoImpacto,
                    impacto.point,
                    Quaternion.LookRotation(impacto.normal));
            }

            impacto.collider.SendMessage(
                "TakeDamage",
                danio,
                SendMessageOptions.DontRequireReceiver);
        }

        if (balasRestantes <= 0)
            IniciarRecarga();
    }

    private void IniciarRecarga()
    {
        if (recargando || balasRestantes >= balasPorCargador)
            return;

        recargando = true;
        progresoRecarga = 0f;
    }

    private void ActualizarRecarga()
    {
        if (!recargando)
            return;

        progresoRecarga += tiempoRecarga > 0f
            ? Time.deltaTime / tiempoRecarga
            : 1f;

        float progreso = Mathf.Clamp01(progresoRecarga);

        if (barraCarga != null)
        {
            barraCarga.maxValue = balasPorCargador;
            barraCarga.value = Mathf.Lerp(balasRestantes, balasPorCargador, progreso);
        }

        if (progreso >= 1f)
        {
            balasRestantes = balasPorCargador;
            recargando = false;
            ActualizarBarra();
        }
    }

    private void ActualizarBarra()
    {
        if (barraCarga == null)
            return;

        barraCarga.maxValue = balasPorCargador;
        barraCarga.value = balasRestantes;
    }

    private void ActualizarRetroceso()
    {
        retrocesoActual = Mathf.MoveTowards(
            retrocesoActual,
            0f,
            velocidadRetorno * Time.deltaTime);

        Vector3 posicionObjetivo =
            posicionInicial + Vector3.back * (fuerzaRetroceso * retrocesoActual);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            posicionObjetivo,
            velocidadRetorno * Time.deltaTime);
    }
}
