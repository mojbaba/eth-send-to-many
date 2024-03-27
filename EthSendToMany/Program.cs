// See https://aka.ms/new-console-template for more information

using System.Globalization;
using EthSendToMany;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.Util;
using Nethereum.Web3;
using Sharprompt;

string privateKey;

if (File.Exists("key.json"))
{
    Console.WriteLine("Key already exists. give me a password to retrieve it");
    var password = Sharprompt.Prompt.Password("Password");

    try
    {
        var keySecureStorage = new KeySecureStorage();
        privateKey = keySecureStorage.RetrieveSecurely(password);
        Console.WriteLine($"successfully retrieved key");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}
else
{
    Console.WriteLine("Give me a password to encrypt your key");
    var password1 = Sharprompt.Prompt.Password("Password");
    Console.WriteLine("Give me a password to confirm your key");
    var password2 = Sharprompt.Prompt.Password("Password");

    if (password1 != password2)
    {
        Console.WriteLine("Passwords do not match");
        return;
    }

    // generate a new private key
    privateKey = EthECKey.GenerateKey().GetPrivateKeyAsBytes().ToHex(true);

    var keySecureStorage = new KeySecureStorage();
    try
    {
        keySecureStorage.PersistSecurely(privateKey, password1);
        Console.WriteLine($"successfully persisted key");
    }
    catch (Exception e)
    {
        Console.WriteLine(e);
        throw;
    }
}


// read csv from file receivers.csv in the same directory as the executable ignores the first line first column is the address second column is the amount
if(File.Exists("receivers.csv") == false)
{
    Console.WriteLine("receivers.csv does not exist");
    return;
}

var receivers = File.ReadAllLines("receivers.csv")
    .Skip(1)
    .Where(line => string.IsNullOrWhiteSpace(line) == false)
    .Select(line => line.Split(','))
    .Select(line => new Receiver()
    {
        Address = line[0].Trim(),
        Amount = decimal.Parse(line[1].Trim(), CultureInfo.InvariantCulture)
    })
    .ToArray();
    
//get url and chain id from user
var chainId = Sharprompt.Prompt.Input<int>("Chain Id", 1);
var url = Sharprompt.Prompt.Input<string>("Url");

// check balance requirement

var balanceRequirement = new BalanceRequirement(url, privateKey, receivers);

var web3 = new Web3(url);

var gasPriceWei = (await web3.Eth.GasPrice.SendRequestAsync()).Value;

var gasPriceGwei =  Web3.Convert.FromWei(gasPriceWei, UnitConversion.EthUnit.Gwei);

gasPriceGwei = (decimal)Sharprompt.Prompt.Input<int>("Gas Price (Gwei)", gasPriceGwei);

var maxPriorityFeePerGasGwei = Sharprompt.Prompt.Input<int>("Max Priority Fee Per Gas (Gwei)", 2);

var requiredBalance = await balanceRequirement.CalculateRequiredBalanceAsync(gasPriceGwei, maxPriorityFeePerGasGwei);

var currentBalance = await balanceRequirement.GetCurrentBalanceAsync();

Console.WriteLine($"Current balance of {balanceRequirement.GetSenderAddress()} is {currentBalance} ether");

Console.WriteLine($"Required balance of {balanceRequirement.GetSenderAddress()} is {requiredBalance} ether");

Console.WriteLine($"Total fee is {balanceRequirement.TotalFee(gasPriceGwei, maxPriorityFeePerGasGwei)} ether");

Console.WriteLine($"Total amount to be sent is {balanceRequirement.TotalAmountToBeSent()} ether");

if (decimal.Parse(requiredBalance) > decimal.Parse(currentBalance))
{
    Console.WriteLine("Insufficient balance");
    
    var lackOfBalance = await balanceRequirement.CalculateLackOfBalanceAsync(gasPriceGwei, maxPriorityFeePerGasGwei);
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.WriteLine($"Please send {lackOfBalance} ether to {balanceRequirement.GetSenderAddress()} and try again");
    Console.ResetColor();
    return;
}

if(Sharprompt.Prompt.Confirm($"Do you want to continue to send {receivers.Length} transactions?") == false)
    return;

// send to many

var sendToMany = new SendToMany(url, chainId, privateKey, receivers);

var results = await sendToMany.SendToManyAsync(gasPriceGwei, maxPriorityFeePerGasGwei);

File.WriteAllLines("results.txt", results);

if(results.Count() == receivers.Count())
    Console.WriteLine("Done. Check results.txt for details");
else
{
    Console.WriteLine("Something went wrong. Check results.txt for details");
}
