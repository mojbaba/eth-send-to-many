using System.Numerics;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;

namespace EthSendToMany;

public class BalanceRequirement
{
    private readonly string _url;
    private readonly string _privateKey;
    private readonly IEnumerable<Receiver> _receivers;

    public BalanceRequirement(string url, string privateKey, IEnumerable<Receiver> receivers)
    {
        _url = url;
        _privateKey = privateKey;
        _receivers = receivers;
    }

    public string GetSenderAddress()
    {
        var senderKey = new EthECKey(_privateKey);
        var senderAddress = senderKey.GetPublicAddress();

        return senderAddress;
    }

    public async Task<string> GetCurrentBalanceAsync()
    {
        var web3 = new Web3(_url);
        var senderAddress = GetSenderAddress();

        var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(senderAddress);

        return Web3.Convert.FromWei(currentBalance.Value, UnitConversion.EthUnit.Ether).ToString();
    }

    public decimal TotalFee(decimal gasPriceGwei, int maxPriorityFeePerGas)
    {
        var gasPrice = Web3.Convert.ToWei(gasPriceGwei, Nethereum.Util.UnitConversion.EthUnit.Gwei);
        var totalGasCost = new BigInteger(21000) * (maxPriorityFeePerGas + gasPrice);
        var count = _receivers.Count();
        var totalFee = totalGasCost * count;

        return Web3.Convert.FromWei(totalFee, UnitConversion.EthUnit.Ether);
    }

    public decimal TotalAmountToBeSent()
    {
        return _receivers.Select(a => a.Amount).Aggregate((a, b) => a + b);
    }


    public async Task<string> CalculateRequiredBalanceAsync(decimal gasPriceGwei, int maxPriorityFeePerGasGwei)
    {
        var totalFee = TotalFee(gasPriceGwei, maxPriorityFeePerGasGwei);
        var totalAmountToBeSent = TotalAmountToBeSent();

        return (totalFee + totalAmountToBeSent).ToString();
    }

    public async Task<string> CalculateLackOfBalanceAsync(decimal gasPriceGwei, int maxPriorityFeePerGasGwei)
    {
        var currentBalance = await GetCurrentBalanceAsync();
        var requiredBalance = await CalculateRequiredBalanceAsync(gasPriceGwei, maxPriorityFeePerGasGwei);

        return (decimal.Parse(requiredBalance) - decimal.Parse(currentBalance)).ToString();
    }
}