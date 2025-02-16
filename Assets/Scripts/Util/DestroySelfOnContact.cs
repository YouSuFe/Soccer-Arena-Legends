using UnityEngine;

public class DestroySelfOnContact : MonoBehaviour
{

    private void OnTriggerEnter2D(Collider2D col)
    {
        //// Beacuse we asigned the team index -1 default, for hosting games everybody has team index -1.
        //// Beacuse of that, nobody can shoot anybody, to fix that we should check it.
        //if(projectile.TeamIndex != -1)
        //{
        //    if(col.attachedRigidbody != null)
        //    {
        //        if (col.attachedRigidbody.TryGetComponent<TankPlayer>(out TankPlayer player))
        //        {
        //            if (player.TeamIndex.Value == projectile.TeamIndex)
        //            {
        //                return;
        //            }
        //        }
        //    }
        //}

        //Destroy(gameObject);
    }
}
