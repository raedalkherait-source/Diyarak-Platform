namespace Diyarak.Platform.BuildingBlocks;

public static class Guard
{
    public static T NotNull<T>(T? value, string parameterName) where T : class => value ?? throw new ArgumentNullException(parameterName);
    public static string NotNullOrWhiteSpace(string? value, string parameterName) => string.IsNullOrWhiteSpace(value) ? throw new ArgumentException("Value cannot be empty.", parameterName) : value;
    public static decimal NotNegative(decimal value, string parameterName) => value < 0m ? throw new ArgumentOutOfRangeException(parameterName) : value;
    public static int InRange(int value, int minimum, int maximum, string parameterName) => value < minimum || value > maximum ? throw new ArgumentOutOfRangeException(parameterName) : value;
}
