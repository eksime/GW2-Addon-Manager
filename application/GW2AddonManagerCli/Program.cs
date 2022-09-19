using System.CommandLine;

namespace GW2AddonManager
{
    public class Program
    {
        public static int Main(string[] args)
        {
            var addon = new Argument<string>("addon");
            var installCommand = new Command("install")
            {
                addon
            };

            var deleteCommand = new Command("delete")
            {
                addon
            };

            var enableCommand = new Command("enable")
            {
                addon
            };

            var disableCommand = new Command("disable")
            {
                addon
            };

            var rootCommand = new RootCommand
            {
                installCommand,
                deleteCommand,
                enableCommand,
                disableCommand,
            };
            
            installCommand.SetHandler<string>(str => Install(str), addon);
            deleteCommand.SetHandler<string>(Delete, addon);
            enableCommand.SetHandler<string>(Enable, addon);
            disableCommand.SetHandler<string>(Disable, addon);

            //rootCommand.Description = "My sample app";
            //rootCommand.SetHandler()
            //rootCommand.SetHandler((int i, bool b, FileInfo f) =>
            //{
            //    Console.WriteLine($"The value for --int-option is: {i}");
            //    Console.WriteLine($"The value for --bool-option is: {b}");
            //    Console.WriteLine($"The value for --file-option is: {f?.FullName ?? "null"}");
            //}, intOption, boolOption, fileOption);

            // Parse the incoming args and invoke the handler
            return rootCommand.Invoke(args);
        }

        private static void Install(string addon)
        {

        }
        private static void Delete(string addon)
        {

        }
        private static void Enable(string addon)
        {

        }
        private static void Disable(string addon)
        {

        }
    }
}