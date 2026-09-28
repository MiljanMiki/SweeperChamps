import * as signalR from "@microsoft/signalr";

const GAME_HUB_URL =
  import.meta.env.VITE_GAME_HUB_URL || "https://localhost:5000/hubs/game";

let connection: signalR.HubConnection | null = null;

export function buildGameConnection(token: string): signalR.HubConnection {
  if (connection) {
    connection.stop();
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(GAME_HUB_URL, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}

export function getGameConnection(): signalR.HubConnection | null {
  return connection;
}

export async function stopGameConnection(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}