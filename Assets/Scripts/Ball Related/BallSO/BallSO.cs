using UnityEngine;

[CreateAssetMenu(fileName = "Ball", menuName = "Game Screen Data/Ball/NewBall")]
public class BallSO : ScriptableObject
{
    [field: SerializeField] public BallData BallData { get; private set; }
}
