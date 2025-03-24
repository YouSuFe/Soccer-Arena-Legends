using UnityEngine;

public class BallReference : MonoBehaviour
{
    [field: SerializeField] public BallSO BallSO { get; private set; }

    public Rigidbody BallRigidbody { get; private set; }

    private void Awake()
    {
        BallRigidbody = GetComponent<Rigidbody>();
    }

}
