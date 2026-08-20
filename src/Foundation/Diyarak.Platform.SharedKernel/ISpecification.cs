namespace Diyarak.Platform.SharedKernel;

public interface ISpecification<in T>
{
    public bool IsSatisfiedBy(T candidate);
}
