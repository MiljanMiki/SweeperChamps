import React from "react";
import type { CellView } from "../../types/game";

interface CellProps {
  cell: CellView;
  onReveal: (x: number, y: number) => void;
  onFlag: (x: number, y: number) => void;
  onUnflag: (x: number, y: number) => void;
}

const Cell: React.FC<CellProps> = ({ cell, onReveal, onFlag, onUnflag }) => {
  // Right-click behavior:
  //  Hidden → Flag
  //  Flagged → Unflag
  //  Revealed → do nothing
  const handleContextMenu = (e: React.MouseEvent) => {
    e.preventDefault();
    if (cell.state === "Hidden")  onFlag(cell.x, cell.y);
    if (cell.state === "Flagged") onUnflag(cell.x, cell.y);
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
        onClick={() => onUnflag(cell.x, cell.y)}       // left-click also unflags
        onContextMenu={handleContextMenu}              // right-click also unflags
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