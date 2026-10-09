using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Range = System.Range;

namespace ConsoleApp;

#pragma warning disable CA1416
public static class Program
{
    private const string Chars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
    
    public static void Main(string[] args)
    {
        // Uncomment the desired test method to run
        // ScanTest();
        // GuidTest();
    }

    // public static void ScanTest()
    // {
    //     var path = Environment.ExpandEnvironmentVariables("%homepath%/Desktop/asd/Scan_20250811+neptun.png");
    //     var qrScanner = new Scanner.CodeScanner(path);
    //     var result = qrScanner.FindCodes();
    //
    //     Console.WriteLine(result.Uuid);
    //     Console.WriteLine(result.UserCode);
    //     
    //     var matrixScanner = new Scanner.MatrixScanner(path);
    //     var matrixBubbles = matrixScanner.FindBubbles(15, 5);
    //     
    //     var houghScanner = new Scanner.HoughScanner(path);
    //     var houghBubbles = houghScanner.FindBubbles(15, 5);
    //     
    //     var bubbleChecker = new Scanner.BubbleChecker(path);
    //     var matrixResults = bubbleChecker.CheckBubbles(matrixBubbles, 15, 5);
    //     var houghResults = bubbleChecker.CheckBubbles(houghBubbles, 15, 5);
    //     for (int i = 0; i < matrixResults.Length; i++)
    //     {
    //         var (mResult, mBest, mNextBest) = matrixResults[i];
    //         var (hResult, hBest, hNextBest) = houghResults[i];
    //         if (mResult == hResult)
    //         {
    //             Console.WriteLine($"Question {i + 1}: Agree on {mResult + 1}, matrix confidence {(mBest - mNextBest) / mBest}, hough confidence {(hBest - hNextBest) / hBest}");
    //         }
    //         else
    //         {
    //             Console.WriteLine($"Question {i + 1}: Disagree, matrix says {mResult + 1} with confidence {(mBest - mNextBest) / mBest}, hough says {hResult + 1} with confidence {(hBest - hNextBest) / hBest}");
    //         }
    //     }
    //     
    // }
    
    public static void GuidTest()
    {
        Guid uuid = Guid.NewGuid();
        byte[] uuidBytes = uuid.ToByteArray();
        Console.WriteLine(uuid);
        Console.WriteLine(uuidBytes);
        // var bg = new Storage.QrBuilder(uuidBytes);
        // bg.Save(Environment.ExpandEnvironmentVariables("%homepath%/Desktop/test-bg0.png"));
    }

    private static void Breaker()
    {
        bool r = Storage.Manager.OpenDatabase(Environment.ExpandEnvironmentVariables("%homepath%/Desktop/test-pwd-cc.db"));
        if (!r)
        {
            Console.WriteLine("Failed to open database. Starting brute force attack...");
            return;
        }
        
        byte[] salt = GetSalt();
        byte[] verify = GetVerify();
        long counter = 0;
        while (true)
        {
            string combination = ToAlphanumeric(counter);
            if (TryPassword(combination, salt, verify))
            {
                Console.WriteLine("Database opened successfully with key: " + combination);
            }
            counter++;
            if (counter % 100 == 0)
            {
                Console.WriteLine(combination);
            }
        }
    }
    
    private static string ToAlphanumeric(long number)
    {
        if (number == 0) return Chars[0].ToString();

        StringBuilder sb = new StringBuilder();
        int baseLen = Chars.Length;
        while (number > 0)
        {
            sb.Insert(0, Chars[(int)(number % baseLen)]);
            number /= baseLen;
        }
        return sb.ToString();
    }

    private static byte[] GetSalt()
    {

        byte[] salt = [];
        var saltCommand = Storage.Manager.Connection.CreateCommand();
        saltCommand.CommandText = "SELECT value FROM Security WHERE key = 'Salt';";
        var saltReader = saltCommand.ExecuteReader();
        while (saltReader.Read())
        {
            salt = Convert.FromBase64String(saltReader.GetString(0));
        }

        if (salt.Length == 0)
        {
            throw new CryptographicException("Salt missing from database");
        }
        return salt;
    }
    

    private static byte[] GetVerify()
    {
        byte[] verify = [];
        var verifyCommand = Storage.Manager.Connection.CreateCommand();
        verifyCommand.CommandText = "SELECT value FROM Security WHERE key = 'Verify';";
        var reader = verifyCommand.ExecuteReader();
        while (reader.Read())
        {
            verify = Convert.FromBase64String(reader.GetString(0));
        }

        if (verify.Length == 0)
        {
            throw new CryptographicException("Verify missing from database");
        }

        return verify;
    }

    private static bool TryPassword(string password, byte[] salt, byte[] verify)
    {
        var key = Rfc2898DeriveBytes.Pbkdf2(password, salt, 10_000, HashAlgorithmName.SHA3_512, 32);

        try
        {
            byte[] data = Convert.FromBase64String(Decrypt(verify[32..], key));
            var challengeHash = SHA3_256.HashData(data);
            return challengeHash.SequenceEqual(verify[..32]);
        }
        catch (CryptographicException)
        {
            return false;
        }
        catch (FormatException)
        {
            return false;
        }
    }
    
    private static string Decrypt(byte[] cipherText, byte[] key)
    {
        const int offset = 16;
        using Aes aesAlg = Aes.Create();
        aesAlg.Key = key;
        aesAlg.IV = cipherText.Take(offset).ToArray();

        ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

        using MemoryStream msDecrypt = new MemoryStream(cipherText.Take(Range.StartAt(offset)).ToArray());
        using CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
        using StreamReader srDecrypt = new StreamReader(csDecrypt);
        
        var plaintext = srDecrypt.ReadToEnd();
        return plaintext;
    }
}
#pragma warning restore CA1416
