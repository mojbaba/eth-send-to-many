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

    public async Task<string> CalculateRequiredBalanceAsync()
    {
        var web3 = new Web3(_url);
        var senderAddress = GetSenderAddress();
        
        var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(senderAddress);
        var gasPrice = (await web3.Eth.GasPrice.SendRequestAsync()).Value;
        
        var totalGasCost = new BigInteger(21000) * gasPrice;
        var totalAmount = _receivers.Select(a=> a.Amount).Aggregate((a, b) => a + b);
        var totalCost = totalGasCost + Web3.Convert.ToWei(totalAmount, UnitConversion.EthUnit.Ether);
        
        var requiredBalance = currentBalance.Value - totalCost;
        
        return Web3.Convert.FromWei(requiredBalance, UnitConversion.EthUnit.Ether).ToString();
    }
}