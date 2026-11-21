using UnityEngine;
using UnityEngine.UI;

public enum EstadoEnemigo
{
    Patrullando,
    Alerta,
    Persiguiendo
}

[RequireComponent(typeof(Rigidbody))]
public class Enemigo : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    [SerializeField] private Slider barraVida;

    [Header("Patrulla")]
    [SerializeField] private Transform[] puntosPatrulla;
    [SerializeField] private float velocidadPatrulla = 2f;
    [SerializeField] private float distanciaLlegada = 0.3f;
    [SerializeField] private float tiempoEspera = 2f;

    [Header("Jugador y detección")]
    [SerializeField] private Transform jugador;
    [SerializeField] private float radioAlerta = 8f;
    [SerializeField] private float radioPersecucion = 12f;
    [SerializeField] private float anguloVision = 45f;
    [SerializeField] private float distanciaDeteccionCercana = 1.5f;
    [SerializeField] private float tiempoAlerta = 2f;
    [SerializeField] private float alturaOjos = 1.4f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 4f;
    [SerializeField] private float distanciaAtaque = 7f;
    [SerializeField] private float velocidadGiro = 6f;

    [Header("Arma invisible")]
    [SerializeField] private float distanciaDisparo = 20f;
    [SerializeField] private float danioJugador = 10f;
    [SerializeField] private float segundosEntreDisparos = 0.8f;
    [SerializeField] private int balasPorCargador = 5;
    [SerializeField] private float segundosRecarga = 2f;

    [SerializeField]
    private EstadoEnemigo estadoActual =
        EstadoEnemigo.Patrullando;

    private Rigidbody cuerpo;
    private float vidaActual;
    private int puntoActual;
    private int balasRestantes;

    private float esperaRestante;
    private float alertaRestante;
    private float siguienteDisparo;
    private float finRecarga;

    private bool esperando;
    private bool recargando;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.constraints = RigidbodyConstraints.FreezeRotation;
        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;

        vidaActual = vidaMaxima;
        balasRestantes = balasPorCargador;

        if (barraVida != null)
        {
            barraVida.maxValue = vidaMaxima;
            barraVida.value = vidaActual;
        }
    }

    private void Update()
    {
        if (jugador == null)
            return;

        ActualizarEstado();

        if (estadoActual == EstadoEnemigo.Persiguiendo)
            IntentarDisparar();
    }

    private void FixedUpdate()
    {
        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                Patrullar();
                break;

            case EstadoEnemigo.Alerta:
                if (jugador != null)
                    GirarHacia(jugador.position);
                break;

            case EstadoEnemigo.Persiguiendo:
                if (jugador != null)
                    Perseguir();
                break;
        }
    }

    private void ActualizarEstado()
    {
        float distancia = Vector3.Distance(transform.position, jugador.position);
        bool visible = distancia <= radioAlerta && JugadorVisible();

        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                if (visible)
                {
                    estadoActual = EstadoEnemigo.Alerta;
                    alertaRestante = tiempoAlerta;
                }
                break;

            case EstadoEnemigo.Alerta:
                if (visible)
                {
                    estadoActual = EstadoEnemigo.Persiguiendo;
                }
                else
                {
                    alertaRestante -= Time.deltaTime;

                    if (alertaRestante <= 0f)
                        estadoActual = EstadoEnemigo.Patrullando;
                }
                break;

            case EstadoEnemigo.Persiguiendo:
                if (distancia > radioPersecucion || !JugadorVisible())
                {
                    estadoActual = EstadoEnemigo.Alerta;
                    alertaRestante = tiempoAlerta;
                }
                break;
        }
    }

    private bool JugadorVisible()
    {
        Vector3 origen = transform.position + Vector3.up * alturaOjos;
        Vector3 destino = jugador.position + Vector3.up;
        Vector3 direccion = destino - origen;
        float distancia = direccion.magnitude;

        if (distancia <= 0.01f)
            return true;

        if (distancia > distanciaDeteccionCercana &&
            Vector3.Angle(transform.forward, direccion) > anguloVision)
        {
            return false;
        }

        RaycastHit[] impactos = Physics.RaycastAll(
            origen,
            direccion.normalized,
            distancia,
            ~0,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(
            impactos,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit impacto in impactos)
        {
            // Ignora los colliders del propio enemigo.
            if (impacto.transform.root == transform.root)
                continue;

            // El primer objeto alcanzado debe pertenecer al jugador.
            return impacto.transform.root == jugador.root;
        }

        return false;
    }

    private void Patrullar()
    {
        if (puntosPatrulla == null || puntosPatrulla.Length == 0)
            return;

        if (esperando)
        {
            esperaRestante -= Time.fixedDeltaTime;

            if (esperaRestante <= 0f)
            {
                esperando = false;
                puntoActual = (puntoActual + 1) % puntosPatrulla.Length;
            }

            return;
        }

        Transform objetivo = puntosPatrulla[puntoActual];

        if (objetivo == null)
        {
            puntoActual = (puntoActual + 1) % puntosPatrulla.Length;
            return;
        }

        Vector3 diferencia = objetivo.position - transform.position;
        diferencia.y = 0f;

        if (diferencia.magnitude <= distanciaLlegada)
        {
            esperando = true;
            esperaRestante = tiempoEspera;
            return;
        }

        MoverHacia(diferencia, velocidadPatrulla);
    }

    private void Perseguir()
    {
        Vector3 diferencia = jugador.position - transform.position;
        diferencia.y = 0f;

        GirarHacia(jugador.position);

        if (diferencia.magnitude > distanciaAtaque)
            MoverHacia(diferencia, velocidadPersecucion);
    }

    private void MoverHacia(Vector3 diferencia, float velocidad)
    {
        if (diferencia.sqrMagnitude < 0.001f)
            return;

        GirarHacia(transform.position + diferencia);

        Vector3 avance =
            diferencia.normalized * velocidad * Time.fixedDeltaTime;

        cuerpo.MovePosition(cuerpo.position + avance);
    }

    private void GirarHacia(Vector3 objetivo)
    {
        Vector3 direccion = objetivo - transform.position;
        direccion.y = 0f;

        if (direccion.sqrMagnitude < 0.001f)
            return;

        Quaternion rotacionObjetivo =
            Quaternion.LookRotation(direccion.normalized);

        cuerpo.MoveRotation(Quaternion.Slerp(
            cuerpo.rotation,
            rotacionObjetivo,
            velocidadGiro * Time.fixedDeltaTime));
    }

    private void IntentarDisparar()
    {
        float distancia = Vector3.Distance(transform.position, jugador.position);

        if (distancia > distanciaAtaque || !JugadorVisible())
            return;

        if (recargando)
        {
            if (Time.time >= finRecarga)
            {
                balasRestantes = balasPorCargador;
                recargando = false;
            }

            return;
        }

        if (Time.time < siguienteDisparo)
            return;

        if (balasRestantes <= 0)
        {
            recargando = true;
            finRecarga = Time.time + segundosRecarga;
            return;
        }

        balasRestantes--;
        siguienteDisparo = Time.time + segundosEntreDisparos;

        Vector3 origen = transform.position + Vector3.up * alturaOjos;
        Vector3 destino = jugador.position + Vector3.up;
        Vector3 direccion = (destino - origen).normalized;

        if (Physics.Raycast(
            origen,
            direccion,
            out RaycastHit impacto,
            distanciaDisparo,
            ~0,
            QueryTriggerInteraction.Ignore))
        {
            PlayerHealth vidaJugador =
                impacto.collider.GetComponentInParent<PlayerHealth>();

            if (vidaJugador != null)
                vidaJugador.TakeDamage(danioJugador);
        }
    }

    public void RecibirDanio(float cantidad)
    {
        if (cantidad <= 0f || vidaActual <= 0f)
            return;

        vidaActual = Mathf.Max(0f, vidaActual - cantidad);

        if (barraVida != null)
            barraVida.value = vidaActual;

        if (vidaActual <= 0f)
            Destroy(gameObject);
    }
}
