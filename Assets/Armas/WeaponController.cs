
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponController : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private Camera camara;
    [SerializeField] private Slider barraCarga;
    [SerializeField] private TMP_Text textoMunicion;
    [SerializeField] private AudioClip sonidoDisparo;
    [SerializeField] private GameObject efectoImpacto;

    [Header("Capas del raycast")]
    [SerializeField] private LayerMask capasDisparoRevolver = ~0;

    [Header("Disparo")]
    [SerializeField] private float distancia = 100f;
    [SerializeField] private float danio = 20f;
    [SerializeField] private float tiempoEntreDisparos = 0.25f;

    [Header("Munición")]
    [SerializeField] private int balasPorCargador = 6;
    [SerializeField] private int reservaInicial = 24;
    [SerializeField] private int reservaMaxima = 60;
    [SerializeField] private float tiempoRecarga = 1.5f;

    [Header("Retroceso")]
    [SerializeField] private float fuerzaRetroceso = 0.06f;
    [SerializeField] private float velocidadRetorno = 12f;
    
    [Header("Inclinación al recargar")]
    [SerializeField] private float inclinacionRecarga = 12f;
    [SerializeField] private float velocidadInclinacion = 10f;

    private Quaternion rotacionInicial;
    private int balasEnCargador;
    private int balasDeReserva;
    private int balasARecargar;
    private int balasTransferidas;

    private float siguienteDisparo;
    private float progresoRecarga;
    private bool recargando;

    private Vector3 posicionInicial;
    private float retrocesoActual;

    private void Start()
    {
        posicionInicial = transform.localPosition;
        balasEnCargador = balasPorCargador;
        balasDeReserva = Mathf.Clamp(reservaInicial, 0, reservaMaxima);
        rotacionInicial = transform.localRotation;

        if (camara == null)
            camara = Camera.main;

        if (camara == null)
            Debug.LogError("WeaponController: asigna la cámara en el Inspector.", this);

        ActualizarUI();
    }

    private void Update()
    {
        ActualizarRecarga();
        ActualizarRetroceso();
        ActualizarInclinacionRecarga();

        if (Input.GetKeyDown(KeyCode.R))
            IniciarRecarga();

        if (!recargando &&
            Input.GetMouseButtonDown(0) &&
            Time.time >= siguienteDisparo)
        {
            Disparar();
        } }
    private void Disparar()
    {
        if (recargando || balasEnCargador <= 0)
        {
            if (balasEnCargador <= 0)
                IniciarRecarga();
            return;
        }

        if (camara == null)     return;

        balasEnCargador--;
        siguienteDisparo = Time.time + tiempoEntreDisparos;
        retrocesoActual = 1f;
        ActualizarUI();

        if (sonidoDisparo != null)
            AudioSource.PlayClipAtPoint(sonidoDisparo, transform.position);  if (Physics.Raycast(
            camara.transform.position,
            camara.transform.forward,
            out RaycastHit impacto,
            distancia,
            capasDisparoRevolver,
            QueryTriggerInteraction.Ignore))
        {
            if (efectoImpacto != null)
            {
                Instantiate(
                    efectoImpacto,
                    impacto.point,
                    Quaternion.LookRotation(impacto.normal));
            }

            Enemigo enemigo =
                impacto.collider.GetComponentInParent<Enemigo>();

            if (enemigo != null)
                enemigo.RecibirDanio(danio, transform.root);
            else
                impacto.collider.SendMessage(
                    "TakeDamage",
                    danio,
                    SendMessageOptions.DontRequireReceiver);
        }

        if (balasEnCargador <= 0)
            IniciarRecarga();
    }

    private void IniciarRecarga()
    {
        if (recargando ||
            balasEnCargador >= balasPorCargador ||
            balasDeReserva <= 0)
        {
            return;
        }

        balasARecargar = Mathf.Min(
            balasPorCargador - balasEnCargador,
            balasDeReserva);

        balasTransferidas = 0;
        progresoRecarga = 0f;
        recargando = true;}

    private void ActualizarRecarga()
    {
        if (!recargando)
            return;

        progresoRecarga += tiempoRecarga > 0f
            ? Time.deltaTime / tiempoRecarga
            : 1f;

        float progreso = Mathf.Clamp01(progresoRecarga);

        int transferidasAhora =
            Mathf.FloorToInt(balasARecargar * progreso);

        int nuevasBalas = transferidasAhora - balasTransferidas;

        if (nuevasBalas > 0)
        {
            balasEnCargador += nuevasBalas;
            balasDeReserva -= nuevasBalas;
            balasTransferidas = transferidasAhora;
        }

        if (barraCarga != null)
        {
            barraCarga.maxValue = balasPorCargador;
            barraCarga.value = Mathf.Min(
                balasPorCargador,
                balasEnCargador +
                (balasARecargar * progreso - balasTransferidas));
        }

        ActualizarTextoMunicion();

        if (progreso >= 1f)
        {        recargando = false;ActualizarUI();
        }
    }

    public bool AgregarMunicion(int cantidad)
    {
        if (cantidad <= 0 || balasDeReserva >= reservaMaxima)
            return false;

        int reservaAnterior = balasDeReserva;
        balasDeReserva = Mathf.Min(
            reservaMaxima,
            balasDeReserva + cantidad);

        ActualizarUI();
        return balasDeReserva > reservaAnterior;
    }

    private void ActualizarUI()
    {
        if (barraCarga != null)
        {
            barraCarga.maxValue = balasPorCargador;
            barraCarga.value = balasEnCargador;
        }

        ActualizarTextoMunicion();
    }

    private void ActualizarTextoMunicion()
    {
        if (textoMunicion != null)
            textoMunicion.text = $"{balasEnCargador} / {balasDeReserva}";
    }

    private void ActualizarRetroceso()
    {
        retrocesoActual = Mathf.MoveTowards(
            retrocesoActual,
            0f,
            velocidadRetorno * Time.deltaTime);

        Vector3 posicionObjetivo =
            posicionInicial +
            Vector3.back * (fuerzaRetroceso * retrocesoActual);

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            posicionObjetivo,
            velocidadRetorno * Time.deltaTime);

    }
    private void ActualizarInclinacionRecarga()
    {
        Quaternion rotacionObjetivo = recargando
            ? rotacionInicial * Quaternion.Euler(0f, 0f, -inclinacionRecarga)
            : rotacionInicial;

        transform.localRotation = Quaternion.Slerp(
            transform.localRotation,
            rotacionObjetivo,
            velocidadInclinacion * Time.deltaTime);
    }

}