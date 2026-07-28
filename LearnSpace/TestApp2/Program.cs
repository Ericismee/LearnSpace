using System;
using System.Threading.Tasks;
using LearnSpace.Core.Services;
using LearnSpace.Core.Models;

class Program
{
    static void Main(string[] args)
    {
        var fb = new FirebaseService();
        var token = "test-token";
        
        // Listen
        var sub = fb.ListenToSecurityCodes(token, codes => {
            Console.WriteLine("Received ExitCode: " + codes.ExitCode);
        });
        
        // Put
        var newCodes = new DeviceSecurityCodes { ExitCode = "777888", UnlockCode = "4444" };
        fb.SetSecurityCodesAsync(token, newCodes).Wait();
        
        System.Threading.Thread.Sleep(3000);
    }
}
