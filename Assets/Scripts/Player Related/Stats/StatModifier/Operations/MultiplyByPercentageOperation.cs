public class MultiplyByPercentageOperation : IOperationStrategy
{
    readonly float percentageValue;

    public MultiplyByPercentageOperation(float value)
    {
        this.percentageValue = value;
    }

    public float Calculate(float value)
    {
        return (value / 100f) * this.percentageValue + value;
    }

    public float GetValue() => this.percentageValue;  // New method to retrieve the percentage value
}