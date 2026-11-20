using UnityEngine;
using UnityEngine.UI;

public class RocketLauncher : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform puntoSalida;
    [SerializeField] private GameObject prefabCohete;
    [SerializeField] private Slider barraCarga;
    [SerializeField] private AudioClip sonidoDisparo;

    [Header("Disparo")]
    [SerializeField] private float velocidadCohete = 25f;
    [SerializeField] private float distanciaMaxima = 80f;
    [SerializeField] private float tiempoEntreDisparos = 0.8f;

    [Header("Cargador y recarga")]
    [SerializeField] private int cohetesPorCargador = 4;
    [SerializeField] private float tiempoRecarga = 2f;

    [Header("Explosión")]
    [SerializeField] private float radioExplosion = 5f;
    [SerializeField] private float danioMaximo = 100f;

    private int cohetesRestantes;
    private float siguienteDisparo;
    private float progresoRecarga;
    private bool recargando;

    private void Start()
    {
        cohetesRestantes = cohetesPorCargador;
        ActualizarBarra();
    }

    private void Update()
    {
        ActualizarRecarga();

        if (recargando)
            return;

        if (Input.GetKeyDown(KeyCode.R) &&
            cohetesRestantes < cohetesPorCargador)
        {
            IniciarRecarga();
            return;
        }

        // Dispara una sola vez por clic, no mientras se mantiene pulsado.
        if (Input.GetButtonDown("Fire1") && Time.time >= siguienteDisparo)
            Disparar();
    }

    private void Disparar()
    {
        if (cohetesRestantes <= 0)
        {
            IniciarRecarga();
            return;
        }

        if (puntoSalida == null)
        {
            Debug.LogError("RocketLauncher: asigna Punto Salida en el Inspector.", this);
            return;
        }

        if (prefabCohete == null)
        {
            Debug.LogError("RocketLauncher: asigna Prefab Cohete en el Inspector.", this);
            return;
        }

        GameObject nuevoCohete = Instantiate(
            prefabCohete,
            puntoSalida.position,
            puntoSalida.rotation);

        RocketProjectile proyectil =
            nuevoCohete.GetComponent<RocketProjectile>();

        if (proyectil == null)
        {
            Debug.LogError(
                "El prefab del cohete necesita el componente RocketProjectile.",
                nuevoCohete);
            Destroy(nuevoCohete);
            return;
        }

        proyectil.Configurar(
            velocidadCohete,
            distanciaMaxima,
            radioExplosion,
            danioMaximo,
            transform.root.gameObject);

        if (sonidoDisparo != null)
            AudioSource.PlayClipAtPoint(sonidoDisparo, transform.position, 3f);
        else
            Debug.LogWarning("RocketLauncher: asigna Sonido Disparo en el Inspector.", this);

        cohetesRestantes--;
        siguienteDisparo = Time.time + tiempoEntreDisparos;
        ActualizarBarra();

        if (cohetesRestantes <= 0)
            IniciarRecarga();
    }

    private void IniciarRecarga()
    {
        if (recargando || cohetesRestantes >= cohetesPorCargador)
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
            barraCarga.maxValue = cohetesPorCargador;
            barraCarga.value = Mathf.Lerp(
                cohetesRestantes,
                cohetesPorCargador,
                progreso);
        }

        if (progreso >= 1f)
        {
            cohetesRestantes = cohetesPorCargador;
            recargando = false;
            ActualizarBarra();
        }
    }

    private void ActualizarBarra()
    {
        if (barraCarga == null)
            return;

        barraCarga.maxValue = cohetesPorCargador;
        barraCarga.value = cohetesRestantes;
    }
}
