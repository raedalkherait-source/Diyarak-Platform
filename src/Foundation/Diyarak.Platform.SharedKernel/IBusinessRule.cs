namespace Diyarak.Platform.SharedKernel;

public interface IBusinessRule
{
    public string Message { get; }
    public bool IsBroken();
}
