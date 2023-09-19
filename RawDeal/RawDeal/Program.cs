using RawDeal;
using RawDealView;

View view = View.BuildConsoleView();
string deckFolder = Path.Combine("data", "01-ValidDecks");
Game game = new Game(view, deckFolder);
game.Play();