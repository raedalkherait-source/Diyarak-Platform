namespace Diyarak.Platform.SharedKernel;

public interface IRepository<in TAggregate> where TAggregate : IAggregateRoot
{
}
