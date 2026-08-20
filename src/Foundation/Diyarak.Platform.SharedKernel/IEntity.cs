namespace Diyarak.Platform.SharedKernel;

public interface IEntity<out TId> where TId : notnull
{
    public TId Id { get; }
}
