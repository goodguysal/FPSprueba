using UnityEngine;

public enum TipoMunicion
{
    Revolver,
    Lanzacohetes
}

public class AmmoPickup : MonoBehaviour
{
    [SerializeField] private TipoMunicion tipo;
    [SerializeField] private int cantidad = 12;

    private void OnTriggerEnter(Collider otro)
    {
        PlayerHealth jugador =
            otro.GetComponentInParent<PlayerHealth>();

        if (jugador == null)
            return;

        bool recogida = false;

        if (tipo == TipoMunicion.Revolver)
        {
            WeaponController revolver =
                jugador.GetComponentInChildren<WeaponController>(true);

            if (revolver != null)
                recogida = revolver.AgregarMunicion(cantidad);
        }
        else if (tipo == TipoMunicion.Lanzacohetes)
        {
            RocketLauncher lanzacohetes =
                jugador.GetComponentInChildren<RocketLauncher>(true);

            if (lanzacohetes != null)
                recogida = lanzacohetes.AgregarMunicion(cantidad);
        }

        if (recogida)
            Destroy(gameObject);
    }
}
