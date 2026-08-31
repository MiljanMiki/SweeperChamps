// src/components/game/Minesweeper.tsx
import React, { useState, useEffect } from 'react';
import './Minesweeper.css';

interface Cell {
  isMine: boolean;
  isRevealed: boolean;
  isFlagged: boolean;
  adjacentMines: number;
}

const Minesweeper: React.FC = () => {
  const [board, setBoard] = useState<Cell[][]>([]);
  const [gameOver, setGameOver] = useState(false);
  const [gameWon, setGameWon] = useState(false);
  const [rows] = useState(9);
  const [cols] = useState(9);
  const [mines] = useState(10);

  useEffect(() => {
    initializeGame();
  }, []);

  const initializeGame = () => {
    const newBoard: Cell[][] = Array(rows)
      .fill(null)
      .map(() =>
        Array(cols)
          .fill(null)
          .map(() => ({
            isMine: false,
            isRevealed: false,
            isFlagged: false,
            adjacentMines: 0,
          }))
      );

    let minesPlaced = 0;
    while (minesPlaced < mines) {
      const row = Math.floor(Math.random() * rows);
      const col = Math.floor(Math.random() * cols);
      if (!newBoard[row][col].isMine) {
        newBoard[row][col].isMine = true;
        minesPlaced++;
      }
    }

    for (let i = 0; i < rows; i++) {
      for (let j = 0; j < cols; j++) {
        if (!newBoard[i][j].isMine) {
          let count = 0;
          for (let di = -1; di <= 1; di++) {
            for (let dj = -1; dj <= 1; dj++) {
              const ni = i + di;
              const nj = j + dj;
              if (ni >= 0 && ni < rows && nj >= 0 && nj < cols && newBoard[ni][nj].isMine) {
                count++;
              }
            }
          }
          newBoard[i][j].adjacentMines = count;
        }
      }
    }

    setBoard(newBoard);
    setGameOver(false);
    setGameWon(false);
  };

  const revealCell = (row: number, col: number) => {
    if (gameOver || gameWon || board[row][col].isFlagged || board[row][col].isRevealed) {
      return;
    }

    const newBoard = [...board];
    revealCellRecursive(newBoard, row, col);

    const allRevealed = newBoard.every((row) =>
      row.every((cell) => cell.isRevealed || cell.isMine)
    );

    if (allRevealed) {
      setGameWon(true);
    }

    setBoard(newBoard);
  };

  const revealCellRecursive = (board: Cell[][], row: number, col: number) => {
    if (row < 0 || row >= rows || col < 0 || col >= cols) return;
    if (board[row][col].isRevealed || board[row][col].isFlagged) return;
    if (board[row][col].isMine) {
      setGameOver(true);
      return;
    }

    board[row][col].isRevealed = true;

    if (board[row][col].adjacentMines === 0) {
      for (let di = -1; di <= 1; di++) {
        for (let dj = -1; dj <= 1; dj++) {
          revealCellRecursive(board, row + di, col + dj);
        }
      }
    }
  };

  const toggleFlag = (row: number, col: number, e: React.MouseEvent) => {
    e.preventDefault();
    if (gameOver || gameWon || board[row][col].isRevealed) return;

    const newBoard = [...board];
    newBoard[row][col].isFlagged = !newBoard[row][col].isFlagged;
    setBoard(newBoard);
  };

  const getCellContent = (cell: Cell): string => {
    if (cell.isFlagged) return '🚩';
    if (!cell.isRevealed) return '';
    if (cell.isMine) return '💣';
    if (cell.adjacentMines === 0) return '';
    return cell.adjacentMines.toString();
  };

  return (
    <div className="game-container">
      <div className="game-header">
        <h1>Minesweeper</h1>
        <div className="game-controls">
          <button onClick={initializeGame} className="game-button">
            New Game
          </button>
          <span className="game-status">
            {gameOver && '💀 Game Over!'}
            {gameWon && '🎉 You Won!'}
          </span>
        </div>
      </div>

      <div className="board">
        {board.map((row, i) => (
          <div key={i} className="board-row">
            {row.map((cell, j) => (
              <button
                key={`${i}-${j}`}
                className={`cell ${cell.isRevealed ? 'revealed' : ''} ${
                  cell.isMine && cell.isRevealed ? 'mine' : ''
                }`}
                onClick={() => revealCell(i, j)}
                onContextMenu={(e) => toggleFlag(i, j, e)}
                disabled={gameOver || gameWon}
              >
                {getCellContent(cell)}
              </button>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
};

export default Minesweeper;