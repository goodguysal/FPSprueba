using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class PlayerHealth : MonoBehaviour
{
    [Header("Vida")]
    [SerializeField] private float vidaMaxima = 100f;
    [SerializeField] private Slider barraVida;

    [Header("Muerte y reinicio")]
    [SerializeField] private GameObject pantallaMuerte;
    [SerializeField] private float retrasoReinicio = 3f;

    private float vidaActual;
    private bool muerto;

    private void Start()
    {
        vidaActual = vidaMaxima;
        ActualizarBarra();

        if (pantallaMuerte != null)
            pantallaMuerte.SetActive(false);
    }

    public void TakeDamage(float cantidad)
    {
        if (muerto || cantidad <= 0f)
            return;

        vidaActual = Mathf.Max(0f, vidaActual - cantidad);
        ActualizarBarra();

        if (vidaActual <= 0f)
            StartCoroutine(MorirYReiniciar());
    }

    private void ActualizarBarra()
    {
        if (barraVida == null)
            return;

        barraVida.maxValue = vidaMaxima;
        barraVida.value = vidaActual;
    }

    private IEnumerator MorirYReiniciar()
    {
        muerto = true;

        CharacterController characterController =
            GetComponent<CharacterController>();

        if (characterController != null)
            characterController.enabled = false;

        PlayerMovement movimiento = GetComponent<PlayerMovement>();
        if (movimiento != null)
            movimiento.enabled = false;

        MouseLook mouseLook = GetComponentInChildren<MouseLook>();
        if (mouseLook != null)
            mouseLook.enabled = false;

        WeaponSwitcher switcher = GetComponentInChildren<WeaponSwitcher>();
        if (switcher != null)
            switcher.enabled = false;

        foreach (WeaponController arma in GetComponentsInChildren<WeaponController>())
            arma.enabled = false;

        foreach (RocketLauncher arma in GetComponentsInChildren<RocketLauncher>())
            arma.enabled = false;

        if (pantallaMuerte != null)
            pantallaMuerte.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        yield return new WaitForSecondsRealtime(retrasoReinicio);

        Time.timeScale = 1f;
        Scene escenaActual = SceneManager.GetActiveScene();
        SceneManager.LoadScene(escenaActual.buildIndex);
    }
}
