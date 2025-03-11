namespace Contrib.KafkaFlow.CryptoShredding.TestContract;

public interface IHaveIdAndOrder
{
    Guid id { get; set; }
    int order { get; set; }
}

public partial class UserActivated : IHaveIdAndOrder;
public partial class UserJoined : IHaveIdAndOrder;
public partial class UserDeactivated: IHaveIdAndOrder;
