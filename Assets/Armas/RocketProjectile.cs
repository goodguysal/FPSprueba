using UnityEngine;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(Rigidbody))]
public class RocketProjectile : MonoBehaviour
{
    [SerializeField] private GameObject efectoExplosion;

    private float velocidad;
    private float distanciaMaxima;
    private float radioExplosion;
    private float danioMaximo;
    private float distanciaRecorrida;
    private GameObject propietario;
    private Rigidbody cuerpo;
    private bool configurado;
    private bool explotado;

    private void Awake()
    {
        cuerpo = GetComponent<Rigidbody>();
        cuerpo.useGravity = false;
        cuerpo.isKinematic = true;

        GetComponent<Collider>().isTrigger = true;
    }

    public void Configurar(
        float velocidad,
        float distanciaMaxima,
        float radioExplosion,
        float danioMaximo,
        GameObject propietario)
    {
        this.velocidad = velocidad;
        this.distanciaMaxima = distanciaMaxima;
        this.radioExplosion = radioExplosion;
        this.danioMaximo = danioMaximo;
        this.propietario = propietario;
        configurado = true;
    }

    private void Update()
    {
        if (!configurado || explotado)
            return;

        float avance = velocidad * Time.deltaTime;
        transform.position += transform.forward * avance;
        distanciaRecorrida += avance;

        if (distanciaRecorrida >= distanciaMaxima)
            Explotar();
    }

    private void OnTriggerEnter(Collider otro)
    {
        if (explotado)
            return;

        if (propietario != null && otro.transform.root.gameObject == propietario)
            return;

        Explotar();
    }

    private void Explotar()
    {
        if (explotado)
            return;

        explotado = true;

        if (efectoExplosion != null)
            Instantiate(efectoExplosion, transform.position, Quaternion.identity);

        Collider[] alcanzados = Physics.OverlapSphere(transform.position, radioExplosion);

        foreach (Collider objetivo in alcanzados)
        {
            if (propietario != null && objetivo.transform.root.gameObject == propietario)
                continue;

            float distancia = Vector3.Distance(transform.position, objetivo.ClosestPoint(transform.position));
            float factorDanio = 1f - Mathf.Clamp01(distancia / radioExplosion);
            float danio = danioMaximo * factorDanio;

            objetivo.SendMessage(
                "TakeDamage",
                danio,
                SendMessageOptions.DontRequireReceiver);
        }

        Destroy(gameObject);
    }
}
