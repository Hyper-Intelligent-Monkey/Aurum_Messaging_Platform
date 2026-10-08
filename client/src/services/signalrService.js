import * as signalr from "@microsoft/signalr";
import { useToastStore } from "../store/toastStore";

class SignalrService {

    constructor() {
        this.connection = null;
        this.connectPromise = null;
        this.currentConversationId = null;
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
        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            return;
        }

        if (this.connectPromise) {
            return this.connectPromise;
        }

        const token = localStorage.getItem("jwt_token");
        if (!token) {
            console.warn("SignalR: Cannot connect without a JWT token.");
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

        this.connection.onreconnected(async (connectionId) => {
            console.log("SignalR: Reconnected successfully, connectionId:", connectionId);
            useToastStore().success("Reconnected.");
            if (this.currentConversationId) {
                await this.joinConversation(this.currentConversationId);
            }
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

        this.connectPromise = this.connection.start()
            .then(async () => {
                console.log("SignalR Connected successfully");
                if (this.currentConversationId) {
                    await this.joinConversation(this.currentConversationId);
                }
            })
            .catch((err) => {
                console.error("SignalR: Connection failed", err);
            })
            .finally(() => {
                this.connectPromise = null;
            });

        return this.connectPromise;
    }

    // disconnects from the server and clean up all handlers
    async disconnect() {
        this.connectPromise = null;
        if (this.connection) {
            await this.connection.stop();
            this.connection = null;
            console.log("SignalR Disconnected");
        }
    }

    // Helper: wait until connected before invoking hub methods
    async ensureConnected() {
        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            return true;
        }
        if (this.connectPromise) {
            await this.connectPromise;
            return this.connection && this.connection.state === signalr.HubConnectionState.Connected;
        }
        await this.connect();
        return this.connection && this.connection.state === signalr.HubConnectionState.Connected;
    }

    // Joins an active conversation
    async joinConversation(conversationId) {
        if (!conversationId) return;
        this.currentConversationId = conversationId;
        const isConnected = await this.ensureConnected();
        if (isConnected) {
            try {
                await this.connection.invoke("JoinConversation", conversationId);
            } catch (err) {
                console.warn("SignalR JoinConversation non-fatal:", err);
            }
        }
    }

    // Exits an active conversation
    async leaveConversation(conversationId) {
        if (!conversationId) return;
        if (this.currentConversationId === conversationId) {
            this.currentConversationId = null;
        }
        if (this.connection && this.connection.state === signalr.HubConnectionState.Connected) {
            try {
                await this.connection.invoke("LeaveConversation", conversationId);
            } catch (err) {
                console.warn("SignalR LeaveConversation non-fatal:", err);
            }
        }
    }

    // Sends typing indicator of the other user
    async sendTyping(conversationId, isTyping) {
        if (!conversationId) return;
        const isConnected = await this.ensureConnected();
        if (isConnected) {
            try {
                await this.connection.invoke("SendTypingIndicator", conversationId, isTyping);
            } catch (err) {
                console.warn("SignalR sendTyping non-fatal:", err);
            }
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