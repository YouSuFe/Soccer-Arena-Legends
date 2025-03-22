public class MultiplyOperation : IOperationStrategy
{
    readonly float value;

    public MultiplyOperation(float value)
    {
        this.value = value;
    }

    public float Calculate(float value) => value * this.value;

    public float GetValue() => this.value;  // New method to retrieve the multiplier value

}