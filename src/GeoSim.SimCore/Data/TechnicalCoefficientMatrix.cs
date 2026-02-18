namespace GeoSim.SimCore.Data;

/// <summary>
/// The Leontief input-output matrix for a country.
/// Entry [i,j] = units of commodity i required to produce one unit of commodity j.
/// </summary>
public sealed class TechnicalCoefficientMatrix
{
    /// <summary>Number of commodities (always 12).</summary>
    public const int Size = CommodityConstants.Count;

    /// <summary>
    /// The coefficient matrix.
    /// Access: Coefficients[input, output] = amount of input needed per unit of output.
    /// </summary>
    public double[,] Coefficients { get; } = new double[Size, Size];

    /// <summary>
    /// Get the amount of input commodity needed per unit of output commodity.
    /// </summary>
    public double this[Commodity input, Commodity output]
    {
        get => Coefficients[(int)input, (int)output];
        set => Coefficients[(int)input, (int)output] = value;
    }

    /// <summary>
    /// Get the amount of input commodity needed per unit of output commodity.
    /// </summary>
    public double this[int input, int output]
    {
        get => Coefficients[input, output];
        set => Coefficients[input, output] = value;
    }

    /// <summary>
    /// Get all input requirements for producing a given output commodity.
    /// Returns array where index = input commodity, value = coefficient.
    /// </summary>
    public void GetInputRequirements(Commodity output, Span<double> result)
    {
        int col = (int)output;
        for (int i = 0; i < Size; i++)
        {
            result[i] = Coefficients[i, col];
        }
    }

    /// <summary>
    /// Calculate total input cost for producing given quantity of output.
    /// </summary>
    public double CalculateInputCost(Commodity output, double quantity, ReadOnlySpan<double> prices)
    {
        int col = (int)output;
        double cost = 0.0;
        for (int i = 0; i < Size; i++)
        {
            cost += Coefficients[i, col] * quantity * prices[i];
        }
        return cost;
    }

    /// <summary>
    /// Create a matrix with all zeros.
    /// </summary>
    public static TechnicalCoefficientMatrix CreateEmpty() => new();

    /// <summary>
    /// Create a matrix from a 2D array.
    /// </summary>
    public static TechnicalCoefficientMatrix FromArray(double[,] data)
    {
        if (data.GetLength(0) != Size || data.GetLength(1) != Size)
            throw new ArgumentException($"Matrix must be {Size}x{Size}");

        var matrix = new TechnicalCoefficientMatrix();
        Array.Copy(data, matrix.Coefficients, Size * Size);
        return matrix;
    }
}
