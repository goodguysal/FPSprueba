using UnityEngine;

public class OneWayDoor : MonoBehaviour
{
    [Header("Referencias")]
    public GameObject obstacleBlocker; // Un objeto físico (pared invisible o puerta cerrada) que bloquea el paso
    private bool isClosed = false;

    private void OnTriggerExit(Collider other)
    {
        // Se activa cuando el jugador termina de cruzar hacia adelante y sale del marco de la puerta
        if (other.CompareTag("Player") && !isClosed)
        {
            CloseDoorPermanently();
        }
    }

    void CloseDoorPermanently()
    {
        isClosed = true;

        // Activamos el bloqueo físico para que el jugador no pueda volver atrás
        if (obstacleBlocker != null)
        {
            obstacleBlocker.SetActive(true);
        }

        // Opcional: Aquí puedes reproducir un sonido metálico de cierre o portazo
        Debug.Log("¡La puerta se ha sellado tras de ti! No hay vuelta atrás.");
    }
}
