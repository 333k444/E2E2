
using System.Text.Json;
using RawDealView;
using RawDealView.Options;

namespace RawDeal
{
    
    public class GameEndException : Exception
    {
        public string Winner { get; private set; }

        public GameEndException(string winner)
        {
            Winner = winner;
        }
    }
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
    
    public class Player 
    {
        public string Name { get; set; }
        public List<string> Deck { get; set; } = new List<string>();
        public List<string> Hand { get; set; } = new List<string>();
        public List<string> RingArea { get; set; } = new List<string>();
        public List<string> RingsidePile { get; set; } = new List<string>();
        public int FortitudeRating { get; set; }
    }
    
    public struct PlayerState
    {
        public string Name;
        public string SuperstarAbility;
        public List<string> Hand;
        public List<string> Ringside;
    }



    public class Superstar
    {
        public string Name { get; set; }
        public string Logo { get; set; }
        public int HandSize { get; set; }
        public int SuperstarValue { get; set; }
        public string SuperstarAbility { get; set; }
    }

    public class CardInfo : RawDealView.Formatters.IViewableCardInfo
    {
        public string Title { get; set; }
        public string Fortitude { get; set; }
        public string Damage { get; set; }
        public string StunValue { get; set; }
        public List<string> Types { get; set; }
        public List<string> Subtypes { get; set; }
        public string CardEffect { get; set; }

        public CardInfo(string title, string fortitude, string damage, string stunValue, List<string> types,
            List<string> subtypes, string cardEffect)
        {
            Title = title;
            Fortitude = fortitude;
            Damage = damage;
            StunValue = stunValue;
            Types = types;
            Subtypes = subtypes;
            CardEffect = cardEffect;
        }
    }
    
    public class PlayInfo : RawDealView.Formatters.IViewablePlayInfo
    {
        public string Title { get; set; }
        public string Type { get; set; }
        public string Fortitude { get; set; }
        public string Damage { get; set; }
        public string StunValue { get; set; }
        public RawDealView.Formatters.IViewableCardInfo CardInfo { get; set; }
        public string PlayedAs { get; set; } 

        public PlayInfo(string title, string type, string fortitude, string damage, string stunValue, RawDealView.Formatters.IViewableCardInfo cardInfo, string playedAs)
        {
            Title = title;
            Type = type;
            Fortitude = fortitude;
            Damage = damage;
            StunValue = stunValue;
            CardInfo = cardInfo;
            PlayedAs = playedAs; 
        }
    }

    
    public class Game
    {
        private View _view;
        private string _deckFolder;
        private int startingPlayer = 0;
        private List<string> player1Hand = new List<string>();
        private List<string> player2Hand = new List<string>();
        private List<string> player1RingsidePile = new List<string>();
        private List<string> player2RingsidePile = new List<string>();
        private List<string> player1RingArea = new List<string>(); 
        private List<string> player2RingArea = new List<string>();
        private int player1FortitudeRating;
        private int player2FortitudeRating;
        private bool usedJockeying;
        private bool wasDamageIncreased;
        private int JockeyingTurn;
        private int JockeyingEffect;
        private List<Card> cardsInfo; 
        private Superstar superstar1;
        private Superstar superstar2;
        private bool isSelfInflictedDamage;
        private PlayerInfo player1;
        private PlayerInfo player2;
        private bool abilityUsedThisTurn;
        
        
        public Game(View view, string deckFolder)
        {
            _view = view;
            _deckFolder = deckFolder;
        }

