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
    [SerializeField] private float radioAlerta = 18f;
    [SerializeField] private float radioPersecucion = 25f;
    [SerializeField] private float anguloVision = 60f;
    [SerializeField] private float distanciaDeteccionCercana = 3f;
    [SerializeField] private float tiempoAlerta = 3f;
    [SerializeField] private float alturaOjos = 1.4f;

    [Header("Persecución")]
    [SerializeField] private float velocidadPersecucion = 4f;
    [SerializeField] private float distanciaAtaque = 7f;
    [SerializeField] private float velocidadGiro = 6f;

    [Header("Arma invisible")]
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private float distanciaDisparo = 60f;
    [SerializeField] private float danioJugador = 10f;
    [SerializeField] private float segundosEntreDisparos = 0.8f;
    [SerializeField] private int balasPorCargador = 5;
    [SerializeField] private float segundosRecarga = 2f;
    [SerializeField] private float retrasoPrimerDisparo = 1.5f;

    [Header("Sonido")]
    [SerializeField] private GameObject prefabSonidoDisparo;
    [SerializeField, Range(0f, 1f)] private float volumenDisparo = 1f;

    [Header("Efectos visuales")]
    [SerializeField] private GameObject efectoBocaArma;
    [SerializeField] private GameObject efectoImpactoEnemigo;
    [SerializeField] private float duracionEfectos = 2f;

    [Header("Animación del arma")]
    [SerializeField] private Transform armaVisual;
    [SerializeField] private float distanciaRetroceso = 0.05f;
    [SerializeField] private float anguloRetroceso = 4f;
    [SerializeField] private float velocidadRetornoArma = 12f;

    [Header("Estado")]
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
    private float retrocesoActual;

    private bool esperando;
    private bool recargando;

    private Vector3 posicionInicialArma;
    private Quaternion rotacionInicialArma;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();

        // Mantiene el enemigo erguido y le permite girar sobre el eje Y.
        cuerpo.constraints =
            RigidbodyConstraints.FreezeRotationX |
            RigidbodyConstraints.FreezeRotationZ;

        cuerpo.interpolation = RigidbodyInterpolation.Interpolate;

        vidaActual = vidaMaxima;
        balasRestantes = balasPorCargador;

        ActualizarBarraVida();

        if (armaVisual != null)
        {
            posicionInicialArma = armaVisual.localPosition;
            rotacionInicialArma = armaVisual.localRotation;
        }
        siguienteDisparo = Time.time + retrasoPrimerDisparo;
    }

    private void Update()
    {
        ActualizarAnimacionArma();

        if (jugador == null)
            return;

        ActualizarEstado();

        // Puede disparar tanto al estar alerta como al perseguir,
        // pero solo si ve al jugador y está dentro del alcance.
        if (estadoActual == EstadoEnemigo.Alerta ||
            estadoActual == EstadoEnemigo.Persiguiendo)
        {
            IntentarDisparar();
        }
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
        bool dentroDelRadioAlerta = distancia <= radioAlerta;
        bool visible = JugadorVisible();

        switch (estadoActual)
        {
            case EstadoEnemigo.Patrullando:
                // Nota al jugador al acercarse, incluso si aún hay una pared.
                if (dentroDelRadioAlerta)
                {
                    estadoActual = EstadoEnemigo.Alerta;
                    alertaRestante = tiempoAlerta;
                }
                break;

            case EstadoEnemigo.Alerta:
                if (visible && distancia <= radioPersecucion)
                {
                    estadoActual = EstadoEnemigo.Persiguiendo;
                }
                else if (visible)
                {
                    // Si lo ve de lejos, permanece alerta y puede disparar.
                    alertaRestante = tiempoAlerta;
                }
                else if (!dentroDelRadioAlerta)
                {
                    alertaRestante -= Time.deltaTime;

                    if (alertaRestante <= 0f)
                        estadoActual = EstadoEnemigo.Patrullando;
                }
                break;

            case EstadoEnemigo.Persiguiendo:
                if (!visible || distancia > radioPersecucion)
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

        if (distancia > distanciaDisparo || !JugadorVisible())
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

        Vector3 origen = puntoDisparo != null
            ? puntoDisparo.position
            : transform.position + Vector3.up * alturaOjos;

        Vector3 destino = jugador.position + Vector3.up;
        Vector3 direccion = (destino - origen).normalized;

        ReproducirSonidoDisparo(origen);
        CrearEfecto(
            efectoBocaArma,
            origen,
            Quaternion.LookRotation(direccion));

        // Busca el primer impacto que no pertenezca al propio enemigo.
        RaycastHit[] impactos = Physics.RaycastAll(
            origen,
            direccion,
           
            distanciaDisparo,
            ~0,
            QueryTriggerInteraction.Ignore);

        System.Array.Sort(
            impactos,
            (a, b) => a.distance.CompareTo(b.distance));

        foreach (RaycastHit impacto in impactos)
        {
            if (impacto.transform.root == transform.root)
                continue;

            CrearEfecto(
                efectoImpactoEnemigo,
                impacto.point,
                Quaternion.LookRotation(impacto.normal));

            PlayerHealth vidaJugador =
                impacto.collider.GetComponentInParent<PlayerHealth>();

            if (vidaJugador != null)
                vidaJugador.TakeDamage(danioJugador);

            // Se detiene en el primer objeto alcanzado.
            break;
        }

        retrocesoActual = 1f;
    }

    private void ReproducirSonidoDisparo(Vector3 posicion)
    {
        if (prefabSonidoDisparo == null)
            return;

        GameObject instancia = Instantiate(
            prefabSonidoDisparo,
            posicion,
            Quaternion.identity);

        AudioSource fuente =
            instancia.GetComponentInChildren<AudioSource>();

        if (fuente == null)
        {
            Debug.LogError(
                "El prefab de sonido necesita un AudioSource.",
                instancia);

            Destroy(instancia);
            return;
        }

        // Desactiva Play On Awake en el prefab para que no suene dos veces.
        fuente.volume = volumenDisparo;
        fuente.Play();

        if (fuente.clip != null)
        {
            float duracion =
                fuente.clip.length /
                Mathf.Max(0.01f, Mathf.Abs(fuente.pitch));

            Destroy(instancia, duracion + 0.2f);
        }
        else
        {
            Debug.LogWarning(
                "El AudioSource del prefab no tiene un Audio Clip asignado.",
                instancia);

            Destroy(instancia, 5f);
        }
    }

    private void CrearEfecto(
        GameObject prefab,
        Vector3 posicion,
        Quaternion rotacion)
    {
        if (prefab == null)
            return;

        GameObject efecto = Instantiate(prefab, posicion, rotacion);
        Destroy(efecto, duracionEfectos);
    }

    private void ActualizarAnimacionArma()
    {
        if (armaVisual == null)
            return;

        retrocesoActual = Mathf.MoveTowards(
            retrocesoActual,
            0f,
            velocidadRetornoArma * Time.deltaTime);

        Vector3 posicionObjetivo =
            posicionInicialArma
            + Vector3.back * (distanciaRetroceso * retrocesoActual);

        Quaternion rotacionObjetivo =
            rotacionInicialArma
            * Quaternion.Euler(-anguloRetroceso * retrocesoActual, 0f, 0f);

        armaVisual.localPosition = Vector3.Lerp(
            armaVisual.localPosition,
            posicionObjetivo,
            velocidadRetornoArma * Time.deltaTime);

        armaVisual.localRotation = Quaternion.Slerp(
            armaVisual.localRotation,
            rotacionObjetivo,
            velocidadRetornoArma * Time.deltaTime);
    }

    public void RecibirDanio(float cantidad)
    {
        RecibirDanio(cantidad, null);
    }

    public void RecibirDanio(float cantidad, Transform atacante)
    {
        if (cantidad <= 0f || vidaActual <= 0f)
            return;

        // Registra al jugador y cancela la espera de patrulla.
        if (atacante != null)
        {
            jugador = atacante.root;
            esperando = false;
            esperaRestante = 0f;
            alertaRestante = tiempoAlerta;
            estadoActual = EstadoEnemigo.Alerta;
        }

        vidaActual = Mathf.Max(0f, vidaActual - cantidad);
        ActualizarBarraVida();

        if (vidaActual <= 0f)
            Destroy(gameObject);
    }

    private void ActualizarBarraVida()
    {
        if (barraVida == null)
            return;

        barraVida.maxValue = vidaMaxima;
        barraVida.value = vidaActual;
    }
}
