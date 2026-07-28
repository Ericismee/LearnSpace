using System;
using System.Threading.Tasks;
using LearnSpace.Core.Services;

class Program
{
    static async Task Main(string[] args)
    {
        var fb = new FirebaseService();
        var pin = await fb.CreatePairingRequestAsync();
        Console.WriteLine("Created PIN: " + pin);
        
        bool res = await fb.ValidateAndPairAsync(pin, "test-token");
        Console.WriteLine("Validation Result: " + res);
    }
}