        public void Play()
        {
            try
            {
                List<string> player1Deck = LoadAndValidateDeck(out var superstarName1);
                if (player1Deck == null) return;

                List<string> player2Deck = LoadAndValidateDeck(out var superstarName2);
                if (player2Deck == null) return;

                InitializePlayerHands(superstarName1, player1Deck, 1);
                InitializePlayerHands(superstarName2, player2Deck, 2);
                DecideStartingPlayer(superstarName1, superstarName2);
                    
                Dictionary<string, object> CreateGameDataDictionary(int turno)
                {
                    Dictionary<string, object> gameData = new Dictionary<string, object>();
                    gameData["playerHand"] = (turno == 1) ? player1Hand : player2Hand;
                    gameData["playerDeck"] = (turno == 1) ? player1Deck : player2Deck;
                    gameData["playerRingArea"] = (turno == 1) ? player1RingArea : player2RingArea;
                    gameData["playerRingAreaOpponent"] = (turno == 1) ? player2RingArea : player1RingArea;
                    gameData["playerHandOpponent"] = (turno == 1) ? player2Hand : player1Hand;
                    gameData["playerRingSidePile"] = (turno == 1) ? player1RingsidePile : player2RingsidePile;
                    gameData["playerDeckOpponent"] = (turno == 1) ? player2Deck : player1Deck; 
                    gameData["ringSidePileOpponent"] = (turno == 1) ? player2RingsidePile : player1RingsidePile; 
                    gameData["cardsInfo"] = cardsInfo; 
                    gameData["superStarName"] = (turno == 1) ? superstarName1 : superstarName2;
                    gameData["superStarNameOpponent"] = (turno == 1) ? superstarName2 : superstarName1;
                    gameData["playerFortitude"] = (turno == 1) ? player1FortitudeRating : player2FortitudeRating; 
                    gameData["playerFortitudeOpponent"] = (turno == 1) ? player2FortitudeRating : player1FortitudeRating; 
                    gameData["turno"] = turno;
                    gameData["turnoOpponent"] = (turno == 1) ? 2 : 1;
                    gameData["JockeyingEffect"] = JockeyingEffect; 
                    gameData["JockeyingTurn"] = JockeyingTurn; 
                    gameData["usedJocking"] = usedJockeying; 

                    return gameData;
                }


                List<string> LoadAndValidateDeck(out string superstarName)
                {
                    string deckPath = _view.AskUserToSelectDeck(_deckFolder);
                    List<string> deck = LoadDeckFromFile(deckPath);
                    superstarName = ExtractSuperstarName(deck);

                    if (!IsDeckValid(deck, superstarName))
                    {
                        _view.SayThatDeckIsInvalid();
                        return null;
                    }

                    return deck;
                }

                string ExtractSuperstarName(List<string> deck)
                {
                    string name = deck[0].Replace(" (Superstar Card)", "");
                    deck.RemoveAt(0);
                    return name;
                }

                bool IsDeckValid(List<string> deck, string superstarName)
                {
                    string cardsPath = Path.Combine("data", "cards.json");
                    cardsInfo = LoadCardsInfo(cardsPath);


                    string superstarPath = Path.Combine("data", "superstar.json");
                    List<Superstar> superstarInfo = LoadSuperstarInfo(superstarPath);

                    return IsDeckCompletelyValid(deck, cardsInfo, superstarInfo, superstarName);
                }


                void InitializePlayerHands(string superstarName, List<string> deck, int playerNumber)
                {
                    var superstar = FindSuperstar(superstarName);
                    if (superstar == null) return;

                    var hand = DeterminePlayerHand(playerNumber);
                    PopulateHandWithCards(superstar, hand, deck);
                }

                List<string> DeterminePlayerHand(int playerNumber)
                {
                    var hand = (playerNumber == 1) ? player1Hand : player2Hand;
                    hand.Clear();
                    return hand;
                }

                void PopulateHandWithCards(Superstar superstar, List<string> hand, List<string> deck)
                {
                    int cardsToAdd = Math.Min(superstar.HandSize, deck.Count);
                    AddCardsToHand(hand, deck, cardsToAdd);
                }
                

                Superstar FindSuperstar(string superstarName)
                {
                    var superstarInfo = LoadSuperstarInfo(Path.Combine("data", "superstar.json"));
                    return superstarInfo.FirstOrDefault(s => s.Name == superstarName);
                }


                void AddCardsToHand(List<string> hand, List<string> deck, int cardsToAdd)
                {
                    hand.AddRange(deck.GetRange(deck.Count - cardsToAdd, cardsToAdd));
                    hand.Reverse();
                    deck.RemoveRange(deck.Count - cardsToAdd, cardsToAdd);
                }


                void DecideStartingPlayer(string superstarName1, string superstarName2)
                {
                    LoadSuperstars(superstarName1, superstarName2);

                    DetermineStartingPlayerAndBeginActions();
                }

                void LoadSuperstars(string superstarName1, string superstarName2)
                {
                    var superstarInfo = LoadSuperstarInfo(Path.Combine("data", "superstar.json"));
                    superstar1 = superstarInfo.FirstOrDefault(s => s.Name == superstarName1);
                    superstar2 = superstarInfo.FirstOrDefault(s => s.Name == superstarName2);
                }

                void DetermineStartingPlayerAndBeginActions()
                {
                    if (superstar1.SuperstarValue >= superstar2.SuperstarValue)
                    {
                        startingPlayer = 1;
                        HandlePlayerActions(1);
                    }
                    else
                    {
                        startingPlayer = 2;
                        HandlePlayerActions(2);
                    }
                }


                void HandlePlayerActions(int turno)
                {
                    InitializeTurnStatus();

                    PlayerInfo player1 = CreatePlayerInfo(superstar1.Name, player1FortitudeRating, player1Hand.Count, player1Deck.Count);
                    PlayerInfo player2 = CreatePlayerInfo(superstar2.Name, player2FortitudeRating, player2Hand.Count, player2Deck.Count);
                    ExecuteTurnBasedActions(turno, player1, player2);

                    HandleContinuousActions(turno);
                }

                void InitializeTurnStatus()
                {
                    abilityUsedThisTurn = false;
                }

                PlayerInfo CreatePlayerInfo(string name, int fortitude, int handCount, int deckCount)
                {
                    return new PlayerInfo(name, fortitude, handCount, deckCount);
                }

                void ExecuteTurnBasedActions(int turno, PlayerInfo player1, PlayerInfo player2)
                {
                    AnnounceTurnBegin(turno);
                    UseSpecialAbilities(turno);
                    if (turno == 1) HandleTurn(player1, player2, turno);
                    else HandleTurn(player2, player1, turno);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                void AnnounceTurnBegin(int turno)
                {
                    _view.SayThatATurnBegins(turno == 1 ? superstarName1 : superstarName2);
                }

                void HandleContinuousActions(int turno)
                {
                    string currentPlayer = DetermineCurrentPlayer(turno);
                    ExecuteActionsUntilGiveUp(currentPlayer, turno);
                    CongratulateWinner(turno);
                }

                string DetermineCurrentPlayer(int turno)
                {
                    return (turno == 1) ? superstarName1 : superstarName2;
                }

                void ExecuteActionsUntilGiveUp(string currentPlayer, int turno)
                {
                    NextPlay action = DetermineAction(currentPlayer);
                    while (action != NextPlay.GiveUp)
                    {
                        HandleAction(action, currentPlayer, turno);
                        action = DetermineAction(currentPlayer);
                    }
                }


                void HandleTurn(PlayerInfo current, PlayerInfo opponent, int turno)
                {
                    (List<string> currentDeck, List<string> currentHand) = GetDeckAndHandBasedOnTurn(turno);

                    DrawCard(currentDeck, currentHand);
                    HandleSpecialDraws(turno, currentDeck, currentHand);
    
                    UpdatePlayerInfo(out current, out opponent);
                    UpdatePlayerInfos();
                }

                (List<string>, List<string>) GetDeckAndHandBasedOnTurn(int turno)
                {
                    var currentDeck = (turno == 1) ? player1Deck : player2Deck;
                    var currentHand = (turno == 1) ? player1Hand : player2Hand;
                    return (currentDeck, currentHand);
                }

                void HandleSpecialDraws(int turno, List<string> currentDeck, List<string> currentHand)
                {
                    string currentPlayer = DetermineCurrentPlayer(turno);
                    if (currentPlayer == "MANKIND" && currentDeck.Count > 0)
                    {
                        DrawCard(currentDeck, currentHand);
                    }
                }

                
                NextPlay DetermineAction(string currentPlayer)
                {
                    var currentHand = GetCurrentHand(currentPlayer);
                    var currentDeck = GetCurrentDeck(currentPlayer);
                    if (CanUseAbility(currentPlayer, currentHand, currentDeck))
                        return _view.AskUserWhatToDoWhenUsingHisAbilityIsPossible();
                    return _view.AskUserWhatToDoWhenHeCannotUseHisAbility();
                }

                
                List<string> GetCurrentHand(string currentPlayer)
                {
                    return (currentPlayer == superstarName1) ? player1Hand : player2Hand;
                }
                
                List<string> GetCurrentDeck(string currentPlayer)
                {
                    return (currentPlayer == superstarName1) ? player1Deck : player2Deck;
                }


                bool CanUseAbility(string player, List<string> hand, List<string> deck)
                {
                    return !abilityUsedThisTurn && EligibleForAbility(player, hand, deck);
                }

                bool EligibleForAbility(string player, List<string> hand, List<string> deck)
                {
                    return hand.Count > 0 && (player == "THE UNDERTAKER" && hand.Count >= 2 
                                              || player == "STONE COLD STEVE AUSTIN" && deck.Count > 0
                                              || player == "CHRIS JERICHO");
                }
                

                void HandleAction(NextPlay action, string currentPlayer, int turno)
                {
                    switch (action)
                    {
                        case NextPlay.UseAbility:
                            HandleUseAbilityAction(turno);
                            break;
                        case NextPlay.ShowCards:
                            HandleShowCardsAction(turno);
                            break;
                        case NextPlay.PlayCard:
                            HandlePlayCardAction(turno);
                            break;
                        case NextPlay.EndTurn:
                            JockeyingEffect=0;
                            JockeyingTurn=0;
                            usedJockeying=false;
                            HandleEndTurnAction(turno);
                            break;
                    }
                }

                
                void HandleUseAbilityAction(int turno)
                {
                    UseSpecialTurnAbilities(turno);
                }


                void HandleShowCardsAction(int turno)
                {
                    CardSet cardSetChoice = _view.AskUserWhatSetOfCardsHeWantsToSee();
                    switch (cardSetChoice)
                    {
                        case CardSet.Hand:
                            HandleShowHandCardsAction(turno);
                            break;
                        case CardSet.RingArea:
                            HandleShowRingAreaAction(turno);
                            break;
                        case CardSet.RingsidePile:
                            HandleShowRingsidePileAction(turno);
                            break;
                        case CardSet.OpponentsRingArea:
                            HandleShowOpponentRingAreaAction(turno);
                            break;
                        case CardSet.OpponentsRingsidePile:
                            HandleShowOpponentRingsidePileAction(turno);
                            break;
                    }
                }

                
                void HandleShowHandCardsAction(int turno)
                {
                    ShowPlayerHandCards(turno == 1 ? player1Hand : player2Hand);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void HandleShowRingAreaAction(int turno)
                {
                    ShowPlayerRingArea(turno == 1 ? player1RingArea : player2RingArea);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void HandleShowRingsidePileAction(int turno)
                {
                    ShowPlayerRingsidePile(turno == 1 ? player1RingsidePile : player2RingsidePile);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void HandleShowOpponentRingAreaAction(int turno)
                {
                    ShowPlayerRingArea(turno == 1 ? player2RingArea : player1RingArea);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void HandleShowOpponentRingsidePileAction(int turno)
                {
                    ShowPlayerRingsidePile(turno == 1 ? player2RingsidePile : player1RingsidePile);
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                
                void PostPlayUpdates(int turno)
                {
                    UpdatePlayerInfos();
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }
                
                

                void HandleEndTurnAction(int turno)
                {
       
                    CheckDeckStatus(turno);
    
                    int opponentTurn = (turno == 1) ? 2 : 1;
                    HandlePlayerActions(opponentTurn);
                }

                
                void CheckDeckStatus(int turno)
                {
                    if (IsDeckEmpty(turno)) 
                    {
                        CongratulateCorrectWinner(turno);
                    }
    
                    if (IsDeckEmpty((turno == 1) ? 2 : 1)) 
                    {
                        CongratulateCorrectWinner((turno == 1) ? 2 : 1);  
                    }
                }


                bool IsDeckEmpty(int turno)
                {
                    bool isEmpty = (turno == 1 && player1Deck.Count == 0) || (turno == 2 && player2Deck.Count == 0);
                    return isEmpty;
                }


                void ShowGameInfoBasedOnCurrentTurn(int turno)
                {
                    PlayerInfo p1 = CreatePlayerInfo(superstar1.Name, player1FortitudeRating, player1Hand.Count, player1Deck.Count);
                    PlayerInfo p2 = CreatePlayerInfo(superstar2.Name, player2FortitudeRating, player2Hand.Count, player2Deck.Count);
    
                    DisplayInfoByTurn(p1, p2, turno);
                }
                

                void DisplayInfoByTurn(PlayerInfo p1, PlayerInfo p2, int turno)
                {
                    if (turno == 1)
                    {
                        _view.ShowGameInfo(p1, p2);
                    }
                    else
                    { 
                        _view.ShowGameInfo(p2, p1);
                    }
                }
                

                void UseSpecialAbilities(int turno)
                {
                    UseTheRockAbility(turno);
                    if ((turno == 1 && superstarName1.ToUpper() == "KANE") ||
                        (turno == 2 && superstarName2.ToUpper() == "KANE"))
                    {
                        ApplyKaneAbility(turno);
                    }
                }

                
                void UseSpecialTurnAbilities(int turno)
                {
                    UseUndertakerAbility(turno);
                    UseJerichoAbility(turno);
                    UseStoneColdAbility(turno);
                    
                    abilityUsedThisTurn = true;
                    ShowGameInfoBasedOnCurrentTurn(turno);
                }

                
                void CongratulateWinner(int turno)
                {
                    string winner = (turno == 1) ? superstarName2 : superstarName1;
                    _view.CongratulateWinner(winner);
                    throw new GameEndException(winner);
                }
                
                
                void CongratulateCorrectWinner(int turnoWithoutCards)
                {
                    int winningTurn = (turnoWithoutCards == 1) ? 2 : 1;
                    string winner = (winningTurn == 1) ? superstarName1 : superstarName2;
                    _view.CongratulateWinner(winner);
                    throw new GameEndException(winner);
                }

                
                void DrawCard(List<string> playerDeck, List<string> playerHand)
                {
                    if (playerDeck.Any())
                    {
                        string drawnCard = playerDeck.Last();
                        playerDeck.RemoveAt(playerDeck.Count - 1);
                        playerHand.Add(drawnCard);
                    }
                }
                
                
                void HandlePlayCardAction(int turno)
                {
                    PlayCardForPlayer(turno);
                    PostPlayUpdates(turno);
                }
                
                void PlayCardForPlayer(int turno)
                {
                    if (turno == 1) PlayCardForPlayer1();
                    else PlayCardForPlayer2();
                }
                
                void PlayCardForPlayer1()
                {
                    Dictionary<string, object> gameData = CreateGameDataDictionary(1);
                    PlayCardAction(gameData);
                }

                void PlayCardForPlayer2()
                {
                    Dictionary<string, object> gameData = CreateGameDataDictionary(2);
                    PlayCardAction(gameData);
                }


                void PlayCardAction(Dictionary<string, object> gameData)
                {
                    var playableData = PreparePlayableCards(gameData);
                    DisplayAndProcessSelection(playableData, gameData);
                }
                
                Tuple<List<Tuple<int, string>>, List<string>> PreparePlayableCards(Dictionary<string, object> gameData)
                {
                    List<string> playerHand = gameData["playerHand"] as List<string>;
                    List<Card> cardsInfo = gameData["cardsInfo"] as List<Card>;
                    int playerFortitude = (int)gameData["playerFortitude"];

                    var playableCardIndicesAndTypes = new List<Tuple<int, string>>();
                    for (int i = 0; i < playerHand.Count; i++)
                    {
                        var cardInfo = ConvertToCardInfo(playerHand[i], cardsInfo);
                        if (CardIsPlayable(cardInfo, playerFortitude))
                        {
                            foreach (var type in cardInfo.Types)
                            {
                                if (cardInfo.Types.Contains("Action") && cardInfo.Types.Contains("Reversal"))
                                {
                                    if (type == "Reversal") continue;
                                }
                                playableCardIndicesAndTypes.Add(new Tuple<int, string>(i, type));
                            }
                        }
                    }

                    List<string> cardsToDisplay = FormatPlayableCardsForDisplay(playableCardIndicesAndTypes, playerHand, cardsInfo);
                    return new Tuple<List<Tuple<int, string>>, List<string>>(playableCardIndicesAndTypes, cardsToDisplay);
                }

                
                void DisplayAndProcessSelection(Tuple<List<Tuple<int, string>>, List<string>> playableData, Dictionary<string, object> gameData)
                {
                    int selectedIndex = _view.AskUserToSelectAPlay(playableData.Item2);
                    if (selectedIndex >= 0 && selectedIndex < playableData.Item1.Count)
                    {
                        var selectedCardData = playableData.Item1[selectedIndex];
                        ProcessSelectedCard(selectedCardData.Item1, selectedCardData.Item2, gameData);
                    }
                }
                
                
                void ProcessSelectedCard(int cardIndex, string cardType, Dictionary<string, object> gameData)
                {
                    if (!gameData.ContainsKey("playerHand")) 
                    {
                        return;
                    }

                    List<string> playerHand = gameData["playerHand"] as List<string>;
                    if (cardIndex < 0 || cardIndex >= playerHand.Count) 
                    {
                        return;
                    }

                    string cardName = playerHand[cardIndex];
                    gameData["currentPlayingCard"] = cardName;
                    
                    
                    ProcessCardAction(cardIndex, cardName, cardType, gameData);
                }




                
            bool ReverseFromDeck(Dictionary<string, object> gameData, Card maneuver, string selectedType)
                {
                    int turno = (int)gameData["turno"];
                    string superStarName = gameData["superStarNameOpponent"] as string;
                    string superStarNameOpponent = gameData["superStarName"] as string;
                    
                    
                    List<Tuple<string, int>> validReversalsInHandWithIndices = GetValidReversalsFromHand(gameData, maneuver, selectedType);
                    List<string> validReversalNames = validReversalsInHandWithIndices.Select(t => t.Item1).ToList();

                    

                    
                    if (validReversalNames.Any())
                    {
                        
                        List<string> formattedReversals = FormatReversalCardsForDisplay(validReversalNames, gameData["cardsInfo"] as List<Card>);
                        int selectedReversalIndex = _view.AskUserToSelectAReversal(superStarName, formattedReversals);
    
                        if (selectedReversalIndex != -1)
                        {
                            JockeyingEffect=0;
                            JockeyingTurn=0;
                            usedJockeying=false;
                            Tuple<string, int> selectedReversalData = validReversalsInHandWithIndices[selectedReversalIndex];
                            string selectedReversalName = selectedReversalData.Item1;
                            int realIndexInHand = selectedReversalData.Item2;
                            Card reversal = (gameData["cardsInfo"] as List<Card>).FirstOrDefault(c => c.Title == selectedReversalName);
                            
                            List<string> playerHandOpponent = gameData["playerHandOpponent"] as List<string>;
                            List<string> playerPile = gameData["playerRingSidePile"] as List<string>;
                            List<string> opponentRingside = gameData["playerRingAreaOpponent"] as List<string>;

                            playerHandOpponent.RemoveAt(realIndexInHand);
                            opponentRingside.Add(selectedReversalName);
                            playerPile.Add(maneuver.Title);

                            List<string> formattedSelectedReversal = FormatReversalCardsForDisplay(new List<string> { selectedReversalName }, gameData["cardsInfo"] as List<Card>);
                            _view.SayThatPlayerReversedTheCard(superStarName, formattedSelectedReversal[0]);
                            
                            if (reversal.Title == "Manager Interferes")
                            {
                                _view.SayThatPlayerDrawCards(superStarName, 1);
                                DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                            }
                            else if (reversal.Title == "Chyna Interferes")
                            {
                                _view.SayThatPlayerDrawCards(superStarName, 2);
                                for (int i = 0; i < 2; i++)
                                {
                                    DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                                }
                            }
                            
                            else if (reversal.Title == "Clean Break" && maneuver.Title == "Jockeying for Position")
                            {
                                var currentPlayerHand = gameData["playerHand"] as List<string>;
                                var currentPlayerName = gameData["superStarName"] as string;
                                var remainingDiscards = 4; 

                                for (int i = 0; i < remainingDiscards; i++)
                                {
                                    int cardIndexToDiscard = PromptPlayerToDiscardCard(currentPlayerHand, currentPlayerName, remainingDiscards - i);
                                    if (cardIndexToDiscard >= 0 && cardIndexToDiscard < currentPlayerHand.Count)
                                    {
                                        var discardedCard = currentPlayerHand[cardIndexToDiscard];
                                        playerPile.Add(discardedCard); 
                                        currentPlayerHand.RemoveAt(cardIndexToDiscard); 
                                    }
                                    else
                                    {
                                        i--; 
                                    }     
                                }
                                _view.SayThatPlayerDrawCards(superStarName, 1);
                                DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                                JockeyingEffect=0;
                                JockeyingTurn=0;
                                usedJockeying=false;
                            }

                            

                            if (reversal != null) 
                            {
                                if (int.TryParse(reversal.Damage, out int reversalDamage))
                                {
                                    gameData["isReversalDamage"] = true; 
                                    ApplyEffectsBasedOnDamageForReversals(reversalDamage, superStarNameOpponent, gameData);
                                }
                                else if (reversal.Damage.Contains("#") && int.TryParse(maneuver.Damage, out int maneuverDamage))
                                {
                                    gameData["isReversalDamage"] = true; 
                                    ApplyEffectsBasedOnDamageForNoDamageReversals(maneuverDamage, superStarNameOpponent, gameData);
                                }
                            }

                            if (maneuver.Title != "Jockeying for Position")
                            {
                                HandleEndTurnAction(turno);
                            }
                            return true;
                        }
                    }
                    
            gameData["isReversalDamage"] = false;
            return false; 
        }

                
                
            List<Tuple<string, int>> GetValidReversalsFromHand(Dictionary<string, object> gameData, Card maneuver, string selectedType)
            {
                List<string> playerHand = gameData["playerHandOpponent"] as List<string>;
                List<Card> cardsInfo = gameData["cardsInfo"] as List<Card>;
                List<Tuple<string, int>> validReversals = new List<Tuple<string, int>>();

                
                for (int i = 0; i < playerHand.Count; i++)
                {
                    string cardName = playerHand[i];
                    Card card = cardsInfo.FirstOrDefault(c => c.Title == cardName);
                    if (JockeyingTurn != 0 && JockeyingEffect == 2)
                    {
           
                        bool isValid = card != null && card.Types.Contains("Reversal") && IsValidReversal(card, maneuver, (int)gameData["playerFortitudeOpponent"] - 8, selectedType, gameData);
                        if (isValid)
                        {
                            validReversals.Add(new Tuple<string, int>(cardName, i));
                        }
                    }
                    else
                    {
                        bool isValid = card != null && card.Types.Contains("Reversal") && IsValidReversal(card, maneuver, (int)gameData["playerFortitudeOpponent"], selectedType, gameData);
                        if (isValid)
                        {
                            validReversals.Add(new Tuple<string, int>(cardName, i));
                        }
                    
                }

                }
                return validReversals;
            }

                

               bool IsValidReversal(Card reversal, Card cardToReverse, int playerFortitude, string selectedType, Dictionary<string, object> gameData)
                {
                    if (IsValidFortitude(reversal, playerFortitude))
                    {
                        return false;
                    }

                    if (selectedType == "Maneuver")
                    {
                        if (CanReverseStrike(reversal, cardToReverse))
                        {
                            return true;
                        }

                        if (CanReverseStrikeSpecial(reversal, cardToReverse))
                        {
                            return true;
                        }

                        if (reversal.Title == "Escape Move" && cardToReverse.Subtypes.Contains("Grapple"))
                        {
                            return true;
                        }
                        else if (reversal.Title == "Escape Move")
                        {
                            Console.WriteLine($"{reversal.Title} no pudo revertir {cardToReverse.Title} ya que {cardToReverse.Title} no es de tipo 'Grapple'.");
                        }

                        if (CanReverseGrappleSpecial(reversal, cardToReverse))
                        {
                            return true;
                        }

                        if (CanReverseSpecialManeuver(reversal, cardToReverse))
                        {
                            return true;
                        }

                        if (CanReverseAnyManeuver(reversal))
                        {
                            return true;
                        }

                        if (CanReverseWithChynaInterferes(reversal))
                        {
                            return true;
                        }

                        if (CanReverseSubmission(reversal, cardToReverse))
                        {
                            return true;
                        }
                    }

                    if (CanReverseJockeyingForPosition(reversal, cardToReverse))
                    {
                        return true;
                    }

                    if (CanReverseCleanBreak(reversal, cardToReverse))
                    {
                        return true;
                    }

                    if (selectedType == "Action" && reversal.Subtypes.Contains("ReversalAction"))
                    {
                        return true;
                    }
                    
                    return false;
                }

                bool IsValidFortitude(Card reversal, int playerFortitude)
                {
                    if (int.TryParse(reversal.Fortitude, out int reversalFortitude) && reversalFortitude > playerFortitude)
                    {
                        return true;
                    }
                    return false;
                }

                bool CanReverseStrike(Card reversal, Card cardToReverse)
                {
                    return cardToReverse.Subtypes.Contains("Strike") && reversal.Subtypes.Contains("ReversalStrike");
                }

                bool CanReverseStrikeSpecial(Card reversal, Card cardToReverse)
                {
                    return reversal.Subtypes.Contains("ReversalStrikeSpecial") && cardToReverse.Subtypes.Contains("Strike") && int.Parse(cardToReverse.Damage) <= 7;
                }

                bool CanReverseGrappleSpecial(Card reversal, Card cardToReverse)
                {
                    return cardToReverse.Subtypes.Contains("Grapple") && reversal.Subtypes.Contains("ReversalGrappleSpecial") && int.TryParse(cardToReverse.Damage, out int damageValue) && damageValue <= 7;
                }

                bool CanReverseSpecialManeuver(Card reversal, Card cardToReverse)
                {
                    return reversal.Subtypes.Contains("ReversalSpecial") && reversal.Title != "Jockeying for Position" && reversal.Title != "Clean Break" && int.TryParse(cardToReverse.Damage, out int damageValue) && damageValue <= 7;
                }

                bool CanReverseAnyManeuver(Card reversal)
                {
                    return reversal.Title == "Manager Interferes";
                }

                bool CanReverseWithChynaInterferes(Card reversal)
                {
                    return reversal.Title == "Chyna Interferes" && reversal.Subtypes.Contains("ReversalSpecial") && (reversal.Subtypes.Contains("HHH") || reversal.Subtypes.Contains("Unique"));
                }

                bool CanReverseSubmission(Card reversal, Card cardToReverse)
                {
                    return cardToReverse.Subtypes.Contains("Submission") && reversal.Subtypes.Contains("ReversalSubmission");
                }

                bool CanReverseJockeyingForPosition(Card reversal, Card cardToReverse)
                {
                    return reversal.Title == "Jockeying for Position" && cardToReverse.Title == "Jockeying for Position";
                }

                bool CanReverseCleanBreak(Card reversal, Card cardToReverse)
                {
                    return reversal.Title == "Clean Break" && cardToReverse.Title == "Jockeying for Position";
                }

                
                
                void ApplyStunValue(Dictionary<string, object> gameData, RawDealView.Formatters.IViewableCardInfo cardInfo)
                {
                    string superStarName = gameData["superStarName"] as string;

                    if (int.TryParse(cardInfo.StunValue, out int stunValue) && stunValue > 0)
                    {

                        int cardsToDraw = _view.AskHowManyCardsToDrawBecauseOfStunValue(superStarName, stunValue);
                        if (cardsToDraw > 0 && cardsToDraw <= stunValue)
                        {
                            for (int i = 0; i < cardsToDraw; i++)
                            {
                                if ((gameData["playerDeck"] as List<string>).Count > 0)
                                {
                                    DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                                }
                                else
                                {
                                    break;
                                }
                            }
                        }
                    }
                }
                
              
                
                
                void ProcessCardAction(int cardIndex, string cardName, string selectedType, Dictionary<string, object> gameData)
                {
                    SetSelectedType(selectedType, gameData);
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    var cardPlayed = FindCardByTitle(cardName, GetCardsInfo(gameData));
                    RemoveCardFromHand(cardIndex, GetPlayerHand(gameData));
                    SetCurrentStunValue(cardInfo.StunValue, gameData);
                    ProcessCardByName(cardName, cardPlayed, gameData);
                }

                void SetSelectedType(string selectedType, Dictionary<string, object> gameData) => gameData["selectedType"] = selectedType;
                

                List<Card> GetCardsInfo(Dictionary<string, object> gameData) => gameData.TryGetValue("cardsInfo", out var cardsInfoObj) && cardsInfoObj is List<Card> cardsInfo ? cardsInfo : new List<Card>();

                List<string> GetPlayerHand(Dictionary<string, object> gameData) => gameData.TryGetValue("playerHand", out var playerHandObj) && playerHandObj is List<string> playerHand ? playerHand : new List<string>();

                void RemoveCardFromHand(int cardIndex, List<string> playerHand)
                {
                    if (cardIndex >= 0 && cardIndex < playerHand.Count)
                        playerHand.RemoveAt(cardIndex);
                }

                Card FindCardByTitle(string cardName, List<Card> cardPlayeds) => cardPlayeds.FirstOrDefault(c => c.Title == cardName);


             void SetCurrentStunValue(string stunValue, Dictionary<string, object> gameData)
             {
                 if (int.TryParse(stunValue, out int parsedStunValue))
                 {
                     gameData["currentStunValue"] = parsedStunValue;
                 }
                 else
                 {
                     gameData["currentStunValue"] = 0;
                 }
             }

             void ProcessCardByName(string cardName, Card cardPlayed, Dictionary<string, object> gameData)
             {
                 if (cardName == "Jockeying for Position")
                 {
                     ProcessJockeyingForPosition(cardPlayed, gameData);
                 }
                 else
                 {
                     ProcessOtherCard(cardPlayed, gameData);
                 }
             }

             void ProcessJockeyingForPosition(Card cardPlayed, Dictionary<string, object> gameData)
             {
                 string superStarName = GetSuperStarName(gameData);
                 var playInfo = CreatePlayInfo(cardPlayed);

                 SayPlayerIsTryingToPlayCard(superStarName, FormatPlayInfo(playInfo));

                 bool wasReversed = ReverseCardIfNecessary(gameData, cardPlayed);

                 HandleJockeyingResult(gameData, wasReversed);
             }

             void HandleJockeyingResult(Dictionary<string, object> gameData, bool wasReversed)
             {
                 if (wasReversed)
                 {
                     HandleReversedJockeyingForPosition(gameData);
                 }
                 else
                 {
                     HandleSuccessfulJockeyingForPosition(gameData);
                 }
             }

             string GetSuperStarName(Dictionary<string, object> gameData)
             {
                 return gameData["superStarName"] as string;
             }

             void SayPlayerIsTryingToPlayCard(string superStarName, string playInfo)
             {
                 _view.SayThatPlayerIsTryingToPlayThisCard(superStarName, playInfo);
             }

             bool ReverseCardIfNecessary(Dictionary<string, object> gameData, Card cardPlayed)
             {
                 return ReverseCardFromDeck(gameData, cardPlayed, "Action");
             }

                

             void HandleReversedJockeyingForPosition(Dictionary<string, object> gameData)
             {
    
                 List<string> playerRingAreaOpponent = gameData["playerRingAreaOpponent"] as List<string>;
                 if (playerRingAreaOpponent.LastOrDefault() == "Jockeying for Position")
                 {
                     ApplyJockeyingForPositionEffectAsReversal(gameData);
                 }
    
                 HandleEndTurnAction((int)gameData["turno"]);
             }

             void HandleSuccessfulJockeyingForPosition(Dictionary<string, object> gameData)
             {
                 _view.SayThatPlayerSuccessfullyPlayedACard();
                 ApplyJockeyingForPositionEffectAsAction(gameData);
             }


             PlayInfo CreatePlayInfo(Card cardPlayed)
             {
                 return new PlayInfo(
                     cardPlayed.Title,
                     "Action",
                     cardPlayed.Fortitude,
                     cardPlayed.Damage,
                     cardPlayed.StunValue,
                     ConvertToCardInfo(cardPlayed.Title, cardsInfo), 
                     "ACTION");
             }

             string FormatPlayInfo(PlayInfo playInfo)
             {
                 return RawDealView.Formatters.Formatter.PlayToString(playInfo);
             }

             bool ReverseCardFromDeck(Dictionary<string, object> gameData, Card card, string cardType)
             {
                 bool wasReversed = ReverseFromDeck(gameData, card, cardType);
                 return wasReversed;
             }


        void ProcessOtherCard(Card cardPlayed, Dictionary<string, object> gameData)
        {
            string superStarName = gameData["superStarName"] as string;
            string selectedType = gameData["selectedType"] as string;
            gameData["succesfullyPlayed"] = false;

            if (!cardPlayed.Subtypes.Contains("Grapple"))
            {
                JockeyingEffect = 0;
                JockeyingTurn = 0;
                usedJockeying = false;
            }

            if (cardPlayed.Types.Contains("Action") && cardPlayed.Types.Contains("Maneuver"))
            {
                if (selectedType == "Action")
                {
                    var playInfo = new PlayInfo(
                        cardPlayed.Title,
                        "Action",
                        cardPlayed.Fortitude,
                        cardPlayed.Damage,
                        cardPlayed.StunValue,
                        ConvertToCardInfo(cardPlayed.Title, cardsInfo),
                        "ACTION");

                    _view.SayThatPlayerIsTryingToPlayThisCard(superStarName, RawDealView.Formatters.Formatter.PlayToString(playInfo));

                    bool wasReversed = ReverseFromDeck(gameData, cardPlayed, selectedType);
                    if (wasReversed)
                    {
                        var cardInfoViewable = ConvertToCardInfo(cardPlayed.Title, cardsInfo);
                        ApplyStunValue(gameData, cardInfoViewable);
                    }
                    else
                    {
                        (gameData["playerRingSidePile"] as List<string>).Add(cardPlayed.Title);
                        DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                        if (! (bool)gameData["succesfullyPlayed"])
                        {
                            _view.SayThatPlayerSuccessfullyPlayedACard();  
                        }
                        _view.SayThatPlayerMustDiscardThisCard(superStarName, cardPlayed.Title);
                        _view.SayThatPlayerDrawCards(superStarName, 1);
                    }
                }
                else
                {
                    DisplayPlayerAction(superStarName, cardPlayed.Title, gameData["cardsInfo"] as List<Card>, selectedType);
                    
                    if (JockeyingTurn == (int)gameData["turno"])
                    {

                        if ((int)gameData["JockeyingEffect"] == 1 && cardPlayed.Subtypes.Contains("Grapple"))
                        {
                            IncreaseDamageOfCard(cardPlayed.Title, cardsInfo, 4);
                            wasDamageIncreased = true;
                            
                            bool wasReversed1 = ReverseFromDeck(gameData, cardPlayed, selectedType);
                            if (wasReversed1)
                            {
                                var cardInfoViewable = ConvertToCardInfo(cardPlayed.Title, cardsInfo);
                                ApplyStunValue(gameData, cardInfoViewable);
                            }
                            else
                            {
                                (gameData["playerRingArea"] as List<string>).Add(cardPlayed.Title);
                                ApplyCardSpecificEffect(cardPlayed.Title, gameData);
                            }
                            
                            if (wasDamageIncreased)
                            {
                                IncreaseDamageOfCard(cardPlayed.Title, cardsInfo, -4);
                                wasDamageIncreased = false;
                            }
                            
                            return;
                        }
                    }
                    
                    bool wasReversed = ReverseFromDeck(gameData, cardPlayed, selectedType);
                    if (wasReversed)
                    {
                        var cardInfoViewable = ConvertToCardInfo(cardPlayed.Title, cardsInfo);
                        ApplyStunValue(gameData, cardInfoViewable);
                    }
                    else
                    {
                        (gameData["playerRingArea"] as List<string>).Add(cardPlayed.Title);
                        ApplyCardSpecificEffect(cardPlayed.Title, gameData);
                    }

                    ApplyCardEffects(cardPlayed.Title, gameData);
                    
                    if (wasDamageIncreased)
                    {
                        IncreaseDamageOfCard(cardPlayed.Title, cardsInfo, -4);
                        wasDamageIncreased = false;
                    }
                }
            }
            else
            {
                DisplayPlayerAction(superStarName, cardPlayed.Title, gameData["cardsInfo"] as List<Card>, selectedType);
                
                if (JockeyingTurn == (int)gameData["turno"])
                {

                    if ((int)gameData["JockeyingEffect"] == 1 && cardPlayed.Subtypes.Contains("Grapple"))
                    {
                        IncreaseDamageOfCard(cardPlayed.Title, cardsInfo, 4);
                        
                        wasDamageIncreased = true;
                    }
                }
                
                bool wasReversed = ReverseFromDeck(gameData, cardPlayed, selectedType);
                if (wasReversed)
                {
                    var cardInfoViewable = ConvertToCardInfo(cardPlayed.Title, cardsInfo);
                    ApplyStunValue(gameData, cardInfoViewable);
                }
                else
                {
                    (gameData["playerRingArea"] as List<string>).Add(cardPlayed.Title);
                    ApplyCardSpecificEffect(cardPlayed.Title, gameData);
                }

                ApplyCardEffects(cardPlayed.Title, gameData);
                        if (wasDamageIncreased)
                        {
                            IncreaseDamageOfCard(cardPlayed.Title, cardsInfo, -4);
                            wasDamageIncreased = false;
                        }

            }
        }


       void ApplyCardSpecificEffect(string cardName, Dictionary<string, object> gameData)
            {
                _view.SayThatPlayerSuccessfullyPlayedACard();
                int originalFortitude = (int)gameData["playerFortitude"];
                string player = gameData["superStarName"] as string;
                string playerOpponent = gameData["superStarNameOpponent"] as string;
                gameData["succesfullyPlayed"] = true; 
                List<string> ringSidePileOpponent = gameData["ringSidePileOpponent"] as List<string>;
                List<string> ringSidePile = gameData["playerRingSidePile"] as List<string>;
                List<string> hand = gameData["playerHand"] as List<string>;
                List<string> handOpponent = gameData["playerHandOpponent"] as List<string>;
                List<string> arsenal = gameData["playerDeck"] as List<string>;
                
                int turn = (int)gameData["turno"];
                List<string> arsenalOpponent = gameData["playerDeckOpponent"] as List<string>;
                List<string> ringside = gameData["playerRingSidePile"] as List<string>;
                

                switch (cardName)
                {
                    case "Head Butt":
                    case "Arm Drag":
                    case "Arm Bar":
                        if (GetPlayerHand(gameData).Count > 0)  
                        {
                            PlayerDiscardsCard(player, GetPlayerHand(gameData), ringSidePile, 1);
                        }
                        break;

                    case "Bear Hug":
                    case "Choke Hold":
                    case "Ankle Lock":
                    case "Spinning Heel Kick":
                    case "Samoan Drop":
                    case "Boston Crab":
                    case "Power Slam":
                    case "Figure Four Leg Lock":
                    case "Torture Rack":
                        if (handOpponent.Count > 0) 
                        {
                            PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                        }

                        break;

                    case "Pump Handle Slam":
                        if (handOpponent.Count > 1) 
                        {
                            PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 2);
                            PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                        }
                        if (handOpponent.Count == 1) 
                        {
                            PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 2);
                        }
                        break;

                    case "Bulldog":
                        
                        if (hand.Count > 0)
                        {
                            PlayerDiscardsCard(player, GetPlayerHand(gameData), ringSidePile, 1);
                        }
                        if (handOpponent.Count > 0)
                        {
                            PlayerDiscardsCardReversed(gameData, handOpponent, ringSidePileOpponent, 1);
                        }

                        break;

                    case "Kick":
                    case "Running Elbow Smash":
 
                        if (arsenal.Count > 0)
                        {
                            _view.SayThatPlayerDamagedHimself(player, 1);
                            isSelfInflictedDamage = true;
                            ApplyEffectsBasedOnDamageForOwn(1, player, gameData);
                            

                        }
                        else
                        {
                            _view.SayThatPlayerDamagedHimself(player, 1);
                            ApplyEffectsBasedOnDamageForOwn(1, player, gameData);
                            EndGame(turn, player);
                            
                        }
                        break;

                    case "Double Leg Takedown":
                        int cardsToDraw = _view.AskHowManyCardsToDrawBecauseOfACardEffect(player, 1);
    
                        if (cardsToDraw > 0) 
                        {
                            _view.SayThatPlayerDrawCards(player, cardsToDraw);
                            DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                        }
                        else
                        {
                            _view.SayThatPlayerDrawCards(player, 0);
                        }
                        break;
                    
                    

                case "Reverse DDT":
                    int cardsToDrawForReverseDDT = _view.AskHowManyCardsToDrawBecauseOfACardEffect(player, 1);
                    
                    if (cardsToDrawForReverseDDT > 0) 
                    {
                        _view.SayThatPlayerDrawCards(player, cardsToDrawForReverseDDT);
                        DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                    }
                    break;

                case "Headlock Takedown":
                case "Standing Side Headlock":
                    _view.SayThatPlayerDrawCards(playerOpponent, 1);
                    DrawCard(gameData["playerDeckOpponent"] as List<string>, gameData["playerHandOpponent"] as List<string>);
                    break;

                case "Undertaker’s Tombstone Piledriver":
                    if (originalFortitude < 30)
                    {
                        _view.SayThatPlayerSuccessfullyPlayedACard();
                        DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                    }
                    break;

                case "Offer Handshake":
                    int cardsToDrawOffer = _view.AskHowManyCardsToDrawBecauseOfACardEffect(player, 3);
                    _view.SayThatPlayerDrawCards(player, cardsToDrawOffer);
                    for (int i = 0; i < cardsToDrawOffer; i++)
                    {
                        DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                    }
                    PlayerDiscardsCard(player, GetPlayerHand(gameData), ringSidePile, 1);
                    break;

                case "Press Slam":
                    isSelfInflictedDamage = true;
                    _view.SayThatPlayerDamagedHimself(player, 1);
                    ApplyEffectsBasedOnDamageForOwn(1, player, gameData);
                    if (handOpponent.Count > 1)
                    {
                        PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 2);
                        PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                    }
                    else if (handOpponent.Count == 1)
                    {
                        PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                    }
                    break;

                case "Fisherman's Suplex":
                    isSelfInflictedDamage = true;
                    _view.SayThatPlayerDamagedHimself(player, 1);
                    ApplyEffectsBasedOnDamageForOwn(1, player, gameData);
                    int cardsToDrawFisherman = _view.AskHowManyCardsToDrawBecauseOfACardEffect(player, 1);
                    _view.SayThatPlayerDrawCards(player, cardsToDrawFisherman);
                    for (int i = 0; i < cardsToDrawFisherman; i++)
                    {
                        DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                    }
                    break;

                case "DDT":
                    isSelfInflictedDamage = true;
                    _view.SayThatPlayerDamagedHimself(player, 1);
                    ApplyEffectsBasedOnDamageForOwn(1, player, gameData);
                    if (handOpponent.Count > 1)
                    {
                        PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 2);
                        PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                    }
                    else if (handOpponent.Count > 0)
                    {
                        PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                    }
                    break;

                case "Guillotine Stretch":
                    PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, 1);
                    int cardsToDrawGuillotine = _view.AskHowManyCardsToDrawBecauseOfACardEffect(player, 1);
                    _view.SayThatPlayerDrawCards(player, cardsToDrawGuillotine);
                    for (int i = 0; i < cardsToDrawGuillotine; i++)
                    {
                        DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                    }
                    break;

                case "Spit At Opponent":
                    if (GetPlayerHand(gameData).Count >= 2)
                    {
                        PlayerDiscardsCard(player, GetPlayerHand(gameData), ringSidePile, 1);
                        int cardsToDiscardOpponent = Math.Min(handOpponent.Count, 4);
                        for (int i = 0; i < cardsToDiscardOpponent; i++)
                        {
                            PlayerDiscardsCard(playerOpponent, handOpponent, ringSidePileOpponent, cardsToDiscardOpponent-i);
                        }
                    }
                    break;

                    case "Chicken Wing":
                    case "Puppies! Puppies!":
                    case "Recovery":
                        int cardsToRecover = _view.AskPlayerToSelectCardsToRecover(player, 2, ringSidePile);
                        for (int i = 0; i < cardsToRecover; i++)
                        {
                            MoveCardBetweenLists(ringSidePile, arsenal, 0);
                        }
                        break;
                    

                default:
                    Console.WriteLine($"[ERROR] No hay efectos definidos para la carta '{cardName}'.");
                    break;

                }
            }
                


                void ApplyJockeyingForPositionEffectAsAction(Dictionary<string, object> gameData)
                {

                    string superStarName = gameData["superStarName"] as string;

                    RawDealView.Options.SelectedEffect effectChoiceEnum = _view.AskUserToSelectAnEffectForJockeyForPosition(superStarName);
                    int effectChoice = (int)effectChoiceEnum;

                    JockeyingEffect = effectChoice + 1;
                    JockeyingTurn = (int)gameData["turno"];
                    usedJockeying = false;
                    
                }


                void ApplyJockeyingForPositionEffectAsReversal(Dictionary<string, object> gameData)
                {

                    string superStarName = gameData["superStarNameOpponent"] as string;

                    RawDealView.Options.SelectedEffect effectChoiceEnum = _view.AskUserToSelectAnEffectForJockeyForPosition(superStarName);
                    int effectChoice = (int)effectChoiceEnum;

                    JockeyingEffect = effectChoice + 1;
                    JockeyingTurn = (int)gameData["turnoOpponent"];
                    usedJockeying = false;
                    
                }

                
      
                void ExtractAndApplyEffects(string cardName, Dictionary<string, object> gameData)
                {
        
                    List<Card> cardsInfo = gameData["cardsInfo"] as List<Card>;
                    
                    string superStarNameOpponent = gameData["superStarNameOpponent"] as string;
                    
                    
                    if (! (bool)gameData["succesfullyPlayed"])
                    {
                        _view.SayThatPlayerSuccessfullyPlayedACard();  
                    }


                    int realDamage = CalculateDamage(cardName, cardsInfo);
                    
                    int adjustedDamage = (superStarNameOpponent == "MANKIND" && realDamage > 0) ? realDamage - 1 : realDamage;

   
                    ApplyEffectsBasedOnDamage(adjustedDamage, superStarNameOpponent, gameData);
                }



                
          void DisplayPlayerAction(string superStarName, string cardName, List<Card> cardsInfo, string selectedType)
          {
              var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
              var playInfo = new PlayInfo(
                  cardInfo.Title,
                  selectedType,
                  cardInfo.Fortitude,
                  cardInfo.Damage,
                  cardInfo.StunValue,
                  cardInfo,
                  selectedType.ToUpper()
              );
              

              _view.SayThatPlayerIsTryingToPlayThisCard(superStarName,
                  RawDealView.Formatters.Formatter.PlayToString(playInfo));
          }

                
                
                bool CardIsPlayable(RawDealView.Formatters.IViewableCardInfo cardInfo, int playerFortitude)
                {
                    if (!int.TryParse(cardInfo.Fortitude, out int cardFortitude))
                    {
                        return false;
                    }
                    return (cardInfo.Types.Contains("Maneuver") || cardInfo.Types.Contains("Action"))
                           && cardFortitude <= playerFortitude;
                }

                
                
                List<string> FormatPlayableCardsForDisplay(List<Tuple<int, string>> playableCardIndicesAndTypes, List<string> playerHand, List<Card> cardsInfo)
                {
                    var formattedCards = new List<string>();
                    foreach (var tuple in playableCardIndicesAndTypes)
                    {
                        int index = tuple.Item1;
                        string type = tuple.Item2;
                        var cardInfo = ConvertToCardInfo(playerHand[index], cardsInfo);
                        var playInfo = new PlayInfo(
                            cardInfo.Title,
                            type,
                            cardInfo.Fortitude,
                            cardInfo.Damage,
                            cardInfo.StunValue,
                            cardInfo,
                            type.ToUpper() 
                        );
                        formattedCards.Add(RawDealView.Formatters.Formatter.PlayToString(playInfo));
                    }
                    return formattedCards;
                }
                
                
                List<string> FormatReversalCardsForDisplay(List<string> reversalCards, List<Card> cardsInfo)
                {
                    var formattedCards = new List<string>();
                    foreach (var cardName in reversalCards)
                    {
                        var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                        var playInfo = new PlayInfo(
                            cardInfo.Title,
                            "Reversal", 
                            cardInfo.Fortitude,
                            cardInfo.Damage,
                            cardInfo.StunValue,
                            cardInfo,
                            "REVERSAL" 
                        );
                        formattedCards.Add(RawDealView.Formatters.Formatter.PlayToString(playInfo));
                    }
                    return formattedCards;
                }


                void ApplyCardEffects(string cardName, Dictionary<string, object> gameData)
                {
                    ExtractAndApplyEffects(cardName, gameData);
                }
                
                
                void ApplyEffectsBasedOnDamage(int damage, string superStarNameOpponent, Dictionary<string, object> gameData)
                {
                    if (JockeyingTurn != 0 && JockeyingEffect == 1)
                    {
                        IncreaseFortitudeForSuperstar(damage-4, superStarNameOpponent, gameData);  
                    }
                    else
                    {
                        IncreaseFortitudeForSuperstar(damage, superStarNameOpponent, gameData);  
                    }
                    
                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarNameOpponent, damage);
                    OverturnCardsForDamage(damage, gameData);
                }
                
                void ApplyEffectsBasedOnDamageForReversals(int damage, string superStarName, Dictionary<string, object> gameData)
                {
                    string superStarNameOpponent = gameData["superStarNameOpponent"] as string;
                    string superStarNameActual = gameData["superStarName"] as string;
                    
                    int originalDamage = damage;
                    
                    if (superStarNameActual == "MANKIND" && damage > 0)
                    {
                        damage -= 1;
                    }
                    
                    IncreaseFortitudeForSuperstarForReversals(originalDamage, superStarNameOpponent, gameData); 

                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarName, damage);
                    OverturnCardsForDamageForReversals(damage, gameData);
                }
                
                void ApplyEffectsBasedOnDamageForOwn(int damage, string superStarName, Dictionary<string, object> gameData)
                {
                    List<string> playerDeck = gameData["playerDeck"] as List<string>;
                    if (damage <= 0) return;
                    
                    _view.SayThatSuperstarWillTakeSomeDamage(superStarName, damage);

                    if (playerDeck.Count <= 0)
                    {
                        _view.SayThatPlayerLostDueToSelfDamage(superStarName);
                        HandleEndTurnAction((int)gameData["turno"]);
                        return;
                    }
                    OverturnCardsForDamageForReversals(damage, gameData);
                }


                void ApplyEffectsBasedOnDamageForNoDamageReversals(int damage, string superStarName, Dictionary<string, object> gameData)
                {
                    string superStarNameActual = gameData["superStarName"] as string;
                    string superStarNameOpponent = gameData["superStarNameOpponent"] as string;
                    
                    if (superStarNameActual == "MANKIND" && damage > 0)
                    {
                        damage -= 1;
                    }
                    if (superStarNameOpponent == "MANKIND" && damage > 0)
                    {
                        damage -= 1;
                    }
                    

                    if (damage <= 0) return;

                    _view.SayThatSuperstarWillTakeSomeDamage(superStarName, damage);
                    OverturnCardsForDamageForReversals(damage, gameData);
                }

                
                void OverturnCardsForDamageForReversals(int damage, Dictionary<string, object> gameData)
                {
                    List<string> playerDeck = gameData["playerDeck"] as List<string>;
                    for (int i = 0; i < damage; i++)
                        ProcessDamageForReversals(i, damage, gameData, playerDeck, gameData["playerRingSidePile"] as List<string>, gameData["cardsInfo"] as List<Card>);
                }

                void IncreaseFortitudeForSuperstar(int damage, string superStarNameOpponent, Dictionary<string, object> gameData)
                {

                    int turno = (int)gameData["turno"];
                    

                    int fortitudeIncrease = (superStarNameOpponent == "MANKIND") ? damage + 1 : damage;

                    IncreaseFortitude(fortitudeIncrease, turno);
                }
                
                void IncreaseFortitudeForSuperstarForReversals(int damage, string superStarNameOpponent, Dictionary<string, object> gameData)
                { ;

                    int turno = (int)gameData["turnoOpponent"];

                    if (damage > 0)
                    {
                        int fortitudeIncrease = damage;

                        IncreaseFortitude(fortitudeIncrease, turno);
                    }   
                }


                
                int CalculateDamage(string cardName, List<Card> cardsInfo)
                {
                    var cardInfo = cardsInfo.FirstOrDefault(card => card.Title == cardName);
                    int damageValue = (cardInfo != null && int.TryParse(cardInfo.Damage, out int result)) ? result : 0;
                    return damageValue;
                }
                

                void IncreaseFortitude(int fortitudeValue, int playerId)
                {
                    if (playerId == 1)
                    {
                        player1FortitudeRating += fortitudeValue;
                    }
                    else if (playerId == 2)
                    {
                        player2FortitudeRating += fortitudeValue;
                    }
                }
                
                void OverturnCardsForDamage(int damage, Dictionary<string, object> gameData)
                {
                    
                    List<string> playerDeckOpponent = gameData["playerDeckOpponent"] as List<string>;
                    for (int i = 0; i < damage; i++)
                        ProcessDamage(i, damage, gameData, playerDeckOpponent, gameData["ringSidePileOpponent"] as List<string>, gameData["cardsInfo"] as List<Card>);
                }

                
                void ProcessDamage(int i, int damage, Dictionary<string, object> gameData, List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo)
                {
                    if (playerDeckOpponent.Count == 0) EndGame((int)gameData["turno"], gameData["superStarName"] as string);
                    HandleCard(playerDeckOpponent, ringSidePileOpponent, cardsInfo, i, damage, gameData);
                }
                
                void ProcessDamageForReversals(int i, int damage, Dictionary<string, object> gameData, List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo)
                {
                    if (playerDeckOpponent.Count == 0) EndGame((int)gameData["turnoOpponent"], gameData["superStarNameOpponent"] as string);
                    HandleCard(playerDeckOpponent, ringSidePileOpponent, cardsInfo, i, damage, gameData);
                }

                
              void HandleCard(List<string> playerDeckOpponent, List<string> ringSidePileOpponent, List<Card> cardsInfo, int i, int damage, Dictionary<string, object> gameData)
            {
                string cardName = playerDeckOpponent.Last();
                int turno = (int)gameData["turno"];
                string superStarName = gameData["superStarName"] as string;
                string superStarNameOpponent = gameData["superStarNameOpponent"] as string;

                string currentPlayingCardName = gameData["currentPlayingCard"] as string;
                Card currentPlayingCard = cardsInfo.FirstOrDefault(c => c.Title == currentPlayingCardName);

                playerDeckOpponent.RemoveAt(playerDeckOpponent.Count - 1);
                ringSidePileOpponent.Add(cardName);

                Card topCard = cardsInfo.FirstOrDefault(c => c.Title == cardName);

                if (wasDamageIncreased)
                {
                    IncreaseDamageOfCard(currentPlayingCardName, cardsInfo, -4);
                }

                _view.ShowCardOverturnByTakingDamage(RawDealView.Formatters.Formatter.CardToString(ConvertToCardInfo(cardName, cardsInfo)), i + 1, damage);

                if (wasDamageIncreased) 
                {
                    IncreaseDamageOfCard(currentPlayingCardName, cardsInfo, 4);
                }

                int originalFortitude = (int)gameData["playerFortitudeOpponent"];
                if ((int)gameData["JockeyingTurn"] != 0 && (int)gameData["JockeyingEffect"] == 2)
                {
                    gameData["playerFortitudeOpponent"] = originalFortitude - 8;
                }
                
                if (gameData.ContainsKey("isReversalDamage") && (bool)gameData["isReversalDamage"])
                {
                    return;
                }
                
                if (isSelfInflictedDamage)
                {
                    isSelfInflictedDamage = false;
                    return;
                }

                if (topCard != null && topCard.Types.Contains("Reversal") && IsValidReversal(topCard, currentPlayingCard, (int)gameData["playerFortitudeOpponent"], (string)gameData["selectedType"], gameData))
                {
                    if (wasDamageIncreased) 
                    {
                        IncreaseDamageOfCard(currentPlayingCardName, cardsInfo, -4);
                        
                        wasDamageIncreased = false;
                    }

                    _view.SayThatCardWasReversedByDeck(superStarNameOpponent);
                    JockeyingEffect = 0;
                    JockeyingTurn = 0;
                    usedJockeying = false;

                    if (i + 1 == damage) 
                    {
                        HandleEndTurnAction(turno);
                        return;
                    }

                    if (gameData.ContainsKey("currentStunValue"))
                    {
                        int currentStunValue = (int)gameData["currentStunValue"];

                        if (currentStunValue > 0)
                        {
                            int cardsToDraw = _view.AskHowManyCardsToDrawBecauseOfStunValue(superStarName, currentStunValue);
                            _view.SayThatPlayerDrawCards(superStarName, cardsToDraw);
                            for (int drawIndex = 0; drawIndex < cardsToDraw; drawIndex++)
                            {
                                DrawCard(gameData["playerDeck"] as List<string>, gameData["playerHand"] as List<string>);
                            }
                            gameData["currentStunValue"] = 0;  
                        }
            
                    }
                    else
                    {;
                    }
                    HandleEndTurnAction(turno);
                }
                gameData["playerFortitudeOpponent"] = originalFortitude;
                JockeyingEffect=0;
                JockeyingTurn=0;
                usedJockeying=false;
                isSelfInflictedDamage = false;

            }


                

                void DisplayCards(List<string> cardNames)
                {
                    List<RawDealView.Formatters.IViewableCardInfo> viewableCardsInfo = 
                        cardNames.Select(cardName => ConvertToCardInfo(cardName, cardsInfo)).ToList();

                    List<string> cardsToDisplay = viewableCardsInfo
                        .Select(cardInfo => RawDealView.Formatters.Formatter.CardToString(cardInfo))
                        .ToList();

                    _view.ShowCards(cardsToDisplay);
                }

                void ShowPlayerHandCards(List<string> playerHand)
                {
                    DisplayCards(playerHand);
                }

                void ShowPlayerRingArea(List<string> ringArea)
                {
                    DisplayCards(ringArea);
                }

                void ShowPlayerRingsidePile(List<string> ringsidePile)
                {
                    DisplayCards(ringsidePile);
                }


                void IncreaseDamageOfCard(string cardName, List<Card> cardsInfo, int additionalDamage)
                    {
                        var card = cardsInfo.FirstOrDefault(c => c.Title == cardName);
                        if (card != null && int.TryParse(card.Damage, out int currentDamage))
                        {
                            card.Damage = (currentDamage + additionalDamage).ToString();
                        }
                    }
                
                
                
                RawDealView.Formatters.IViewableCardInfo ConvertToCardInfo(string cardName, List<Card> cardsInfoList)
                {
                    var cardData = cardsInfoList.FirstOrDefault(c => c.Title == cardName);
                    if (cardData != null)
                    {
                        return new CardInfo(cardData.Title, cardData.Fortitude, cardData.Damage, cardData.StunValue,
                            cardData.Types, cardData.Subtypes, cardData.CardEffect);
                    }
                    return null; 
                }
                
                

                void EndGame(int turno, string superStarName)
                {
                    CongratulateWinner((turno == 1) ? 2 : 1);
                    throw new GameEndException(superStarName);
                }


                void UpdatePlayerInfo(out PlayerInfo player1, out PlayerInfo player2)
                {
                    player1 = new PlayerInfo(superstarName1, player1FortitudeRating,
                        player1Hand.Count,
                        player1Deck.Count);
                    player2 = new PlayerInfo(superstarName2, player2FortitudeRating,
                        player2Hand.Count,
                        player2Deck.Count);
                }


                void UpdatePlayerInfos()
                {
                    player1 = new PlayerInfo(superstar1.Name, player1FortitudeRating, player1Hand.Count,
                        player1Deck.Count);
                    player2 = new PlayerInfo(superstar2.Name, player2FortitudeRating, player2Hand.Count,
                        player2Deck.Count);
                }


                List<string> LoadDeckFromFile(string filePath)
                {
                    {
                        return File.ReadAllLines(filePath).ToList();
                    }
                }


                List<Card> LoadCardsInfo(string filePath)
                {
                    List<Card> cardsInfo = new List<Card>();
                    string json = File.ReadAllText(filePath);
                    cardsInfo = JsonSerializer.Deserialize<List<Card>>(json);
                
                    return cardsInfo;
                }


                List<Superstar> LoadSuperstarInfo(string filePath)
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        return JsonSerializer.Deserialize<List<Superstar>>(json) ?? new List<Superstar>();
                    }
                    catch (Exception ex)
                    {
                        return new List<Superstar>();
                    }
                }
                

