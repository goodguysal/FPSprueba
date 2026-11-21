using UnityEngine;

public class DoorTransition : MonoBehaviour
{
    [Header("Habitación de destino")]
    [SerializeField] private GameObject prefabHabitacion;
    [SerializeField] private Transform puntoLlegada;

    [Header("Jugador")]
    [SerializeField] private string tagJugador = "Player";

    [Header("Opcional")]
    [SerializeField] private bool destruirHabitacionActual = true;

    private bool usandoPuerta;

    private void OnTriggerEnter(Collider otro)
    {
        if (usandoPuerta || !otro.transform.root.CompareTag(tagJugador))
            return;

        if (prefabHabitacion == null || puntoLlegada == null)
        {
            Debug.LogError("Asigna el prefab de destino y el punto de llegada en la puerta.", this);
            return;
        }

        usandoPuerta = true;

        GameObject habitacionActual = GameObject.FindGameObjectWithTag("HabitacionActual");
        GameObject nuevaHabitacion = Instantiate(prefabHabitacion);

        Transform llegada = nuevaHabitacion.transform.Find(puntoLlegada.name);
        if (llegada == null)
        {
            Debug.LogError(
                $"No se encontró un punto de llegada llamado '{puntoLlegada.name}' en la habitación.",
                nuevaHabitacion);
            Destroy(nuevaHabitacion);
            usandoPuerta = false;
            return;
        }

        CharacterController characterController =
            otro.transform.root.GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        otro.transform.root.SetPositionAndRotation(
            llegada.position,
            llegada.rotation);

        if (characterController != null)
            characterController.enabled = true;

        if (destruirHabitacionActual && habitacionActual != null)
            Destroy(habitacionActual);
    }
}