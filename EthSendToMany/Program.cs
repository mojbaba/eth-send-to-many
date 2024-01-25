// See https://aka.ms/new-console-template for more information

using EthSendToMany;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
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

var receivers = File.ReadAllLines("receivers.csv")
    .Skip(1)
    .Select(line => line.Split(','))
    .Select(line => new Receiver()
    {
        Address = line[0],
        Amount = decimal.Parse(line[1])
    })
    .ToArray();
    
//get url and chain id from user
var chainId = Sharprompt.Prompt.Input<int>("Chain Id", 1);
var url = Sharprompt.Prompt.Input<string>("Url");

// check balance requirement

var balanceRequirement = new BalanceRequirement(url, privateKey, receivers);

var requiredBalance = await balanceRequirement.CalculateRequiredBalanceAsync();

var currentBalance = await balanceRequirement.GetCurrentBalanceAsync();

Console.WriteLine($"Current balance of {balanceRequirement.GetSenderAddress()} is {currentBalance} ether");

Console.WriteLine($"Required balance of {balanceRequirement.GetSenderAddress()} is {requiredBalance} ether");

if (decimal.Parse(requiredBalance) > 0)
{
    Console.WriteLine("Insufficient balance");
    
    Console.WriteLine($"{balanceRequirement.GetSenderAddress()} needs {requiredBalance} ether to send {receivers.Length} transactions");
    
    Console.WriteLine($"Please send {requiredBalance} ether to {balanceRequirement.GetSenderAddress()} and try again");
}

// send to many

var sendToMany = new SendToMany(url, chainId, privateKey, receivers);

var results = await sendToMany.SendToManyAsync();

File.WriteAllLines("results.txt", results);