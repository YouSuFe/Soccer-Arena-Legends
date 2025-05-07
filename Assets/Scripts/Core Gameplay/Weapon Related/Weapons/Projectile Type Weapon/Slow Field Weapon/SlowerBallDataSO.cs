using UnityEngine;

[CreateAssetMenu(fileName = "SlowerBallData", menuName = "Game Screen Data/External Datas/Slower Ball Data", order = 1)]
public class SlowerBallDataSO : ScriptableObject
{
    public LayerMask interactableLayerMask;
    public GameObject slowFieldObject; // Reference to the child object you want to Instantiate

    public GameObject projectileTrail;

    public SoundData SoundData;

}
