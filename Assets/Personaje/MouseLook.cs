using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [SerializeField] private Transform jugador;
    [SerializeField] private float sensibilidad = 200f;

    private float rotacionVertical;


private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidad * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidad * Time.deltaTime;

        rotacionVertical -= mouseY;
        rotacionVertical = Mathf.Clamp(rotacionVertical, -85f, 85f);

        transform.localRotation = Quaternion.Euler(rotacionVertical, 0f, 0f);

        if (jugador != null)
            jugador.Rotate(Vector3.up * mouseX);
    }

    private void OnApplicationFocus(bool tieneFoco)
    {
        Cursor.lockState = tieneFoco ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !tieneFoco;
    }
}