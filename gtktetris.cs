using Gtk;
using Cairo;
using System;

public class Tetromino
{
    public int[,] Shape { get; } = new int[4, 2];
    public double[] Color { get; } = new double[3];

    public Tetromino(int[,] shape, double[] color)
    {
        Array.Copy(shape, Shape, shape.Length);
        Array.Copy(color, Color, color.Length);
    }
}

public class GameState
{
    public int[,] Board { get; } = new int[20, 10];
    public int[,] BoardColors { get; } = new int[20, 10];
    public int CurrentX { get; set; }
    public int CurrentY { get; set; }
    public int[,] CurrentPiece { get; } = new int[4, 2];
    public int CurrentType { get; set; }
    public int NextType { get; set; }
    public int Score { get; set; }
    public int Level { get; set; } = 1;
    public int GameSpeed { get; set; } = 500;
    public string ScoreText { get; set; } = "Score: 0  Level: 1";
    public bool GameOver { get; set; }
    public bool Paused { get; set; }
    public uint TimeoutId { get; set; }
}

public class Widgets
{
    public DrawingArea DrawingArea { get; set; }
    public Label ScoreLabel { get; set; }
    public Button NewGameButton { get; set; }
    public Button PauseButton { get; set; }
    public DrawingArea PreviewArea { get; set; }
}

public class TetrisGame
{
    private const int BOARD_WIDTH = 10;
    private const int BOARD_HEIGHT = 20;
    private const int BLOCK_SIZE = 30;
    private const int LEVEL_THRESHOLD = 5000;
    private const int PREVIEW_SIZE = 5;
    private const int MAX_LEVEL = 10;

    private static readonly Tetromino[] tetrominoes = new[]
    {
        new Tetromino(new[,] {{0,0}, {0,1}, {1,0}, {1,1}}, new[] {1.0, 1.0, 0.0}), // Square
        new Tetromino(new[,] {{0,0}, {0,1}, {0,2}, {0,3}}, new[] {0.0, 1.0, 1.0}), // Line
        new Tetromino(new[,] {{0,0}, {0,1}, {1,1}, {1,2}}, new[] {1.0, 0.0, 0.0}), // Z
        new Tetromino(new[,] {{0,1}, {0,2}, {1,0}, {1,1}}, new[] {0.0, 1.0, 0.0}), // S
        new Tetromino(new[,] {{0,0}, {0,1}, {0,2}, {1,1}}, new[] {1.0, 0.0, 1.0}), // T
        new Tetromino(new[,] {{0,0}, {1,0}, {2,0}, {2,1}}, new[] {1.0, 0.5, 0.0}), // L
        new Tetromino(new[,] {{0,1}, {1,1}, {2,0}, {2,1}}, new[] {0.0, 0.0, 1.0})  // J
    };

    private static GameState game = new GameState();
    private static Widgets widgets = new Widgets();
    private static Random rand = new Random();

    private static int SecureRand(int max) => rand.Next(max);

    private static void InitGameState()
    {
        game = new GameState();
        game.GameSpeed = 500;
        game.Level = 1;
    }

    private static void NewPiece()
    {
        game.CurrentType = game.NextType;
        game.NextType = SecureRand(7);
        game.CurrentX = BOARD_WIDTH / 2 - 2;
        game.CurrentY = 0;
        Array.Copy(tetrominoes[game.CurrentType].Shape, game.CurrentPiece, 8);
        widgets.PreviewArea.QueueDraw();
    }

    private static void StartNewGame(object sender, EventArgs e)
    {
        InitGameState();
        if (game.TimeoutId != 0)
        {
            GLib.Source.Remove(game.TimeoutId);
        }
        game.TimeoutId = GLib.Timeout.Add((uint)game.GameSpeed, GameLoop);
        game.ScoreText = $"Score: {game.Score}  Level: {game.Level}";
        widgets.ScoreLabel.Text = game.ScoreText;
        widgets.PauseButton.Label = "Pause";
        NewPiece();
        widgets.DrawingArea.QueueDraw();
    }

    private static void TogglePause(object sender, EventArgs e)
    {
        if (game.GameOver) return;
        game.Paused = !game.Paused;
        widgets.PauseButton.Label = game.Paused ? "Resume" : "Pause";
    }

