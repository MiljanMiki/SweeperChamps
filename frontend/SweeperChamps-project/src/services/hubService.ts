// src/services/hubService.ts
import * as signalR from "@microsoft/signalr";

const HUB_URL = import.meta.env.REACT_APP_HUB_URL || "http://localhost:5000/hubs/game";

let connection: signalR.HubConnection | null = null;

export function buildConnection(token: string): signalR.HubConnection {
  // Stop existing connection if any
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

export function getConnection(): signalR.HubConnection | null {
  return connection;
}

export async function stopConnection(): Promise<void> {
  if (connection) {
    await connection.stop();
    connection = null;
  }
}