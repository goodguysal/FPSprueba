using UnityEngine;
using TMPro;

public class VictoryManager : MonoBehaviour
{
    public static VictoryManager Instance { get; private set; }

    [Header("Objetivo")]
    [SerializeField] private int totalEnemigos = 7;
    [SerializeField] private TMP_Text textoEnemigosEliminados;
    [SerializeField] private GameObject pantallaVictoria;
    [SerializeField] private GameObject jugador;

    private int enemigosEliminados;
    private bool victoriaActivada;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        enemigosEliminados = 0;
        ActualizarTexto();

        if (pantallaVictoria != null)
            pantallaVictoria.SetActive(false);
    }

    public void RegistrarEnemigoEliminado()
    {
        if (victoriaActivada)
            return;

        enemigosEliminados = Mathf.Min(
            enemigosEliminados + 1,
            totalEnemigos);

        ActualizarTexto();

        if (enemigosEliminados >= totalEnemigos)
            MostrarVictoria();
    }

    private void ActualizarTexto()
    {
        if (textoEnemigosEliminados != null)
        {
            textoEnemigosEliminados.text =
                $"Enemigos eliminados {enemigosEliminados}/{totalEnemigos}";
        }
    }

    private void MostrarVictoria()
    {
        victoriaActivada = true;

        if (pantallaVictoria != null)
            pantallaVictoria.SetActive(true);

        if (jugador != null)
        {
            PlayerMovement movimiento =
                jugador.GetComponent<PlayerMovement>();
            if (movimiento != null)
                movimiento.enabled = false;

            MouseLook mouseLook =
                jugador.GetComponentInChildren<MouseLook>();
            if (mouseLook != null)
                mouseLook.enabled = false;

            WeaponSwitcher switcher =
                jugador.GetComponentInChildren<WeaponSwitcher>();
            if (switcher != null)
                switcher.enabled = false;

            foreach (WeaponController arma in
                     jugador.GetComponentsInChildren<WeaponController>(true))
            {
                arma.enabled = false;
            }

            foreach (RocketLauncher arma in
                     jugador.GetComponentsInChildren<RocketLauncher>(true))
            {
                arma.enabled = false;
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }
}
