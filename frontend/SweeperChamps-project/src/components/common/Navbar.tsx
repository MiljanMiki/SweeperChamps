import React from "react";
import { Link, useNavigate } from "react-router-dom";
import { useAuth } from "../../context/AuthContext";

const Navbar: React.FC = () => {
  const { user, logout, isAuthenticated, isAdmin } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate("/login");
  };

  return (
    <nav className="navbar">
      <div className="navbar-container">
        <Link to="/" className="navbar-brand">
          🧨 Minesweeper
        </Link>

        <div className="navbar-menu">
          {isAuthenticated ? (
            <>
              <Link to="/lobby" className="navbar-link">
                Lobby
              </Link>

              {isAdmin && (
                <Link to="/admin" className="navbar-link navbar-link--admin">
                  🛡 Admin
                </Link>
              )}

             <Link to={`/profile/${user?.username}`} className="navbar-user-link">
                {user?.username}
              </Link>

              <button onClick={handleLogout} className="navbar-button">
                Logout
              </button>
            </>
          ) : (
            <>
              <Link to="/login" className="navbar-link">
                Login
              </Link>
              <Link to="/register" className="navbar-link navbar-link-primary">
                Register
              </Link>
            </>
          )}
        </div>
      </div>
    </nav>
  );
};

export default Navbar;