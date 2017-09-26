
namespace RingCentralDataIntegration
{
    internal class Program
    {
        private static void Main(string[] args)
        {
#if DEBUG
            const string objectToRun = "customers";
#else
            var objectToRun = args[0];
#endif
            HttpRestClient.Execute(objectToRun);
        }
    }
}