    private static void DrawPreview(object o, DrawnArgs args)
    {
        Cairo.Context cr = args.Cr;
        cr.SetSourceRGB(0.2, 0.2, 0.2);
        cr.Paint();

        cr.SetSourceRGB(0.5, 0.5, 0.5);
        cr.Rectangle(5, 5, PREVIEW_SIZE * BLOCK_SIZE / 2 - 10, PREVIEW_SIZE * BLOCK_SIZE / 2 - 10);
        cr.Stroke();

        var color = tetrominoes[game.NextType].Color;
        cr.SetSourceRGB(color[0], color[1], color[2]);

        for (int i = 0; i < 4; i++)
        {
            int x = tetrominoes[game.NextType].Shape[i, 0] + 1;
            int y = tetrominoes[game.NextType].Shape[i, 1] + 1;
            cr.Rectangle(x * BLOCK_SIZE / 2, y * BLOCK_SIZE / 2, BLOCK_SIZE / 2 - 1, BLOCK_SIZE / 2 - 1);
            cr.Fill();
        }
        cr.Dispose();
    }

    private static void DrawCallback(object o, DrawnArgs args)
    {
        Cairo.Context cr = args.Cr;
        cr.SetSourceRGB(0.1, 0.1, 0.1);
        cr.Paint();

        for (int y = 0; y < BOARD_HEIGHT; y++)
        {
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                if (game.Board[y, x] != 0)
                {
                    int colorIdx = game.BoardColors[y, x] - 1;
                    if (colorIdx >= 0 && colorIdx < 7)
                    {
                        var color = tetrominoes[colorIdx].Color;
                        cr.SetSourceRGB(color[0], color[1], color[2]);
                        cr.Rectangle(x * BLOCK_SIZE, y * BLOCK_SIZE, BLOCK_SIZE - 1, BLOCK_SIZE - 1);
                        cr.Fill();
                    }
                }
            }
        }

        var currentColor = tetrominoes[game.CurrentType].Color;
        cr.SetSourceRGB(currentColor[0], currentColor[1], currentColor[2]);
        for (int i = 0; i < 4; i++)
        {
            int x = game.CurrentX + game.CurrentPiece[i, 0];
            int y = game.CurrentY + game.CurrentPiece[i, 1];
            if (y >= 0 && x >= 0 && x < BOARD_WIDTH)
            {
                cr.Rectangle(x * BLOCK_SIZE, y * BLOCK_SIZE, BLOCK_SIZE - 1, BLOCK_SIZE - 1);
                cr.Fill();
            }
        }

