using System.Numerics;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Model;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Sharprompt;

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

    public async Task<IEnumerable<string>> SendToManyAsync(decimal gasPriceGwei , int maxPriorityFeePerGasGwei)
    {
        var web3 = new Web3(_url);
        var senderKey = new EthECKey(_privateKey);
        var senderAddress = senderKey.GetPublicAddress();
        


        var nonce = (await web3.Eth.Transactions.GetTransactionCount.SendRequestAsync(senderAddress)).Value;        

        if(Sharprompt.Prompt.Confirm($"current nonce is {(long)nonce}, do you want to set it custom?")){
            var customNonce = Prompt.Input<long>("enter the custom nonce");
            nonce = new BigInteger(customNonce);

            if(Prompt.Confirm($"are you sure about this nonce? {(long)nonce}") == false){
                throw new TaskCanceledException("nonce is not confirmed");
            }
        }

        var gasPrice = Web3.Convert.ToWei(gasPriceGwei, Nethereum.Util.UnitConversion.EthUnit.Gwei);

        var maxPriorityFeePerGas = Web3.Convert.ToWei(maxPriorityFeePerGasGwei, Nethereum.Util.UnitConversion.EthUnit.Gwei);



        var gasLimit = new BigInteger(21000);
        var chainId = new BigInteger(_chainId);

        var results = new List<string>();

        for (var i = 0; i < _recievers.Count(); i++)
        {
            try
            {
                var receiverAddress = _recievers.ElementAt(i).Address.ToLower();
                var value = Web3.Convert.ToWei(_recievers.ElementAt(i).Amount, UnitConversion.EthUnit.Ether);
                var transaction1559 = new Transaction1559(chainId, nonce, maxPriorityFeePerGas, gasPrice, gasLimit,
                    receiverAddress, value, null, null);

                transaction1559.Sign(senderKey);

                var rawTransaction = transaction1559.GetRLPEncoded().ToHex(true);

                var transactionHash = await web3.Eth.Transactions.SendRawTransaction.SendRequestAsync(rawTransaction);

                var result =
                    $"[{i + 1}/{_recievers.Count()}] Sent {value} wei to {receiverAddress} successfully with transaction hash {transactionHash}";
                
                Console.WriteLine(result);

                results.Add(result);
                nonce = nonce + 1;
                
                await Task.Delay(2000);
            }
            catch (Exception e)
            {
                var result =
                    e.Message;
                
                results.Add(result);
                Console.WriteLine(result);
                break;
            }
        }

        return results;
    }
}