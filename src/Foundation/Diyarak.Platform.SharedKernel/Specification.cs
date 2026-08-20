namespace Diyarak.Platform.SharedKernel;

public abstract class Specification<T> : ISpecification<T>
{
    public abstract bool IsSatisfiedBy(T candidate);
    public Specification<T> And(ISpecification<T> other) => new AndSpecification(this, other);
    public Specification<T> Or(ISpecification<T> other) => new OrSpecification(this, other);
    public Specification<T> Not() => new NotSpecification(this);

    private sealed class AndSpecification(ISpecification<T> left, ISpecification<T> right) : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) => left.IsSatisfiedBy(candidate) && right.IsSatisfiedBy(candidate);
    }
    private sealed class OrSpecification(ISpecification<T> left, ISpecification<T> right) : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) => left.IsSatisfiedBy(candidate) || right.IsSatisfiedBy(candidate);
    }
    private sealed class NotSpecification(ISpecification<T> inner) : Specification<T>
    {
        public override bool IsSatisfiedBy(T candidate) => !inner.IsSatisfiedBy(candidate);
    }
}
