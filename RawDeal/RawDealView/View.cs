namespace RawDealView;

public class View
{
    private readonly AbstractView _view;

    public static View BuildConsoleView()
        => new View(new ConsoleView());
    
    public static View BuildTestingView(string pathTestScript)
        => new View(new TestingView(pathTestScript));

    private View(AbstractView newView)
        => _view = newView;
    
    public void ExportScript(string path)
        => _view.ExportScript(path);

    public string[] GetScript()
        => _view.GetScript();

    public void SayThatDeckIsInvalid()
    {
        _view.WriteLine($"El mazo ingresado es inválido.");
        
    }

    public void SayThatATurnBegins(string superstarName)
    {
        _view.WriteLine("\n--------------------------------------------");
        _view.WriteLine($"Comienza el turno de {superstarName}.");
    }

    public void ShowGameInfo(PlayerInfo player1, PlayerInfo player2)
    {
        _view.WriteLine("--------------------------------------------");
        _view.WriteLine(player1);
        _view.WriteLine(player2);
        _view.WriteLine("--------------------------------------------");
    }

    public void CongratulateWinner(string winnersName)
    {
        _view.WriteLine("\n--------------------------------------------");
        _view.WriteLine($"Felicidades, gana {winnersName}.");
    }

    public string AskUserToSelectDeck(string folder)
    {
        string[] decks = Directory.GetFiles(folder, "*.txt");
        _view.WriteLine("-------------------------");
        _view.WriteLine("Elige un mazo");
        Array.Sort(decks);
        return AskUserToSelectOption(decks);
    }
    
    public NextPlay AskUserWhatToDoWhenItIsNotPossibleToUseItsAbility()
    {
        string[] options = NextPlayOptions.GetOptionsWithoutSuperstarAbility();
        string selectedOption = AskUserToSelectOption(options);
        return NextPlayOptions.GetNextPlayFromText(selectedOption);
    }
    
    private string AskUserToSelectOption(string[] options)
    {
        ShowOptions(options);
        int idOption = AskUserToSelectANumber(1, options.Length) - 1;
        return options[idOption];
    }

    private void ShowOptions(string[] options)
    {
        for (int i = 0; i < options.Length; i++)
        {
            string normalizedOption = ReplaceBackslashesWithSlashesToFixPathIncompatibilitiesWithWindows(options[i]);
            _view.WriteLine($"{i+1}- {normalizedOption}");
        }
    }

    private string ReplaceBackslashesWithSlashesToFixPathIncompatibilitiesWithWindows(string path)
        => path.Replace("\\", "/");
    
    private int AskUserToSelectANumber(int minValue, int maxValue)
    {
        _view.WriteLine($"(Ingresa un número entre {minValue} y {maxValue})");
        int value;
        bool wasParsePossible;
        do
        {
            string? userInput = _view.ReadLine();
            wasParsePossible = int.TryParse(userInput, out value);
        } while (!wasParsePossible || IsValueOutsideTheValidRange(minValue, value, maxValue));

        return value;
    }

    private bool IsValueOutsideTheValidRange(int minValue, int value, int maxValue)
        => value < minValue || value > maxValue;
}