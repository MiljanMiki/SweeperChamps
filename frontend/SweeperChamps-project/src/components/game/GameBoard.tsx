// src/components/game/GameBoard.tsx
import React from "react";
import type { CellView } from "../../types/game";
import Cell from "./Cell";
import "./GameBoard.css";

interface GameBoardProps {
  board: CellView[][];
  onReveal: (x: number, y: number) => void;
  onFlag: (x: number, y: number) => void;
}

const GameBoard: React.FC<GameBoardProps> = ({ board, onReveal, onFlag }) => {
  if (board.length === 0) {
    return <div className="board-loading">Loading board…</div>;
  }

  return (
    <div className="board">
      {board.map((row, y) => (
        <div key={y} className="board-row">
          {row.map((cell, x) => (
            <Cell key={`${x}-${y}`} cell={cell} onReveal={onReveal} onFlag={onFlag} />
          ))}
        </div>
      ))}
    </div>
  );
};

export default GameBoard;