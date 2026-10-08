import * as signalr from "@microsoft/signalr";
import { useToastStore } from "../store/toastStore";

class SignalrService {

    constructor() {
        this.connection = null;
        this.handler = {
            ReceiveMessage: [],
            UserPresenceChanged: [],
            UserAvatarChanged: [],
            UserTyping: [],
            MessageEdited: [],
            MessageDeleted: [],
            MessagesSeen: [],
        };
    }

    async connect() {
        const token = await localStorage.getItem("jwt_token");
        if (!token) {
            console.warn("SignalR: Cannot connect without a JWT token.");
            return;
        }

        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            return;
        }

        // establishes websocket connection to the server using the JWT token
        this.connection = new signalr.HubConnectionBuilder()
            .withUrl( import.meta.env.VITE_HUB_URL, {
                accessTokenFactory: () => localStorage.getItem("jwt_token") || "",
            }).withAutomaticReconnect().build();
        
        // reconnects if the server went down
        this.connection.onreconnecting((error) => {
            console.warn("SignalR: Connection lost, attempting to reconnect...", error);
            useToastStore().error("Connection to server lost.");
        });
        //
        this.connection.onreconnected((connectionId) => {
            console.log("SignalR: Reconnected successfully, connectionId:", connectionId);
            useToastStore().success("Reconnected.");
        });

        this.connection.onclose((error) => {
            console.error("SignalR: Connection closed completely.", error);
            if (error) {
                useToastStore().error("Disconnected.");
            }
        });

        // Registers SignalR listeners and forwards incoming server events to all registered callbacks
        Object.keys(this.handler).forEach((eventName) => {
            this.connection.on(eventName, data => {
                this.handler[eventName].forEach(callback => callback(data));
            });
        });

        try {
            await this.connection.start();
            console.log("SignalR Connected successfully");
        }
        catch (err) {
            console.error("SignalR: Connection failed", err);
        }
    }

    // disconnects from the server and clean up all handlers
    async disconnect() {
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
            console.log("SignalR Disconnected");
        }
    }

    // Joins an active conversation
    async joinConversation(conversationId) {
        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            await this.connection.invoke("JoinConversation", conversationId);
        }
    }

    // Exits an active conversation
    async leaveConversation(conversationId) {
        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            await this.connection.invoke("LeaveConversation", conversationId);
        }
    }

    // Sends typing indicator of the other user
    async sendTyping(conversationId, isTyping) {
        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            await this.connection.invoke("SendTypingIndicator", conversationId, isTyping);
        }
    }

    // setting up a callback for a specific handler
    on(eventName, callback) {
        if (this.handler[eventName] && !this.handler[eventName].includes(callback)) {
            this.handler[eventName].push(callback);
        }
    }

    // turning a specific callback in a handler off, 
    // disconnect does the same thing but for all handers and their callbacks
    off(eventName, callback) {
        if (this.handler[eventName]) {
            const index = this.handler[eventName].indexOf(callback);
            if (index !== -1) {
                this.handler[eventName].splice(index, 1);
            }
        }
    }
}

export const signalrService = new SignalrService();