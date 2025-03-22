public class AddOperation : IOperationStrategy
{
    readonly float value;

    public AddOperation(float value)
    {
        this.value = value;
    }

    public float Calculate(float value) => value + this.value;

    public float GetValue() => this.value;  // New method to retrieve the add value
}