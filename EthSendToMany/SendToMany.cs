using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;

namespace EthSendToMany;

public class SendToMany
{
    private readonly string _url;
    private readonly string _privateKey;
    private readonly int _chainId;
    private readonly IEnumerable<Receiver> _recievers;

    public SendToMany(string url, int chainId, string privateKey, IEnumerable<Receiver> receivers)
    {
        _url = url;
        _privateKey = privateKey;
        _recievers = receivers;
        _chainId = chainId;
    }

    public async Task<IEnumerable<string>> SendToManyAsync()
    {
        var web3 = new Web3(_url);
        var senderKey = new EthECKey(_privateKey);
        var senderAddress = senderKey.GetPublicAddress();
        var nonce = (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(senderAddress)).Value;
        var gasPrice = (await web3.Eth.GasPrice.SendRequestAsync()).Value;
        var gasLimit = new BigInteger(21000);
        var chainId = new BigInteger(_chainId);
        var maxPriorityFeePerGas = Web3.Convert.ToWei(1, Nethereum.Util.UnitConversion.EthUnit.Gwei);

        var results = new List<string>();

        for (var i = 0; i < _recievers.Count(); i++)
        {
            try
            {
                var receiverAddress = _recievers.ElementAt(i).Address;
                var value = Web3.Convert.ToWei(_recievers.ElementAt(i).Amount, UnitConversion.EthUnit.Ether);
                var transaction1559 = new Transaction1559(chainId, nonce, maxPriorityFeePerGas, gasPrice, gasLimit,
                    receiverAddress, value, null, new List<AccessListItem>());

                transaction1559.Sign(senderKey);

                var rawTransaction = transaction1559.GetRLPEncodedRaw().ToHex(true);

                var transactionHash = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(rawTransaction);

                var result =
                    $"[{i + 1}/{_recievers.Count()}] Sent {value} wei to {receiverAddress} successfully with transaction hash {transactionHash}";

                results.Add(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                break;
            }
        }

        return results;
    }
}