using System.Numerics;

namespace Example.D10;

public static class Rules
{
    public static TOutput Saturate<TInput, TOutput>(TInput value)
        where TInput : INumberBase<TInput>
        where TOutput : INumberBase<TOutput> => TOutput.CreateSaturating(value);

    public static TOutput Forward<TInput, TOutput>(TInput value)
        where TInput : INumberBase<TInput>
        where TOutput : INumberBase<TOutput> => Saturate<TInput, TOutput>(value);

    public static byte Run(long value) => Forward<long, byte>(value);
}
