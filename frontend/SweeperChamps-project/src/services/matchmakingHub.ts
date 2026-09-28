// src/services/matchmakingHub.ts
import * as signalR from "@microsoft/signalr";

const HUB_URL =
  import.meta.env.VITE_MATCHMAKING_HUB_URL ||
  "https://localhost:7204/hubs/matchmaking";

let connection: signalR.HubConnection | null = null;

export function buildMatchmakingConnection(
  token: string
): signalR.HubConnection {
  if (connection) {
    connection.stop();
  }

  connection = new signalR.HubConnectionBuilder()
    .withUrl(HUB_URL, {
      accessTokenFactory: () => token,
    })
    .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
    .configureLogging(signalR.LogLevel.Information)
    .build();

  return connection;
}

export function getMatchmakingConnection(): signalR.HubConnection | null {
  return connection;
}

export async function stopMatchmakingConnection(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}