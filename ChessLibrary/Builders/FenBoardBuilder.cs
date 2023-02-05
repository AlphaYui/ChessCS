﻿// *****************************************************
// *                                                   *
// * O Lord, Thank you for your goodness in our lives. *
// *     Please bless this code to our compilers.      *
// *                     Amen.                         *
// *                                                   *
// *****************************************************
//                                    Made by Geras1mleo

namespace Chess;

internal class FenBoardBuilder
{
    private readonly Piece?[,] pieces;

    /// <summary>
    /// "Begin Situation"
    /// </summary>
    internal Piece?[,] Pieces => (Piece?[,])pieces.Clone();
    internal PieceColor Turn { get; private set; }

    internal bool CastleWK { get; private set; }
    internal bool CastleWQ { get; private set; }
    internal bool CastleBK { get; private set; }
    internal bool CastleBQ { get; private set; }

    internal Position EnPassant { get; private set; }

    /// <summary>
    /// Count since the last pawn advance or piece capture
    /// </summary>
    internal int HalfMoves { get; private set; }
    /// <summary>
    /// Black moves Count
    /// </summary>
    internal int FullMoves { get; private set; }

    internal Piece[] WhiteCaptured { get; private set; }
    internal Piece[] BlackCaptured { get; private set; }

    private FenBoardBuilder(Piece?[,] pieces)
    {
        this.pieces = pieces;
    }

    private FenBoardBuilder()
    {
        pieces = new Piece[8, 8];
    }

    internal static (bool succeeded, ChessException? exception) TryLoad(string fen, out FenBoardBuilder? builder)
    {
        builder = null;

        var matches = Regexes.regexFen.Matches(fen);

        if (matches.Count == 0)
            return (false, new ChessArgumentException(null, "FEN board string should match pattern: " + Regexes.FenPattern));

        if (!Regexes.regexFenContainsOneWhiteKing.IsMatch(fen) || !Regexes.regexFenContainsOneBlackKing.IsMatch(fen))
            return (false, new ChessArgumentException(null, "Chess board should have exact 1 white king and exact 1 black king"));

        builder = new FenBoardBuilder();

        foreach (var group in matches[0].Groups.Values)
        {
            switch (group.Name)
            {
                case "1":
                    // Set pieces to given positions
                    int x = 0, y = 7;
                    for (int i = 0; i < group.Length; i++)
                    {
                        if (group.Value[i] == '/')
                        {
                            y--;
                            x = 0;
                            continue;
                        }
                        if (x < 8)
                            if (char.IsLetter(group.Value[i]))
                            {
                                builder.pieces[y, x] = new Piece(group.Value[i]);
                                x++;
                            }
                            else if (char.IsDigit(group.Value[i]))
                                x += int.Parse(group.Value[i].ToString());
                    }
                    break;
                case "3":
                    builder.Turn = PieceColor.FromChar(group.Value[0]);
                    break;
                case "4":
                    if (group.Value != "-")
                    {
                        if (group.Value.Contains('K'))
                            builder.CastleWK = true;
                        if (group.Value.Contains('Q'))
                            builder.CastleWQ = true;
                        if (group.Value.Contains('k'))
                            builder.CastleBK = true;
                        if (group.Value.Contains('q'))
                            builder.CastleBQ = true;
                    }
                    break;
                case "5":
                    if (group.Value == "-")
                        builder.EnPassant = new();
                    else
                        builder.EnPassant = new Position(group.Value);
                    break;
                case "6":
                    (builder.HalfMoves, builder.FullMoves) = group.Value.Split(' ').Select(s => int.Parse(s)).ToArray();
                    break;
            }
        }

        AddCapturedPieces(builder);

        return (true, null);
    }