        if (game.GameOver)
        {
            cr.SetSourceRGBA(0.0, 0.0, 0.0, 0.9);
            cr.Rectangle(BOARD_WIDTH * BLOCK_SIZE / 2 - 100, BOARD_HEIGHT * BLOCK_SIZE / 2 - 40, 200, 80);
            cr.Fill();

            cr.SetSourceRGB(0.5, 0.5, 0.5);
            cr.Rectangle(BOARD_WIDTH * BLOCK_SIZE / 2 - 100, BOARD_HEIGHT * BLOCK_SIZE / 2 - 40, 200, 80);
            cr.Stroke();

            cr.SetSourceRGB(1.0, 1.0, 1.0);
            cr.SelectFontFace("Sans", FontSlant.Normal, FontWeight.Bold);
            cr.SetFontSize(40);
            cr.MoveTo(BOARD_WIDTH * BLOCK_SIZE / 2 - 90, BOARD_HEIGHT * BLOCK_SIZE / 2 + 15);
            cr.ShowText("GAME OVER");
        }
        cr.Dispose();
    }

    private static bool CanMove(int dx, int dy)
    {
        for (int i = 0; i < 4; i++)
        {
            int newX = game.CurrentX + game.CurrentPiece[i, 0] + dx;
            int newY = game.CurrentY + game.CurrentPiece[i, 1] + dy;
            if (newX < 0 || newX >= BOARD_WIDTH || newY >= BOARD_HEIGHT ||
                (newY >= 0 && game.Board[newY, newX] != 0))
            {
                return false;
            }
        }
        return true;
    }

    private static void LandPiece()
    {
        for (int i = 0; i < 4; i++)
        {
            int x = game.CurrentX + game.CurrentPiece[i, 0];
            int y = game.CurrentY + game.CurrentPiece[i, 1];
            if (y >= 0 && x >= 0 && x < BOARD_WIDTH && y < BOARD_HEIGHT)
            {
                game.Board[y, x] = 1;
                game.BoardColors[y, x] = game.CurrentType + 1;
            }
        }
    }

    private static void ClearLines()
    {
        int lines = 0;
        for (int y = BOARD_HEIGHT - 1; y >= 0; y--)
        {
            bool full = true;
            for (int x = 0; x < BOARD_WIDTH; x++)
            {
                if (game.Board[y, x] == 0)
                {
                    full = false;
                    break;
                }
            }
            if (full)
            {
                lines++;
                for (int yy = y; yy > 0; yy--)
                {
                    for (int x = 0; x < BOARD_WIDTH; x++)
                    {
                        game.Board[yy, x] = game.Board[yy - 1, x];
                        game.BoardColors[yy, x] = game.BoardColors[yy - 1, x];
                    }
                }
                for (int x = 0; x < BOARD_WIDTH; x++)
                {
                    game.Board[0, x] = 0;
                    game.BoardColors[0, x] = 0;
                }
                y++;
            }
        }

        game.Score = Math.Min(game.Score + lines * 100 * game.Level, int.MaxValue);
        game.ScoreText = $"Score: {game.Score}  Level: {game.Level}";
        widgets.ScoreLabel.Text = game.ScoreText;

        if (game.Score >= game.Level * LEVEL_THRESHOLD && game.Level < MAX_LEVEL)
        {
            game.Level++;
            game.GameSpeed = 500 / game.Level;
            GLib.Source.Remove(game.TimeoutId);
            game.TimeoutId = GLib.Timeout.Add((uint)game.GameSpeed, GameLoop);
        }
    }

    private static bool GameLoop()
    {
        if (game.Paused || game.GameOver) return true;

        if (CanMove(0, 1))
        {
            game.CurrentY++;
        }
        else
        {
            LandPiece();
            ClearLines();
            NewPiece();
            if (!CanMove(0, 0))
            {
                game.GameOver = true;
                widgets.DrawingArea.QueueDraw();
                return false;
            }
        }
        widgets.DrawingArea.QueueDraw();
        return true;
    }

    private static void KeyPress(object o, KeyPressEventArgs args)
    {
        if (game.GameOver) return;

        switch (args.Event.Key)
        {
            case Gdk.Key.p:
                TogglePause(null, null);
                break;
            case Gdk.Key.Left:
                if (!game.Paused && CanMove(-1, 0)) game.CurrentX--;
                break;
            case Gdk.Key.Right:
                if (!game.Paused && CanMove(1, 0)) game.CurrentX++;
                break;
            case Gdk.Key.Down:
                if (!game.Paused && CanMove(0, 1)) game.CurrentY++;
                break;
            case Gdk.Key.Up:
                if (!game.Paused)
                {
                    int[,] tempPiece = new int[4, 2];
                    Array.Copy(game.CurrentPiece, tempPiece, 8);
                    for (int i = 0; i < 4; i++)
                    {
                        int x = game.CurrentPiece[i, 0];
                        int y = game.CurrentPiece[i, 1];
                        game.CurrentPiece[i, 0] = y;
                        game.CurrentPiece[i, 1] = -x;
                    }
                    if (!CanMove(0, 0))
                    {
                        Array.Copy(tempPiece, game.CurrentPiece, 8);
                    }
                }
                break;
        }
        if (!game.Paused) widgets.DrawingArea.QueueDraw();
    }

    public static void Main(string[] args)
    {
        Application.Init();

        Window window = new Window("Tetris");
        window.Resizable = false;
        window.DeleteEvent += (o, e) => Application.Quit();
        window.KeyPressEvent += KeyPress;

        HBox mainBox = new HBox(false, 5);
        window.Add(mainBox);

        widgets.DrawingArea = new DrawingArea();
        widgets.DrawingArea.SetSizeRequest(BOARD_WIDTH * BLOCK_SIZE, BOARD_HEIGHT * BLOCK_SIZE);
        widgets.DrawingArea.Drawn += DrawCallback;
        mainBox.PackStart(widgets.DrawingArea, false, false, 0);

        VBox rightBox = new VBox(false, 5);
        mainBox.PackStart(rightBox, false, false, 0);

        widgets.PreviewArea = new DrawingArea();
        widgets.PreviewArea.SetSizeRequest(PREVIEW_SIZE * BLOCK_SIZE / 2, PREVIEW_SIZE * BLOCK_SIZE / 2);
        widgets.PreviewArea.Drawn += DrawPreview;
        rightBox.PackStart(widgets.PreviewArea, false, false, 0);

        widgets.NewGameButton = new Button("New Game");
        widgets.NewGameButton.Clicked += StartNewGame;
        rightBox.PackStart(widgets.NewGameButton, false, false, 0);

        widgets.PauseButton = new Button("Pause");
        widgets.PauseButton.Clicked += TogglePause;
        rightBox.PackStart(widgets.PauseButton, false, false, 0);

        widgets.ScoreLabel = new Label("Score: 0  Level: 1");
        rightBox.PackStart(widgets.ScoreLabel, false, false, 0);

        InitGameState();
        game.NextType = SecureRand(7);
        NewPiece();
        game.TimeoutId = GLib.Timeout.Add((uint)game.GameSpeed, GameLoop);

        window.ShowAll();
        Application.Run();
    }
}
