
using System.Text.Json;
using RawDealView;

namespace RawDeal
{
    public class Card
    {
        public string Title { get; set; }
        public List<string> Types { get; set; }
        public List<string> Subtypes { get; set; }
        public string Fortitude { get; set; }
        public string Damage { get; set; }
        public string StunValue { get; set; }
        public string CardEffect { get; set; }
    }

    public class Superstar
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public int HandSize { get; set; }
        public int SuperstarValue { get; set; }
        public string SuperstarAbility { get; set; }
    }

    public class Game
    {
        private View _view;
        private string _deckFolder;

        public Game(View view, string deckFolder)
        {
            _view = view;
            _deckFolder = deckFolder;
        }

        public void Play()
        {
            // Console.WriteLine("Cargando mazo del Jugador 1...");
            string player1DeckPath = _view.AskUserToSelectDeck(_deckFolder);
            List<string> player1Deck = LoadDeckFromFile(player1DeckPath);
            string superstarName1 = player1Deck[0].Replace(" (Superstar Card)", "");
            player1Deck.RemoveAt(0);
            
            string cardsPath = Path.Combine("data", "cards.json");
            List<Card> cardsInfo = LoadCardsInfo(cardsPath);
            
            string superstarPath = Path.Combine("data", "superstar.json");
            List<Superstar> superstarInfo = LoadSuperstarInfo(superstarPath);
            
            if (!IsDeckCompletelyValid(player1Deck, cardsInfo, superstarInfo, superstarName1))
            {
                _view.SayThatDeckIsInvalid();
                return;
            }

            // Console.WriteLine("Cargando mazo del Jugador 2...");
            string player2DeckPath = _view.AskUserToSelectDeck(_deckFolder);
            List<string> player2Deck = LoadDeckFromFile(player2DeckPath);
            string superstarName2 = player2Deck[0].Replace(" (Superstar Card)", "");
            player2Deck.RemoveAt(0);
            
            if (!IsDeckCompletelyValid(player2Deck, cardsInfo, superstarInfo, superstarName2))
            {
                _view.SayThatDeckIsInvalid();
                return;
            }

            PlayerInfo p1 = null;
            PlayerInfo p2 = null;
            int sval1 = 0;
            int sval2 = 0;
            int handSize1 = 0;
            int handSize2 = 0;

            
            
            Superstar superstar1 = superstarInfo.FirstOrDefault(s => s.Name == superstarName1);
            if (superstar1 != null)
            {
                handSize1 = superstar1.HandSize;
                sval1 = superstar1.SuperstarValue;
                p1 = new PlayerInfo(superstarName1, 0, handSize1+1, 59-handSize1);
                
            }
            
            Superstar superstar2 = superstarInfo.FirstOrDefault(s => s.Name == superstarName2);
            if (superstar2 != null)
            {
                handSize2 = superstar2.HandSize;
                sval2 = superstar2.SuperstarValue;
                p2 = new PlayerInfo(superstarName2, 0, handSize2, 60-handSize2);
                
            }
            
            if (sval1 > sval2)
            {
                _view.SayThatATurnBegins(superstarName1);
                _view.ShowGameInfo(p1, p2);
                _view.AskUserWhatToDoWhenItIsNotPossibleToUseItsAbility();
                _view.CongratulateWinner(superstarName2);
            }
            else if (sval2 > sval1)
            {
                p2 = new PlayerInfo(superstarName2, 0, handSize2+1, 59-handSize2);
                p1 = new PlayerInfo(superstarName1, 0, handSize1, 60-handSize1);
                _view.SayThatATurnBegins(superstarName2);
                _view.ShowGameInfo(p2, p1);
                _view.AskUserWhatToDoWhenItIsNotPossibleToUseItsAbility();
                _view.CongratulateWinner(superstarName1);
            }
            else
            {
                _view.SayThatATurnBegins(superstarName1);
                _view.ShowGameInfo(p1, p2);
                _view.AskUserWhatToDoWhenItIsNotPossibleToUseItsAbility();
                _view.CongratulateWinner(superstarName2);
            }
            

        }

        private List<string> LoadDeckFromFile(string filePath)
        {
            List<string> deck = new List<string>();

            try
            {
                string[] lines = File.ReadAllLines(filePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    deck.Add(lines[i]);
                }
            }
            catch (Exception ex)
            {
                // Console.WriteLine("Error al cargar el mazo desde el archivo: " + ex.Message);
            }

            return deck;
        }

        private List<Card> LoadCardsInfo(string filePath)
        {
            List<Card> cardsInfo = new List<Card>();

            try
            {
                string json = File.ReadAllText(filePath);
                cardsInfo = JsonSerializer.Deserialize<List<Card>>(json);
            }
            catch (Exception ex)
            {
                // Console.WriteLine("Error al cargar la informacion de las cartas: " + ex.Message);
            }

            return cardsInfo;
        }

        private List<Superstar> LoadSuperstarInfo(string filePath)
        {
            List<Superstar> superstarInfo = new List<Superstar>();

            try
            {
                string json = File.ReadAllText(filePath);
                superstarInfo = JsonSerializer.Deserialize<List<Superstar>>(json);
            }
            catch (Exception ex)
            {
                // Console.WriteLine("Error al cargar la informacion de las superestrellas: " + ex.Message);
            }

            return superstarInfo;
        }
        

        private bool IsDeckCompletelyValid(List<string> deck, List<Card> cardsInfo, List<Superstar> superstarInfo,
            string superstarName)
        {
            int totalFortitude = 0;
            HashSet<string> uniqueCardTitles = new HashSet<string>();
            bool hasSetupCard = false;
            bool hasHeelCard = false;
            bool hasFaceCard = false;

            Dictionary<string, int> cardCounts = new Dictionary<string, int>();

            foreach (string cardTitle in deck)
            {
                Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                if (card == null)
                {
                    // Console.WriteLine("Card not found: " + cardTitle);
                    return false;
                }

                // Console.WriteLine("Card Title: " + card.Title);
                // Console.WriteLine("Card Subtypes: " + string.Join(", ", card.Subtypes));
                totalFortitude += int.Parse(card.Fortitude);

                if (!cardCounts.ContainsKey(card.Title))
                {
                    cardCounts[card.Title] = 1;
                }
                else
                {
                    cardCounts[card.Title]++;
                }

                if (card.Subtypes.Contains("Unique"))
                {
                    if (!card.Subtypes.Contains("SetUp")) 
                    {
                        if (uniqueCardTitles.Contains(card.Title))
                        {
                            return false;
                        }
                
                        uniqueCardTitles.Add(card.Title);
                    }
                }

                if (card.Subtypes.Contains("SetUp"))
                {
                    hasSetupCard = true;
                }

                if (card.Subtypes.Contains("Heel"))
                {
                    hasHeelCard = true;
                }

                if (card.Subtypes.Contains("Face"))
                {
                    hasFaceCard = true;
                }
            }

            foreach (var pair in cardCounts)
            {
                Card currentCard = cardsInfo.FirstOrDefault(c => c.Title == pair.Key);
                if (pair.Value > 3 && (currentCard == null || !currentCard.Subtypes.Contains("SetUp")))
                {
                    return false;
                }
            }

            Superstar superstar = superstarInfo.FirstOrDefault(s => s.Name == superstarName);
            if (deck.Count != 60 || (hasHeelCard && hasFaceCard) || superstar == null)
            {
                // Console.WriteLine("El mazo no tiene 61 cartas. O el mazo tiene heel y fface");
                return false;
            }

            foreach (string cardTitle in deck)
            {
                Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                if (card.Subtypes.Any(subtype =>
                        superstarInfo.Any(s => s.Logo == subtype) && subtype != superstar.Logo))
                {
                    return false;
                }
            }

            return true;
        }

    }
}
        
        