    private static void AddCapturedPieces(FenBoardBuilder builder)
    {
        var whiteCaptured = new List<Piece>();
        var blackCaptured = new List<Piece>();
        var counts = new Dictionary<PieceType, int> { { PieceType.Pawn, 8 }, { PieceType.Rook, 2 }, { PieceType.Bishop, 2 }, { PieceType.Knight, 2 }, { PieceType.Queen, 1 } };

        foreach (var piece in builder.pieces!.PiecesSpan())
        {
            if (counts.ContainsKey(piece.Type))
            {
                counts[piece.Type]--;
            }
        }

        for (var i = 0; i < counts[PieceType.Pawn]; i++)
        {
            whiteCaptured.Add(new Piece(PieceColor.White, PieceType.Pawn));
            blackCaptured.Add(new Piece(PieceColor.Black, PieceType.Pawn));
        }
        for (var i = 0; i < counts[PieceType.Rook]; i++)
        {
            whiteCaptured.Add(new Piece(PieceColor.White, PieceType.Rook));
            blackCaptured.Add(new Piece(PieceColor.Black, PieceType.Rook));
        }
        for (var i = 0; i < counts[PieceType.Bishop]; i++)
        {
            whiteCaptured.Add(new Piece(PieceColor.White, PieceType.Bishop));
            blackCaptured.Add(new Piece(PieceColor.Black, PieceType.Bishop));
        }
        for (var i = 0; i < counts[PieceType.Knight]; i++)
        {
            whiteCaptured.Add(new Piece(PieceColor.White, PieceType.Knight));
            blackCaptured.Add(new Piece(PieceColor.Black, PieceType.Knight));
        }
        for (var i = 0; i < counts[PieceType.Queen]; i++)
        {
            whiteCaptured.Add(new Piece(PieceColor.White, PieceType.Queen));
            blackCaptured.Add(new Piece(PieceColor.Black, PieceType.Queen));
        }
        
        builder.WhiteCaptured = whiteCaptured.ToArray();
        builder.BlackCaptured = blackCaptured.ToArray();
    }
    
    internal static FenBoardBuilder Load(ChessBoard board)
    {
        return new FenBoardBuilder(board.pieces)
        {
            Turn = board.Turn,
            CastleWK = ChessBoard.HasRightToCastle(PieceColor.White, CastleType.King, board),
            CastleWQ = ChessBoard.HasRightToCastle(PieceColor.White, CastleType.Queen, board),
            CastleBK = ChessBoard.HasRightToCastle(PieceColor.Black, CastleType.King, board),
            CastleBQ = ChessBoard.HasRightToCastle(PieceColor.Black, CastleType.Queen, board),
            EnPassant = ChessBoard.LastMoveEnPassantPosition(board),
            HalfMoves = board.GetHalfMovesCount(),
            FullMoves = board.GetFullMovesCount()
        };
    }

    public override string ToString()
    {
        StringBuilder piecesBuilder = new();

        for (int i = 7; i >= 0; i--)
        {
            int emptyCount = 0;
            for (int j = 0; j < 8; j++)
            {
                if (pieces[i, j] is null)
                    emptyCount++;
                else
                {
                    if (emptyCount > 0)
                    {
                        piecesBuilder.Append(emptyCount);
                        emptyCount = 0;
                    }
                    piecesBuilder.Append(pieces[i, j].ToFenChar());
                }
            }
            if (emptyCount > 0)
                piecesBuilder.Append(emptyCount);
            if (i - 1 >= 0)
                piecesBuilder.Append('/');
        }

        StringBuilder castlesBuilder = new();

        if (CastleWK)
            castlesBuilder.Append('K');
        if (CastleWQ)
            castlesBuilder.Append('Q');
        if (CastleBK)
            castlesBuilder.Append('k');
        if (CastleBQ)
            castlesBuilder.Append('q');

        if (castlesBuilder.Length == 0)
            castlesBuilder.Append('-');

        string enPasBuilder;

        if (EnPassant.HasValue)
            enPasBuilder = EnPassant.ToString();
        else
            enPasBuilder = "-";

        return string.Join(' ', piecesBuilder, Turn.AsChar, castlesBuilder, enPasBuilder, HalfMoves, FullMoves);
    }
}