                bool IsEveryCardValid(List<string> deck, List<Card> cardsInfo)
                {
                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                        if (card == null)
                        {
                            return false;
                        }
                    }
                    return true;
                }


                bool HasUniqueTitles(List<string> deck, List<Card> cardsInfo)
                {
                    HashSet<string> uniqueCardTitles = new HashSet<string>();

                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);

                        if (card.Subtypes.Contains("Unique") && !card.Subtypes.Contains("SetUp"))
                        {
                            if (uniqueCardTitles.Contains(card.Title))
                            {
                                return false;
                            }
                            uniqueCardTitles.Add(card.Title);
                        }
                    }

                    return true;
                }


                bool AreCardTitlesUnique(List<string> deck, List<Card> cardsInfo)
                {
                    return IsEveryCardValid(deck, cardsInfo) && HasUniqueTitles(deck, cardsInfo);
                }

                
                bool IsCardPresentInInfo(List<string> deck, List<Card> cardsInfo)
                {
                    foreach (string cardTitle in deck)
                    {
                        Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);
                        if (card == null)
                        {
                            return false;
                        }
                    }
                    return true;
                }
                
                
                bool HasSubtype(string cardTitle, List<Card> cardsInfo, string subtype)
                {
                    Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);
                    return card?.Subtypes.Contains(subtype) ?? false;
                }

                
                bool BothHeelAndFacePresent(bool hasHeelCard, bool hasFaceCard)
                {
                    return hasHeelCard && hasFaceCard;
                }

                
                bool CheckSubtypes(List<string> deck, List<Card> cardsInfo)
                {
                    bool hasSetupCard = deck.Any(cardTitle => HasSubtype(cardTitle, cardsInfo, "SetUp"));
                    bool hasHeelCard = deck.Any(cardTitle => HasSubtype(cardTitle, cardsInfo, "Heel"));
                    bool hasFaceCard = deck.Any(cardTitle => HasSubtype(cardTitle, cardsInfo, "Face"));

                    return !BothHeelAndFacePresent(hasHeelCard, hasFaceCard);
                }
                
                
                bool AreSubtypesValid(List<string> deck, List<Card> cardsInfo)
                {
                    return IsCardPresentInInfo(deck, cardsInfo) && CheckSubtypes(deck, cardsInfo);
                }

                
                bool IsValidCardTitlesAndSubtypes(List<string> deck, List<Card> cardsInfo)
                {
                    return AreCardTitlesUnique(deck, cardsInfo) && AreSubtypesValid(deck, cardsInfo);
                }
                

                bool IsCardInInfo(string cardTitle, List<Card> cardsInfo)
                {
                    return cardsInfo.Any(c => c.Title == cardTitle);
                }

                
                void UpdateCardCounts(string cardTitle, Dictionary<string, int> cardCounts)
                {
                    if (!cardCounts.ContainsKey(cardTitle))
                    {
                        cardCounts[cardTitle] = 1;
                    }
                    else
                    {
                        cardCounts[cardTitle]++;
                    }
                }

                
                bool ExceedsMaxCardLimit(Dictionary<string, int> cardCounts, List<Card> cardsInfo)
                {
                    foreach (var pair in cardCounts)
                    {
                        Card currentCard = cardsInfo.FirstOrDefault(c => c.Title == pair.Key);
                        if (pair.Value > 3 && (currentCard == null || !currentCard.Subtypes.Contains("SetUp")))
                        {
                            return true;
                        }
                    }
                    return false;
                }

                
                bool HasValidDeckSize(List<string> deck)
                {
                    return deck.Count == 60;
                }

                
                bool IsValidCardCount(List<string> deck, List<Card> cardsInfo)
                {
                    Dictionary<string, int> cardCounts = new Dictionary<string, int>();

                    foreach (string cardTitle in deck)
                    {
                        if (!IsCardInInfo(cardTitle, cardsInfo))
                        {
                            return false;
                        }
                        UpdateCardCounts(cardTitle, cardCounts);
                    }

                    return !ExceedsMaxCardLimit(cardCounts, cardsInfo) && HasValidDeckSize(deck);
                }

                
            bool IsValidDeckStructure(List<string> deck, List<Card> cardsInfo)
            {
                return IsValidCardTitlesAndSubtypes(deck, cardsInfo) && IsValidCardCount(deck, cardsInfo);
            }

                
            Superstar GetSuperstarByName(List<Superstar> superstarInfo, string superstarName)
            {
                return superstarInfo.FirstOrDefault(s => s.Name == superstarName);
            }

            
            bool IsInvalidSubtypeForSuperstar(Card card, Superstar superstar, List<Superstar> superstarInfo)
            {
                return card.Subtypes.Any(subtype => superstarInfo.Any(s => s.Logo == subtype) && subtype != superstar.Logo);
            }

            
            bool AreAllCardsValidForSuperstar(List<string> deck, List<Card> cardsInfo, Superstar superstar, List<Superstar> superstarInfo)
            {
                foreach (string cardTitle in deck)
                {
                    Card card = cardsInfo.FirstOrDefault(c => c.Title == cardTitle);
                    if (IsInvalidSubtypeForSuperstar(card, superstar, superstarInfo))
                    {
                        return false;
                    }
                }
                return true;
            }

            
            bool IsValidDeckForSuperstar(List<string> deck, List<Card> cardsInfo, List<Superstar> superstarInfo, string superstarName)
            {
                Superstar superstar = GetSuperstarByName(superstarInfo, superstarName);
                if (superstar == null)
                {
                    return false;
                }

                return AreAllCardsValidForSuperstar(deck, cardsInfo, superstar, superstarInfo);
            }
                
                
            bool IsDeckCompletelyValid(List<string> deck, List<Card> cardsInfo, List<Superstar> superstarInfo, string superstarName)
            {
                return IsValidDeckStructure(deck, cardsInfo) && IsValidDeckForSuperstar(deck, cardsInfo, superstarInfo, superstarName);
            }
                
                
            void ApplyKaneAbility(int turn)
            {
                if (IsTurnPlayerOne(turn))
                {
                    UseAbility("KANE", superstar1.SuperstarAbility, superstarName2, player2Deck, player2RingsidePile);
                }
                else
                {
                    UseAbility("KANE", superstar2.SuperstarAbility, superstarName1, player1Deck, player1RingsidePile);
                }
            }

            
            bool IsTurnPlayerOne(int turn)
            {
                return turn == 1;
            }

            
            void UseAbility(string player, string ability, string opponentName, List<string> opponentDeck, List<string> opponentRingside)
            {
                AnnounceAbilityUsage(player, ability);
                AnnounceDamage(opponentName);
                ApplyDamageToDeck(opponentDeck, opponentRingside);
            }

            
            void AnnounceAbilityUsage(string player, string ability)
            {
                _view.SayThatPlayerIsGoingToUseHisAbility(player, ability);
            }

            
            void AnnounceDamage(string opponentName)
            {
                _view.SayThatSuperstarWillTakeSomeDamage(opponentName, 1);
            }

            
            void ApplyDamageToDeck(List<string> opponentDeck, List<string> opponentRingside)
            {
                if (!opponentDeck.Any()) return;
                var overturnedCardName = OverturnCard(opponentDeck);
                opponentRingside.Add(overturnedCardName);
                ShowOverturnedCard(overturnedCardName);
            }

                
            string OverturnCard(List<string> deck)
            {
                string cardName = deck.Last();
                deck.RemoveAt(deck.Count - 1);
                return cardName;
            }

            
            void ShowOverturnedCard(string cardName)
            {
                var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                string cardInfoString = RawDealView.Formatters.Formatter.CardToString(cardInfo);
                _view.ShowCardOverturnByTakingDamage(cardInfoString, 1, 1);
            }
                

            void UseTheRockAbility(int turn)
            {
                if (!IsCurrentPlayerTheRock(turn)) return;
                if (GetCurrentRingSide(turn).Count == 0) return;
                if (!_view.DoesPlayerWantToUseHisAbility("THE ROCK")) return;

                ExecuteRockAbility(turn);
            }

            bool IsCurrentPlayerTheRock(int turn)
            {
                string currentPlayer = GetPlayerName(turn);
                return currentPlayer == "THE ROCK";
            }

            string GetPlayerName(int turn) => (turn == 1) ? superstar1.Name : superstar2.Name;

            List<string> GetCurrentRingSide(int turn) => (turn == 1) ? player1RingsidePile : player2RingsidePile;

            void ExecuteRockAbility(int turn)
            {
                List<string> formattedRingSide = FormatRingSide(GetCurrentRingSide(turn));
                UseAbilityAndRecoverCard(turn, formattedRingSide);
            }

            List<string> FormatRingSide(List<string> ringSide)
            {
                return ringSide.Select(cardName => RawDealView.Formatters.Formatter.CardToString(ConvertToCardInfo(cardName, cardsInfo))).ToList();
            }

            void UseAbilityAndRecoverCard(int turn, List<string> formattedRingSide)
            {
                string currentPlayer = GetPlayerName(turn);
                string superstarAbility = GetSuperstarAbility(turn);
                _view.SayThatPlayerIsGoingToUseHisAbility("THE ROCK", superstarAbility);
                int cardId = _view.AskPlayerToSelectCardsToRecover(currentPlayer, 1, formattedRingSide);
                MoveCardFromRingsideToArsenal(turn, cardId);
            }

            string GetSuperstarAbility(int turn) => (turn == 1) ? superstar1.SuperstarAbility : superstar2.SuperstarAbility;

            void MoveCardFromRingsideToArsenal(int turn, int cardId)
            {
                List<string> currentArsenal = (turn == 1) ? player1Deck : player2Deck;
                List<string> currentRingSide = GetCurrentRingSide(turn);
                currentArsenal.Insert(0, currentRingSide[cardId]);
                currentRingSide.RemoveAt(cardId);
            }

                
            void UseUndertakerAbility(int turn)
            {
                string currentPlayer, superstarAbility;
                List<string> currentPlayerHand, currentPlayerRingside;

                SetupPlayersForTurn(turn, out currentPlayer, out superstarAbility, out currentPlayerHand, out currentPlayerRingside);

                if (IsAbilityUseAllowed(currentPlayer, currentPlayerHand))
                {
                    AnnounceAbilityUsage(currentPlayer, superstarAbility);
                    PerformAbilityActions(currentPlayer, currentPlayerHand, currentPlayerRingside);
                }
            }

            void SetupPlayersForTurn(int turn, out string currentPlayer, out string superstarAbility, out List<string> currentPlayerHand, out List<string> currentPlayerRingside)
            {
                string[] superstarNames = {superstarName1, superstarName2};
                List<string>[] playerHands = {player1Hand, player2Hand};
                List<string>[] playerRingsides = {player1RingsidePile, player2RingsidePile};

                SetupPlayers(turn, superstarNames, playerHands, playerRingsides, out currentPlayer, out superstarAbility, out currentPlayerHand, out currentPlayerRingside);
            }

            bool IsAbilityUseAllowed(string currentPlayer, List<string> currentPlayerHand)
            {
                return currentPlayer == "THE UNDERTAKER" && currentPlayerHand.Count >= 2;
            }
                
            void PerformAbilityActions(string currentPlayer, List<string> currentPlayerHand, List<string> currentPlayerRingside)
            {
                DiscardCards(currentPlayer, currentPlayerHand, currentPlayerRingside);
                RecoverCard(currentPlayer, currentPlayerRingside, currentPlayerHand);
            }


            void SetupPlayers(int turn, string[] superstarNames, List<string>[] playerHands, List<string>[] playerRingsides, out string currentPlayer, out string superstarAbility, out List<string> currentPlayerHand, out List<string> currentPlayerRingside)
            {
                string[] superstarAbilities = {superstar1.SuperstarAbility, superstar2.SuperstarAbility};

                currentPlayer = superstarNames[turn - 1];
                superstarAbility = superstarAbilities[turn - 1];
                currentPlayerHand = playerHands[turn - 1];
                currentPlayerRingside = playerRingsides[turn - 1];
            }

            void DiscardCards(string currentPlayer, List<string> currentPlayerHand, List<string> currentPlayerRingside)
            {
                for (int i = 0; i < 2; i++)
                {
                    int cardIdToDiscard = PromptPlayerToDiscardCard(currentPlayerHand, "THE UNDERTAKER", 2 - i);
                    MoveCardBetweenLists(currentPlayerHand, currentPlayerRingside, cardIdToDiscard);
                }
            }

            void RecoverCard(string currentPlayer, List<string> currentPlayerRingside, List<string> currentPlayerHand)
            {
                int cardIdToRecover = PromptPlayerToRecoverCard(currentPlayerRingside, "THE UNDERTAKER");
                MoveCardBetweenLists(currentPlayerRingside, currentPlayerHand, cardIdToRecover);
            }


            int PromptPlayerToDiscardCard(List<string> hand, string player, int remaining)
            {
                List<string> formattedHand = hand.Select(cardName =>
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                }).ToList();

                return _view.AskPlayerToSelectACardToDiscard(formattedHand, player, player, remaining);
            }

            int PromptPlayerToRecoverCard(List<string> ringside, string player)
            {
                List<string> formattedRingSide = ringside.Select(cardName =>
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                }).ToList();

                return _view.AskPlayerToSelectCardsToPutInHisHand(player, 1, formattedRingSide);
            }

            void MoveCardBetweenLists(List<string> source, List<string> destination, int cardId)
            {

                string card = source[cardId];
                source.RemoveAt(cardId);
                destination.Add(card);
                
            }

            
            void InitPlayers(int turn, out PlayerState currentPlayer, out PlayerState opponentPlayer)
            {
                currentPlayer = (turn == 1) 
                    ? new PlayerState { Name = superstarName1, SuperstarAbility = superstar1.SuperstarAbility, Hand = player1Hand, Ringside = player1RingsidePile }
                    : new PlayerState { Name = superstarName2, SuperstarAbility = superstar2.SuperstarAbility, Hand = player2Hand, Ringside = player2RingsidePile };

                opponentPlayer = (turn == 1) 
                    ? new PlayerState { Name = superstarName2, SuperstarAbility = superstar2.SuperstarAbility, Hand = player2Hand, Ringside = player2RingsidePile }
                    : new PlayerState { Name = superstarName1, SuperstarAbility = superstar1.SuperstarAbility, Hand = player1Hand, Ringside = player1RingsidePile };
            }

            
            void UseJerichoAbility(int turn)
            {
                InitPlayers(turn, out PlayerState currentPlayer, out PlayerState opponentPlayer);

                if (currentPlayer.Name == "CHRIS JERICHO" && currentPlayer.Hand.Count >= 1)
                {
                    AnnounceAbilityUsage(currentPlayer.Name, currentPlayer.SuperstarAbility);
                    PlayerDiscardsCard(currentPlayer.Name, currentPlayer.Hand, currentPlayer.Ringside, 1);
                    PlayerDiscardsCard(opponentPlayer.Name, opponentPlayer.Hand, opponentPlayer.Ringside, 1);
                }
            }


            void PlayerDiscardsCard(string player, List<string> hand, List<string> ringside, int number)
            {

                List<string> formattedHand = FormatHand(hand);
                int cardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedHand, player, player, number);
                MoveCardBetweenLists(hand, ringside, cardIdToDiscard);
            }
            
            void PlayerDiscardsCardReversed(Dictionary<string, object> gameData, List<string> hand, List<string> ringside, int number)
            {
                string player = gameData["superStarName"] as string;
                string playerOpponent = gameData["superStarNameOpponent"] as string;
                List<string> formattedHand = FormatHand(hand);
                int cardIdToDiscard = _view.AskPlayerToSelectACardToDiscard(formattedHand, playerOpponent, player, number);
                MoveCardBetweenLists(hand, ringside, cardIdToDiscard);
            }


            List<string> FormatHand(List<string> hand)
            {
                return hand.Select(cardName =>
                {
                    var cardInfo = ConvertToCardInfo(cardName, cardsInfo);
                    return RawDealView.Formatters.Formatter.CardToString(cardInfo);
                }).ToList();
            }

            void UseStoneColdAbility(int turn)
            {
                InitStoneCold(turn, out PlayerState currentPlayer, out List<string> currentPlayerDeck);
    
                if (IsAbilityUsable(currentPlayer.Name, currentPlayerDeck))
                {
                    AnnounceAbilityUsage(currentPlayer.Name, currentPlayer.SuperstarAbility);
                    DrawAndAnnounce(currentPlayerDeck, currentPlayer.Hand, currentPlayer.Name);
                    ReturnCardToArsenal(currentPlayer, currentPlayerDeck);
                }
            }

            void InitStoneCold(int turn, out PlayerState currentPlayer, out List<string> currentPlayerDeck)
            {
                InitPlayers(turn, out currentPlayer, out PlayerState opponentPlayer);
                currentPlayerDeck = (turn == 1) ? player1Deck : player2Deck;
            }

            bool IsAbilityUsable(string playerName, List<string> playerDeck)
            {
                return playerName == "STONE COLD STEVE AUSTIN" && playerDeck.Count > 0 && !abilityUsedThisTurn;
            }
                

            void DrawAndAnnounce(List<string> playerDeck, List<string> playerHand, string playerName)
            {
                DrawCard(playerDeck, playerHand);
                _view.SayThatPlayerDrawCards(playerName, 1);
            }

            void ReturnCardToArsenal(PlayerState currentPlayer, List<string> currentPlayerDeck)
            {
                List<string> formattedHand = FormatHand(currentPlayer.Hand);
                int cardIdToReturn = _view.AskPlayerToReturnOneCardFromHisHandToHisArsenal(currentPlayer.Name, formattedHand);
                ReturnSelectedCard(currentPlayer.Hand, currentPlayerDeck, cardIdToReturn);
            }
                

            void ReturnSelectedCard(List<string> hand, List<string> deck, int cardId)
            {
                string returnedCard = hand[cardId];
                hand.RemoveAt(cardId);
                deck.Insert(0, returnedCard); 
                abilityUsedThisTurn = true;
            }
            }

                
            catch (GameEndException exception)
            {
                Console.WriteLine("");
            }
        }
    }
}




        
        
