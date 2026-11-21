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
            Disparar();   }
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
            AudioSource.PlayClipAtPoint(
                sonidoDisparo,
                puntoSalida.position);

        cohetesEnCargador--;
        siguienteDisparo = Time.time + tiempoEntreDisparos;
        ActualizarUI();

        if (cohetesEnCargador <= 0)
            IniciarRecarga();
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
        {     recargando = false;
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
            textoMunicion.text =
                $"{cohetesEnCargador} / {cohetesDeReserva}";
    }
}
