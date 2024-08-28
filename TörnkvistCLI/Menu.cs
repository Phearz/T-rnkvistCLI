namespace TörnkvistCLI
{

    public interface IMenuOption
    {
        int MenuIdentifier { get; }
        string Description { get; }
        void Execute();
    }
    public class MenuIdentifier
    {
        private List<IMenuOption> _menuOptions;
        public void ShowMenyOptions(List<IMenuOption> menuOptions)
        {
            _menuOptions = menuOptions;
            System.Console.WriteLine("What do you want to do?");
            foreach (var option in _menuOptions)
            {
                System.Console.WriteLine($"{option.MenuIdentifier}. {option.Description}");
            }

        }
        public static void PrintSplashScreen()
        {
            Console.Clear();
            Console.WriteLine("Welcome to Törnkvist Feature CLI");
            Console.WriteLine(" _____  \\/\\/  ____  _      _  __ _     _  ____  _____  ");
            Console.WriteLine("/__ __\\/  _ \\/  __\\/ \\  /|/ |/ // \\ |\\/ \\/ ___\\/__ __\\ ");
            Console.WriteLine("  / \\  | / \\||  \\/|| |\\ |||   / | | //| ||    \\  / \\   ");
            Console.WriteLine("  | |  | \\_/||    /| | \\|||   \\ | \\// | |\\___ |  | |   ");
            Console.WriteLine("  \\_/  \\____/\\_/\\_\\\\_/  \\|\\_|\\_\\\\__/  \\_/\\____/  \\_/   ");
            Console.WriteLine("                                                      ");
            Console.WriteLine(" ____  _     ____  _____ ____  _      _____ _____ ____ ");
            Console.WriteLine("/ ___\\/ \\ /\\/  __\\/  __//  __\\/ \\__/|/  __//  __//  _ \\");
            Console.WriteLine("|    \\| | |||  \\/||  \\  |  \\/|| |\\/|||  \\  | |  _| / \\|");
            Console.WriteLine("\\___ || \\_/||  __/|  /_ |    /| |  |||  /_ | |_//| |-||");
            Console.WriteLine("\\____/\\____/\\_/   \\____\\\\_/\\_\\\\_/  \\|\\____\\\\____\\\\_/ \\|");
            Console.WriteLine("                                                      ");
        }
    }
}