using System.Collections;
using UnityEngine;

public class WeaponSwitcher : MonoBehaviour
{
    [System.Serializable]
    public class Arma
    {
        public GameObject objeto;
        public Behaviour[] controladores;
    }

    [Header("Armas en orden de selección")]
    [SerializeField] private Arma[] armas;

    [Header("Animación de cambio")]
    [SerializeField] private float distanciaBajada = 0.5f;
    [SerializeField] private float duracionBajada = 0.15f;
    [SerializeField] private float duracionSubida = 0.2f;

    private Vector3[] posicionesIniciales;
    private Renderer[][] renderers;
    private int armaActual;
    private bool cambiando;

    private void Start()
    {
        if (armas == null || armas.Length == 0)
            return;

        posicionesIniciales = new Vector3[armas.Length];
        renderers = new Renderer[armas.Length][];

        for (int i = 0; i < armas.Length; i++)
        {
            if (armas[i].objeto == null)
                continue;

            posicionesIniciales[i] = armas[i].objeto.transform.localPosition;
            renderers[i] = armas[i].objeto.GetComponentsInChildren<Renderer>(true);
        }

        MostrarArma(armaActual);
    }

    private void Update()
    {
        if (cambiando || armas == null || armas.Length < 2)
            return;

        float rueda = Input.GetAxis("Mouse ScrollWheel");

        if (rueda > 0f)
            CambiarArma(1);
        else if (rueda < 0f)
            CambiarArma(-1);
    }

    private void CambiarArma(int direccion)
    {
        int siguiente = (armaActual + direccion + armas.Length) % armas.Length;

        if (siguiente != armaActual)
            StartCoroutine(AnimarCambio(siguiente));
    }

    private IEnumerator AnimarCambio(int siguiente)
    {
        cambiando = true;

        Transform armaVieja = armas[armaActual].objeto.transform;
        Vector3 posicionInicial = posicionesIniciales[armaActual];
        Vector3 posicionAbajo = posicionInicial + Vector3.down * distanciaBajada;

        ActivarControladores(armaActual, false);

        yield return MoverArma(
            armaVieja, posicionInicial, posicionAbajo, duracionBajada);

        MostrarRenderers(armaActual, false);

        armaActual = siguiente;

        Transform armaNueva = armas[armaActual].objeto.transform;
        Vector3 posicionNueva = posicionesIniciales[armaActual];
        Vector3 posicionNuevaAbajo = posicionNueva + Vector3.down * distanciaBajada;

        armaNueva.localPosition = posicionNuevaAbajo;
        MostrarRenderers(armaActual, true);

        yield return MoverArma(
            armaNueva, posicionNuevaAbajo, posicionNueva, duracionSubida);

        ActivarControladores(armaActual, true);
        cambiando = false;
    }

    private IEnumerator MoverArma(
        Transform arma,
        Vector3 desde,
        Vector3 hasta,
        float duracion)
    {
        float transcurrido = 0f;

        while (transcurrido < duracion)
        {
            transcurrido += Time.deltaTime;
            float progreso = duracion > 0f
                ? Mathf.Clamp01(transcurrido / duracion)
                : 1f;

            arma.localPosition = Vector3.Lerp(desde, hasta, progreso);
            yield return null;
        }

        arma.localPosition = hasta;
    }

    private void MostrarArma(int indice)
    {
        for (int i = 0; i < armas.Length; i++)
        {
            bool seleccionada = i == indice;
            MostrarRenderers(i, seleccionada);
            ActivarControladores(i, seleccionada);

            if (armas[i].objeto != null)
                armas[i].objeto.transform.localPosition = posicionesIniciales[i];
        }
    }

    private void MostrarRenderers(int indice, bool mostrar)
    {
        if (renderers[indice] == null)
            return;

        foreach (Renderer renderer in renderers[indice])
        {
            if (renderer != null)
                renderer.enabled = mostrar;
        }
    }

    private void ActivarControladores(int indice, bool activar)
    {
        foreach (Behaviour controlador in armas[indice].controladores)
        {
            if (controlador != null)
                controlador.enabled = activar;
        }
    }
}