using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Signer;
using Nethereum.KeyStore;

namespace EthSendToMany;

public class KeySecureStorage
{
    public KeySecureStorage()
    {
        
    }
    
    public void PersistSecurely(string privateKey, string password)
    {
        var keystoreService = new KeyStoreService();
        
        var ecKey = new EthECKey(privateKey);
        
        var encryptedKeyJson = keystoreService.EncryptAndGenerateDefaultKeyStoreAsJson(password, ecKey.GetPrivateKeyAsBytes(), ecKey.GetPublicAddress());
        
        File.WriteAllText("key.json", encryptedKeyJson);
    }
    
    public string RetrieveSecurely(string password)
    {
        var keystoreService = new KeyStoreService();
        
        var encryptedKeyJson = File.ReadAllText("key.json");
        
        var privateKey = keystoreService.DecryptKeyStoreFromJson(password, encryptedKeyJson).ToHex(true);
        
        return privateKey;
    }
}