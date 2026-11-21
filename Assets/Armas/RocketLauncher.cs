using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RocketLauncher : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Transform puntoSalida;
    [SerializeField] private GameObject prefabCohete;
    [SerializeField] private Slider barraCarga;
    [SerializeField] private TMP_Text textoMunicion;
    [SerializeField] private AudioClip sonidoDisparo;

    [Header("Disparo")]
    [SerializeField] private float velocidadCohete = 25f;
    [SerializeField] private float distanciaMaxima = 80f;
    [SerializeField] private float tiempoEntreDisparos = 0.8f;

    [Header("Munición")]
    [SerializeField] private int cohetesPorCargador = 4;
    [SerializeField] private int reservaInicial = 8;
    [SerializeField] private int reservaMaxima = 20;
    [SerializeField] private float tiempoRecarga = 2f;

    [Header("Explosión")]
    [SerializeField] private float radioExplosion = 5f;
    [SerializeField] private float danioMaximo = 80f;

    [Header("Retroceso")]
    [SerializeField] private float fuerzaRetroceso = 0.12f;
    [SerializeField] private float anguloRetroceso = 7f;
    [SerializeField] private float velocidadRetorno = 18f;

    private Vector3 posicionInicialArma;
    private Quaternion rotacionInicialArma;
    private float retrocesoActual;

    private int cohetesEnCargador;
    private int cohetesDeReserva;
    private int cohetesARecargar;
    private int cohetesTransferidos;

    private float siguienteDisparo;
    private float progresoRecarga;
    private bool recargando;

    private void Start()
    {
        cohetesEnCargador = cohetesPorCargador;
        cohetesDeReserva = Mathf.Clamp(reservaInicial, 0, reservaMaxima);

        posicionInicialArma = transform.localPosition;
        rotacionInicialArma = transform.localRotation;

        ActualizarUI();
    }

    private void Update()
    {
        ActualizarRecarga();

        if (Input.GetKeyDown(KeyCode.R))
            IniciarRecarga();

        if (!recargando &&
            Input.GetMouseButtonDown(0) &&
            Time.time >= siguienteDisparo)
        {
            Disparar();
        }

        ActualizarRetroceso();
    }

    private void Disparar()
    {
        if (recargando || cohetesEnCargador <= 0)
        {
            if (cohetesEnCargador <= 0)
                IniciarRecarga();

            return;
        }

        if (puntoSalida == null || prefabCohete == null)
        {
            Debug.LogError(
                "RocketLauncher: asigna Punto Salida y Prefab Cohete.",
                this);
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
                "El prefab necesita el componente RocketProjectile.",
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
        {
            AudioSource.PlayClipAtPoint(
                sonidoDisparo,
                puntoSalida.position);
        }

        cohetesEnCargador--;
        siguienteDisparo = Time.time + tiempoEntreDisparos;

        // Activa la animación únicamente después de un disparo válido.
        retrocesoActual = 1f;

        ActualizarUI();

        if (cohetesEnCargador <= 0)
            IniciarRecarga();
    }

    private void ActualizarRetroceso()
    {
        if (retrocesoActual <= 0f)
        {
            retrocesoActual = 0f;
        }
        else
        {
            retrocesoActual = Mathf.MoveTowards(
                retrocesoActual,
                0f,
                velocidadRetorno * Time.deltaTime);
        }

        Vector3 posicionObjetivo =
            posicionInicialArma +
            Vector3.back * (fuerzaRetroceso * retrocesoActual);

        Quaternion rotacionObjetivo =
            rotacionInicialArma *
            Quaternion.Euler(-anguloRetroceso * retrocesoActual, 0f, 0f);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            posicionObjetivo,
            velocidadRetorno * Time.deltaTime);

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            rotacionObjetivo,
            velocidadRetorno * Time.deltaTime);
    }

    private void IniciarRecarga()
    {
        if (recargando ||
            cohetesEnCargador >= cohetesPorCargador ||
            cohetesDeReserva <= 0)
        {
            return;
        }

        cohetesARecargar = Mathf.Min(
            cohetesPorCargador - cohetesEnCargador,
            cohetesDeReserva);

        cohetesTransferidos = 0;
        progresoRecarga = 0f;
        recargando = true;
    }

    private void ActualizarRecarga()
    {
        if (!recargando)
            return;

        progresoRecarga += tiempoRecarga > 0f
            ? Time.deltaTime / tiempoRecarga
            : 1f;

        float progreso = Mathf.Clamp01(progresoRecarga);

        int transferidosAhora =
            Mathf.FloorToInt(cohetesARecargar * progreso);

        int nuevosCohetes = transferidosAhora - cohetesTransferidos;

        if (nuevosCohetes > 0)
        {
            cohetesEnCargador += nuevosCohetes;
            cohetesDeReserva -= nuevosCohetes;
            cohetesTransferidos = transferidosAhora;
        }

        if (barraCarga != null)
        {
            barraCarga.maxValue = cohetesPorCargador;
            barraCarga.value = Mathf.Min(
                cohetesPorCargador,
                cohetesEnCargador +
                (cohetesARecargar * progreso - cohetesTransferidos));
        }

        ActualizarTextoMunicion();

        if (progreso >= 1f)
        {
            recargando = false;
            ActualizarUI();
        }
    }

    public bool AgregarMunicion(int cantidad)
    {
        if (cantidad <= 0 || cohetesDeReserva >= reservaMaxima)
            return false;

        int reservaAnterior = cohetesDeReserva;

        cohetesDeReserva = Mathf.Min(
            reservaMaxima,
            cohetesDeReserva + cantidad);

        ActualizarUI();
        return cohetesDeReserva > reservaAnterior;
    }

    private void ActualizarUI()
    {
        if (barraCarga != null)
        {
            barraCarga.maxValue = cohetesPorCargador;
            barraCarga.value = cohetesEnCargador;
        }

        ActualizarTextoMunicion();
    }

    private void ActualizarTextoMunicion()
    {
        if (textoMunicion != null)
        {
            textoMunicion.text =
                $"{cohetesEnCargador} / {cohetesDeReserva}";
        }
    }
}
