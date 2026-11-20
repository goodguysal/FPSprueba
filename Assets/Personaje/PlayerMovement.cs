using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float velocidad = 5f;
    [SerializeField] private float velocidadDash = 12f;
    [SerializeField] private Transform camara;

    [Header("Energía")]
    [SerializeField] private float energiaMaxima = 100f;
    [SerializeField] private float consumoPorSegundo = 35f;
    [SerializeField] private float recargaPorSegundo = 20f;
    [SerializeField] private Slider barraEnergia;

    private CharacterController controller;
    private float energiaActual;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        energiaActual = energiaMaxima;

        if (barraEnergia != null)
        {
            barraEnergia.maxValue = energiaMaxima;
            barraEnergia.value = energiaActual;
        }
    }

    private void Update()
    {
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        Transform referencia = camara != null ? camara : transform;
        Vector3 adelante = referencia.forward;
        Vector3 derecha = referencia.right;

        adelante.y = 0f;
        derecha.y = 0f;
        adelante.Normalize();
        derecha.Normalize();

        Vector3 movimiento = derecha * horizontal + adelante * vertical;
        movimiento = Vector3.ClampMagnitude(movimiento, 1f);

        bool usandoDash = Input.GetKey(KeyCode.LeftShift) && energiaActual > 0f;

        if (usandoDash)
            energiaActual -= consumoPorSegundo * Time.deltaTime;
        else
            energiaActual += recargaPorSegundo * Time.deltaTime;

        energiaActual = Mathf.Clamp(energiaActual, 0f, energiaMaxima);

        float velocidadActual = usandoDash ? velocidadDash : velocidad;
        controller.Move(movimiento * velocidadActual * Time.deltaTime);

        if (barraEnergia != null)
            barraEnergia.value = energiaActual;
    }
}