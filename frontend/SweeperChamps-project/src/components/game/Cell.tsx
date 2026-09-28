import React from "react";
import type { CellView } from "../../types/game";

interface CellProps {
  cell: CellView;
  onReveal: (x: number, y: number) => void;
  onFlag: (x: number, y: number) => void;
}

const Cell: React.FC<CellProps> = ({ cell, onReveal, onFlag }) => {
  const handleContextMenu = (e: React.MouseEvent) => {
    e.preventDefault();
    if (cell.state !== "Revealed") onFlag(cell.x, cell.y);
  };

  if (cell.state === "Hidden") {
    return (
      <button
        className="cell cell--hidden"
        onClick={() => onReveal(cell.x, cell.y)}
        onContextMenu={handleContextMenu}
        aria-label={`Cell ${cell.x},${cell.y}`}
      />
    );
  }

  if (cell.state === "Flagged") {
    return (
      <button
        className="cell cell--flagged"
        onClick={() => onFlag(cell.x, cell.y)}
        onContextMenu={handleContextMenu}
      >
        🚩
      </button>
    );
  }

  return (
    <div className={`cell cell--revealed ${cell.isMine ? "cell--mine" : ""}`}>
      {cell.isMine
        ? "💣"
        : cell.adjacentMineCount > 0
        ? <span className={`num num--${cell.adjacentMineCount}`}>{cell.adjacentMineCount}</span>
        : null}
    </div>
  );
};

export default Cell;